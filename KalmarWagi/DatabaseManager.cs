using System;
using MySqlConnector;
using System.Collections.Generic;

namespace MyApp
{
    public class DatabaseManager
    {
        private string _connectionString;
        private MySqlConnection _connection;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
            InitializeDatabaseConnection();
        }

        private bool InitializeDatabaseConnection()
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();
                Console.WriteLine("Połączenie z bazą danych udane.");
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Błąd połączenia z bazą danych: {ex.Message}");
                return false;
            }
        }

        public bool TestDatabaseConnection()
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand("SELECT 1;", _connection))
                {
                    command.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd testowania połączenia z bazą danych: {ex.Message}");
                return false;
            }
        }

        public (double coefficient, double zeroReading)? GetCalibrationData(string sensorId)
        {
            try
            {
                string query = "SELECT coefficient, zero_reading FROM czujniki_kalibracja WHERE sensor_id = @sensorId LIMIT 1;";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@sensorId", sensorId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double coefficient = reader.IsDBNull(reader.GetOrdinal("coefficient")) ? 0.0 : reader.GetDouble("coefficient");
                            double zeroReading = reader.IsDBNull(reader.GetOrdinal("zero_reading")) ? 0.0 : reader.GetDouble("zero_reading");

                            return (coefficient, zeroReading);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytu danych kalibracyjnych: {ex.Message}");
            }

            return null;
        }

        public void CloseConnection()
        {
            if (_connection != null)
            {
                _connection.Close();
                Console.WriteLine("Połączenie z bazą danych zostało zamknięte.");
            }
        }
    }
}
