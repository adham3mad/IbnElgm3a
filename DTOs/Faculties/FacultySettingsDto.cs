using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Faculties
{
    public class FacultySettingsDto
    {
        [JsonPropertyName("accept_admissions")]
        public bool AcceptAdmissions { get; set; }

        [JsonPropertyName("public_profile")]
        public bool PublicProfile { get; set; }

        [JsonPropertyName("ai_chatbot_enabled")]
        public bool AiChatbotEnabled { get; set; }
    }
}
