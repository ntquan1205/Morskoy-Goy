using Morskoy_Goy.Network.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Morskoy_Goy.Network.Server
{
    public class ConnectedClient
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private GameServer _server;
        public string ClientId { get; } = Guid.NewGuid().ToString();
        public string PlayerName { get; set; }
        public GameRoom CurrentRoom { get; set; }

        public ConnectedClient(TcpClient client, GameServer server)
        {
            _tcpClient = client;
            _stream = client.GetStream();
            _server = server;
        }

        public async Task StartListeningAsync()
        {
            try
            {
                var buffer = new byte[4096];

                while (_tcpClient.Connected)
                {
                    var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonSerializer.Deserialize<NetworkMessage>(json);

                    await ProcessMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка клиента {ClientId}: {ex.Message}");
            }
            finally
            {
                _server.RemoveClient(this);
            }
        }

        private async Task ProcessMessageAsync(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.Connect:
                    PlayerName = (message.Data as JsonElement?)?.GetProperty("playerName").GetString();
                    await SendRoomList();
                    break;

                case MessageType.CreateRoom:
                    var createRequest = JsonSerializer.Deserialize<CreateRoomRequest>(
                        (message.Data as JsonElement?)?.ToString() ?? "{}");

                    var room = _server.CreateRoom(
                        createRequest.RoomName,
                        createRequest.PlayerName,
                        createRequest.MaxPlayers);

                    CurrentRoom = room;
                    room.AddPlayer(this, isHost: true);

                    await SendRoomInfo(room);
                    break;

                case MessageType.JoinRoom:
                    var joinRequest = JsonSerializer.Deserialize<JoinRoomRequest>(
                        (message.Data as JsonElement?)?.ToString() ?? "{}");

                    var targetRoom = _server.FindRoom(joinRequest.RoomId);
                    if (targetRoom != null && targetRoom.CanJoin())
                    {
                        CurrentRoom = targetRoom;
                        targetRoom.AddPlayer(this, isHost: false);

                        await SendRoomInfo(targetRoom);
                        await targetRoom.BroadcastPlayerList();
                    }
                    else
                    {
                        await SendError("Не удалось присоединиться к комнате");
                    }
                    break;

                case MessageType.LeaveRoom:
                    CurrentRoom?.RemovePlayer(this);
                    CurrentRoom = null;
                    await SendRoomList();
                    break;

                case MessageType.StartGame:
                    CurrentRoom?.StartGame();
                    break;
            }
        }

        public async Task SendMessage(NetworkMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(data, 0, data.Length);
        }

        private async Task SendRoomList()
        {
            var rooms = _server.GetRoomList();
            await SendMessage(new NetworkMessage
            {
                Type = MessageType.RoomList,
                Data = rooms
            });
        }

        private async Task SendRoomInfo(GameRoom room)
        {
            await SendMessage(new NetworkMessage
            {
                Type = MessageType.RoomList,
                Data = new { room = room.GetInfo(), players = room.GetPlayerList() }
            });
        }

        private async Task SendError(string error)
        {
            await SendMessage(new NetworkMessage
            {
                Type = MessageType.Error,
                Data = new { message = error }
            });
        }

        public void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
    }
}
