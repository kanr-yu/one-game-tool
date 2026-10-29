using System.Windows.Media;

namespace GameDeck.Models;

// Storage and backup tools are still preview-only; the game library no longer reads demo records.
public sealed record CachePreview(string Name, string Size, string Description, string Glyph, Brush Accent)
{
    public string CleanAction => $"立即清理 · {Name}";
}

public sealed record BackupPreview(string Game, string Detail, string Size)
{
    public string RestoreAction => $"恢复存档 · {Game} · {Detail}";
}

public static class PreviewData
{
    public static IReadOnlyList<CachePreview> Caches { get; } =
    [
        new("着色器缓存", "18.2", "游戏渲染过程中生成的着色器文件", "\uE7F4", Brush("#6FE7BF")),
        new("下载残留包", "5.1", "平台下载与更新后遗留的安装包", "\uE896", Brush("#F2BC83")),
        new("系统临时垃圾", "1.2", "系统和应用产生的临时文件", "\uE74D", Brush("#B2A6F4"))
    ];

    public static IReadOnlyList<BackupPreview> Backups { get; } =
    [
        new("星际远航", "2026-10-28  18:30  ·  手动备份", "24.6 MB"),
        new("霓虹边界", "2026-10-27  21:00  ·  手动备份", "12.8 MB"),
        new("荒野回响", "2026-10-25  16:45  ·  手动备份", "8.2 MB")
    ];

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
