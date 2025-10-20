using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MMOServer.Configuration
{
    public class ConfigLoader
    {
        private static ConfigLoader? instance;
        public static ConfigLoader Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConfigLoader();
                return instance;
            }
        }

        public AppSettings Settings { get; private set; } = new AppSettings();

        public void LoadConfiguration()
        {
            try
            {
                string appSettingsPath = "appsettings.json";

                if (!File.Exists(appSettingsPath))
                {
                    Console.WriteLine($"⚠️ {appSettingsPath} not found! Using default settings.");
                    CreateDefaultAppSettings();
                    return;
                }

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                Settings = new AppSettings();
                configuration.Bind(Settings);

                Console.WriteLine("✅ Configuration loaded from appsettings.json:");
                Console.WriteLine($"   Server: {Settings.ServerSettings.Host}:{Settings.ServerSettings.Port}");
                Console.WriteLine($"   Database: {Settings.DatabaseSettings.Server}:{Settings.DatabaseSettings.Port}/{Settings.DatabaseSettings.Database}");
                Console.WriteLine($"   Max Connections: {Settings.ServerSettings.MaxConnections}");
                Console.WriteLine($"   Update Rate: {Settings.ServerSettings.UpdateRate}ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading appsettings.json: {ex.Message}");
                Console.WriteLine("   Using default settings.");
                Settings = new AppSettings();
            }
        }

        private void CreateDefaultAppSettings()
        {
            Console.WriteLine("📝 Creating default appsettings.json...");
            
            string defaultConfig = @"{
										""ServerSettings"": {
										""Host"": ""0.0.0.0"",
										""Port"": 8080,
										""MaxConnections"": 1000,
										""UpdateRate"": 100
										},
										""DatabaseSettings"": {
										""Server"": ""localhost"",
										""Database"": ""mmo_game"",
										""UserId"": ""root"",
										""Password"": """",
										""Port"": 3306
									}
									}";
            
            try
            {
                File.WriteAllText("appsettings.json", defaultConfig);
                Console.WriteLine("✅ Created default appsettings.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Could not create appsettings.json: {ex.Message}");
            }
        }
    }
}