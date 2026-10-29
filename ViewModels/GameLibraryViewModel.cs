using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;
using GameDeck.Infrastructure;
using GameDeck.Models;
using GameDeck.Services;

namespace GameDeck.ViewModels;

public sealed class GameLibraryViewModel : ObservableObject
{
    private readonly GameService _games;
    private readonly IUserDialogs _dialogs;
    private readonly HashSet<GameItem> _observedGames = [];
    private bool _isBusy;
    private bool _hasLoaded;
    private string _searchText = "";
    private string _statusMessage = "添加本地游戏，建立你的游戏库。";
    private string _errorMessage = "";

    public ObservableCollection<GameItem> Games { get; } = [];
    public ICollectionView GamesView { get; }
    public AsyncRelayCommand AddGameCommand { get; }
    public AsyncRelayCommand LaunchGameCommand { get; }
    public AsyncRelayCommand ReloadCommand { get; }
    public AsyncRelayCommand DeleteGameCommand { get; }

    public GameLibraryViewModel(GameService? games = null, IUserDialogs? dialogs = null)
    {
        _games = games ?? new GameService();
        _dialogs = dialogs ?? new UserDialogs();
        GamesView = CollectionViewSource.GetDefaultView(Games);
        GamesView.Filter = item => item is GameItem game &&
            (game.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
             || game.Platform.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
             || game.ExePath.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        AddGameCommand = new AsyncRelayCommand(_ => AddGameAsync(), ReportErrorAsync, _ => !IsBusy && HasLoaded);
        LaunchGameCommand = new AsyncRelayCommand(p => LaunchGameAsync((GameItem)p!),
            ReportErrorAsync, p => p is GameItem && !IsBusy && HasLoaded);
        DeleteGameCommand = new AsyncRelayCommand(p => DeleteGameAsync((GameItem)p!),
            ReportErrorAsync, p => p is GameItem && !IsBusy && HasLoaded);
        ReloadCommand = new AsyncRelayCommand(_ => LoadAsync(), ReportErrorAsync, _ => !IsBusy);
        Games.CollectionChanged += Games_CollectionChanged;
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value)) return;
            AddGameCommand.NotifyCanExecuteChanged();
            LaunchGameCommand.NotifyCanExecuteChanged();
            DeleteGameCommand.NotifyCanExecuteChanged();
            ReloadCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
    public bool HasLoaded
    {
        get => _hasLoaded;
        private set
        {
            if (!SetProperty(ref _hasLoaded, value)) return;
            AddGameCommand.NotifyCanExecuteChanged();
            LaunchGameCommand.NotifyCanExecuteChanged();
            DeleteGameCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(IsEmpty));
        }
    }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value ?? "")) return;
            GamesView.Refresh();
            NotifyViewState();
        }
    }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public int GameCount => Games.Count;
    public long TotalSizeBytes => Games.Aggregate(0L, (total, game) =>
        game.SizeBytes > long.MaxValue - total ? long.MaxValue : total + game.SizeBytes);
    public string TotalSizeFormatted => FileSizeFormatter.Format(TotalSizeBytes);
    public string ResultsLabel => $"{GamesView.Cast<GameItem>().Count()} 款游戏";
    public bool IsEmpty => HasLoaded && !IsBusy && GamesView.IsEmpty;
    public string EmptyTitle => Games.Count == 0 ? "游戏库还是空的" : "没有找到匹配的游戏";
    public string EmptyDescription => Games.Count == 0
        ? "点击「手动添加游戏」，选择游戏 EXE 开始收录。"
        : "换个关键词，或清空搜索查看全部游戏。";

    public Task InitializeAsync() => HasLoaded || IsBusy ? Task.CompletedTask : LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        StatusMessage = "正在读取本地游戏库…";
        try
        {
            var savedGames = await _games.GetAllGamesAsync();
            Games.Clear();
            foreach (var game in savedGames) Games.Add(game);
            HasLoaded = true;
            StatusMessage = GameCount == 0 ? "添加本地游戏，建立你的游戏库。" : $"已加载 {GameCount} 款本地游戏。";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "游戏库加载失败，请检查数据目录权限后点击「重新加载」。";
        }
        finally { IsBusy = false; NotifyViewState(); }
    }

    private async Task AddGameAsync()
    {
        string? path = _dialogs.PickExecutable();
        if (path is null) return;
        IsBusy = true;
        ErrorMessage = "";
        StatusMessage = "正在提取图标并统计安装目录大小，较大的目录可能需要一些时间…";
        try
        {
            GameItem game = await _games.AddGameAsync(path);
            Games.Insert(0, game);
            StatusMessage = $"已添加「{game.Name}」。";
        }
        finally { IsBusy = false; NotifyViewState(); }
    }

    private async Task LaunchGameAsync(GameItem game)
    {
        IsBusy = true;
        ErrorMessage = "";
        StatusMessage = $"正在启动「{game.Name}」…";
        try
        {
            GameLaunchResult result = await _games.LaunchGameAsync(game);
            game.LastPlayed = result.LastPlayed;
            StatusMessage = $"已请求启动「{game.Name}」。";
            if (result.PersistenceWarning is not null)
            {
                ErrorMessage = result.PersistenceWarning;
                await _dialogs.ShowMessageAsync("游戏已启动", result.PersistenceWarning);
            }
        }
        finally { IsBusy = false; }
    }


    private async Task DeleteGameAsync(GameItem game)
    {
        IsBusy = true;
        ErrorMessage = "";
        StatusMessage = "请选择游戏移除方式。";
        try
        {
            var mode = await _dialogs.ConfirmGameDeletionAsync(game);
            if (mode == GameRemovalMode.Cancel)
            {
                StatusMessage = "已取消删除。";
                return;
            }
            StatusMessage = mode == GameRemovalMode.DeleteFiles ? "正在删除游戏文件…" : "正在移除游戏记录…";
            var result = await _games.DeleteGameAsync(game, mode);
            if (!result.Removed) return;
            foreach (var item in Games.Where(item => item.Id == game.Id).ToArray()) Games.Remove(item);
            StatusMessage = mode == GameRemovalMode.DeleteFiles
                ? $"已移除「{game.Name}」并清理游戏目录。"
                : $"已从列表移除「{game.Name}」，游戏文件已保留。";
            if (result.Warning is not null)
            {
                ErrorMessage = result.Warning;
                await _dialogs.ShowMessageAsync("游戏已移除", result.Warning);
            }
        }
        finally { IsBusy = false; NotifyViewState(); }
    }

    private async Task ReportErrorAsync(Exception error)
    {
        ErrorMessage = error.Message;
        StatusMessage = "操作未完成，请查看提示后重试。";
        await _dialogs.ShowMessageAsync("操作未完成", error.Message);
    }

    private void Games_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var removed in _observedGames.Where(g => !Games.Contains(g)).ToArray())
        {
            removed.PropertyChanged -= Game_PropertyChanged;
            _observedGames.Remove(removed);
        }
        foreach (var game in Games)
            if (_observedGames.Add(game)) game.PropertyChanged += Game_PropertyChanged;
        NotifyStatistics();
        NotifyViewState();
    }

    private void Game_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameItem.SizeBytes)) NotifyStatistics();
        if (e.PropertyName is nameof(GameItem.Name) or nameof(GameItem.Platform) or nameof(GameItem.ExePath))
        {
            GamesView.Refresh();
            NotifyViewState();
        }
    }

    private void NotifyStatistics()
    {
        OnPropertyChanged(nameof(GameCount));
        OnPropertyChanged(nameof(TotalSizeBytes));
        OnPropertyChanged(nameof(TotalSizeFormatted));
    }

    private void NotifyViewState()
    {
        OnPropertyChanged(nameof(ResultsLabel));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyDescription));
    }
}
