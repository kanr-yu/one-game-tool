using System.Windows;
using System.Windows.Input;
using GameDeck.Views.Pages;
using Wpf.Ui.Controls;

namespace GameDeck;

public partial class MainWindow : FluentWindow
{
    public MainWindow() => InitializeComponent();

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => RootNavigation.Navigate(typeof(GameLibraryPage));

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => SystemCommands.MinimizeWindow(this);

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        // Also updates when the window is maximized with keyboard shortcuts or snapping.
        if (MaximizeButton is null) return;
        bool maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
        MaximizeButton.ToolTip = maximized ? "向下还原" : "最大化";
        System.Windows.Automation.AutomationProperties.SetName(MaximizeButton, MaximizeButton.ToolTip.ToString());
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if (e.ClickCount == 2)
            Maximize_Click(sender, e);
        else
            DragMove();
    }
}
