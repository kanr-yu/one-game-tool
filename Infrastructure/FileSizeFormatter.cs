using System.Globalization;

namespace GameDeck.Infrastructure;

public static class FileSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    public static string Format(long bytes)
    {
        double size = Math.Max(0, bytes);
        int unit = 0;
        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return size.ToString(unit == 0 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + Units[unit];
    }
}
