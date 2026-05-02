using IbnElgm3a.DTOs.RAGBot;
using System.IO;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface IRAGBotService
    {
        Task<ChatResponseDto?> ChatAsync(ChatRequestDto request);
        Task<Stream> ChatStreamAsync(ChatRequestDto request);
        Task<object?> IngestStudentAsync(StudentIngestDto request);
        Task<bool> DeleteStudentAsync(string facultyId, string studentId);
        Task<object?> GetStudentAsync(string facultyId, string studentId);
        Task<object?> GetStudentsAsync(string? facultyId = null, int limit = 100, int offset = 0);
        Task<object?> GetStudentStatsAsync();
        Task<object?> UploadFileAsync(Stream fileStream, string fileName, string tags = "regulations", bool resetSource = false);
        Task<object?> GetFilesAsync();
        Task<bool> DeleteFileAsync(string fileName);
        Task<object?> GetSystemStatsAsync();
        Task<object?> GetModelsAsync();
    }
}
