using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MyApp
{
    public class MqttHandler
    {
        private MqttClient _client;
        private string _brokerAddress;
        private int _brokerPort;
        private string _topic;
        private string _userName;
        private string _password;
        private Dictionary<string, double> _lastReceivedWeights = new Dictionary<string, double>();
        private HashSet<string> _missingCalibrationWarnings = new HashSet<string>();
        private DatabaseManager _dbManager;

        public MqttHandler(string brokerAddress, int brokerPort, string topic, string userName, string password, DatabaseManager dbManager)
        {
            _brokerAddress = brokerAddress;
            _brokerPort = brokerPort;
            _topic = topic;
            _userName = userName;
            _password = password;
            _dbManager = dbManager;
        }

        public void Connect()
        {
            try
            {
                _client = new MqttClient(_brokerAddress, _brokerPort, false, null, null, MqttSslProtocols.None);
                _client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived; // Powiązanie z metodą obsługi wiadomości
                _client.Connect("CSharpClient", _userName, _password);
                _client.Subscribe(new string[] { _topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

                Console.WriteLine($"Połączono z MQTT. Nasłuchiwanie tematu: {_topic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd połączenia z MQTT: {ex.Message}");
            }
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);

            try
            {
                JObject currentData = JObject.Parse(message);

                foreach (var weight in currentData)
                {
                    string sensorId = weight.Key;
                    double rawWeight = weight.Value.ToObject<double>();

                    var calibrationData = _dbManager.GetCalibrationData(sensorId);

                    if (calibrationData == null)
                    {
                        if (!_missingCalibrationWarnings.Contains(sensorId))
                        {
                            _missingCalibrationWarnings.Add(sensorId);
                            Console.WriteLine($"Brak danych kalibracyjnych dla {sensorId}.");
                        }
                        continue;
                    }

                    double calibratedWeight = (rawWeight - calibrationData.Value.zeroReading) * calibrationData.Value.coefficient;

                    if (!_lastReceivedWeights.TryGetValue(sensorId, out double lastWeight) || Math.Abs(lastWeight - rawWeight) >= 1.0)
                    {
                        Console.WriteLine($"{sensorId} | Surowa: {rawWeight:F2} | Skalibrowana: {calibratedWeight:F3} | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        _lastReceivedWeights[sensorId] = rawWeight;
                    }
                }
            }
            catch (Exception)
            {
                // Brak wydruku błędu, by nie spamować konsoli
            }
        }

        public void Disconnect()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.Disconnect();
                Console.WriteLine("Rozłączono z MQTT.");
            }
        }
    }
}
