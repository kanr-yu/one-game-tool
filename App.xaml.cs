using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace GameDeck;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        ApplicationAccentColorManager.Apply(Color.FromRgb(111, 231, 191));
    }
}
