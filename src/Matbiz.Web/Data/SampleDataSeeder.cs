using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Teams.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Matbiz.Web.Modules.Tasks.Models.TaskStatus;

namespace Matbiz.Web.Data;

/// <summary>
/// Seeds demo data for test / dev environments only. Activated via
/// <c>Matbiz:SeedSampleData=true</c> in configuration. Idempotent: skips if any
/// customers already exist, so it never adds duplicates on restart.
/// </summary>
public static class SampleDataSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg, ILogger logger, CancellationToken ct = default)
    {
        if (!cfg.GetValue<bool>("Matbiz:SeedSampleData")) return;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        var hasCustomers = await db.Customers.AnyAsync(ct);
        var hasTeams = await db.Teams.AnyAsync(ct);
        var hasTags = await db.Tags.AnyAsync(ct);
        var hasGroups = await db.CustomerGroups.AnyAsync(ct);
        var hasCompanies = await db.Companies.AnyAsync(ct);
        if (hasCustomers && hasTeams && hasTags && hasGroups && hasCompanies)
        {
            logger.LogInformation("Sample data: already present, skipping.");
            return;
        }

        // --- Demo user "Mitarbeiter A" ---------------------------------------
        var demoEmail = "mitarbeiter.a@matbiz.local";
        var demoUser = await users.FindByEmailAsync(demoEmail);
        if (demoUser is null)
        {
            demoUser = new ApplicationUser
            {
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true,
                DisplayName = "Mitarbeiter A",
                IsActive = true
            };
            var res = await users.CreateAsync(demoUser, "Demo!2026");
            if (res.Succeeded)
            {
                if (!await roles.RoleExistsAsync(Roles.User))
                    await roles.CreateAsync(new IdentityRole(Roles.User));
                await users.AddToRoleAsync(demoUser, Roles.User);
            }
        }

        Customer[] customers = Array.Empty<Customer>();
        if (!hasCustomers)
        {
        // --- Custom fields (Contact-Entity) -----------------------------------
        var kundennr = new Matbiz.Web.Modules.CustomFields.Models.CustomFieldDefinition
            { EntityType = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact,
              Key = "kundennummer", Label = "Kundennummer", Type = Matbiz.Web.Modules.CustomFields.Models.CustomFieldType.Text, SortOrder = 1 };
        var vip = new Matbiz.Web.Modules.CustomFields.Models.CustomFieldDefinition
            { EntityType = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact,
              Key = "vip", Label = "VIP-Kunde", Type = Matbiz.Web.Modules.CustomFields.Models.CustomFieldType.Boolean, SortOrder = 2 };
        var seitJahr = new Matbiz.Web.Modules.CustomFields.Models.CustomFieldDefinition
            { EntityType = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact,
              Key = "kunde_seit", Label = "Kunde seit", Type = Matbiz.Web.Modules.CustomFields.Models.CustomFieldType.Date, SortOrder = 3 };
        db.CustomFieldDefinitions.AddRange(kundennr, vip, seitJahr);

        // --- Customers --------------------------------------------------------
        customers = new[]
        {
            new Customer { Name = "Anna Schmidt", CompanyName = "Schmidt & Partner GmbH", Email = "anna@schmidt-partner.de", Phone = "+49 30 12345678", Street = "Hauptstraße 12", PostalCode = "10115", City = "Berlin", Country = "Deutschland", Notes = "Bestandskunde seit 2019, kommuniziert bevorzugt per E-Mail." },
            new Customer { Name = "Markus Weber", CompanyName = "Weber Logistik AG", Email = "weber@weber-logistik.at", Phone = "+43 1 9876543", Street = "Marienplatz 4", PostalCode = "1010", City = "Wien", Country = "Österreich", Notes = "Großer Auftrag Q3 in Planung." },
            new Customer { Name = "Lisa Müller", CompanyName = "Müller Design Studio", Email = "lisa.mueller@mueller-design.ch", Phone = "+41 44 5556677", Street = "Bahnhofstraße 8", PostalCode = "8001", City = "Zürich", Country = "Schweiz" },
            new Customer { Name = "Thomas Becker", Email = "t.becker@gmail.com", Phone = "+49 89 4445566", Street = "Sendlinger Straße 22", PostalCode = "80331", City = "München", Country = "Deutschland", Notes = "Privatkunde." },
            new Customer { Name = "Sophie Klein", CompanyName = "Klein Consulting", Email = "sk@klein-consulting.de", Phone = "+49 40 3334455", Street = "Reeperbahn 1", PostalCode = "20359", City = "Hamburg", Country = "Deutschland" },
            new Customer { Name = "Daniel Hofmann", CompanyName = "Hofmann Engineering", Email = "daniel@hofmann-eng.de", Phone = "+49 711 2223344", Street = "Königstraße 50", PostalCode = "70173", City = "Stuttgart", Country = "Deutschland", Notes = "Interesse an Wartungsvertrag." },
        };
        db.Customers.AddRange(customers);

        // Custom field values
        var contactEt = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact;
        for (var i = 0; i < customers.Length; i++)
        {
            var c = customers[i];
            db.CustomFieldValues.Add(new Matbiz.Web.Modules.CustomFields.Models.CustomFieldValue
                { EntityType = contactEt, EntityId = c.Id, FieldDefinitionId = kundennr.Id, Value = $"K-{1000 + i}" });
            if (i is 0 or 2)
                db.CustomFieldValues.Add(new Matbiz.Web.Modules.CustomFields.Models.CustomFieldValue
                    { EntityType = contactEt, EntityId = c.Id, FieldDefinitionId = vip.Id, Value = "true" });
            db.CustomFieldValues.Add(new Matbiz.Web.Modules.CustomFields.Models.CustomFieldValue
                { EntityType = contactEt, EntityId = c.Id, FieldDefinitionId = seitJahr.Id,
                  Value = new DateOnly(2019 + (i % 5), 1 + (i % 9), 1 + (i % 27)).ToString("yyyy-MM-dd") });
        }

        // History samples
        var anchor = DateTime.UtcNow.AddDays(-30);
        db.CustomerHistoryEntries.AddRange(
            new CustomerHistoryEntry { CustomerId = customers[0].Id, At = anchor, Action = "Note", Details = "Erstkontakt auf der Messe.", ActorUserId = demoUser?.Id ?? "" },
            new CustomerHistoryEntry { CustomerId = customers[0].Id, At = anchor.AddDays(7), Action = "Note", Details = "Angebot V1 verschickt.", ActorUserId = demoUser?.Id ?? "" },
            new CustomerHistoryEntry { CustomerId = customers[1].Id, At = anchor.AddDays(2), Action = "Note", Details = "Telefonat zu Q3-Planung.", ActorUserId = demoUser?.Id ?? "" });

        // --- Tasks (persönlich) ----------------------------------------------
        var today = DateTime.UtcNow.Date;
        if (demoUser is not null)
        {
            db.Tasks.AddRange(
                new TaskItem { Title = "Angebot Schmidt & Partner nachfassen", Description = "Status zum Angebot V1 erfragen.", AssignedUserId = demoUser.Id, CustomerId = customers[0].Id, Status = TaskStatus.Open, Priority = TaskPriority.High, DueDate = today.AddDays(-2) },
                new TaskItem { Title = "Wartungsvertrag Hofmann ausarbeiten", AssignedUserId = demoUser.Id, CustomerId = customers[5].Id, Status = TaskStatus.InProgress, Priority = TaskPriority.Normal, DueDate = today.AddDays(7) },
                new TaskItem { Title = "Weber Logistik: Termin Q3-Meeting", AssignedUserId = demoUser.Id, CustomerId = customers[1].Id, Status = TaskStatus.Open, Priority = TaskPriority.Normal, DueDate = today },
                new TaskItem { Title = "Visitenkarten nachbestellen", AssignedUserId = demoUser.Id, Status = TaskStatus.Done, Priority = TaskPriority.Low, DueDate = today.AddDays(-5) });
        }
        } // end !hasCustomers
        var today2 = DateTime.UtcNow.Date;
        var adminUser = await users.FindByEmailAsync(cfg["Matbiz:SeedAdmin:Email"] ?? "");

        // --- Firmen + Verknüpfungen ------------------------------------------
        if (!hasCompanies)
        {
            var schmidtPartner = new Company
            {
                Name = "Schmidt & Partner GmbH",
                Description = "Beratungsgesellschaft mit Sitz in Berlin.",
                Email = "info@schmidt-partner.de",
                Phone = "+49 30 12345600",
                Street = "Hauptstraße 12",
                PostalCode = "10115",
                City = "Berlin",
                Country = "Deutschland"
            };
            var weberLogistik = new Company
            {
                Name = "Weber Logistik AG",
                Description = "Spedition und Lagerlogistik DACH.",
                Email = "office@weber-logistik.at",
                Phone = "+43 1 9876500",
                Street = "Marienplatz 4",
                PostalCode = "1010",
                City = "Wien",
                Country = "Österreich"
            };
            var muellerDesign = new Company
            {
                Name = "Müller Design Studio",
                Description = "Markenentwicklung und Web-Design.",
                Email = "hello@mueller-design.ch",
                Phone = "+41 44 5556600",
                Street = "Bahnhofstraße 8",
                PostalCode = "8001",
                City = "Zürich",
                Country = "Schweiz"
            };
            var kleinConsulting = new Company
            {
                Name = "Klein Consulting",
                Description = "Strategieberatung im Mittelstand.",
                Email = "kontakt@klein-consulting.de",
                Phone = "+49 40 3334400",
                Street = "Reeperbahn 1",
                PostalCode = "20359",
                City = "Hamburg",
                Country = "Deutschland"
            };
            var hofmannEng = new Company
            {
                Name = "Hofmann Engineering",
                Description = "Maschinenbau und Anlagentechnik.",
                Email = "info@hofmann-eng.de",
                Phone = "+49 711 2223300",
                Street = "Königstraße 50",
                PostalCode = "70173",
                City = "Stuttgart",
                Country = "Deutschland"
            };
            db.Companies.AddRange(schmidtPartner, weberLogistik, muellerDesign, kleinConsulting, hofmannEng);
            await db.SaveChangesAsync(ct);

            // Verknüpfe vorhandene Sample-Kontakte mit den passenden Firmen — und
            // lösche das Freitext-CompanyName-Feld, damit der strukturierte
            // Datensatz die Anzeige übernimmt.
            var byCompanyName = new Dictionary<string, Company>
            {
                ["Schmidt & Partner GmbH"] = schmidtPartner,
                ["Weber Logistik AG"] = weberLogistik,
                ["Müller Design Studio"] = muellerDesign,
                ["Klein Consulting"] = kleinConsulting,
                ["Hofmann Engineering"] = hofmannEng
            };
            foreach (var contact in await db.Customers.ToListAsync(ct))
            {
                if (contact.CompanyName is not null && byCompanyName.TryGetValue(contact.CompanyName, out var co))
                {
                    contact.CompanyId = co.Id;
                    contact.CompanyName = null;
                }
            }
            await db.SaveChangesAsync(ct);
        }

        // --- Tags + Tag-Zuweisungen + Beispielgruppen ------------------------
        if (!hasTags)
        {
            var tagVip = new Tag { Name = "VIP", Color = "#b54708" };
            var tagAffe = new Tag { Name = "Affe", Color = "#7c3aed" };
            var tagInteressent = new Tag { Name = "Interessent", Color = "#0e7490" };
            var tagBestandskunde = new Tag { Name = "Bestandskunde", Color = "#027a48" };
            db.Tags.AddRange(tagVip, tagAffe, tagInteressent, tagBestandskunde);
            await db.SaveChangesAsync(ct);

            // Wende Tags auf Sample-Kunden an, falls vorhanden.
            var allCustomers = await db.Customers.ToListAsync(ct);
            if (allCustomers.Count >= 6)
            {
                db.CustomerTags.AddRange(
                    new CustomerTag { CustomerId = allCustomers[0].Id, TagId = tagVip.Id },
                    new CustomerTag { CustomerId = allCustomers[0].Id, TagId = tagBestandskunde.Id },
                    new CustomerTag { CustomerId = allCustomers[1].Id, TagId = tagBestandskunde.Id },
                    new CustomerTag { CustomerId = allCustomers[2].Id, TagId = tagVip.Id },
                    new CustomerTag { CustomerId = allCustomers[3].Id, TagId = tagAffe.Id },
                    new CustomerTag { CustomerId = allCustomers[4].Id, TagId = tagInteressent.Id },
                    new CustomerTag { CustomerId = allCustomers[5].Id, TagId = tagInteressent.Id });
                await db.SaveChangesAsync(ct);
            }
        }

        if (!hasGroups)
        {
            var rules = new CustomerGroupRules
            {
                Combinator = RuleCombinator.All,
                Conditions = new List<CustomerGroupCondition>
                {
                    new() { Field = RuleField.Tag, Operator = RuleOperator.Contains, Value = "VIP" }
                }
            };
            db.CustomerGroups.Add(new CustomerGroup
            {
                Name = "VIP-Kunden",
                Description = "Dynamisch: alle Kunden mit Tag VIP",
                Kind = CustomerGroupKind.Dynamic,
                RulesJson = CustomerGroupService.SerializeRules(rules)
            });

            db.CustomerGroups.Add(new CustomerGroup
            {
                Name = "Newsletter-Empfänger",
                Description = "Statische Auswahl für den nächsten Versand",
                Kind = CustomerGroupKind.Static
            });
        }

        // --- Team + geteilte Aufgaben ----------------------------------------
        if (!hasTeams && adminUser is not null && demoUser is not null)
        {
            var vertrieb = new Team
            {
                Name = "Vertrieb",
                Description = "Vertriebsteam — geteilte Kunden-Aufgaben",
                Members = new List<TeamMember>
                {
                    new() { UserId = adminUser.Id },
                    new() { UserId = demoUser.Id }
                }
            };
            db.Teams.Add(vertrieb);

            db.Tasks.AddRange(
                new TaskItem { Title = "Newsletter Q3 vorbereiten", Description = "Themenvorschläge sammeln, Entwurf bis Freitag.", AssignedTeamId = vertrieb.Id, Status = TaskStatus.Open, Priority = TaskPriority.Normal, DueDate = today2.AddDays(5) },
                new TaskItem { Title = "Messe-Auftritt planen", AssignedTeamId = vertrieb.Id, Status = TaskStatus.InProgress, Priority = TaskPriority.High, DueDate = today2.AddDays(21) });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sample data seeded.");
    }
}
