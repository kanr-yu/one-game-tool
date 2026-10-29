using System.Diagnostics;
using System.Windows;
using GameDeck.Models;
using Microsoft.Win32;


namespace GameDeck.Services;

public sealed class UserDialogs : IUserDialogs
{
    public string? PickExecutable()
    {
        try
        {
            var picker = new OpenFileDialog
            {
                Title = "选择游戏可执行文件",
                Filter = "游戏可执行文件 (*.exe)|*.exe",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };
            var owner = Application.Current?.MainWindow;
            bool? result = owner is null ? picker.ShowDialog() : picker.ShowDialog(owner);
            return result == true ? picker.FileName : null;
        }
        catch (Exception ex) { throw new GameOperationException($"无法打开文件选择窗口：{ex.Message}", ex); }
    }

    public async Task<GameRemovalMode> ConfirmGameDeletionAsync(GameItem game)
    {
        try
        {
            var choice = CreateDialog("移除或删除游戏",
                $"游戏：{game.Name}\n安装目录：{game.InstallDirectory}\n\n"
                + "仅从列表移除：删除游戏库记录，保留电脑上的游戏文件。\n"
                + "彻底删除游戏本体：递归删除整个安装目录及其中全部文件，并清理图标缓存。");
            choice.PrimaryButtonText = "仅从列表移除";
            choice.SecondaryButtonText = "彻底删除游戏本体";
            choice.SecondaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger;
            choice.CloseButtonText = "取消";
            var selected = await choice.ShowDialogAsync();
            if (selected == Wpf.Ui.Controls.MessageBoxResult.Primary) return GameRemovalMode.RemoveFromLibrary;
            if (selected != Wpf.Ui.Controls.MessageBoxResult.Secondary) return GameRemovalMode.Cancel;

            var confirmation = CreateDialog("确认永久删除游戏文件？",
                $"游戏：{game.Name}\n将永久删除以下目录及其中的所有文件：\n\n{game.InstallDirectory}\n\n"
                + "此操作不经过回收站，无法撤销。目录内的存档、模组和其他文件也会被删除。"
                + "若删除中途失败，部分文件可能已经被移除。");
            confirmation.PrimaryButtonText = "确认永久删除";
            confirmation.PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Danger;
            confirmation.CloseButtonText = "取消";
            return await confirmation.ShowDialogAsync() == Wpf.Ui.Controls.MessageBoxResult.Primary
                ? GameRemovalMode.DeleteFiles : GameRemovalMode.Cancel;
        }
        catch (Exception ex)
        {
            // A failed confirmation never grants permission to delete anything.
            await ShowMessageAsync("无法确认删除", $"本次未执行删除：{ex.Message}");
            return GameRemovalMode.Cancel;
        }
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        try { await CreateDialog(title, message).ShowDialogAsync(); }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            try { System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception fallbackError) { Debug.WriteLine(fallbackError); }
        }
    }

    private static Wpf.Ui.Controls.MessageBox CreateDialog(string title, string content) => new()
    {
        Title = title, Content = content, CloseButtonText = "知道了", MaxWidth = 680,
        Owner = Application.Current?.MainWindow,
        WindowStartupLocation = Application.Current?.MainWindow is null
            ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
        Topmost = false
    };
}
