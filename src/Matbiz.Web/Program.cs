using Matbiz.Web.Data;
using Matbiz.Web.Impersonation;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Files;
using Matbiz.Web.Modules.Files.Services;
using Matbiz.Web.Modules.SystemSettings;
using Matbiz.Web.Modules.SystemSettings.Services;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Razor Pages --------------------------------------------------------------
builder.Services.AddRazorPages(options =>
{
    // Authentication requirement is applied selectively via [Authorize] attributes
    // on the PageModels; Identity pages stay anonymous.
});

builder.Services.AddMemoryCache();
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

// --- Identity (cookie auth, Razor-Pages-friendly) ----------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.LogoutPath = "/Account/Logout";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    o.SlidingExpiration = true;
});

// --- Impersonation -----------------------------------------------------------
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddTransient<IClaimsTransformation, ImpersonationClaimsTransformation>();

// --- Shared / Module services ------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CustomerFieldService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<AttachedFileService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<CustomerGroupService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<BrandingService>();

builder.Services.AddAuthorization();

var app = builder.Build();

// --- Migrate + seed ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
    await SampleDataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

// --- Pipeline ----------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapImpersonationEndpoints();
app.MapBrandingEndpoints();
app.MapFileEndpoints();

app.Run();

static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg, ILogger logger)
{
    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in Roles.All)
        if (!await roles.RoleExistsAsync(r))
            await roles.CreateAsync(new IdentityRole(r));

    var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var email = cfg["Matbiz:SeedAdmin:Email"];
    var pass = cfg["Matbiz:SeedAdmin:Password"];
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) return;

    if (await users.FindByEmailAsync(email) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            IsActive = true
        };
        var res = await users.CreateAsync(admin, pass);
        if (res.Succeeded)
        {
            await users.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Seeded admin user {Email}", email);
        }
        else
        {
            logger.LogError("Failed to seed admin: {Errors}", string.Join("; ", res.Errors.Select(e => e.Description)));
        }
    }
}
