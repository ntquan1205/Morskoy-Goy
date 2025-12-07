using System.Text.Json.Serialization;

namespace Morskoy_Goy.Network.Common
{
    public class ShotData
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class ShotResultData
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("isHit")]
        public bool IsHit { get; set; }

        [JsonPropertyName("isShipDestroyed")]
        public bool IsShipDestroyed { get; set; }

        [JsonPropertyName("shouldRepeatTurn")]
        public bool ShouldRepeatTurn { get; set; }

        [JsonPropertyName("isGameOver")]
        public bool IsGameOver { get; set; }
    }
}