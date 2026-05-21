using CallBell.Config;
using Microsoft.Data.Sqlite;

namespace CallBell.Data.Db;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(CallBellSettings settings)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = settings.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            DefaultTimeout = 10
        };

        _connectionString = builder.ConnectionString;
    }

    public SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var foreignKeys = connection.CreateCommand();
        foreignKeys.CommandText = "PRAGMA foreign_keys = ON;";
        foreignKeys.ExecuteNonQuery();

        using var busyTimeout = connection.CreateCommand();
        busyTimeout.CommandText = "PRAGMA busy_timeout = 5000;";
        busyTimeout.ExecuteNonQuery();

        using var journalMode = connection.CreateCommand();
        journalMode.CommandText = "PRAGMA journal_mode = DELETE;";
        journalMode.ExecuteNonQuery();

        return connection;
    }
}
