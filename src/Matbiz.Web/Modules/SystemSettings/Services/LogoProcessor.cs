using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Matbiz.Web.Modules.SystemSettings.Services;

/// <summary>
/// Logo-Verarbeitung: sauberer Hintergrund + optional einfärben.
///
/// Statt einer fixen Luma-Schwelle (zu unzuverlässig bei off-white,
/// hell-grauen oder leicht texturierten Hintergründen) erkennen wir
/// die Hintergrund-Farbe aus den vier Bild-Ecken und entfernen alle
/// Pixel die in RGB-Distanz nahe genug dran liegen.
/// </summary>
public static class LogoProcessor
{
    public record Options(
        bool RemoveBackground,
        int ColorTolerance,        // 0..200 — wie nah am Eckfarbwert ein Pixel sein muss um „BG" zu sein
        string? RecolorHex);

    public record Result(byte[] Bytes, int TotalPixels, int PixelsMadeTransparent, Rgba32 DetectedBg);

    public static Result Process(byte[] sourceBytes, Options opts)
    {
        using var img = Image.Load<Rgba32>(sourceBytes);
        var w = img.Width;
        var h = img.Height;
        var totalPixels = w * h;
        var tolerance = Math.Clamp(opts.ColorTolerance, 5, 200);
        var softZone = tolerance / 3;  // weiche Übergangszone

        var bg = opts.RemoveBackground ? DetectBackgroundColor(img) : new Rgba32(255, 255, 255, 0);
        var recolor = TryParseHex(opts.RecolorHex);

        int madeTransparent = 0;

        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    var px = row[x];
                    if (px.A == 0) continue;

                    byte newAlpha = px.A;

                    if (opts.RemoveBackground)
                    {
                        var dist = ColorDistance(px, bg);
                        if (dist <= tolerance)
                        {
                            // Voll im BG-Bereich
                            if (dist <= tolerance - softZone)
                            {
                                row[x] = new Rgba32(0, 0, 0, 0);
                                madeTransparent++;
                                continue;
                            }
                            // Soft-Edge — linear ausblenden zwischen (tol-softZone) und tol
                            var t = (tolerance - dist) / (float)softZone;  // 0..1
                            newAlpha = (byte)(px.A * (1f - t));
                        }
                    }

                    if (recolor is Rgba32 rc)
                    {
                        // Inkdichte = Komplement der Helligkeit. Volle Schwarz → voll opaque
                        // in der Zielfarbe; helles Grau → halbtransparent.
                        var luma = (px.R * 0.2126f + px.G * 0.7152f + px.B * 0.0722f) / 255f;
                        var ink = 1f - luma;
                        // Bonus: wenn das Pixel sehr nah am BG ist, eher transparenter halten
                        // damit die Recolor nicht den ganzen ehemaligen Background anmalt.
                        var combined = (byte)(newAlpha * ink);
                        row[x] = new Rgba32(rc.R, rc.G, rc.B, combined);
                    }
                    else
                    {
                        row[x] = new Rgba32(px.R, px.G, px.B, newAlpha);
                    }
                }
            }
        });

        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return new Result(ms.ToArray(), totalPixels, madeTransparent, bg);
    }

    /// <summary>
    /// Median-Farbe aus den 4 Ecken (5×5-Patches). Ausreißer werden so geglättet —
    /// klassische Logos haben überall in den Ecken den gleichen Hintergrund.
    /// </summary>
    private static Rgba32 DetectBackgroundColor(Image<Rgba32> img)
    {
        var samples = new List<Rgba32>(100);
        SampleCorner(img, 0, 0, samples);
        SampleCorner(img, img.Width - 5, 0, samples);
        SampleCorner(img, 0, img.Height - 5, samples);
        SampleCorner(img, img.Width - 5, img.Height - 5, samples);

        if (samples.Count == 0) return new Rgba32(255, 255, 255, 255);

        // Median pro Kanal
        byte med(Func<Rgba32, byte> sel)
        {
            var arr = samples.Select(sel).OrderBy(x => x).ToArray();
            return arr[arr.Length / 2];
        }
        return new Rgba32(med(p => p.R), med(p => p.G), med(p => p.B), 255);
    }

    private static void SampleCorner(Image<Rgba32> img, int x0, int y0, List<Rgba32> output)
    {
        for (int y = y0; y < Math.Min(y0 + 5, img.Height); y++)
        {
            for (int x = x0; x < Math.Min(x0 + 5, img.Width); x++)
            {
                if (x < 0 || y < 0) continue;
                var p = img[x, y];
                if (p.A > 0) output.Add(p);
            }
        }
    }

    private static float ColorDistance(Rgba32 a, Rgba32 b)
    {
        // Euklid in RGB. Reicht für Logos vollkommen.
        var dr = a.R - b.R;
        var dg = a.G - b.G;
        var db = a.B - b.B;
        return MathF.Sqrt(dr * dr + dg * dg + db * db);
    }

    private static Rgba32? TryParseHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.Trim().TrimStart('#');
        if (hex.Length != 6) return null;
        if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var v)) return null;
        return new Rgba32((byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF), 255);
    }
}
