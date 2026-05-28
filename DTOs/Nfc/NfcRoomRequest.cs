using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Nfc
{
    public class NfcRoomRequest : NfcBaseRequest
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }
    }
}
