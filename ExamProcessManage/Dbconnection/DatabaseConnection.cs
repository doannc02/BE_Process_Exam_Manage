namespace ExamProcessManage.Dbconnection
{
    //public class DatabaseConnection
    //{
    //    private readonly string _connection;

    //    public DatabaseConnection(IConfiguration configuration)
    //    {
    //        _connection = configuration.GetConnectionString("DefaultConnection");
    //    }

    //    public string GetConnectionString()
    //    {
    //        return _connection;
    //    }
    //}

    public class DatabaseConnection
    {
        private static readonly Lazy<string> _connectionString;

        static DatabaseConnection()
        {
            _connectionString = new Lazy<string>(() =>
            {
                var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
                return configuration.GetConnectionString("DefaultConnection");
            });
        }

        public string GetConnectionString()
        {
            return _connectionString.Value;
        }
    }

}
