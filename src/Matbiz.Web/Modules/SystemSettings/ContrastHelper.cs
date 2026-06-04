namespace Matbiz.Web.Modules.SystemSettings;

/// <summary>
/// Picks a readable foreground for a given background color. Uses W3C
/// relative-luminance — same formula browsers use for forcing contrast in
/// high-contrast modes — so the result lines up with native rendering.
/// </summary>
public static class ContrastHelper
{
    public static string OnColor(string hex) =>
        Luminance(hex) > 0.55 ? "#1f2328" : "#ffffff";

    private static double Luminance(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith('#') || hex.Length < 7) return 1.0;
        int r = Convert.ToInt32(hex.Substring(1, 2), 16);
        int g = Convert.ToInt32(hex.Substring(3, 2), 16);
        int b = Convert.ToInt32(hex.Substring(5, 2), 16);
        double[] linear = { Linear(r), Linear(g), Linear(b) };
        return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2];
    }

    private static double Linear(int c)
    {
        var v = c / 255.0;
        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }
}
