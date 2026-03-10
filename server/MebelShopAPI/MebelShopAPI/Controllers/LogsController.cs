using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MebelShopAPI.Models;

namespace TechShopAPI.Controllers
{
    [Authorize(Roles = "Директор,Администратор")]
    [Route("api/[controller]")]
    [ApiController]
    public class LogsController : ControllerBase
    {
        private readonly MebelShopContext _context;

        public LogsController(MebelShopContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetLogs(
            string? email = null,
            string? role = null,
            string? description = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var logs = _context.AuditLogs
                .Include(a => a.User)
                .Select(a => new AuditLogViewModel
                {
                    IdAudit = a.IdAudit,
                    Email = a.User != null ? a.User.Email : "Системный процесс",
                    Role = a.User != null ? a.User.Role.Name : "-",
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                });

            if (!string.IsNullOrWhiteSpace(email))
                logs = logs.Where(l => l.Email.Contains(email));

            if (!string.IsNullOrWhiteSpace(role) && role != "Все")
                logs = logs.Where(l => l.Role == role);

            if (!string.IsNullOrWhiteSpace(description))
                logs = logs.Where(l => l.Description.Contains(description));

            if (dateFrom.HasValue)
                logs = logs.Where(l => l.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                logs = logs.Where(l => l.CreatedAt <= dateTo.Value);

            var result = await logs.OrderByDescending(l => l.CreatedAt).ToListAsync();
            return Ok(result);
        }
    }

    public class AuditLogViewModel
    {
        public int IdAudit { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
