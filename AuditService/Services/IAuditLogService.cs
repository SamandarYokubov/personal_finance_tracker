using AuditService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuditService.Services
{
    public interface IAuditLogService
    {
        Task<object> GetAuditLogs(Guid? userId, string? action, string? entityName, Guid? entityId, DateTime? dateFrom, DateTime? dateTo, string sortBy, string sortOrder, int pageNumber, int pageSize);
        Task<AuditLog?> GetAuditLog(Guid id);
        Task<AuditLog> CreateAuditLog(AuditLog log);
        Task<IActionResult> DeleteAuditLog(Guid id);
    }
} 