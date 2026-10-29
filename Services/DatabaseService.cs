using System.IO;
using System.Globalization;
using Microsoft.Data.Sqlite;
using GameDeck.Models;

namespace GameDeck.Services;

public sealed class DatabaseService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string DatabasePath { get; }
    public string DataDirectory => Path.GetDirectoryName(DatabasePath)!;

    public DatabaseService(string? databasePath = null)
    {
        DatabasePath = Path.GetFullPath(databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameSentinel", "data.db"));
    }

    public Task InitializeAsync() => ExecuteAsync("初始化数据库", _ => true);

    public Task<List<GameItem>> GetAllGamesAsync() => ExecuteAsync("读取游戏库", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Games ORDER BY Id DESC;";
        using var reader = command.ExecuteReader();
        var games = new List<GameItem>();
        while (reader.Read()) games.Add(ReadGame(reader));
        return games;
    });

    public Task<GameItem?> GetByExePathAsync(string exePath) => ExecuteAsync("查找游戏", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Games WHERE ExePath = $path COLLATE NOCASE LIMIT 1;";
        command.Parameters.AddWithValue("$path", exePath);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadGame(reader) : null;
    });

    public Task<GameItem> AddGameAsync(GameItem game) => ExecuteAsync("保存游戏", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Games (Name, ExePath, InstallDirectory, SizeBytes, SizeFormatted, Platform, CoverPath, LastPlayed)
            VALUES ($name, $exe, $directory, $bytes, $size, $platform, $cover, $played);
            SELECT last_insert_rowid();
            """;
        AddParameters(command, game);
        // Populate Id after awaiting this method, so observable models are never mutated on the worker.
        var saved = ReadCopy(game);
        saved.Id = checked((int)(long)command.ExecuteScalar()!);
        return saved;
    });

    public Task UpdateGameAsync(GameItem game) => ExecuteAsync("更新游戏", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Games SET Name=$name, ExePath=$exe, InstallDirectory=$directory, SizeBytes=$bytes,
                SizeFormatted=$size, Platform=$platform, CoverPath=$cover, LastPlayed=$played
            WHERE Id=$id;
            """;
        AddParameters(command, game);
        command.Parameters.AddWithValue("$id", game.Id);
        if (command.ExecuteNonQuery() != 1) throw new GameOperationException("游戏记录不存在，请重新加载游戏库。");
        return true;
    });

    public Task UpdateLastPlayedAsync(int id, DateTimeOffset time) => ExecuteAsync("保存游玩时间", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Games SET LastPlayed=$played WHERE Id=$id;";
        command.Parameters.AddWithValue("$played", time.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() != 1) throw new GameOperationException("游戏记录不存在，无法保存游玩时间。");
        return true;
    });

    public Task<GameItem?> GetGameByIdAsync(int id) => ExecuteAsync("查找游戏记录", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Games WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadGame(reader) : null;
    });

    // The requested ID-based API remains asynchronous to avoid blocking the WPF thread.
    public Task<bool> DeleteGameById(int id) => DeleteGameAsync(id);
    public Task<bool> DeleteGameAsync(int id) => ExecuteAsync("删除游戏记录", connection =>
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Games WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    });

    private async Task<T> ExecuteAsync<T>(string operation, Func<SqliteConnection, T> action)
    {
        await _gate.WaitAsync();
        try
        {
            // Microsoft.Data.Sqlite's ADO.NET async methods still perform synchronous I/O.
            return await Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(DataDirectory);
                    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
                    {
                        DataSource = DatabasePath,
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        Pooling = false,
                        DefaultTimeout = 5
                    }.ToString());
                    connection.Open();
                    using var schema = connection.CreateCommand();
                    schema.CommandText = """
                        PRAGMA journal_mode=WAL;
                        CREATE TABLE IF NOT EXISTS Games (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            ExePath TEXT NOT NULL COLLATE NOCASE UNIQUE,
                            InstallDirectory TEXT NOT NULL,
                            SizeBytes INTEGER NOT NULL DEFAULT 0,
                            SizeFormatted TEXT NOT NULL DEFAULT '0 B',
                            Platform TEXT NOT NULL DEFAULT 'LOCAL',
                            CoverPath TEXT NOT NULL DEFAULT '',
                            LastPlayed TEXT NOT NULL DEFAULT ''
                        );
                        """;
                    schema.ExecuteNonQuery();
                    return action(connection);
                }
                catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
                {
                    throw new DuplicateGameException(ex);
                }
                catch (GameOperationException) { throw; }
                catch (Exception ex)
                {
                    throw new GameOperationException($"无法{operation}：{ex.Message}", ex);
                }
            });
        }
        finally { _gate.Release(); }
    }

    private static void AddParameters(SqliteCommand command, GameItem game)
    {
        command.Parameters.AddWithValue("$name", game.Name);
        command.Parameters.AddWithValue("$exe", game.ExePath);
        command.Parameters.AddWithValue("$directory", game.InstallDirectory);
        command.Parameters.AddWithValue("$bytes", game.SizeBytes);
        command.Parameters.AddWithValue("$size", game.SizeFormatted);
        command.Parameters.AddWithValue("$platform", game.Platform);
        command.Parameters.AddWithValue("$cover", game.CoverPath);
        command.Parameters.AddWithValue("$played", game.LastPlayed);
    }

    private static GameItem ReadCopy(GameItem game) => new()
    {
        Name = game.Name, ExePath = game.ExePath, InstallDirectory = game.InstallDirectory,
        SizeBytes = game.SizeBytes, SizeFormatted = game.SizeFormatted, Platform = game.Platform,
        CoverPath = game.CoverPath, LastPlayed = game.LastPlayed
    };

    private static GameItem ReadGame(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Name = ReadText(reader, "Name"), ExePath = ReadText(reader, "ExePath"),
        InstallDirectory = ReadText(reader, "InstallDirectory"),
        SizeBytes = reader.IsDBNull(reader.GetOrdinal("SizeBytes")) ? 0 : reader.GetInt64(reader.GetOrdinal("SizeBytes")),
        SizeFormatted = ReadText(reader, "SizeFormatted"), Platform = ReadText(reader, "Platform"),
        CoverPath = ReadText(reader, "CoverPath"), LastPlayed = ReadText(reader, "LastPlayed")
    };

    private static string ReadText(SqliteDataReader reader, string name)
        => reader.IsDBNull(reader.GetOrdinal(name)) ? "" : reader.GetString(reader.GetOrdinal(name));
}
