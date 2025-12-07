using System.Text.Json.Serialization;

namespace Morskoy_Goy.Network.Common
{
    public enum MessageType
    {
        Connect,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        RoomList,
        PlayerList,
        StartGame,
        GameStart,
        GameState,
        Shot,
        GameOver,
        Error,
        Ping,
        Disconnect,
        ShotResult         
    }

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
}