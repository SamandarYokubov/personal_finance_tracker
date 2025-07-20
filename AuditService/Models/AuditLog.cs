using System;

namespace AuditService.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
} 