using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using GameDeck.Views.Pages;
using GameDeck.Infrastructure;
using GameDeck.Models;
using GameDeck.Services;
using GameDeck.ViewModels;

namespace GameDeck.IntegrationTests;

internal static class Program
{
    private static string _root = "";
    private static int _passed;

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--launch-probe")
        {
            try { File.WriteAllText(args[1], Environment.CurrentDirectory); return 0; }
            catch { return 1; }
        }

        // Keep CollectionView and ObservableCollection continuations on a single WPF dispatcher.
        var dispatcher = Dispatcher.CurrentDispatcher;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        Task<int> tests = RunAsync();
        _ = tests.ContinueWith(_ => dispatcher.BeginInvokeShutdown(DispatcherPriority.Background),
            TaskScheduler.Default);
        Dispatcher.Run();
        return tests.GetAwaiter().GetResult();
    }

    private static async Task<int> RunAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), "GameDeck-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(_root);
            await Check("SQLite persistence, parameterized CRUD, and uniqueness", DatabaseAsync);
            await Check("Recursive size and formatting", SizeAsync);
            await Check("Executable import, PNG cover, duplicate and invalid paths", ImportAsync);
            await Check("Real controlled process launch uses installation working directory", LaunchAsync);
            await Check("Failed launch preserves timestamp; DB failure reports launched state", LaunchFailureAsync);
            await Check("ViewModel load, add, filtering, totals, persistence, and cancel", ViewModelAsync);
            await Check("Database errors remain in UI state; missing covers fall back", ErrorBoundaryAsync);
            await Check("List removal, cancellation, and live statistics", RemovalChoicesAsync);
            await Check("Permanent deletion and already-missing directories", PermanentDeletionAsync);
            await Check("Locked files, shared folders, and changed paths", DeletionFailuresAsync);
            await Check("Protected and ambiguous deletion targets", DeletionGuardsAsync);
            await Check("Locked and untrusted cover cleanup paths", CoverCleanupFailuresAsync);
            await Check("WPF page renders real items and binds card launch command", PageBindingsAsync);
            await Check("Navigation-hosted page-root/card/blank-area wheel scrolling", ScrollWheelAsync);
            Console.WriteLine($"PASS: {_passed} integration checks. No real game was launched.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            try
            {
                string expectedParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
                string actualParent = Path.GetDirectoryName(Path.GetFullPath(_root))!;
                if (string.Equals(expectedParent, actualParent, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(_root).StartsWith("GameDeck-tests-", StringComparison.Ordinal))
                    Directory.Delete(_root, true);
            }
            catch (Exception ex) { Console.Error.WriteLine($"Temporary test cleanup: {ex.Message}"); }
        }
    }

    private static async Task Check(string name, Func<Task> check)
    {
        await check();
        _passed++;
        Console.WriteLine($"PASS: {name}");
    }

    private static DatabaseService Database(string name) => new(Path.Combine(_root, name, "data.db"));
    private static GameItem Record(string exe = "C:\\games\\test.exe") => new()
    {
        Name = "Game 'quoted'; DROP TABLE Games; --", ExePath = exe,
        InstallDirectory = "C:\\games", SizeBytes = 1024, SizeFormatted = "1.0 KB",
        Platform = "LOCAL", CoverPath = "", LastPlayed = ""
    };

    private static async Task DatabaseAsync()
    {
        var db = Database("crud");
        await db.InitializeAsync();
        Assert((await db.GetAllGamesAsync()).Count == 0, "Database must start empty.");
        GameItem saved = await db.AddGameAsync(Record());
        Assert(saved.Id > 0, "Auto-increment ID not assigned.");
        var reopened = new DatabaseService(db.DatabasePath);
        var rows = await reopened.GetAllGamesAsync();
        Assert(rows.Count == 1 && rows[0].Name == saved.Name, "Reopened DB lost quoted data.");
        await Expect<DuplicateGameException>(() => reopened.AddGameAsync(Record(saved.ExePath.ToUpperInvariant())));
        saved.Name = "Updated";
        await reopened.UpdateGameAsync(saved);
        var now = DateTimeOffset.Now;
        await reopened.UpdateLastPlayedAsync(saved.Id, now);
        rows = await db.GetAllGamesAsync();
        Assert(rows[0].Name == "Updated" && DateTimeOffset.Parse(rows[0].LastPlayed) == now, "Update not persisted.");
        Assert(await db.DeleteGameById(saved.Id), "Delete failed.");
        Assert(!await db.DeleteGameAsync(saved.Id), "Missing row delete should return false.");
        Assert((await db.GetAllGamesAsync()).Count == 0, "Delete not persisted.");
    }

    private static Task SizeAsync()
    {
        string folder = Path.Combine(_root, "size");
        Directory.CreateDirectory(Path.Combine(folder, "nested"));
        File.WriteAllBytes(Path.Combine(folder, "one.bin"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(folder, "nested", "two.bin"), new byte[2048]);
        Assert(DirectorySizeService.Calculate(folder) == 3072, "Nested directory size is wrong.");
        Assert(DirectorySizeService.Calculate(Path.Combine(folder, "missing")) == 0, "Missing directory should be skipped.");
        Assert(FileSizeFormatter.Format(0) == "0 B", "Zero format.");
        Assert(FileSizeFormatter.Format(13314398618) == "12.4 GB", "GB format.");
        return Task.CompletedTask;
    }

    private static string CreateExecutable(string folder)
    {
        Directory.CreateDirectory(folder);
        string executable = Path.Combine(folder, "sample game.exe");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "GameDeck.IntegrationTests.exe"), executable);
        Directory.CreateDirectory(Path.Combine(folder, "resources"));
        File.WriteAllBytes(Path.Combine(folder, "resources", "asset.bin"), new byte[2048]);
        return executable;
    }

    private static async Task ImportAsync()
    {
        var db = Database("import-data");
        string exe = CreateExecutable(Path.Combine(_root, "sample game 中文"));
        long expected = DirectorySizeService.Calculate(Path.GetDirectoryName(exe)!);
        var service = new GameService(db);
        GameItem added = await service.AddGameAsync(exe);
        Assert(added.Name == "sample game" && added.Platform == "LOCAL", "Import metadata.");
        Assert(added.SizeBytes == expected && added.SizeFormatted == FileSizeFormatter.Format(expected), "Import size.");
        Assert(!string.IsNullOrWhiteSpace(added.CoverPath) && File.Exists(added.CoverPath), "PNG cover missing.");
        using (var image = System.Drawing.Image.FromFile(added.CoverPath))
            Assert(image.Width == 256 && image.Height == 256, "Expected a 256px extracted icon.");
        await Expect<DuplicateGameException>(() => service.AddGameAsync(exe));
        Assert((await db.GetAllGamesAsync()).Count == 1, "Duplicate imported twice.");
        await Expect<GameOperationException>(() => service.AddGameAsync(Path.Combine(_root, "missing.exe")));
        await Expect<GameOperationException>(() => service.AddGameAsync(Path.ChangeExtension(exe, ".txt")));
    }

    private static async Task<GameItem> ProbeRecord(DatabaseService db)
    {
        string folder = Path.Combine(_root, Guid.NewGuid().ToString("N"), "working directory 中文");
        Directory.CreateDirectory(folder);
        var game = Record(Path.Combine(AppContext.BaseDirectory, "GameDeck.IntegrationTests.exe"));
        game.InstallDirectory = folder;
        return await db.AddGameAsync(game);
    }

    private static Func<ProcessStartInfo, Process?> ProbeLauncher(string marker, Action<ProcessStartInfo>? inspect = null)
        => info =>
        {
            inspect?.Invoke(info);
            info.ArgumentList.Add("--launch-probe");
            info.ArgumentList.Add(marker);
            // Only the test subprocess is hidden; production launch options are inspected above.
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            return Process.Start(info);
        };

    private static async Task WaitForMarker(string marker, string directory)
    {
        for (int i = 0; i < 100; i++)
        {
            if (File.Exists(marker))
            {
                try
                {
                    if (File.ReadAllText(marker) == directory) return;
                }
                catch (IOException) { }
            }
            await Task.Delay(50);
        }
        throw new Exception("Launch probe did not observe the expected working directory.");
    }

    private static async Task LaunchAsync()
    {
        var db = Database("launch");
        var game = await ProbeRecord(db);
        string marker = Path.Combine(_root, "launch-marker.txt");
        var service = new GameService(db, startProcess: ProbeLauncher(marker, info =>
        {
            Assert(info.WorkingDirectory == game.InstallDirectory, "WorkingDirectory was omitted.");
            Assert(info.FileName == game.ExePath && info.UseShellExecute, "Launch executable/shell options.");
        }));
        var result = await service.LaunchGameAsync(game);
        await WaitForMarker(marker, game.InstallDirectory);
        var saved = (await new DatabaseService(db.DatabasePath).GetAllGamesAsync()).Single();
        Assert(saved.LastPlayed == result.LastPlayed && result.PersistenceWarning is null, "LastPlayed not persisted.");
    }

    private static async Task LaunchFailureAsync()
    {
        var db = Database("launch-errors");
        var game = await ProbeRecord(db);
        var fail = new GameService(db, startProcess: _ => throw new Win32Exception("Simulated start failure"));
        await Expect<GameOperationException>(() => fail.LaunchGameAsync(game));
        Assert((await db.GetAllGamesAsync()).Single().LastPlayed == "", "Failed launch changed LastPlayed.");
        await db.DeleteGameAsync(game.Id);
        string marker = Path.Combine(_root, "launched-with-db-warning.txt");
        var warning = new GameService(db, startProcess: ProbeLauncher(marker));
        var result = await warning.LaunchGameAsync(game);
        await WaitForMarker(marker, game.InstallDirectory);
        Assert(result.PersistenceWarning is not null && result.LastPlayed != "", "DB warning lost launched state.");
    }

    private static async Task ViewModelAsync()
    {
        var db = Database("vm");
        var dialogs = new TestDialogs();
        var vm = new GameLibraryViewModel(new GameService(db), dialogs);
        await vm.InitializeAsync();
        Assert(vm.HasLoaded && vm.GameCount == 0 && vm.IsEmpty, "Empty initial view.");
        dialogs.Path = CreateExecutable(Path.Combine(_root, "vm game"));
        await vm.AddGameCommand.ExecuteAsync();
        Assert(vm.GameCount == 1 && vm.TotalSizeBytes == vm.Games[0].SizeBytes, "Dynamic totals.");
        vm.SearchText = "no such game";
        Assert(vm.IsEmpty && vm.ResultsLabel == "0 款游戏", "Empty search results.");
        vm.SearchText = "local";
        Assert(!vm.IsEmpty && vm.ResultsLabel == "1 款游戏", "Platform filter.");
        await vm.AddGameCommand.ExecuteAsync();
        Assert(vm.GameCount == 1 && dialogs.Messages.Count == 1 && !vm.IsBusy, "Duplicate error not handled.");
        dialogs.Path = null;
        await vm.AddGameCommand.ExecuteAsync();
        Assert(vm.GameCount == 1 && !vm.IsBusy, "Cancel should have no effects.");
        var restarted = new GameLibraryViewModel(new GameService(new DatabaseService(db.DatabasePath)), dialogs);
        await restarted.InitializeAsync();
        Assert(restarted.GameCount == 1 && restarted.TotalSizeFormatted == vm.TotalSizeFormatted, "Reload persistence.");
        restarted.Games[0].SizeBytes = 4096;
        Assert(restarted.TotalSizeFormatted == "4.0 KB", "Model changes should update totals.");
    }

    private static async Task ErrorBoundaryAsync()
    {
        string blocker = Path.Combine(_root, "not-a-directory");
        File.WriteAllText(blocker, "test");
        var vm = new GameLibraryViewModel(
            new GameService(new DatabaseService(Path.Combine(blocker, "data.db"))), new TestDialogs());
        await vm.InitializeAsync();
        Assert(vm.HasError && !vm.IsBusy && !vm.HasLoaded, "Startup error escaped UI state.");
        Assert(vm.ReloadCommand.CanExecute(null) && !vm.AddGameCommand.CanExecute(null), "Error retry availability.");
        var converter = new LocalCoverConverter();
        Assert(converter.Convert(Path.Combine(_root, "missing.png"), typeof(object), null!, CultureInfo.InvariantCulture) is null,
            "Missing cover must fall back.");
    }

    private static async Task PageBindingsAsync()
    {
        var app = new GameDeck.App();
        app.InitializeComponent();
        var db = Database("page");
        await db.AddGameAsync(Record());
        var vm = new GameLibraryViewModel(new GameService(db), new TestDialogs());
        await vm.InitializeAsync();
        var page = new GameLibraryPage(vm);
        page.Measure(new System.Windows.Size(1040, 1000));
        page.Arrange(new Rect(0, 0, 1040, 1000));
        page.UpdateLayout();
        var launch = Descendants(page).OfType<Wpf.Ui.Controls.Button>()
            .Single(button => button.Content as string == "启动游戏");
        Assert(ReferenceEquals(launch.Command, vm.LaunchGameCommand), "Card command is not bound to the page ViewModel.");
        Assert(ReferenceEquals(launch.CommandParameter, vm.Games[0]), "Card command parameter is not GameItem.");
        var add = Descendants(page).OfType<Wpf.Ui.Controls.Button>()
            .Single(button => button.Content as string == "手动添加游戏");
        Assert(ReferenceEquals(add.Command, vm.AddGameCommand), "Add command is not bound.");
        var delete = Descendants(page).OfType<Wpf.Ui.Controls.Button>()
            .Single(button => button.Name == "DeleteGameButton");
        Assert(ReferenceEquals(delete.Command, vm.DeleteGameCommand)
            && ReferenceEquals(delete.CommandParameter, vm.Games[0]), "Delete command/parameter is not bound.");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
        {
            var child = VisualTreeHelper.GetChild(node, index);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static async Task RemovalChoicesAsync()
    {
        var db = Database("remove-only");
        var service = new GameService(db);
        var first = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "remove first")));
        var second = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "remove second")));
        var dialogs = new TestDialogs();
        var vm = new GameLibraryViewModel(service, dialogs);
        await vm.InitializeAsync();
        var selected = vm.Games.Single(game => game.Id == first.Id);
        await vm.DeleteGameCommand.ExecuteAsync(selected); // Cancel by default.
        Assert(vm.GameCount == 2 && await db.GetGameByIdAsync(first.Id) is not null, "Cancel changed records.");
        dialogs.DeletionChoice = GameRemovalMode.RemoveFromLibrary;
        await vm.DeleteGameCommand.ExecuteAsync(selected);
        Assert(vm.GameCount == 1 && vm.TotalSizeBytes == second.SizeBytes, "Removal did not update totals.");
        Assert(vm.ResultsLabel == "1 款游戏" && await db.GetGameByIdAsync(first.Id) is null, "Removal not persisted.");
        Assert(File.Exists(first.ExePath) && File.Exists(first.CoverPath), "List-only removal touched files.");
        await vm.DeleteGameCommand.ExecuteAsync(vm.Games.Single());
        Assert(vm.GameCount == 0 && vm.TotalSizeFormatted == "0 B" && vm.IsEmpty, "Last removal left stale statistics.");
    }

    private static async Task PermanentDeletionAsync()
    {
        var db = Database("delete-files");
        var service = new GameService(db);
        var game = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "delete whole game")));
        string outside = Path.Combine(_root, "keep-outside.txt");
        File.WriteAllText(outside, "keep");
        var vm = new GameLibraryViewModel(service, new TestDialogs { DeletionChoice = GameRemovalMode.DeleteFiles });
        await vm.InitializeAsync();
        await vm.DeleteGameCommand.ExecuteAsync(vm.Games.Single());
        Assert(vm.GameCount == 0 && vm.TotalSizeBytes == 0 && !vm.HasError, "Permanent deletion UI state.");
        Assert(!Directory.Exists(game.InstallDirectory) && !File.Exists(game.CoverPath), "Game or icon still exists.");
        Assert((await new DatabaseService(db.DatabasePath).GetAllGamesAsync()).Count == 0, "Permanent deletion not persisted.");
        Assert(File.Exists(outside), "Deletion escaped its installation folder.");

        var missing = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "already missing game")));
        DeleteFixtureDirectory(missing.InstallDirectory);
        var result = await service.DeleteGameAsync(missing, GameRemovalMode.DeleteFiles);
        Assert(result.Removed && !result.FilesDeleted && !File.Exists(missing.CoverPath), "Missing folder cleanup failed.");
    }

    private static async Task DeletionFailuresAsync()
    {
        var db = Database("delete-errors");
        var service = new GameService(db);
        var game = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "locked game")));
        var dialogs = new TestDialogs { DeletionChoice = GameRemovalMode.DeleteFiles };
        var vm = new GameLibraryViewModel(service, dialogs);
        await vm.InitializeAsync();
        using (var locked = new FileStream(game.ExePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            await vm.DeleteGameCommand.ExecuteAsync(vm.Games.Single());
            Assert(vm.HasError && !vm.IsBusy && vm.GameCount == 1, "Locked file failure escaped UI or removed card.");
            Assert(await db.GetGameByIdAsync(game.Id) is not null && File.Exists(game.CoverPath), "Failure removed record/icon.");
        }

        string sharedFolder = Path.Combine(_root, "shared game folder");
        var shared = await service.AddGameAsync(CreateExecutable(sharedFolder));
        string secondExe = Path.Combine(sharedFolder, "second.exe");
        File.Copy(shared.ExePath, secondExe);
        await service.AddGameAsync(secondExe);
        await Expect<GameOperationException>(() => service.DeleteGameAsync(shared, GameRemovalMode.DeleteFiles));
        Assert(File.Exists(shared.ExePath) && File.Exists(secondExe), "Shared folder protection failed.");

        var stale = new GameItem { Id = shared.Id, ExePath = shared.ExePath, InstallDirectory = _root };
        await Expect<GameOperationException>(() => service.DeleteGameAsync(stale, GameRemovalMode.DeleteFiles));
        Assert(File.Exists(secondExe), "Changed UI directory was trusted.");
    }

    private static Task DeletionGuardsAsync()
    {
        // Call the pure validator only: a regression must never attempt to delete a real system folder.
        string[] guarded = [Path.GetPathRoot(_root)!, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            AppContext.BaseDirectory, Database("guard-data").DataDirectory];
        foreach (string directory in guarded.Where(path => !string.IsNullOrEmpty(path)))
        {
            var game = Record(Path.Combine(directory, "sample.exe"));
            game.InstallDirectory = directory;
            ExpectSync<GameOperationException>(() => GameDeletionSafety.ValidateInstallation(
                game, Database("guard-data").DataDirectory, []));
        }
        ExpectSync<GameOperationException>(() => GameDeletionSafety.Normalize("relative/game"));
        ExpectSync<GameOperationException>(() => GameDeletionSafety.Normalize(@"\\?\C:\Games"));
        return Task.CompletedTask;
    }

    private static async Task CoverCleanupFailuresAsync()
    {
        var db = Database("cache-errors");
        var service = new GameService(db);
        var game = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "locked cache game")));
        using (var locked = new FileStream(game.CoverPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var result = await service.DeleteGameAsync(game, GameRemovalMode.DeleteFiles);
            Assert(result.Removed && result.Warning is not null, "Cache lock should warn after record removal.");
            Assert(!Directory.Exists(game.InstallDirectory) && await db.GetGameByIdAsync(game.Id) is null,
                "Cache warning left an inconsistent library record.");
        }
        var redirected = await service.AddGameAsync(CreateExecutable(Path.Combine(_root, "redirected cache game")));
        string outside = Path.Combine(_root, "unrelated-cover.png");
        File.WriteAllText(outside, "must not delete");
        redirected.CoverPath = outside;
        await db.UpdateGameAsync(redirected);
        var redirectedResult = await service.DeleteGameAsync(redirected, GameRemovalMode.DeleteFiles);
        Assert(redirectedResult.Removed && redirectedResult.Warning is not null && File.Exists(outside),
            "Untrusted cache path was deleted.");
    }


    private static async Task ScrollWheelAsync()
    {
        var vm = new GameLibraryViewModel(new GameService(Database("scroll")), new TestDialogs());
        await vm.InitializeAsync();
        for (int index = 0; index < 24; index++)
        {
            var item = Record();
            item.Id = index + 1;
            vm.Games.Add(item);
        }
        var page = new GameLibraryPage(vm);
        var host = new Wpf.Ui.Controls.NavigationViewContentPresenter
        {
            TransitionDuration = 0,
            Style = (Style)Application.Current.FindResource("DefaultNavigationViewContentPresenterStyle")
        };
        host.ApplyTemplate();
        var navigated = new TaskCompletionSource();
        host.Navigated += (_, _) => navigated.TrySetResult();
        Assert(host.Navigate(page), "Navigation host rejected the game library page.");
        await Task.WhenAny(navigated.Task, Task.Delay(5000));
        Assert(navigated.Task.IsCompleted, "Navigation host did not finish loading.");
        host.Measure(new System.Windows.Size(1040, 650));
        host.Arrange(new Rect(0, 0, 1040, 650));
        host.UpdateLayout();
        Assert(!host.IsDynamicScrollViewerEnabled,
            "Wpf.Ui must not wrap the page in another dynamic scroll viewer.");
        Assert(!System.Windows.Controls.ScrollViewer.GetCanContentScroll(page),
            "The Page-level CanContentScroll flag must disable the navigation wrapper.");
        Assert(!Descendants(host).OfType<Wpf.Ui.Controls.DynamicScrollViewer>().Any(),
            "An extra navigation scroll viewer was created.");
        var viewer = (System.Windows.Controls.ScrollViewer)page.FindName("GameListScrollViewer");
        var content = (System.Windows.Controls.StackPanel)page.FindName("LibraryContent");
        var items = (System.Windows.Controls.ItemsControl)page.FindName("GamesItemsControl");
        Assert(!viewer.CanContentScroll && viewer.ScrollableHeight > 0, $"Pixel scroll viewport not constrained. Host={host.ActualHeight}; Page={page.ActualHeight}; Viewer={viewer.ActualHeight}; Extent={viewer.ExtentHeight}; Viewport={viewer.ViewportHeight}; HostVisuals={Descendants(host).Count()}; PageParent={VisualTreeHelper.GetParent(page)?.GetType().Name}");
        Assert(viewer.Background is SolidColorBrush { Color.A: 0 }
            && content.Background is SolidColorBrush { Color.A: 0 }
            && items.Background is SolidColorBrush { Color.A: 0 },
            "Scroll hit-test surfaces must have explicit transparent backgrounds.");

        Assert(!Descendants(items).OfType<System.Windows.Controls.ScrollViewer>().Any(),
            "The card ItemsControl template must not contain an inner scroll viewer.");
        // Resolve actual geometric hit-test targets inside the Wpf.Ui navigation host.
        var viewportBlank = HitWithinViewer(page, viewer, new Point(10, 100));
        Rect itemsBounds = items.TransformToAncestor(page).TransformBounds(new Rect(items.RenderSize));
        var cardGap = HitWithinViewer(page, viewer, new Point(itemsBounds.Left + 2, itemsBounds.Top + 20));
        var card = HitWithinViewer(page, viewer, new Point(itemsBounds.Left + 100, itemsBounds.Top + 70));

        // Even a descendant that consumes bubbling wheel input must not block the outer preview handler.
        var launch = Descendants(page).OfType<Wpf.Ui.Controls.Button>()
            .First(button => button.Content as string == "启动游戏");
        launch.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler((_, e) => e.Handled = true));

        await WheelAndLayout(page, page, -24);
        Assert(viewer.VerticalOffset == 24, "Wheel raised at the Page root did not reach the game viewer.");
        await WheelAndLayout(page, page, 24);
        Assert(viewer.VerticalOffset == 0, "Page-root wheel did not return to the top.");
        await WheelAndLayout(page, card, -120);
        Assert(viewer.VerticalOffset == 120, "Wheel over a hit-tested card must move exactly Delta pixels.");
        await WheelAndLayout(page, viewportBlank, -60);
        Assert(viewer.VerticalOffset == 180, "Wheel over viewport whitespace did not scroll.");
        await WheelAndLayout(page, cardGap, -30);
        Assert(viewer.VerticalOffset == 210, "Wheel over the card-grid gap did not scroll.");

        await WheelAndLayout(page, launch, 210);
        Assert(viewer.VerticalOffset == 0, "Positive Delta did not return to the top.");
        await WheelAndLayout(page, viewportBlank, 120);
        Assert(viewer.VerticalOffset == 0, "Top boundary should stay clamped while consuming the event.");
        viewer.ScrollToBottom();
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        page.UpdateLayout();
        double bottom = viewer.VerticalOffset;
        await WheelAndLayout(page, viewer, -120);
        Assert(viewer.VerticalOffset == bottom, "Bottom boundary did not stay clamped.");
    }

    private static UIElement HitWithinViewer(GameLibraryPage page,
        System.Windows.Controls.ScrollViewer viewer, Point point)
    {
        var hit = VisualTreeHelper.HitTest(page, point)?.VisualHit;
        Assert(hit is not null, $"No visual hit at {point}.");
        UIElement? target = null;
        bool inViewer = false;
        for (DependencyObject? node = hit; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            target ??= node as UIElement;
            if (ReferenceEquals(node, viewer)) { inViewer = true; break; }
        }
        Assert(inViewer && target is not null, $"Hit at {point} escaped the scroll viewer.");
        return target!;
    }

    private static async Task WheelAndLayout(GameLibraryPage page, UIElement target, int delta)
    {
        var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, delta)
            { RoutedEvent = UIElement.PreviewMouseWheelEvent };
        target.RaiseEvent(wheel);
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
        page.UpdateLayout();
        Assert(wheel.Handled, "Page-root preview handler did not consume wheel input.");
    }

    private static void DeleteFixtureDirectory(string path)
    {
        string fullPath = Path.GetFullPath(path);
        Assert(fullPath.StartsWith(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Refusing recursive deletion outside the temporary fixture root.");
        Directory.Delete(fullPath, true);
    }

    private static void ExpectSync<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }

    private static async Task Expect<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class TestDialogs : IUserDialogs
    {
        public string? Path { get; set; }
        public GameRemovalMode DeletionChoice { get; set; }
        public Task<GameRemovalMode> ConfirmGameDeletionAsync(GameItem game) => Task.FromResult(DeletionChoice);
        public List<string> Messages { get; } = [];
        public string? PickExecutable() => Path;
        public Task ShowMessageAsync(string title, string message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
