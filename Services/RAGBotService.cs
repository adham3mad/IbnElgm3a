using IbnElgm3a.DTOs.RAGBot;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IbnElgm3a.Services
{
    public class RAGBotService : IRAGBotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _ingestKey;
        private readonly string _adminKey;
        private readonly JsonSerializerOptions _jsonOptions;

        public RAGBotService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = Environment.GetEnvironmentVariable("RAGBOT_BASE_URL")?.TrimEnd('/') ?? throw new Exception("RAGBOT_BASE_URL is missing");
            _ingestKey = Environment.GetEnvironmentVariable("RAGBOT_INGEST_SECRET_KEY") ?? throw new Exception("RAGBOT_INGEST_SECRET_KEY is missing");
            _adminKey = Environment.GetEnvironmentVariable("RAGBOT_ADMIN_SECRET_KEY") ?? throw new Exception("RAGBOT_ADMIN_SECRET_KEY is missing");
            
            _jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ChatResponseDto?> ChatAsync(ChatRequestDto request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}/chat", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ChatResponseDto>(json, _jsonOptions);
        }

        public async Task<Stream> ChatStreamAsync(ChatRequestDto request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/stream")
            {
                Content = content
            };
            
            // SSE usually requires long timeouts or no buffering
            var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<object?> IngestStudentAsync(StudentIngestDto request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/ingest/student")
            {
                Content = content
            };
            requestMessage.Headers.Add("X-Ingest-Key", _ingestKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<bool> DeleteStudentAsync(string facultyId, string studentId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/ingest/student/{facultyId}/{studentId}");
            requestMessage.Headers.Add("X-Ingest-Key", _ingestKey);

            var response = await _httpClient.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }

        public async Task<object?> GetStudentAsync(string facultyId, string studentId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/ingest/students/{facultyId}/{studentId}");
            requestMessage.Headers.Add("X-Ingest-Key", _ingestKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<object?> GetStudentsAsync(string? facultyId = null, int limit = 100, int offset = 0)
        {
            var url = $"{_baseUrl}/ingest/students?limit={limit}&offset={offset}";
            if (!string.IsNullOrEmpty(facultyId)) url += $"&faculty_id={facultyId}";

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Add("X-Ingest-Key", _ingestKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<object?> GetStudentStatsAsync()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/ingest/students/stats");
            requestMessage.Headers.Add("X-Ingest-Key", _ingestKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<object?> UploadFileAsync(Stream fileStream, string fileName, string tags = "regulations", bool resetSource = false)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(tags), "tags");
            content.Add(new StringContent(resetSource.ToString().ToLower()), "reset_source");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/files/upload")
            {
                Content = content
            };
            requestMessage.Headers.Add("X-Admin-Key", _adminKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<object?> GetFilesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/files");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/files/{fileName}");
            requestMessage.Headers.Add("X-Admin-Key", _adminKey);

            var response = await _httpClient.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }

        public async Task<object?> GetSystemStatsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/stats");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }

        public async Task<object?> GetModelsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/models");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json, _jsonOptions);
        }
    }
}
