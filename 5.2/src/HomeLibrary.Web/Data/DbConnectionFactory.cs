namespace HomeLibrary.Web.Data;

using System.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// Фабрика соединений с базой данных. Dapper работает поверх открытого SqlConnection.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
