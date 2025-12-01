using Morskoy_Goy.Network.Common;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Morskoy_Goy.Network.Client
{
    public class GameClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private bool _isConnected;

        public event Action<NetworkMessage> MessageReceived;
        public event Action<string> ConnectionStatusChanged;

        public async Task Connect(string ip, int port, string playerName)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);
                _stream = _tcpClient.GetStream();
                _isConnected = true;

                ConnectionStatusChanged?.Invoke("Подключено");

                await SendMessage(new NetworkMessage
                {
                    Type = MessageType.Connect,
                    Data = new { playerName = playerName }
                });

                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke($"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task StartListeningAsync()
        {
            var buffer = new byte[4096];

            while (_isConnected && _tcpClient.Connected)
            {
                try
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                    MessageReceived?.Invoke(message);
                }
                catch (Exception)
                {
                    break;
                }
            }

            ConnectionStatusChanged?.Invoke("Отключено");
            _isConnected = false;
        }

        public async Task SendMessage(NetworkMessage message)
        {
            if (!_isConnected) return;

            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _tcpClient?.Close();
        }
    }
}