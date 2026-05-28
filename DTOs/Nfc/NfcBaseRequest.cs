namespace IbnElgm3a.DTOs.Nfc
{
    public class NfcBaseRequest
    {
        public string Uid { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
    }
}
