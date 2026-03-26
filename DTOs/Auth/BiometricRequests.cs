using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Auth
{
    public class BiometricLoginRequestDto
    {
        [Required]
        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("biometric_signature")]
        public string BiometricSignature { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = string.Empty;

        [JsonPropertyName("fcm_token")]
        public string? FcmToken { get; set; }
    }

    public class BiometricRegisterRequestDto
    {
        [Required]
        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("public_key")]
        public string PublicKey { get; set; } = string.Empty;

        [JsonPropertyName("device_name")]
        public string? DeviceName { get; set; }

        [JsonPropertyName("device_os")]
        public string? DeviceOs { get; set; }
    }
}
