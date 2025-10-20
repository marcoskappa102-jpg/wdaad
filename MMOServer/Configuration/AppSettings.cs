using System;

namespace MMOServer.Configuration
{
    public class AppSettings
    {
        public ServerSettings ServerSettings { get; set; } = new ServerSettings();
        public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();
    }

    public class ServerSettings
    {
        public string Host { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 8080;
        public int MaxConnections { get; set; } = 1000;
        public int UpdateRate { get; set; } = 100;
    }

    public class DatabaseSettings
    {
        public string Server { get; set; } = "localhost";
        public string Database { get; set; } = "mmo_game";
        public string UserId { get; set; } = "root";
        public string Password { get; set; } = "";
        public int Port { get; set; } = 3306;

        public string GetConnectionString()
        {
            return $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};Port={Port};";
        }
    }
}