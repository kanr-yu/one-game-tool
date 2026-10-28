using System.Windows.Media;

namespace GameDeck.Models;

public sealed record GamePreview(string Name, string EnglishName, string Genre, string Platform,
    string Size, string LastPlayed, string Number, Brush PosterBrush)
{
    public string LaunchAction => $"启动游戏 · {Name}";
    public string BackupAction => $"备份存档 · {Name}";
    public string MoveAction => $"无损搬家 · {Name}";
}

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
    public static IReadOnlyList<GamePreview> Games { get; } =
    [
        new("星际远航", "STELLAR VOYAGE", "太空探索 · 开放世界", "STEAM", "86.4 GB", "今天 18:42", "01",
            Gradient("#183E51", "#14222D")),
        new("霓虹边界", "NEON FRONTIER", "动作冒险 · 赛博都市", "EPIC GAMES", "104.8 GB", "昨天 21:16", "02",
            Gradient("#56334B", "#271D36")),
        new("荒野回响", "WILDERNESS ECHO", "角色扮演 · 奇幻世界", "XBOX", "51.4 GB", "10 月 25 日", "03",
            Gradient("#475842", "#1E302D"))
    ];

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

    private static LinearGradientBrush Gradient(string start, string end)
    {
        var brush = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(start),
            (Color)ColorConverter.ConvertFromString(end), 55);
        brush.Freeze();
        return brush;
    }
}
