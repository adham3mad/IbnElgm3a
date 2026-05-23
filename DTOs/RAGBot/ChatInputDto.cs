namespace IbnElgm3a.DTOs.RAGBot
{
    public class ChatInputDto
    {
        public string Question { get; set; } = string.Empty;
        public string? Session_Id { get; set; }
        public int Top_K { get; set; } = 5;
        public string[]? Tags { get; set; }
    }
}
