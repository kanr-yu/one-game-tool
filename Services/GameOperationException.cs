namespace GameDeck.Services;

public class GameOperationException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class DuplicateGameException(Exception? innerException = null)
    : GameOperationException("这个游戏已经收录，请勿重复添加。", innerException);
