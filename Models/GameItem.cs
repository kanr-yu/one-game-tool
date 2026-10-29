using System.Globalization;
using GameDeck.Infrastructure;

namespace GameDeck.Models;

public sealed class GameItem : ObservableObject
{
    private string _name = "";
    private string _exePath = "";
    private string _installDirectory = "";
    private long _sizeBytes;
    private string _sizeFormatted = "0 B";
    private string _platform = "LOCAL";
    private string _coverPath = "";
    private string _lastPlayed = "";

    public int Id { get; set; }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string ExePath { get => _exePath; set => SetProperty(ref _exePath, value); }
    public string InstallDirectory { get => _installDirectory; set => SetProperty(ref _installDirectory, value); }
    public long SizeBytes { get => _sizeBytes; set => SetProperty(ref _sizeBytes, Math.Max(0, value)); }
    public string SizeFormatted { get => _sizeFormatted; set => SetProperty(ref _sizeFormatted, value); }
    public string Platform { get => _platform; set => SetProperty(ref _platform, value); }
    public string CoverPath { get => _coverPath; set => SetProperty(ref _coverPath, value); }
    public string LastPlayed
    {
        get => _lastPlayed;
        set
        {
            if (SetProperty(ref _lastPlayed, value)) OnPropertyChanged(nameof(LastPlayedDisplay));
        }
    }

    public string LastPlayedDisplay => DateTimeOffset.TryParse(LastPlayed, CultureInfo.InvariantCulture,
        DateTimeStyles.None, out var time)
        ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        : "尚未游玩";

    public string BackupAction => $"备份存档 · {Name}";
    public string MoveAction => $"无损搬家 · {Name}";
}
