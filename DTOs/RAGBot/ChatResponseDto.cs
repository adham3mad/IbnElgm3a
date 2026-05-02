using System.Collections.Generic;

namespace IbnElgm3a.DTOs.RAGBot
{
    public class ChatResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public string Session_Id { get; set; } = string.Empty;
        public List<SourceDto> Sources { get; set; } = new();
        public int Chunks_Used { get; set; }
        public int Latency_Ms { get; set; }
    }

    public class SourceDto
    {
        public string Chunk_Id { get; set; } = string.Empty;
        public string Source_File { get; set; } = string.Empty;
        public int? Page { get; set; }
        public string? Article { get; set; }
        public double Score { get; set; }
        public string Snippet { get; set; } = string.Empty;
    }
}
