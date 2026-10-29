using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GameDeck.Services;

public static class IconService
{
    public static string TrySaveCover(string exePath, string coverDirectory)
    {
        string? coverPath = null;
        try
        {
            Directory.CreateDirectory(coverDirectory);
            coverPath = Path.Combine(coverDirectory, Guid.NewGuid().ToString("N") + ".png");
            IntPtr largeIcon = IntPtr.Zero;
            IntPtr smallIcon = IntPtr.Zero;
            try
            {
                // Request the embedded 256px icon instead of enlarging a 16px shell icon.
                int result = SHDefExtractIcon(exePath, 0, 0, out largeIcon, out smallIcon, 256);
                if (result == 0 && largeIcon != IntPtr.Zero)
                {
                    using var icon = Icon.FromHandle(largeIcon);
                    using var bitmap = icon.ToBitmap();
                    bitmap.Save(coverPath, ImageFormat.Png);
                    return coverPath;
                }
            }
            finally
            {
                if (largeIcon != IntPtr.Zero) DestroyIcon(largeIcon);
                if (smallIcon != IntPtr.Zero && smallIcon != largeIcon) DestroyIcon(smallIcon);
            }

            using var fallback = Icon.ExtractAssociatedIcon(exePath);
            if (fallback is not null)
            {
                using var bitmap = fallback.ToBitmap();
                bitmap.Save(coverPath, ImageFormat.Png);
                return coverPath;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"Cover extraction failed: {ex.Message}"); }
        TryDelete(coverPath);
        return "";
    }

    internal static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { File.Delete(path); }
        catch (Exception ex) { Debug.WriteLine($"Cover cleanup failed: {ex.Message}"); }
    }

    [DllImport("shell32.dll", EntryPoint = "SHDefExtractIconW", CharSet = CharSet.Unicode)]
    private static extern int SHDefExtractIcon(string iconFile, int index, uint flags,
        out IntPtr largeIcon, out IntPtr smallIcon, uint iconSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
