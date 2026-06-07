using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Nfc
{
    public class NfcRoomRequest : NfcBaseRequest
    {
        [JsonPropertyName("room_id")]
        public string RoomId { get; set; }
    }
}
