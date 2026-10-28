using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GameDeck.Models;

namespace GameDeck.Views.Pages;

public partial class GameLibraryPage : Page
{
    private readonly CollectionViewSource _gamesSource = new() { Source = PreviewData.Games };

    public GameLibraryPage()
    {
        InitializeComponent();
        _gamesSource.Filter += (_, e) =>
        {
            var game = (GamePreview)e.Item;
            string query = SearchBox.Text.Trim();
            e.Accepted = game.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || game.EnglishName.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || game.Platform.Contains(query, StringComparison.OrdinalIgnoreCase);
        };
        GamesList.ItemsSource = _gamesSource.View;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // TextChanged may run while InitializeComponent is still constructing the page.
        if (GamesList is null || EmptyState is null) return;
        _gamesSource.View.Refresh();
        int count = _gamesSource.View.Cast<GamePreview>().Count();
        ResultsLabel.Text = $"{count} 款游戏";
        EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
