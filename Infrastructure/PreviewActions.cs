using System.Windows;
using System.Windows.Input;

namespace GameDeck.Infrastructure;

/// <summary>Shared placeholder interaction. No filesystem or process operations.</summary>
public static class PreviewActions
{
    public static ICommand ShowFeature { get; } = new FeatureCommand();

    private sealed class FeatureCommand : ICommand
    {
        private bool _isShowing;
        public bool CanExecute(object? parameter) => !_isShowing;
        public event EventHandler? CanExecuteChanged;

        public async void Execute(object? parameter)
        {
            if (_isShowing) return;
            _isShowing = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "功能开发中",
                    Content = $"{parameter ?? "此功能"}\n\n当前为界面预览，功能将在后续版本中开放。",
                    CloseButtonText = "知道了",
                    Owner = Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    MaxWidth = 460,
                    Topmost = false
                };
                await dialog.ShowDialogAsync();
            }
            finally
            {
                _isShowing = false;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
