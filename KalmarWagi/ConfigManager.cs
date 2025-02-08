using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyApp
{
    public static class ConfigManager
    {
        private static Dictionary<string, string> configValues = new Dictionary<string, string>();
        private static string configFilePath = "config.ini";

        // 24-znakowy statyczny klucz szyfrowania
        private static readonly string EncryptionKey = "5Jd!P1x2L3M@v9X6D8Zq3wR2@UuI3xF"; // Klucz szyfrowania

        static ConfigManager()
        {
            // Wczytanie konfiguracji tylko raz, podczas inicjalizacji
            LoadConfig();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfigFile();
                Console.WriteLine("Utworzono domyślny plik konfiguracyjny: config.ini");
            }
            else
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFilePath);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("="))
                        {
                            string[] parts = line.Split('=', 2);
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            // Zaszyfrowanie wartości, jeśli jest to wymagane
                            string decryptedValue = Decrypt(value);
                            configValues[key] = decryptedValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas wczytywania konfiguracji: {ex.Message}");
                }
            }
        }

        public static string GetConfigValue(string key, string defaultValue = "")
        {
            return configValues.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public static void UpdateConfigValue(string key, string newValue)
        {
            if (configValues.ContainsKey(key))
            {
                configValues[key] = newValue;
                Console.WriteLine($"Zaktualizowano wartość {key} na {newValue}.");

                // Zapisz zmieniony plik konfiguracyjny
                SaveConfig();
            }
            else
            {
                Console.WriteLine($"Klucz {key} nie istnieje w konfiguracji.");
            }
        }

        public static void SaveConfig()
        {
            try
            {
                StringBuilder configContent = new StringBuilder();

                foreach (var kvp in configValues)
                {
                    configContent.AppendLine($"{kvp.Key}={Encrypt(kvp.Value)}");
                }

                // Zapisanie nowej zawartości do pliku
                File.WriteAllText(configFilePath, configContent.ToString());
                Console.WriteLine("Konfiguracja została zapisana.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zapisywania konfiguracji: {ex.Message}");
            }
        }

        private static void CreateDefaultConfigFile()
        {
            string defaultConfig = @"[MQTT]
BrokerAddress=88.156.225.50
BrokerPort=1883
Topic=KANBAN/SCSF/11/05
UserName=SCSF1105
Password=scsf1105

[Database]
ConnectionString=Server=88.156.225.50;Database=Wagi;User=wagi;Password=WagiKalmar1;";

            // Zaszyfrowanie wartości przed zapisaniem do pliku
            defaultConfig = EncryptConfigValues(defaultConfig);

            File.WriteAllText(configFilePath, defaultConfig);
        }

        private static string EncryptConfigValues(string configContent)
        {
            var lines = configContent.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("="))
                {
                    string[] parts = lines[i].Split('=', 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Zaszyfrowanie wartości
                    lines[i] = key + "=" + Encrypt(value);
                }
            }

            return string.Join("\n", lines);
        }

        private static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32, ' ')); // Użyj 32-bajtowego klucza
                aesAlg.IV = new byte[16]; // IV wypełnione zerami

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32, ' ')); // Użyj 32-bajtowego klucza
                aesAlg.IV = new byte[16]; // IV wypełnione zerami

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        public static string GetConfigSummary()
        {
            StringBuilder summary = new StringBuilder();

            foreach (var key in configValues.Keys)
            {
                summary.AppendLine($"{key}: {configValues[key]}");
            }

            return summary.ToString();
        }
    }
}
