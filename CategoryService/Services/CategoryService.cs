using CategoryService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace CategoryService.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly CategoryDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CategoryService> _logger;
        private readonly HttpClient _httpClient;
        private const string AuditServiceUrl = "http://audit_service:80/api/AuditLog";

        public CategoryService(CategoryDbContext context, IDistributedCache cache, ILogger<CategoryService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<object> GetCategories(string? name, Guid? userId, string sortBy, string sortOrder, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting categories");
            var query = _context.Categories.AsQueryable();
            query = query.Where(c => !c.IsDeleted);
            if (!string.IsNullOrEmpty(name)) query = query.Where(c => c.Name.Contains(name));
            if (userId.HasValue) query = query.Where(c => c.UserId == userId);

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("name", "asc") => query.OrderBy(c => c.Name),
                ("name", "desc") => query.OrderByDescending(c => c.Name),
                ("color", "asc") => query.OrderBy(c => c.Color),
                ("color", "desc") => query.OrderByDescending(c => c.Color),
                _ => query.OrderBy(c => c.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            _logger.LogInformation("Categories retrieved successfully");
            return new
            {
                items,
                totalCount,
                pageNumber,
                pageSize
            };
        }

        public async Task<Category?> GetCategory(Guid id)
        {
            _logger.LogInformation("Getting category");
            var cacheKey = $"category-{id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return JsonSerializer.Deserialize<Category>(cached);
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return null;
            var json = JsonSerializer.Serialize(category);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("Category retrieved successfully");
            return category;
        }

        public async Task<Category> CreateCategory(Category category)
        {
            _logger.LogInformation("Creating category");
            category.Id = Guid.NewGuid();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            await LogAudit(category.UserId, category.Id, "Create", "Category", null, JsonSerializer.Serialize(category));
            _logger.LogInformation("Category created successfully");
            return category;
        }

        public async Task<IActionResult> UpdateCategory(Guid id, Category category)
        {
            _logger.LogInformation("Updating category");
            if (id != category.Id) return new BadRequestResult();
            var oldCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            _context.Entry(category).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return new ConflictObjectResult("The record you attempted to edit was modified by another user after you got the original value."); }
            await LogAudit(category.UserId, category.Id, "Update", "Category", JsonSerializer.Serialize(oldCategory), JsonSerializer.Serialize(category));
            _logger.LogInformation("Category updated successfully");
            return new NoContentResult();
        }

        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            _logger.LogInformation("Deleting category");
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return new NotFoundResult();
            category.IsDeleted = true;
            await _context.SaveChangesAsync();
            await LogAudit(category.UserId, category.Id, "Delete", "Category", JsonSerializer.Serialize(category), null);
            _logger.LogInformation("Category deleted successfully");
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