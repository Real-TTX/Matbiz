using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Matbiz.Web.Modules.Documents.Services;

/// <summary>
/// Hängt eine XRechnung-XML als Datei-Attachment an ein bestehendes PDF an —
/// das Ergebnis ist ein ZUGFeRD-2.x-Hybrid (PDF mit eingebetteter XML).
///
/// Streng genommen muss das PDF dazu PDF/A-3-konform sein. PdfSharp produziert
/// nicht direkt PDF/A-3, aber für viele Empfänger ist die /AFRelationship
/// Annotation + /EmbeddedFile bereits ausreichend. Für strenge PDF/A-3-Validierung
/// (z.B. amtliche Stellen mit KoSIT-Validator) bräuchte man iText7 / Apryse.
/// </summary>
public static class PdfXmlEmbedder
{
    public static byte[] Embed(byte[] pdfBytes, byte[] xmlBytes, string filename = "factur-x.xml")
    {
        using var inputStream = new MemoryStream(pdfBytes);
        var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

        // Embedded file stream
        var stream = new PdfDictionary(document);
        stream.Elements.SetName("/Type", "/EmbeddedFile");
        stream.Elements.SetString("/Subtype", "text/xml");
        stream.CreateStream(xmlBytes);
        // Params
        var paramsDict = new PdfDictionary(document);
        paramsDict.Elements.SetInteger("/Size", xmlBytes.Length);
        paramsDict.Elements.SetDateTime("/ModDate", DateTime.Now);
        stream.Elements["/Params"] = paramsDict;
        document.Internals.AddObject(stream);

        // File Specification Dictionary
        var fileSpec = new PdfDictionary(document);
        fileSpec.Elements.SetName("/Type", "/Filespec");
        fileSpec.Elements.SetString("/F", filename);
        fileSpec.Elements.SetString("/UF", filename);
        fileSpec.Elements.SetString("/Desc", "ZUGFeRD/XRechnung XML");

        var ef = new PdfDictionary(document);
        ef.Elements["/F"]  = stream.Reference;
        ef.Elements["/UF"] = stream.Reference;
        fileSpec.Elements["/EF"] = ef;

        // ZUGFeRD-Pflicht-Attribut: AFRelationship Alternative
        fileSpec.Elements.SetName("/AFRelationship", "/Alternative");
        document.Internals.AddObject(fileSpec);

        // EmbeddedFiles Namen-Eintrag
        var names = document.Internals.Catalog.Elements.GetDictionary("/Names")
            ?? new PdfDictionary(document);
        var embeddedFiles = names.Elements.GetDictionary("/EmbeddedFiles")
            ?? new PdfDictionary(document);
        var namesArray = embeddedFiles.Elements.GetArray("/Names") ?? new PdfArray(document);
        namesArray.Elements.Add(new PdfString(filename));
        namesArray.Elements.Add(fileSpec.Reference!);
        embeddedFiles.Elements["/Names"] = namesArray;
        names.Elements["/EmbeddedFiles"] = embeddedFiles;
        document.Internals.Catalog.Elements["/Names"] = names;

        // AssociatedFiles auf Catalog-Ebene
        var af = document.Internals.Catalog.Elements.GetArray("/AF") ?? new PdfArray(document);
        af.Elements.Add(fileSpec.Reference!);
        document.Internals.Catalog.Elements["/AF"] = af;

        // ZUGFeRD-Metadata-Schema (XMP) wäre optimal — wir lassen das aus,
        // weil PdfSharp keinen einfachen XMP-Writer bietet. Empfänger-Software
        // findet die XML trotzdem über das /EmbeddedFiles-Name-Tree.

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }
}
