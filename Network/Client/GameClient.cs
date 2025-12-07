using Morskoy_Goy.GameLogic.Models;
using Morskoy_Goy.Network.Common;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Morskoy_Goy.Network.Client
{
    public class GameClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private bool _isConnected;

        private Player _clientPlayer;
        private Player _hostPlayer;

        public event Action<string> Connected;
        public event Action Disconnected;
        public event Action<ShotResultData> ShotResultReceived;

        public async System.Threading.Tasks.Task Connect(string hostIp, int port, string playerName)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(hostIp, port);
            _stream = _client.GetStream();
            _isConnected = true;

            _clientPlayer = new Player(false);
            _hostPlayer = new Player(true);

            Connected?.Invoke("Хост");

            _listenThread = new Thread(ListenForMessages);
            _listenThread.Start();
        }

        private void ListenForMessages()
        {
            byte[] buffer = new byte[4096];

            while (_isConnected && _client.Connected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                    if (message.Type == MessageType.Shot)
                    {
                        ProcessIncomingShot(message);
                    }
                    else if (message.Type == MessageType.ShotResult)
                    {
                        ProcessShotResult(message);
                    }
                }
                catch
                {
                    break;
                }
            }

            Disconnected?.Invoke();
        }

        private void ProcessIncomingShot(NetworkMessage message)
        {
            var shotData = JsonSerializer.Deserialize<ShotData>(message.Data.ToString());

            var result = _clientPlayer.ReceiveShot(shotData.X, shotData.Y);

            var resultData = new ShotResultData
            {
                X = shotData.X,
                Y = shotData.Y,
                IsHit = result.IsHit,
                IsShipDestroyed = result.IsShipDestroyed,
                ShouldRepeatTurn = result.IsHit
            };

            if (_clientPlayer.AllShipsDestroyed())
            {
                resultData.IsGameOver = true;
                resultData.ShouldRepeatTurn = false;
            }

            SendMessage(new NetworkMessage
            {
                Type = MessageType.ShotResult,
                Data = resultData
            });

            if (!result.IsHit)
            {
                _clientPlayer.IsMyTurn = true;
                _hostPlayer.IsMyTurn = false;
            }
        }

        private void ProcessShotResult(NetworkMessage message)
        {
            var resultData = JsonSerializer.Deserialize<ShotResultData>(message.Data.ToString());

            ShotResultReceived?.Invoke(resultData);

            if (!resultData.ShouldRepeatTurn && !resultData.IsGameOver)
            {
                _clientPlayer.IsMyTurn = false;
                _hostPlayer.IsMyTurn = true;
            }
        }

        public void SendShot(int x, int y)
        {
            if (!_clientPlayer.IsMyTurn) return;

            var shotData = new ShotData { X = x, Y = y };

            SendMessage(new NetworkMessage
            {
                Type = MessageType.Shot,
                Data = shotData
            });

            _clientPlayer.IsMyTurn = false;
            _hostPlayer.IsMyTurn = true;
        }

        public void SendMessage(NetworkMessage message)
        {
            if (!_isConnected || !_client.Connected) return;

            string json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            _stream.Write(data, 0, data.Length);
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}