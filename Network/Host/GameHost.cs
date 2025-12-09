using Morskoy_Goy.Network.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Morskoy_Goy.Views;
using Morskoy_Goy.Models;

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

        private Player _hostPlayer;
        private Player _clientPlayer;

        public event Action<int, int> IncomingShotReceived;
        public event Action<string> ClientConnected;
        public event Action ClientDisconnected;
        public event Action<ShotResultData> ShotResultReceived;
        public event Action<bool> TurnChanged; 

        public void Start(int port, string playerName)
        {
            _playerName = playerName;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            _client = _listener.AcceptTcpClient();
            _stream = _client.GetStream();

            _hostPlayer = new Player(true);
            _clientPlayer = new Player(false);

            ClientConnected?.Invoke("Соперник");

            // Отправляем начальное состояние игры клиенту
            SendGameStart();

            _listenThread = new Thread(ListenForMessages);
            _listenThread.Start();
        }

        private void SendGameStart()
        {
            var startData = new
            {
                HostName = _playerName,
                HostStarts = true, 
                Turn = _hostPlayer.IsMyTurn
            };

            SendMessage(new NetworkMessage
            {
                Type = MessageType.StartGame,
                Data = startData
            });
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

            ClientDisconnected?.Invoke();
        }

        private void ProcessIncomingShot(NetworkMessage message)
        {
            var shotData = JsonSerializer.Deserialize<ShotData>(message.Data.ToString());
            IncomingShotReceived?.Invoke(shotData.X, shotData.Y);

            var result = _hostPlayer.ReceiveShot(shotData.X, shotData.Y);

            var resultData = new ShotResultData
            {
                X = shotData.X,
                Y = shotData.Y,
                IsHit = result.IsHit,
                IsShipDestroyed = result.IsShipDestroyed,
                ShouldRepeatTurn = result.IsHit
            };

            if (_hostPlayer.AllShipsDestroyed())
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
                _hostPlayer.IsMyTurn = true;
                _clientPlayer.IsMyTurn = false;
                TurnChanged?.Invoke(true); 
            }
        }

        private void ProcessShotResult(NetworkMessage message)
        {
            var resultData = JsonSerializer.Deserialize<ShotResultData>(message.Data.ToString());

            var cell = _hostPlayer.Field.GetCell(resultData.X, resultData.Y);
            if (cell != null)
            {
                if (resultData.IsHit)
                {
                    cell.Status = resultData.IsShipDestroyed ?
                        CellStatus.ShipDestroyed : CellStatus.ShipHited;
                }
                else
                {
                    cell.Status = CellStatus.Miss;
                }
            }

            ShotResultReceived?.Invoke(resultData);

            if (!resultData.ShouldRepeatTurn && !resultData.IsGameOver)
            {
                _hostPlayer.IsMyTurn = true;
                _clientPlayer.IsMyTurn = false;
                TurnChanged?.Invoke(true);
            }
            else if (resultData.IsHit && !resultData.IsGameOver)
            {
                _hostPlayer.IsMyTurn = false;
                _clientPlayer.IsMyTurn = true;
                TurnChanged?.Invoke(false);
            }
        }

        public void SendShot(int x, int y)
        {
            if (!_hostPlayer.IsMyTurn) return;

            var shotData = new ShotData { X = x, Y = y };

            SendMessage(new NetworkMessage
            {
                Type = MessageType.Shot,
                Data = shotData
            });

            _hostPlayer.IsMyTurn = false;
            _clientPlayer.IsMyTurn = true;
            TurnChanged?.Invoke(false);
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