using System;
using System.Collections.Generic;

namespace MyApp
{
    public class CommandHandler
    {
        private DatabaseManager _dbManager;
        private MqttHandler _mqttHandler;
        private bool isListening = true;

        private Dictionary<string, double> lastReceivedWeights = new Dictionary<string, double>();
        private HashSet<string> missingCalibrationWarnings = new HashSet<string>();

        public CommandHandler(DatabaseManager dbManager, MqttHandler mqttHandler)
        {
            _dbManager = dbManager;
            _mqttHandler = mqttHandler;
        }

        public void ExecuteCommand(string command)
        {
            // Usunięcie nadmiarowych spacji z początku i końca komendy
            command = command.Trim();

            // Obsługuje komendę ignorując wielkość liter
            switch (command.ToLower())
            {
                case "stop":
                    StopListening();
                    break;

                case "start":
                    StartListening();
                    break;

                case "restart":
                    RestartProgram();
                    break;

                case "loaddata":
                    ReloadCalibrationData();
                    break;

                case "testdb":
                    Console.WriteLine(_dbManager.TestDatabaseConnection()
                        ? "Połączenie z bazą danych działa."
                        : "Błąd połączenia z bazą danych.");
                    break;

                case "calibration":
                    Console.WriteLine("Podaj ID czujnika:");
                    string sensorId = Console.ReadLine();
                    var calibrationData = _dbManager.GetCalibrationData(sensorId);
                    if (calibrationData.HasValue)
                    {
                        Console.WriteLine($"Kalibracja {sensorId}: Coefficient={calibrationData.Value.coefficient}, ZeroReading={calibrationData.Value.zeroReading}");
                    }
                    else
                    {
                        Console.WriteLine($"Brak danych kalibracyjnych dla {sensorId}.");
                    }
                    break;

                case "config":
                    Console.WriteLine("Obecne ustawienia konfiguracji:");
                    Console.WriteLine(ConfigManager.GetConfigSummary());
                    break;

                case "updateconfig":
                    UpdateConfigInteractively();
                    break;

                case "help":
                    DisplayHelp();
                    break;

                case "exit":
                    Console.WriteLine("Zamykanie programu...");
                    _mqttHandler.Disconnect();
                    _dbManager.CloseConnection();
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Nieznana komenda. Wpisz 'help', aby zobaczyć dostępne komendy.");
                    break;
            }
        }

        private void StopListening()
        {
            isListening = false;
            Console.WriteLine("Zatrzymano odbiór danych.");
        }

        private void StartListening()
        {
            isListening = true;
            Console.WriteLine("Wznowiono odbiór danych.");
        }

        private void RestartProgram()
        {
            try
            {       
                Console.WriteLine("Resetowanie programu...");
        
                // Uruchomienie aplikacji na nowo
                System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.FriendlyName);
        
                // Zamknięcie obecnej instancji
                Environment.Exit(0); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas restartu programu: {ex.Message}");
            }
        }

        private void ReloadCalibrationData()
        {
            Console.WriteLine("Ponowne ładowanie danych kalibracyjnych...");
            // Tu można zaimplementować logikę ponownego ładowania danych z bazy
        }

        private void UpdateConfigInteractively()
        {
            Console.WriteLine("Wybierz, który parametr chcesz zmienić:");

            // Przykładowe opcje konfiguracji, które użytkownik może zmienić
            Console.WriteLine("1. BrokerAddress (adres brokera MQTT)");
            Console.WriteLine("2. BrokerPort (port brokera MQTT)");
            Console.WriteLine("3. UserName (nazwa użytkownika)");
            Console.WriteLine("4. Password (hasło)");
            Console.WriteLine("5. Topic (temat)");
            Console.WriteLine("6. Database (ConnectionString)");
            Console.WriteLine("Wpisz numer opcji, aby edytować, lub 'cancel', aby anulować:");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("Podaj nowy adres brokera:");
                    string brokerAddress = Console.ReadLine();
                    ConfigManager.UpdateConfigValue("BrokerAddress", brokerAddress);
                    break;

                case "2":
                    Console.WriteLine("Podaj nowy port brokera:");
                    string brokerPort = Console.ReadLine();
                    ConfigManager.UpdateConfigValue("BrokerPort", brokerPort);
                    break;

                case "3":
                    Console.WriteLine("Podaj nową nazwę użytkownika:");
                    string userName = Console.ReadLine();
                    ConfigManager.UpdateConfigValue("UserName", userName);
                    break;

                case "4":
                    Console.WriteLine("Podaj nowe hasło:");
                    string password = Console.ReadLine();
                    ConfigManager.UpdateConfigValue("Password", password);
                    break;

                case "5":
                    Console.WriteLine("Podaj nowy temat:");
                    string topic = Console.ReadLine();
                    ConfigManager.UpdateConfigValue("Topic", topic);
                    break;

                case "6":
                    UpdateDatabaseConnectionString();
                    break;

                case "cancel":
                    Console.WriteLine("Anulowano edycję.");
                    break;

                default:
                    Console.WriteLine("Nieznana opcja.");
                    break;
            }
        }

        private void UpdateDatabaseConnectionString()
        {
            Console.WriteLine("Podaj nowy ConnectionString do bazy danych (np. Server=127.0.0.1;Database=MyDb;User=myUser;Password=myPassword):");
            string newConnectionString = Console.ReadLine();

            // Zakładając, że w sekcji [Database] mamy klucz "ConnectionString"
            ConfigManager.UpdateConfigValue("ConnectionString", newConnectionString);
            // Automatyczny zapis po zmianie
            ConfigManager.SaveConfig();  
        }

        private void DisplayHelp()
        {
            Console.WriteLine("Dostępne komendy:");
            Console.WriteLine("'stop' - Zatrzymanie odbioru danych.");
            Console.WriteLine("'start' - Wznowienie odbioru danych.");
            Console.WriteLine("'restart' - Resetowanie programu.");
            Console.WriteLine("'loaddata' - Ponowne załadowanie danych kalibracyjnych.");
            Console.WriteLine("'testdb' - Test połączenia z bazą danych.");
            Console.WriteLine("'calibration' - Sprawdzenie danych kalibracyjnych.");
            Console.WriteLine("'config' - Wyświetlenie konfiguracji.");
            Console.WriteLine("'updateconfig' - Zmiana wartości konfiguracji.");
            Console.WriteLine("'exit' - Zamyka program.");
        }
    }
}
