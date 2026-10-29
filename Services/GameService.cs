using System.Diagnostics;
using System.Globalization;
using System.IO;
using GameDeck.Infrastructure;
using GameDeck.Models;

namespace GameDeck.Services;

public sealed record GameLaunchResult(string LastPlayed, string? PersistenceWarning = null);

public sealed class GameService
{
    private readonly DatabaseService _database;
    private readonly string _coverDirectory;
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public GameService(DatabaseService? database = null, string? coverDirectory = null,
        Func<ProcessStartInfo, Process?>? startProcess = null)
    {
        _database = database ?? new DatabaseService();
        _coverDirectory = coverDirectory ?? Path.Combine(_database.DataDirectory, "Covers");
        _startProcess = startProcess ?? Process.Start;
    }

    public Task<List<GameItem>> GetAllGamesAsync() => _database.GetAllGamesAsync();

    public async Task<GameItem> AddGameAsync(string exePath)
    {
        string coverPath = "";
        try
        {
            string fullPath = ValidateExecutable(exePath);
            if (await _database.GetByExePathAsync(fullPath) is not null) throw new DuplicateGameException();

            string directory = Path.GetDirectoryName(fullPath)!;
            var game = await Task.Run(() =>
            {
                coverPath = IconService.TrySaveCover(fullPath, _coverDirectory);
                long size = DirectorySizeService.Calculate(directory);
                return new GameItem
                {
                    Name = Path.GetFileNameWithoutExtension(fullPath),
                    ExePath = fullPath, InstallDirectory = directory, Platform = "LOCAL",
                    CoverPath = coverPath, SizeBytes = size, SizeFormatted = FileSizeFormatter.Format(size)
                };
            });
            return await _database.AddGameAsync(game);
        }
        catch (Exception ex)
        {
            // Each cover has a new GUID: a failed insert cannot remove another game's cover.
            IconService.TryDelete(coverPath);
            if (ex is GameOperationException) throw;
            throw new GameOperationException($"添加游戏失败：{ex.Message}", ex);
        }
    }

    public async Task<GameLaunchResult> LaunchGameAsync(GameItem game)
    {
        DateTimeOffset startedAt;
        try
        {
            startedAt = await Task.Run(() =>
            {
                string executable = ValidateExecutable(game.ExePath);
                if (!Directory.Exists(game.InstallDirectory))
                    throw new GameOperationException("游戏安装目录不存在，请检查游戏是否已被移动。");
                var startInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = game.InstallDirectory,
                    UseShellExecute = true
                };
                // Disposing the handle does not stop the launched game.
                using var process = _startProcess(startInfo);
                if (process is null) throw new GameOperationException("系统未能启动该游戏。");
                return DateTimeOffset.Now;
            });
        }
        catch (GameOperationException) { throw; }
        catch (Exception ex) { throw new GameOperationException($"启动游戏失败：{ex.Message}", ex); }

        string played = startedAt.ToString("O", CultureInfo.InvariantCulture);
        try
        {
            await _database.UpdateLastPlayedAsync(game.Id, startedAt);
            return new GameLaunchResult(played);
        }
        catch (Exception ex)
        {
            // Never report a failed launch (and encourage a second process) if only the DB write failed.
            return new GameLaunchResult(played, $"游戏已启动，但游玩时间未能保存：{ex.Message}");
        }
    }


    public async Task<GameRemovalResult> DeleteGameAsync(GameItem selected, GameRemovalMode mode)
    {
        if (mode == GameRemovalMode.Cancel) return new GameRemovalResult(false);
        try
        {
            if (selected.Id <= 0) throw new GameOperationException("游戏记录无效，请重新加载游戏库。");
            if (mode == GameRemovalMode.RemoveFromLibrary)
            {
                await _database.DeleteGameById(selected.Id);
                return new GameRemovalResult(true);
            }
            if (mode != GameRemovalMode.DeleteFiles)
                throw new GameOperationException("请选择有效的移除方式。");

            // Read the authoritative DB row rather than accepting an arbitrary UI folder.
            var game = await _database.GetGameByIdAsync(selected.Id);
            if (game is null)
                return new GameRemovalResult(true, Warning: "记录已不在游戏库中，本次没有删除本体文件。");
            if (!string.Equals(GameDeletionSafety.Normalize(game.InstallDirectory),
                    GameDeletionSafety.Normalize(selected.InstallDirectory), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(GameDeletionSafety.Normalize(game.ExePath),
                    GameDeletionSafety.Normalize(selected.ExePath), StringComparison.OrdinalIgnoreCase))
                throw new GameOperationException("游戏路径在确认后发生变化，请重新加载并确认删除目录。");

            var allGames = await _database.GetAllGamesAsync();
            bool filesDeleted = await Task.Run(() =>
            {
                string target = GameDeletionSafety.ValidateInstallation(game, _database.DataDirectory, allGames);
                try
                {
                    // The exact absolute target was checked above. Directory.Delete does not traverse
                    // child reparse points on Windows; it removes those links, not their targets.
                    Directory.Delete(target, recursive: true);
                    return true;
                }
                catch (DirectoryNotFoundException) when (GameDeletionSafety.IsMissing(target)) { return false; }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                    or System.Security.SecurityException)
                {
                    throw new GameOperationException(
                        "无法删除游戏目录，文件可能正在使用、设为只读或权限不足。请关闭游戏及相关程序后重试。"
                        + "游戏库记录已保留；目录中可能已有部分文件被移除。\n" + ex.Message, ex);
                }
            });

            try { await _database.DeleteGameById(game.Id); }
            catch (Exception ex)
            {
                throw new GameOperationException(
                    "游戏目录已删除或不存在，但数据库记录未能移除。列表记录已保留，请选择「仅从列表移除」重试。\n"
                    + ex.Message, ex);
            }

            string? warning = await Task.Run(() => TryRemoveCover(game, allGames));
            return new GameRemovalResult(true, filesDeleted, warning);
        }
        catch (GameOperationException) { throw; }
        catch (Exception ex) { throw new GameOperationException($"移除游戏失败：{ex.Message}", ex); }
    }

    private string? TryRemoveCover(GameItem game, IEnumerable<GameItem> otherGames)
    {
        if (string.IsNullOrWhiteSpace(game.CoverPath)) return null;
        try
        {
            string cover = GameDeletionSafety.Normalize(game.CoverPath);
            string directory = GameDeletionSafety.Normalize(_coverDirectory);
            if (!string.Equals(Path.GetDirectoryName(cover), directory, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(cover), ".png", StringComparison.OrdinalIgnoreCase)
                || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(cover), "N", out _))
                return "游戏已移除，但图标路径不属于本软件的图标缓存，已保留该文件。";
            if (otherGames.Any(other => other.Id != game.Id
                    && string.Equals(other.CoverPath, game.CoverPath, StringComparison.OrdinalIgnoreCase)))
                return "游戏已移除，该图标仍被其他游戏记录使用，因此保留。";
            GameDeletionSafety.EnsureNoReparseAncestors(directory);
            var attributes = File.GetAttributes(cover);
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                return "游戏已移除，图标缓存不是普通 PNG 文件，已跳过清理。";
            File.Delete(cover);
            return null;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (Exception ex) { return $"游戏已移除，但图标缓存未能清理：{ex.Message}"; }
    }

    private static string ValidateExecutable(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new GameOperationException("请选择游戏的 EXE 文件。");
        string fullPath = Path.GetFullPath(path);
        if (!string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
            throw new GameOperationException("只支持添加 .exe 可执行文件。");
        if (!File.Exists(fullPath)) throw new GameOperationException("游戏 EXE 不存在或无法访问，请检查文件路径。");
        return fullPath;
    }
}
