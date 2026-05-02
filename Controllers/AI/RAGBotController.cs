using IbnElgm3a.DTOs.RAGBot;
using IbnElgm3a.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IbnElgm3a.Controllers.AI
{
    [ApiController]
    [Route("api/v1/ragbot")]
    [Authorize(Roles ="student")]
    public class RAGBotController : ControllerBase
    {
        private readonly IRAGBotService _ragBotService;

        public RAGBotController(IRAGBotService ragBotService)
        {
            _ragBotService = ragBotService;
        }

        [HttpPost("chat")] // Chat is open as per documentation, but you can restrict it to Authenticated users if needed
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
        {
            var response = await _ragBotService.ChatAsync(request);
            return Ok(response);
        }

        [HttpPost("chat/stream")]
        [AllowAnonymous]
        public async Task ChatStream([FromBody] ChatRequestDto request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var stream = await _ragBotService.ChatStreamAsync(request);
            
            await stream.CopyToAsync(Response.Body);
            await Response.Body.FlushAsync();
        }

        [HttpPost("ingest/student")]
        [Authorize(Roles = "admin")] 
        public async Task<IActionResult> IngestStudent([FromBody] StudentIngestDto request)
        {
            var response = await _ragBotService.IngestStudentAsync(request);
            return Ok(response);
        }

        [HttpDelete("ingest/student/{facultyId}/{studentId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteStudent(string facultyId, string studentId)
        {
            var success = await _ragBotService.DeleteStudentAsync(facultyId, studentId);
            return success ? Ok() : NotFound();
        }

        [HttpGet("ingest/students")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetStudents([FromQuery] string? faculty_id, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var response = await _ragBotService.GetStudentsAsync(faculty_id, limit, offset);
            return Ok(response);
        }

        [HttpGet("ingest/students/stats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetStudentStats()
        {
            var response = await _ragBotService.GetStudentStatsAsync();
            return Ok(response);
        }

        [HttpGet("ingest/students/{facultyId}/{studentId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetStudent(string facultyId, string studentId)
        {
            var response = await _ragBotService.GetStudentAsync(facultyId, studentId);
            return Ok(response);
        }

        [HttpPost("files/upload")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string tags = "regulations", [FromForm] bool reset_source = false)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty");
            
            using var stream = file.OpenReadStream();
            var response = await _ragBotService.UploadFileAsync(stream, file.FileName, tags, reset_source);
            return Ok(response);
        }

        [HttpGet("files")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFiles()
        {
            var response = await _ragBotService.GetFilesAsync();
            return Ok(response);
        }

        [HttpDelete("files/{fileName}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var success = await _ragBotService.DeleteFileAsync(fileName);
            return success ? Ok() : NotFound();
        }

        [HttpGet("stats")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetStats()
        {
            var response = await _ragBotService.GetSystemStatsAsync();
            return Ok(response);
        }

        [HttpGet("models")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetModels()
        {
            var response = await _ragBotService.GetModelsAsync();
            return Ok(response);
        }
    }
}
