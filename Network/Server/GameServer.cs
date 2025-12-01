using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Morskoy_Goy.Network.Common;

namespace Morskoy_Goy.Network.Server
{
    public class GameServer
    {
        private TcpListener _listener;
        private List<ConnectedClient> _clients = new();
        private List<GameRoom> _rooms = new();
        private bool _isRunning;

        public event Action<string> LogMessage;

        public async Task Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            LogMessage?.Invoke($"Сервер запущен на порту {port}");

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                var connectedClient = new ConnectedClient(client, this);
                _clients.Add(connectedClient);

                LogMessage?.Invoke($"Новое подключение: {client.Client.RemoteEndPoint}");
                _ = connectedClient.StartListeningAsync();
            }
        }

        public void RemoveClient(ConnectedClient client)
        {
            _clients.Remove(client);

            // Удаляем игрока из всех комнат
            foreach (var room in _rooms)
            {
                room.RemovePlayer(client);
            }
        }

        public GameRoom CreateRoom(string roomName, string hostName, int maxPlayers)
        {
            var room = new GameRoom(roomName, maxPlayers);
            _rooms.Add(room);

            LogMessage?.Invoke($"Создана комната: {roomName}");
            return room;
        }

        public List<RoomInfo> GetRoomList()
        {
            var roomInfos = new List<RoomInfo>();

            foreach (var room in _rooms)
            {
                roomInfos.Add(new RoomInfo
                {
                    Id = room.Id,
                    Name = room.Name,
                    CurrentPlayers = room.PlayerCount,
                    MaxPlayers = room.MaxPlayers,
                    HostName = room.HostName,
                    InGame = room.IsGameStarted
                });
            }

            return roomInfos;
        }

        public GameRoom FindRoom(string roomId)
        {
            return _rooms.Find(r => r.Id == roomId);
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            foreach (var client in _clients)
            {
                client.Disconnect();
            }
        }
    }
}
