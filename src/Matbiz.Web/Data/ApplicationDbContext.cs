using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Modules.SystemSettings.Models;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Users.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CustomerFieldDefinition> CustomerFieldDefinitions => Set<CustomerFieldDefinition>();
    public DbSet<CustomerFieldValue> CustomerFieldValues => Set<CustomerFieldValue>();
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

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<CustomerFieldDefinition>()
            .HasIndex(x => x.Key).IsUnique();

        b.Entity<CustomerFieldValue>()
            .HasIndex(x => new { x.CustomerId, x.FieldDefinitionId }).IsUnique();

        b.Entity<CustomerFieldValue>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.CustomFieldValues)
            .HasForeignKey(x => x.CustomerId)
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
    }
}
