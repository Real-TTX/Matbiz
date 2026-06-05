using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.SystemSettings.Models;
using Matbiz.Web.Modules.SystemSettings.Services;
using s2industries.ZUGFeRD;

namespace Matbiz.Web.Modules.Documents.Services;

/// <summary>
/// Erzeugt XRechnung-konforme XML (ZUGFeRD 2.x mit XRechnung-Profil). Output ist
/// reines UBL/CII-XML — kann als Datei runtergeladen oder in einen PDF/A-3
/// eingebettet werden (= ZUGFeRD-Hybrid). Validierung gegen offizielles KoSIT-Schema
/// machen wir hier nicht — das ist Sache des Empfängers.
///
/// Mapping der Felder folgt EN 16931 / XRechnung 3.0.
/// </summary>
public class XRechnungGenerator(BrandingService branding)
{
    public async Task<byte[]> GenerateAsync(Document doc, CancellationToken ct = default)
    {
        var b = await branding.GetAsync(ct);
        var invoice = BuildInvoice(doc, b);

        using var ms = new MemoryStream();
        invoice.Save(ms, ZUGFeRDVersion.Version23, Profile.XRechnung);
        return ms.ToArray();
    }

    /// <summary>Validiert ob die Pflichtfelder für XRechnung gesetzt sind.</summary>
    public List<string> ValidateRequired(Document doc, BrandingSettings b)
    {
        var problems = new List<string>();

        // Verkäufer
        if (string.IsNullOrWhiteSpace(b.CompanyLegalName)) problems.Add("Firma-Stammdaten: Firmenname fehlt.");
        if (string.IsNullOrWhiteSpace(b.CompanyStreet))    problems.Add("Firma-Stammdaten: Straße fehlt.");
        if (string.IsNullOrWhiteSpace(b.CompanyPostalCode))problems.Add("Firma-Stammdaten: PLZ fehlt.");
        if (string.IsNullOrWhiteSpace(b.CompanyCity))      problems.Add("Firma-Stammdaten: Ort fehlt.");
        if (string.IsNullOrWhiteSpace(b.VatId) && string.IsNullOrWhiteSpace(b.TaxNumber))
            problems.Add("Firma-Stammdaten: USt-ID oder Steuernummer erforderlich.");
        if (string.IsNullOrWhiteSpace(b.CompanyEmail))     problems.Add("Firma-Stammdaten: E-Mail-Adresse erforderlich (BT-34).");

        // Käufer
        if (string.IsNullOrWhiteSpace(doc.CustomerNameSnapshot))    problems.Add("Beleg: Empfängername fehlt.");
        if (string.IsNullOrWhiteSpace(doc.CustomerAddressSnapshot)) problems.Add("Beleg: Empfängeradresse fehlt.");

        // Positionen
        if (doc.Positions.Count == 0) problems.Add("Beleg: Mindestens eine Position erforderlich.");

        // B2G-XRechnung speziell
        if (string.IsNullOrWhiteSpace(doc.BuyerReference))
            problems.Add("Beleg: Leitweg-ID fehlt (BT-10) — Pflicht für B2G-XRechnung.");

        return problems;
    }

    private InvoiceDescriptor BuildInvoice(Document doc, BrandingSettings b)
    {
        var inv = InvoiceDescriptor.CreateInvoice(
            doc.Number,
            doc.DocumentDate.ToLocalTime(),
            CurrencyCodes.EUR);  // ZUGFeRD-csharp 18 supports limited currency enum; EUR default

        inv.Type = doc.Type switch
        {
            DocumentType.CreditNote => InvoiceType.Correction,
            _ => InvoiceType.Invoice
        };

        // === Verkäufer (eigene Firma) ===
        inv.SetSeller(
            id: null,
            globalID: null,
            name: b.CompanyLegalName ?? b.AppName,
            postcode: b.CompanyPostalCode ?? "",
            city: b.CompanyCity ?? "",
            street: b.CompanyStreet ?? "",
            country: MapCountry(b.CompanyCountry));

        if (!string.IsNullOrWhiteSpace(b.VatId))
            inv.AddSellerTaxRegistration(b.VatId, TaxRegistrationSchemeID.VA);
        if (!string.IsNullOrWhiteSpace(b.TaxNumber))
            inv.AddSellerTaxRegistration(b.TaxNumber, TaxRegistrationSchemeID.FC);

        // BT-34 (elektronische Adresse): Die Lib bietet kein EM-Scheme — wir
        // benutzen die USt-ID als EAS-Identifier (Code 9930 für Deutschland);
        // Email-Kontakt wird über SetSellerContact mitgeschickt.
        if (!string.IsNullOrWhiteSpace(b.VatId))
            inv.SetSellerElectronicAddress(b.VatId, ElectronicAddressSchemeIdentifiers.GermanyVatNumber);

        if (!string.IsNullOrWhiteSpace(b.CompanyPhone) || !string.IsNullOrWhiteSpace(b.CompanyEmail))
            inv.SetSellerContact(b.ManagingDirector ?? "", "", b.CompanyPhone ?? "", "", b.CompanyEmail ?? "");

        // === Käufer ===
        var (buyerStreet, buyerPlz, buyerCity, buyerCountry) = SplitAddress(doc.CustomerAddressSnapshot);
        inv.SetBuyer(
            id: null,
            globalID: null,
            name: doc.CustomerNameSnapshot ?? "",
            postcode: buyerPlz,
            city: buyerCity,
            street: buyerStreet,
            country: MapCountry(buyerCountry));

        if (!string.IsNullOrWhiteSpace(doc.BuyerVatIdSnapshot))
            inv.AddBuyerTaxRegistration(doc.BuyerVatIdSnapshot, TaxRegistrationSchemeID.VA);
        if (!string.IsNullOrWhiteSpace(doc.BuyerVatIdSnapshot))
            inv.SetBuyerElectronicAddress(doc.BuyerVatIdSnapshot, ElectronicAddressSchemeIdentifiers.GermanyVatNumber);

        // === Pflicht-Referenzen für XRechnung ===
        // BT-10 Buyer reference (Leitweg-ID für B2G)
        inv.ReferenceOrderNo = doc.BuyerReference ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(doc.BuyerOrderNumber))
            inv.OrderNo = doc.BuyerOrderNumber;
        if (!string.IsNullOrWhiteSpace(doc.ContractNumber))
            inv.ContractReferencedDocument = new ContractReferencedDocument { ID = doc.ContractNumber };

        // === Daten ===
        if (doc.ServiceDate is DateTime sd) inv.ActualDeliveryDate = sd.ToLocalTime();
        if (doc.DueDate is DateTime due)
        {
            // Payment Means + Due Date
            inv.AddTradePaymentTerms(doc.PaymentTerms ?? "", due.ToLocalTime());
        }
        else if (!string.IsNullOrWhiteSpace(doc.PaymentTerms))
        {
            inv.AddTradePaymentTerms(doc.PaymentTerms);
        }

        // Bankverbindung — SEPA Credit Transfer (Code 58)
        if (!string.IsNullOrWhiteSpace(b.Iban))
        {
            inv.AddCreditorFinancialAccount(b.Iban!, b.Bic ?? "", name: b.BankName);
            inv.PaymentMeans = new PaymentMeans
            {
                TypeCode = PaymentMeansTypeCodes.SEPACreditTransfer,
                Information = "SEPA-Überweisung"
            };
        }

        // === Positionen ===
        int posIdx = 1;
        foreach (var p in doc.Positions.OrderBy(x => x.Position))
        {
            var line = inv.AddTradeLineItem(
                lineID: posIdx.ToString(),
                name: p.Description,
                billedQuantity: p.Quantity,
                unitCode: MapUnit(p.Unit),
                netUnitPrice: p.NetPrice,
                grossUnitPrice: 0m,
                categoryCode: ParseTaxCategory(p.VatCategoryCode),
                taxPercent: p.TaxRatePercent,
                taxType: TaxTypes.VAT);

            if (!string.IsNullOrEmpty(p.ArticleNumber))
                line.SellerAssignedID = p.ArticleNumber;

            // Rabatt: NetPrice ist bereits Snapshot — die Rabatt-Information geht
            // im aktuellen Setup als reduzierter LineTotal in den Beleg. Für die
            // XML lassen wir's bei der ohnehin gerechneten Netto-Summe.
            posIdx++;
        }

        // === Summen ===
        inv.SetTotals(
            lineTotalAmount: doc.NetTotal,
            chargeTotalAmount: 0m,
            allowanceTotalAmount: 0m,
            taxBasisAmount: doc.NetTotal,
            taxTotalAmount: doc.TaxTotal,
            grandTotalAmount: doc.GrossTotal,
            totalPrepaidAmount: 0m,
            duePayableAmount: doc.GrossTotal);

        // VAT-Aufschlüsselung nach Steuersatz aggregieren
        foreach (var grp in doc.Positions.GroupBy(p => (p.TaxRatePercent, Cat: p.VatCategoryCode)))
        {
            var baseAmount = grp.Sum(p => p.NetTotal);
            var taxAmount  = grp.Sum(p => p.TaxTotal);
            inv.AddApplicableTradeTax(
                basisAmount: baseAmount,
                percent: grp.Key.TaxRatePercent,
                taxAmount: taxAmount,
                typeCode: TaxTypes.VAT,
                categoryCode: ParseTaxCategory(grp.Key.Cat));
        }

        // Kopf- und Fußtexte
        if (!string.IsNullOrWhiteSpace(doc.HeaderText))
            inv.AddNote(doc.HeaderText!, SubjectCodes.AAI);
        if (!string.IsNullOrWhiteSpace(doc.FooterText))
            inv.AddNote(doc.FooterText!, SubjectCodes.AAI);

        return inv;
    }

    private static TaxCategoryCodes ParseTaxCategory(string code) => code?.ToUpperInvariant() switch
    {
        "S"  => TaxCategoryCodes.S,
        "Z"  => TaxCategoryCodes.Z,
        "E"  => TaxCategoryCodes.E,
        "AE" => TaxCategoryCodes.AE,
        "K"  => TaxCategoryCodes.K,
        "G"  => TaxCategoryCodes.G,
        "O"  => TaxCategoryCodes.O,
        _    => TaxCategoryCodes.S
    };

    private static QuantityCodes MapUnit(string unit) => unit?.ToLowerInvariant() switch
    {
        "stück" or "stk" or "stueck" or "piece" => QuantityCodes.H87,
        "h" or "std" or "stunde" => QuantityCodes.HUR,
        "tag" or "day" => QuantityCodes.DAY,
        "monat" or "month" => QuantityCodes.MON,
        "pauschal" => QuantityCodes.LS,
        "kg" => QuantityCodes.KGM,
        "g" => QuantityCodes.GRM,
        "l" or "liter" => QuantityCodes.LTR,
        "m" => QuantityCodes.MTR,
        "m²" or "m2" => QuantityCodes.MTK,
        "m³" or "m3" => QuantityCodes.MTQ,
        _ => QuantityCodes.C62  // „one"-Default
    };

    private static CountryCodes MapCountry(string? c)
    {
        if (string.IsNullOrWhiteSpace(c)) return CountryCodes.DE;
        var s = c.Trim().ToLowerInvariant();
        return s switch
        {
            "deutschland" or "germany" or "de" => CountryCodes.DE,
            "österreich" or "austria" or "at"  => CountryCodes.AT,
            "schweiz" or "switzerland" or "ch" => CountryCodes.CH,
            "frankreich" or "france" or "fr"   => CountryCodes.FR,
            "italien" or "italy" or "it"       => CountryCodes.IT,
            "niederlande" or "netherlands" or "nl" => CountryCodes.NL,
            "belgien" or "belgium" or "be"     => CountryCodes.BE,
            "polen" or "poland" or "pl"        => CountryCodes.PL,
            _ => CountryCodes.DE
        };
    }

    /// <summary>Splittet Multi-Line-Adresse (z.B. „Firma\nStraße\n12345 Stadt\nLand")
    /// in (street, plz, city, country) so gut wie möglich.</summary>
    private static (string street, string plz, string city, string? country) SplitAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return ("", "", "", null);
        var lines = address.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        // Heuristik: vorletzte Zeile = „PLZ Ort", letzte Zeile = Land, vor PLZ-Zeile = Straße
        string country = lines.Count >= 1 ? lines[^1] : "";
        var plzCityIdx = lines.Count >= 2 ? lines.Count - 2 : -1;
        var (plz, city) = plzCityIdx >= 0 ? SplitPlzCity(lines[plzCityIdx]) : ("", "");
        var street = plzCityIdx >= 1 ? lines[plzCityIdx - 1] : (lines.Count > 0 ? lines[0] : "");

        // Wenn letzte Zeile gar kein Land sein kann (PLZ-Format), korrigieren
        if (country.Any(char.IsDigit) || country.Length > 30)
        {
            var (pz, ct) = SplitPlzCity(country);
            return (street, pz, ct, null);
        }
        return (street, plz, city, country);
    }

    private static (string plz, string city) SplitPlzCity(string line)
    {
        var parts = line.Split(' ', 2);
        if (parts.Length == 2 && parts[0].Any(char.IsDigit))
            return (parts[0], parts[1]);
        return ("", line);
    }
}
