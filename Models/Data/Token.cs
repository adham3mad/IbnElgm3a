using IbnElgm3a.Enums;
using System;

namespace IbnElgm3a.Model.Data
{
    public class Token
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string UserId { get; set; } = string.Empty;
        public virtual User? User { get; set; }

        public string TokenValue { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;

        public AccountType AccountType { get; set; }

        public DateTimeOffset ExpiryDate { get; set; }
        public bool IsRevoked { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}