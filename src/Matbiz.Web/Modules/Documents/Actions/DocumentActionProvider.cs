using Matbiz.Web.Modules.Modules.Services;
using Matbiz.Web.Shared.Actions;

namespace Matbiz.Web.Modules.Documents.Actions;

/// <summary>
/// Registriert die „Beleg erstellen aus Kontakt/Firma"-Aktionen.
/// Sichtbar nur wenn das Documents-Modul aktiv ist.
/// </summary>
public class DocumentActionProvider(ModuleRegistry modules) : IEntityActionProvider
{
    public Task<IReadOnlyList<EntityAction>> GetAsync(EntityActionContext ctx, CancellationToken ct = default)
    {
        if (!modules.IsEnabled("Documents"))
            return Task.FromResult<IReadOnlyList<EntityAction>>(Array.Empty<EntityAction>());

        var queryKey = ctx.EntityType switch
        {
            EntityTypes.Contact => "customerId",
            EntityTypes.Company => "companyId",
            _ => null
        };
        if (queryKey is null)
            return Task.FromResult<IReadOnlyList<EntityAction>>(Array.Empty<EntityAction>());

        var actions = new List<EntityAction>
        {
            new("Angebot",    "bi-file-earmark-plus",  $"/Documents/Create?type=Offer&{queryKey}={ctx.EntityId}",      Tooltip: "Angebot anlegen",   SortOrder: 10, Group: "Auftragsbearbeitung"),
            new("Rechnung",   "bi-file-earmark-text",  $"/Documents/Create?type=Invoice&{queryKey}={ctx.EntityId}",    Tooltip: "Rechnung anlegen",  SortOrder: 20, Group: "Auftragsbearbeitung"),
        };
        return Task.FromResult<IReadOnlyList<EntityAction>>(actions);
    }
}
