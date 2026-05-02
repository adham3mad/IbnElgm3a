namespace IbnElgm3a.DTOs.RAGBot
{
    public class ChatRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public string? Session_Id { get; set; }
        public string? Student_Id { get; set; }
        public string? Faculty_Id { get; set; }
        public string? Department { get; set; }
        public int Top_K { get; set; } = 5;
        public string[]? Tags { get; set; }
    }
}
