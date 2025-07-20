using AuditService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AuditService.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AuditDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(AuditDbContext context, IDistributedCache cache, ILogger<AuditLogService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<object> GetAuditLogs(Guid? userId, string? action, string? entityName, Guid? entityId, DateTime? dateFrom, DateTime? dateTo, string sortBy, string sortOrder, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting audit logs");
            var query = _context.AuditLogs.AsQueryable();
            if (userId.HasValue) query = query.Where(a => a.UserId == userId);
            if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action.Contains(action));
            if (!string.IsNullOrEmpty(entityName)) query = query.Where(a => a.EntityName.Contains(entityName));
            if (entityId.HasValue) query = query.Where(a => a.EntityId == entityId);
            if (dateFrom.HasValue) query = query.Where(a => a.CreatedAt >= dateFrom);
            if (dateTo.HasValue) query = query.Where(a => a.CreatedAt <= dateTo);

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("createdat", "asc") => query.OrderBy(a => a.CreatedAt),
                ("createdat", "desc") => query.OrderByDescending(a => a.CreatedAt),
                ("action", "asc") => query.OrderBy(a => a.Action),
                ("action", "desc") => query.OrderByDescending(a => a.Action),
                ("entityname", "asc") => query.OrderBy(a => a.EntityName),
                ("entityname", "desc") => query.OrderByDescending(a => a.EntityName),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            _logger.LogInformation("Audit logs retrieved successfully");
            return new
            {
                items,
                totalCount,
                pageNumber,
                pageSize
            };
        }

        public async Task<AuditLog?> GetAuditLog(Guid id)
        {
            _logger.LogInformation("Getting audit log");
            var cacheKey = $"auditlog-{id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return JsonSerializer.Deserialize<AuditLog>(cached);
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null) return null;
            var json = JsonSerializer.Serialize(log);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("Audit log retrieved successfully");
            return log;
        }

        public async Task<AuditLog> CreateAuditLog(AuditLog log)
        {
            _logger.LogInformation("Creating audit log");
            log.Id = Guid.NewGuid();
            log.CreatedAt = DateTime.UtcNow;
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Audit log created successfully");
            return log;
        }

        public async Task<IActionResult> DeleteAuditLog(Guid id)
        {
            _logger.LogInformation("Deleting audit log");
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null) return new NotFoundResult();
            _context.AuditLogs.Remove(log);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Audit log deleted successfully");
            return new NoContentResult();
        }
    }
} 