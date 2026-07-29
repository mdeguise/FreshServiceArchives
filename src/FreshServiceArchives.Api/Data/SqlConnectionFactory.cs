using Microsoft.Data.SqlClient;

namespace FreshServiceArchives.Api.Data;

public interface ISqlConnectionFactory
{
    SqlConnection Create();
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("ArchiveDb")
            ?? throw new InvalidOperationException(
                "Missing connection string 'ArchiveDb'. Set ConnectionStrings:ArchiveDb in appsettings.json.");
    }

    public SqlConnection Create() => new(_connectionString);
}
