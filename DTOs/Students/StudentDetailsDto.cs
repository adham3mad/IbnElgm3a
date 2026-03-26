using IbnElgm3a.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IbnElgm3a.DTOs.Guardians;

namespace IbnElgm3a.DTOs.Students
{
    public class StudentDetailsDto
    {
        [JsonPropertyName("birth_date")]
        public DateTimeOffset BirthDate { get; set; }
        
        [JsonPropertyName("gender")]
        public Gender Gender { get; set; }
        
        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }

        [JsonPropertyName("guardians")]
        public List<CreateGuardianRequestDto> Guardians { get; set; } = new();
    }
}
