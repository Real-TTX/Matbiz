using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Documents.Navigation;

public class DocumentsNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Documents";

    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            // Haupt-Eintrag heißt jetzt genauso wie die Sektion — ein „Alle Belege"-Link mehr braucht's nicht.
            new NavMenuEntry("documents:all",      "Auftragsbearbeitung", "bi-files",              "/Documents",                 SortOrder: 40, ActiveOnPrefix: "/Documents"),

            // Quick-Filter sind Default versteckt. Admin schaltet sie pro Bedarf in /Admin/NavLayout ein.
            // Section bewusst null — sie sollen ohne Header direkt unter dem Hauptpunkt hängen.
            new NavMenuEntry("documents:offer",    "Angebote",     "bi-file-earmark-plus",  "/Documents?type=Offer",      SortOrder: 41, IsSub: true, HiddenByDefault: true),
            new NavMenuEntry("documents:order",    "Aufträge",     "bi-file-earmark-check", "/Documents?type=Order",      SortOrder: 42, IsSub: true, HiddenByDefault: true),
            new NavMenuEntry("documents:invoice",  "Rechnungen",   "bi-file-earmark-text",  "/Documents?type=Invoice",    SortOrder: 43, IsSub: true, HiddenByDefault: true),
            new NavMenuEntry("documents:credit",   "Gutschriften", "bi-file-earmark-x",     "/Documents?type=CreditNote", SortOrder: 44, IsSub: true, HiddenByDefault: true),
        });
}
