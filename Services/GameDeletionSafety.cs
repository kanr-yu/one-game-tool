using System.IO;
using GameDeck.Models;

namespace GameDeck.Services;

/// <summary>Checks the exact directory before any recursive filesystem mutation.</summary>
internal static class GameDeletionSafety
{
    public static string ValidateInstallation(GameItem game, string dataDirectory,
        IEnumerable<GameItem> otherGames)
    {
        string directory = Normalize(game.InstallDirectory);
        string executable = Normalize(game.ExePath);
        if (!string.Equals(directory, Path.GetDirectoryName(executable), StringComparison.OrdinalIgnoreCase))
            throw new GameOperationException("安装目录与游戏 EXE 所在目录不一致，请重新加载游戏库，或仅从列表移除。");
        if (string.Equals(directory, Normalize(Path.GetPathRoot(directory)!), StringComparison.OrdinalIgnoreCase))
            throw new GameOperationException("不能删除磁盘或共享目录的根目录，请选择「仅从列表移除」。");

        var protectedDirectories = new List<string> { dataDirectory, AppContext.BaseDirectory, Path.GetTempPath() };
        foreach (var folder in new[]
        {
            Environment.SpecialFolder.Windows, Environment.SpecialFolder.System,
            Environment.SpecialFolder.SystemX86, Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.CommonProgramFiles,
            Environment.SpecialFolder.CommonProgramFilesX86, Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolder.DesktopDirectory,
            Environment.SpecialFolder.MyDocuments, Environment.SpecialFolder.MyMusic,
            Environment.SpecialFolder.MyPictures, Environment.SpecialFolder.MyVideos
        })
        {
            string path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrWhiteSpace(path)) protectedDirectories.Add(path);
        }
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile)) protectedDirectories.Add(Path.Combine(profile, "Downloads"));
        if (protectedDirectories.Any(path => IsWithin(Normalize(path), directory)))
            throw new GameOperationException("该目录是系统、用户资料或本软件数据目录，或包含这些目录，不能整体删除。请仅从列表移除。");

        foreach (var other in otherGames.Where(item => item.Id != game.Id))
        {
            if (IsWithin(Normalize(other.ExePath), directory) || IsWithin(Normalize(other.InstallDirectory), directory))
                throw new GameOperationException("该安装目录还包含其他已收录游戏。为避免一并删除，请选择「仅从列表移除」。");
        }

        EnsureNoReparseAncestors(directory);
        return directory;
    }

    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)
            || path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal))
            throw new GameOperationException("删除目标必须是明确的绝对路径，不能使用设备路径。");
        string fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        // Win32 deletion normalizes trailing spaces/dots. Reject ambiguous components instead.
        string root = Path.GetPathRoot(fullPath)!;
        if (fullPath[root.Length..].Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.EndsWith(' ') || part.EndsWith('.')))
            throw new GameOperationException("删除路径包含不明确的末尾空格或点，请仅从列表移除。");
        return fullPath;
    }

    public static bool IsWithin(string path, string directory)
        => string.Equals(path, directory, StringComparison.OrdinalIgnoreCase)
           || path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
               StringComparison.OrdinalIgnoreCase);

    public static bool IsMissing(string path)
    {
        try { File.GetAttributes(path); return false; }
        catch (FileNotFoundException) { return true; }
        catch (DirectoryNotFoundException) { return true; }
    }
    public static void EnsureNoReparseAncestors(string path)
    {
        for (DirectoryInfo? directory = new(path); directory is not null; directory = directory.Parent)
        {
            try
            {
                var attributes = File.GetAttributes(directory.FullName);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new GameOperationException("安装或缓存目录经过符号链接或目录联接，无法确认实际删除范围。请仅从列表移除。");
                if ((attributes & FileAttributes.Directory) == 0)
                    throw new GameOperationException("删除目标不是文件夹，请仅从列表移除。");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
