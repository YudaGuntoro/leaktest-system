namespace LeakTestWorker.Singletone;

public static class dbConfig
{
    private const string DefaultConnectionString =
        "Server=127.0.0.1;Port=3306;User ID=root;Password=YOUR_PASSWORD;Database=yanmarleaktest;SslMode=None;AllowPublicKeyRetrieval=True;";

    public static string MysqlConnString =>
        Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("Database__ConnectionString")
        ?? Config.Instance.Read("ConnectionString", "Database")
        ?? DefaultConnectionString;
}
