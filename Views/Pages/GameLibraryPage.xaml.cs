using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameDeck.ViewModels;

namespace GameDeck.Views.Pages;

public partial class GameLibraryPage : Page
{
    public GameLibraryViewModel ViewModel { get; }

    public GameLibraryPage() : this(new GameLibraryViewModel()) { }

    public GameLibraryPage(GameLibraryViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
        => await ViewModel.InitializeAsync();

    private void Root_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (GameListScrollViewer is not null)
        {
            double newOffset = GameListScrollViewer.VerticalOffset - e.Delta;
            GameListScrollViewer.ScrollToVerticalOffset(newOffset);
            e.Handled = true;
        }
    }
}
