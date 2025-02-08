using System;

namespace MyApp
{
    class Program
    {
        static DatabaseManager dbManager;
        static MqttHandler mqttHandler;
        static CommandHandler commandHandler;

        static void Main(string[] args)
        {
            // Wczytanie konfiguracji
            ConfigManager.LoadConfig();

            // Pobranie danych konfiguracyjnych bez ustawiania domyślnych wartości
            string connectionString = ConfigManager.GetConfigValue("ConnectionString");
            string brokerAddress = ConfigManager.GetConfigValue("BrokerAddress");
            int brokerPort = int.Parse(ConfigManager.GetConfigValue("BrokerPort"));
            string topic = ConfigManager.GetConfigValue("Topic");
            string userName = ConfigManager.GetConfigValue("UserName");
            string password = ConfigManager.GetConfigValue("Password");

            // Inicjalizacja bazy danych
            dbManager = new DatabaseManager(connectionString);
            if (!dbManager.TestDatabaseConnection())
            {
                Console.WriteLine("Połączenie z bazą danych nieudane, zakończono program.");
                return;
            }

            // Inicjalizacja MQTT
            mqttHandler = new MqttHandler(brokerAddress, brokerPort, topic, userName, password, dbManager);
            mqttHandler.Connect();

            // Inicjalizacja obsługi komend
            commandHandler = new CommandHandler(dbManager, mqttHandler);

            Console.WriteLine("Wpisz 'help' aby zobaczyć dostępne komendy.");
            Console.WriteLine("Wpisz 'exit' aby zakończyć program.");

            // Obsługa komend użytkownika
            while (true)
            {
                string command = Console.ReadLine();
                commandHandler.ExecuteCommand(command);
            }
        }
    }
}
