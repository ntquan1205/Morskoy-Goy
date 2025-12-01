using Morskoy_Goy.Network.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Morskoy_Goy.Network.Host
{
    public class GameHost
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private string _playerName;
        private Thread _listenThread;
        private bool _isRunning;

        public event Action<string> ClientConnected;
        public event Action ClientDisconnected;

        public void Start(int port, string playerName)
        {
            _playerName = playerName;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            _client = _listener.AcceptTcpClient();
            _stream = _client.GetStream();

            ClientConnected?.Invoke("Соперник");

            _listenThread = new Thread(ListenForMessages);
            _listenThread.Start();
        }

        private void ListenForMessages()
        {
            byte[] buffer = new byte[4096];

            while (_isRunning && _client.Connected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                }
                catch
                {
                    break;
                }
            }

            ClientDisconnected?.Invoke();
        }

        public void SendMessage(NetworkMessage message)
        {
            if (!_isRunning || !_client.Connected) return;

            string json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            _stream.Write(data, 0, data.Length);
        }

        public void Stop()
        {
            _isRunning = false;
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();
        }
    }
}
