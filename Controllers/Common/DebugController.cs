using IbnElgm3a.Models;
using IbnElgm3a.Models.Seeder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace IbnElgm3a.Controllers.Common
{
    [ApiController]
    [Route("v1/debug")]
    public class DebugController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public DebugController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("seed")]
        [AllowAnonymous] // Ideally restricted to development or specific keys
        public async Task<IActionResult> Seed()
        {
            try
            {
                await DatabaseSeeder.SeedAllAsync(_context, _config);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.CreateSuccess(new { message = "Database seeded successfully" }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.CreateError(
                    "SEED_ERROR", 
                    ex.Message, 
                    "حدث خطأ أثناء ملء البيانات"
                ));
            }
        }
    }
}
