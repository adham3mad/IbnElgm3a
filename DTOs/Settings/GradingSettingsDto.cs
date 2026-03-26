using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IbnElgm3a.DTOs.Settings
{
    public class GradingSettingsDto
    {
        [JsonPropertyName("scale")]
        public List<GradingScaleDto> Scale { get; set; } = new List<GradingScaleDto>();

        [JsonPropertyName("credit_hour_rules")]
        public CreditHourRulesDto CreditHourRules { get; set; } = new CreditHourRulesDto();
    }
}
