using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditService.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using AuditService.Services;

namespace AuditService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAuditLogs(
            Guid? userId = null,
            string? action = null,
            string? entityName = null,
            Guid? entityId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 20)
        {
            var result = await _auditLogService.GetAuditLogs(userId, action, entityName, entityId, dateFrom, dateTo, sortBy, sortOrder, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLog>> GetAuditLog(Guid id)
        {
            var log = await _auditLogService.GetAuditLog(id);
            if (log == null) return NotFound();
            return Ok(log);
        }

        [HttpPost]
        public async Task<ActionResult<AuditLog>> CreateAuditLog(AuditLog log)
        {
            var created = await _auditLogService.CreateAuditLog(log);
            return CreatedAtAction(nameof(GetAuditLog), new { id = created.Id }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditLog(Guid id)
        {
            return await _auditLogService.DeleteAuditLog(id);
        }
    }
} 