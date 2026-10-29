namespace GameDeck.Services;

public enum GameRemovalMode
{
    Cancel,
    RemoveFromLibrary,
    DeleteFiles
}

public sealed record GameRemovalResult(bool Removed, bool FilesDeleted = false, string? Warning = null);
