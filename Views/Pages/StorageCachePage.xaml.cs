using System.Windows.Controls;
using GameDeck.Models;

namespace GameDeck.Views.Pages;

public partial class StorageCachePage : Page
{
    public StorageCachePage()
    {
        InitializeComponent();
        CachesList.ItemsSource = PreviewData.Caches;
        BackupsList.ItemsSource = PreviewData.Backups;
    }
}
