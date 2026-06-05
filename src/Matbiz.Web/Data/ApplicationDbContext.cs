using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.Modules.Models;
using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Modules.SystemSettings.Models;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Users.Models;
using Matbiz.Web.Modules.Wiki.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Company> Companies => Set<Company>();
    // Custom-Fields polymorph (Modules/CustomFields)
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
    public DbSet<CustomFieldSection> CustomFieldSections => Set<CustomFieldSection>();
    public DbSet<CustomerHistoryEntry> CustomerHistoryEntries => Set<CustomerHistoryEntry>();
    public DbSet<CompanyHistoryEntry> CompanyHistoryEntries => Set<CompanyHistoryEntry>();
    public DbSet<CompanyGroupMember> CompanyGroupMembers => Set<CompanyGroupMember>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CustomerTag> CustomerTags => Set<CustomerTag>();
    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<CustomerGroupMember> CustomerGroupMembers => Set<CustomerGroupMember>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskHistoryEntry> TaskHistoryEntries => Set<TaskHistoryEntry>();

    public DbSet<ImpersonationSession> ImpersonationSessions => Set<ImpersonationSession>();

    public DbSet<AuthSessionTicket> AuthSessionTickets => Set<AuthSessionTicket>();

    public DbSet<BrandingSettings> BrandingSettings => Set<BrandingSettings>();

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Department> Departments => Set<Department>();

    public DbSet<AttachedFile> AttachedFiles => Set<AttachedFile>();

    public DbSet<CustomMenuItem> CustomMenuItems => Set<CustomMenuItem>();
    public DbSet<CustomMenuItemDepartment> CustomMenuItemDepartments => Set<CustomMenuItemDepartment>();
    public DbSet<CustomMenuItemTeam> CustomMenuItemTeams => Set<CustomMenuItemTeam>();

    public DbSet<WikiPage> WikiPages => Set<WikiPage>();

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<NumberRange> NumberRanges => Set<NumberRange>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPosition> DocumentPositions => Set<DocumentPosition>();
    public DbSet<ModuleSetting> ModuleSettings => Set<ModuleSetting>();
    public DbSet<NavMenuLayout> NavMenuLayouts => Set<NavMenuLayout>();

    public DbSet<Matbiz.Web.Modules.Warehouse.Models.Warehouse> Warehouses => Set<Matbiz.Web.Modules.Warehouse.Models.Warehouse>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptPosition> GoodsReceiptPositions => Set<GoodsReceiptPosition>();
    public DbSet<WikiPageDepartment> WikiPageDepartments => Set<WikiPageDepartment>();
    public DbSet<WikiPageTeam> WikiPageTeams => Set<WikiPageTeam>();
    public DbSet<WikiPageEditor> WikiPageEditors => Set<WikiPageEditor>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Polymorphic Custom Fields (Modules/CustomFields)
        b.Entity<CustomFieldDefinition>()
            .HasIndex(x => new { x.EntityType, x.Key }).IsUnique();
        b.Entity<CustomFieldDefinition>()
            .HasOne(d => d.Section).WithMany(s => s.Fields).HasForeignKey(d => d.SectionId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<CustomFieldSection>()
            .HasIndex(x => new { x.EntityType, x.Name }).IsUnique();
        b.Entity<CustomFieldValue>()
            .HasIndex(x => new { x.EntityType, x.EntityId, x.FieldDefinitionId }).IsUnique();
        b.Entity<CustomFieldValue>()
            .HasOne(x => x.FieldDefinition).WithMany().HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CustomerHistoryEntry>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ImpersonationSession>()
            .HasIndex(x => new { x.AdminUserId, x.EndedAt });
        b.Entity<ImpersonationSession>()
            .HasIndex(x => new { x.TargetUserId, x.EndedAt });

        b.Entity<Customer>().HasIndex(x => x.Name);
        b.Entity<Customer>()
            .HasOne(c => c.Company).WithMany(co => co.Contacts).HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<TaskHistoryEntry>()
            .HasOne(h => h.Task).WithMany(t => t.History).HasForeignKey(h => h.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<TaskHistoryEntry>().HasIndex(x => x.TaskId);
        b.Entity<Company>().HasIndex(x => x.Name);

        b.Entity<CompanyHistoryEntry>()
            .HasOne(h => h.Company).WithMany(c => c.History).HasForeignKey(h => h.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CompanyHistoryEntry>().HasIndex(x => x.CompanyId);

        b.Entity<CompanyGroupMember>().HasKey(x => new { x.GroupId, x.CompanyId });
        b.Entity<CompanyGroupMember>()
            .HasOne(x => x.Group).WithMany(g => g.CompanyMembers).HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CompanyGroupMember>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<TaskItem>().HasIndex(x => x.AssignedUserId);
        b.Entity<TaskItem>().HasIndex(x => x.AssignedTeamId);
        b.Entity<TaskItem>().HasIndex(x => x.Status);

        b.Entity<Tag>().HasIndex(x => x.Name).IsUnique();

        b.Entity<CustomerTag>().HasKey(x => new { x.CustomerId, x.TagId });
        b.Entity<CustomerTag>()
            .HasOne(x => x.Customer).WithMany(c => c.Tags).HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CustomerTag>()
            .HasOne(x => x.Tag).WithMany(t => t.Customers).HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CompanyTag>().HasKey(x => new { x.CompanyId, x.TagId });
        b.Entity<CompanyTag>()
            .HasOne(x => x.Company).WithMany(c => c.Tags).HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CompanyTag>()
            .HasOne(x => x.Tag).WithMany(t => t.Companies).HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CustomerGroup>().HasIndex(x => x.Name).IsUnique();
        b.Entity<CustomerGroupMember>().HasKey(x => new { x.GroupId, x.CustomerId });
        b.Entity<CustomerGroupMember>()
            .HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CustomerGroupMember>()
            .HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Team>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Team>()
            .HasOne(t => t.Department).WithMany(d => d.Teams).HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<TeamMember>()
            .HasOne(m => m.Team).WithMany(t => t.Members).HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<TeamMember>().HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
        b.Entity<TeamMember>().HasIndex(x => x.UserId);

        b.Entity<Department>().HasIndex(x => x.Name).IsUnique();

        b.Entity<AttachedFile>().HasIndex(x => new { x.OwnerType, x.OwnerId });

        b.Entity<CustomMenuItemDepartment>().HasKey(x => new { x.CustomMenuItemId, x.DepartmentId });
        b.Entity<CustomMenuItemDepartment>()
            .HasOne(x => x.CustomMenuItem).WithMany(i => i.Departments).HasForeignKey(x => x.CustomMenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CustomMenuItemDepartment>()
            .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CustomMenuItemTeam>().HasKey(x => new { x.CustomMenuItemId, x.TeamId });
        b.Entity<CustomMenuItemTeam>()
            .HasOne(x => x.CustomMenuItem).WithMany(i => i.Teams).HasForeignKey(x => x.CustomMenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<CustomMenuItemTeam>()
            .HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<WikiPage>().HasIndex(x => x.Slug).IsUnique();

        b.Entity<Article>().HasIndex(x => x.Number).IsUnique();
        b.Entity<Article>().Property(x => x.NetPrice).HasPrecision(18, 4);
        b.Entity<Article>().Property(x => x.PurchasePrice).HasPrecision(18, 4);
        b.Entity<Article>()
            .HasOne(x => x.TaxRate).WithMany().HasForeignKey(x => x.TaxRateId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<TaxRate>().Property(x => x.Percent).HasPrecision(6, 3);
        b.Entity<NumberRange>().HasIndex(x => x.Key).IsUnique();

        b.Entity<ModuleSetting>().HasKey(x => x.Key);

        // === Warehouse ===
        b.Entity<Matbiz.Web.Modules.Warehouse.Models.Warehouse>().HasIndex(x => x.Name);
        b.Entity<StockLevel>().Property(x => x.Quantity).HasPrecision(18, 4);
        b.Entity<StockLevel>().Property(x => x.ReorderLevel).HasPrecision(18, 4);
        b.Entity<StockLevel>().HasIndex(x => new { x.ArticleId, x.WarehouseId }).IsUnique();
        b.Entity<StockLevel>().HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<StockLevel>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>().Property(x => x.Quantity).HasPrecision(18, 4);
        b.Entity<StockMovement>().HasIndex(x => new { x.ArticleId, x.WarehouseId, x.At });
        b.Entity<StockMovement>().HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<GoodsReceipt>().HasIndex(x => x.Number).IsUnique();
        b.Entity<GoodsReceipt>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoodsReceipt>().HasOne(x => x.SupplierCompany).WithMany().HasForeignKey(x => x.SupplierCompanyId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<GoodsReceiptPosition>().Property(x => x.Quantity).HasPrecision(18, 4);
        b.Entity<GoodsReceiptPosition>().Property(x => x.PurchasePrice).HasPrecision(18, 4);
        b.Entity<GoodsReceiptPosition>().HasOne(x => x.Receipt).WithMany(r => r.Positions).HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<GoodsReceiptPosition>().HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoodsReceiptPosition>().HasIndex(x => new { x.ReceiptId, x.Position });

        b.Entity<Document>().HasIndex(x => new { x.Type, x.Number }).IsUnique();
        b.Entity<Document>().Property(x => x.NetTotal).HasPrecision(18, 4);
        b.Entity<Document>().Property(x => x.TaxTotal).HasPrecision(18, 4);
        b.Entity<Document>().Property(x => x.GrossTotal).HasPrecision(18, 4);
        b.Entity<Document>()
            .HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Document>()
            .HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Document>()
            .HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<DocumentPosition>().Property(x => x.Quantity).HasPrecision(18, 4);
        b.Entity<DocumentPosition>().Property(x => x.NetPrice).HasPrecision(18, 4);
        b.Entity<DocumentPosition>().Property(x => x.DiscountPercent).HasPrecision(6, 3);
        b.Entity<DocumentPosition>().Property(x => x.TaxRatePercent).HasPrecision(6, 3);
        b.Entity<DocumentPosition>().Property(x => x.NetTotal).HasPrecision(18, 4);
        b.Entity<DocumentPosition>().Property(x => x.TaxTotal).HasPrecision(18, 4);
        b.Entity<DocumentPosition>().Property(x => x.GrossTotal).HasPrecision(18, 4);
        b.Entity<DocumentPosition>()
            .HasOne(x => x.Document).WithMany(d => d.Positions).HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<DocumentPosition>()
            .HasOne(x => x.Article).WithMany().HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.SetNull);
        b.Entity<DocumentPosition>().HasIndex(x => new { x.DocumentId, x.Position });

        b.Entity<WikiPageDepartment>().HasKey(x => new { x.WikiPageId, x.DepartmentId });
        b.Entity<WikiPageDepartment>()
            .HasOne(x => x.WikiPage).WithMany(p => p.Departments).HasForeignKey(x => x.WikiPageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<WikiPageDepartment>()
            .HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<WikiPageTeam>().HasKey(x => new { x.WikiPageId, x.TeamId });
        b.Entity<WikiPageTeam>()
            .HasOne(x => x.WikiPage).WithMany(p => p.Teams).HasForeignKey(x => x.WikiPageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<WikiPageTeam>()
            .HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<WikiPageEditor>().HasKey(x => new { x.WikiPageId, x.UserId });
        b.Entity<WikiPageEditor>()
            .HasOne(x => x.WikiPage).WithMany(p => p.Editors).HasForeignKey(x => x.WikiPageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<WikiPageEditor>().HasIndex(x => x.UserId);
    }
}
