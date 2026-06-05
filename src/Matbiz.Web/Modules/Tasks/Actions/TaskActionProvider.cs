using Matbiz.Web.Modules.Modules.Services;
using Matbiz.Web.Shared.Actions;

namespace Matbiz.Web.Modules.Tasks.Actions;

public class TaskActionProvider(ModuleRegistry modules) : IEntityActionProvider
{
    public Task<IReadOnlyList<EntityAction>> GetAsync(EntityActionContext ctx, CancellationToken ct = default)
    {
        if (!modules.IsEnabled("Tasks")) return Task.FromResult<IReadOnlyList<EntityAction>>(Array.Empty<EntityAction>());

        if (ctx.EntityType != EntityTypes.Contact)
            return Task.FromResult<IReadOnlyList<EntityAction>>(Array.Empty<EntityAction>());

        return Task.FromResult<IReadOnlyList<EntityAction>>(new[]
        {
            new EntityAction("Aufgabe", "bi-check2-square", $"/Tasks/Edit?customerId={ctx.EntityId}", Tooltip: "Aufgabe anlegen", SortOrder: 5, Group: "Allgemein")
        });
    }
}
