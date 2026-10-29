using GameDeck.Models;

namespace GameDeck.Services;

public interface IUserDialogs
{
    string? PickExecutable();
    Task ShowMessageAsync(string title, string message);
    Task<GameRemovalMode> ConfirmGameDeletionAsync(GameItem game);
}
