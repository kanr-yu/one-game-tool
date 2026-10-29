using System.Diagnostics;
using System.IO;
using System.Security;

namespace GameDeck.Services;

public static class DirectorySizeService
{
    public static long Calculate(string directory)
    {
        long total = 0;
        var pending = new Stack<string>();
        pending.Push(directory);
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        while (pending.TryPop(out var current))
        {
            try
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(current, "*", options))
                {
                    try
                    {
                        var attributes = File.GetAttributes(entry);
                        if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                        if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
                        else
                        {
                            long length = new FileInfo(entry).Length;
                            total = length > long.MaxValue - total ? long.MaxValue : total + length;
                        }
                    }
                    catch (Exception ex) when (IsFileError(ex)) { Debug.WriteLine(ex.Message); }
                }
            }
            catch (Exception ex) when (IsFileError(ex)) { Debug.WriteLine(ex.Message); }
        }
        return total;
    }

    private static bool IsFileError(Exception ex)
        => ex is IOException or UnauthorizedAccessException or SecurityException or ArgumentException
            or NotSupportedException;
}
