namespace IbnElgm3a.DTOs.Nfc
{
    public class NfcAdminRequest : NfcBaseRequest
    {
        public string Action { get; set; } = string.Empty; // "enroll", "deactivate", "info"
    }
}
