using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Morskoy_Goy.Network.Common
{
    public class NetworkMessage
    {
        [JsonPropertyName("type")]
        public MessageType Type { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("senderId")]
        public string SenderId { get; set; }

        [JsonPropertyName("roomId")]
        public string RoomId { get; set; }
    }
    public class CreateRoomRequest
    {
        public string RoomName { get; set; }
        public string PlayerName { get; set; }
        public int MaxPlayers { get; set; } = 2;
    }

    public class RoomInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string HostName { get; set; }
        public bool InGame { get; set; }
    }

    public class JoinRoomRequest
    {
        public string RoomId { get; set; }
        public string PlayerName { get; set; }
    }

    public class PlayerInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsHost { get; set; }
        public bool IsReady { get; set; }
    }
}
