using Morskoy_Goy.Network.Common;
using Morskoy_Goy.Network.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Morskoy_Goy.Network.Server
{
    public class GameRoom
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name { get; }
        public int MaxPlayers { get; }
        public bool IsGameStarted { get; private set; }

        private Dictionary<string, ConnectedClient> _players = new();
        private ConnectedClient _host;

        public int PlayerCount => _players.Count;
        public string HostName => _host?.PlayerName ?? "Нет хоста";

        public GameRoom(string name, int maxPlayers)
        {
            Name = name;
            MaxPlayers = maxPlayers;
        }

        public bool CanJoin()
        {
            return !IsGameStarted && _players.Count < MaxPlayers;
        }

        public void AddPlayer(ConnectedClient client, bool isHost)
        {
            _players[client.ClientId] = client;

            if (isHost)
            {
                _host = client;
            }

            client.CurrentRoom = this;
        }

        public void RemovePlayer(ConnectedClient client)
        {
            _players.Remove(client.ClientId);

            if (client == _host && _players.Any())
            {
                _host = _players.First().Value;
            }

            BroadcastPlayerList();

            if (_players.Count == 0)
            {
                // Удалить комнату из сервера (потом добавить логику пока леньььььььь)
            }
        }

        public void StartGame()
        {
            IsGameStarted = true;

            var startMessage = new NetworkMessage
            {
                Type = MessageType.GameStart,
                Data = new { roomId = Id }
            };

            BroadcastMessage(startMessage);
        }

        public async Task BroadcastMessage(NetworkMessage message)
        {
            var tasks = _players.Values.Select(client => client.SendMessage(message));
            await Task.WhenAll(tasks);
        }

        public async Task BroadcastPlayerList()
        {
            var playerList = GetPlayerList();
            var message = new NetworkMessage
            {
                Type = MessageType.PlayerList,
                Data = playerList
            };

            await BroadcastMessage(message);
        }

        public List<PlayerInfo> GetPlayerList()
        {
            return _players.Values.Select(client => new PlayerInfo
            {
                Id = client.ClientId,
                Name = client.PlayerName,
                IsHost = client == _host,
                IsReady = false // Здесь тоже надо потом  добавить логику готовности пока хзхзхзхз
            }).ToList();
        }

        public RoomInfo GetInfo()
        {
            return new RoomInfo
            {
                Id = Id,
                Name = Name,
                CurrentPlayers = PlayerCount,
                MaxPlayers = MaxPlayers,
                HostName = HostName,
                InGame = IsGameStarted
            };
        }
    }
}