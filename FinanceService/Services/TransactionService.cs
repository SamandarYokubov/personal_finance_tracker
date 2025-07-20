using FinanceService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FinanceService.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly FinanceDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TransactionService> _logger;
        private readonly HttpClient _httpClient;
        private const string AuditServiceUrl = "http://audit_service:80/api/AuditLog";

        public TransactionService(FinanceDbContext context, IDistributedCache cache, ILogger<TransactionService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<object> GetTransactions(Guid? userId, Guid? categoryId, TransactionType? type, DateTime? dateFrom, DateTime? dateTo, string sortBy, string sortOrder, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting transactions");
            var query = _context.Transactions.AsQueryable();
            query = query.Where(t => !t.IsDeleted);
            if (userId.HasValue) query = query.Where(t => t.UserId == userId);
            if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId);
            if (type.HasValue) query = query.Where(t => t.Type == type);
            if (dateFrom.HasValue) query = query.Where(t => t.CreatedAt >= dateFrom);
            if (dateTo.HasValue) query = query.Where(t => t.CreatedAt <= dateTo);

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("amount", "asc") => query.OrderBy(t => t.Amount),
                ("amount", "desc") => query.OrderByDescending(t => t.Amount),
                ("createdat", "asc") => query.OrderBy(t => t.CreatedAt),
                ("createdat", "desc") => query.OrderByDescending(t => t.CreatedAt),
                ("type", "asc") => query.OrderBy(t => t.Type),
                ("type", "desc") => query.OrderByDescending(t => t.Type),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            _logger.LogInformation("Transactions retrieved successfully");
            return new
            {
                items,
                totalCount,
                pageNumber,
                pageSize
            };
        }

        public async Task<Transaction?> GetTransaction(Guid id)
        {
            _logger.LogInformation("Getting transaction");
            var cacheKey = $"transaction-{id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return JsonSerializer.Deserialize<Transaction>(cached);
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
            if (transaction == null) return null;
            var json = JsonSerializer.Serialize(transaction);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("Transaction retrieved successfully");
            return transaction;
        }

        public async Task<object> GetMonthlySummary(Guid userId, int year, int month)
        {
            _logger.LogInformation("Getting monthly summary");
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && !t.IsDeleted && t.CreatedAt.Year == year && t.CreatedAt.Month == month)
                .ToListAsync();
            var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expenditure = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var balance = income - expenditure;
            _logger.LogInformation("Monthly summary retrieved successfully");
            return new
            {
                UserId = userId,
                Year = year,
                Month = month,
                Income = income,
                Expenditure = expenditure,
                Balance = balance
            };
        }

        public async Task<Transaction> CreateTransaction(Transaction transaction)
        {
            _logger.LogInformation("Creating transaction");
            transaction.Id = Guid.NewGuid();
            transaction.CreatedAt = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            await LogAudit(transaction.UserId, transaction.Id, "Create", "Transaction", null, JsonSerializer.Serialize(transaction));
            _logger.LogInformation("Transaction created successfully");
            return transaction;
        }

        public async Task<IActionResult> UpdateTransaction(Guid id, Transaction transaction)
        {
            _logger.LogInformation("Updating transaction");
            if (id != transaction.Id) return new BadRequestResult();
            var oldTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            _context.Entry(transaction).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return new ConflictObjectResult("The record you attempted to edit was modified by another user after you got the original value."); }
            await LogAudit(transaction.UserId, transaction.Id, "Update", "Transaction", JsonSerializer.Serialize(oldTransaction), JsonSerializer.Serialize(transaction));
            _logger.LogInformation("Transaction updated successfully");
            return new NoContentResult();
        }

        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            _logger.LogInformation("Deleting transaction");
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return new NotFoundResult();
            transaction.IsDeleted = true;
            await _context.SaveChangesAsync();
            await LogAudit(transaction.UserId, transaction.Id, "Delete", "Transaction", JsonSerializer.Serialize(transaction), null);
            _logger.LogInformation("Transaction deleted successfully");
            return new NoContentResult();
        }

        private async Task LogAudit(Guid userId, Guid entityId, string action, string entityName, string? oldValue, string? newValue)
        {
            _logger.LogInformation("Logging audit");
            var audit = new
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                CreatedAt = DateTime.UtcNow,
                OldValue = oldValue,
                NewValue = newValue
            };
            var content = new StringContent(JsonSerializer.Serialize(audit), Encoding.UTF8, "application/json");
            try { 
                await _httpClient.PostAsync(AuditServiceUrl, content); 
                _logger.LogInformation("Audit logged successfully");
            } 
            catch { 
                _logger.LogError("Audit log error");
                throw new Exception("Audit log error"); 
            }
        }
    }
} 