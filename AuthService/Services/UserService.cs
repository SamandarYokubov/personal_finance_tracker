using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Services
{
    public class UserService : IUserService
    {
        private readonly AuthDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly HttpClient _httpClient;
        private const string AuditServiceUrl = "http://audit_service:80/api/AuditLog";

        public UserService(AuthDbContext context, IDistributedCache cache, ILogger<UserService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _passwordHasher = new PasswordHasher<User>();
            _httpClient = new HttpClient();
        }

        public async Task<object> GetUsers(string? username, string? email, string? role, string sortBy, string sortOrder, int pageNumber, int pageSize)
        {
            _logger.LogInformation("Getting users");
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(username)) query = query.Where(u => u.Username.Contains(username));
            if (!string.IsNullOrEmpty(email)) query = query.Where(u => u.Email.Contains(email));
            if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role == role);

            query = (sortBy.ToLower(), sortOrder.ToLower()) switch
            {
                ("username", "asc") => query.OrderBy(u => u.Username),
                ("username", "desc") => query.OrderByDescending(u => u.Username),
                ("email", "asc") => query.OrderBy(u => u.Email),
                ("email", "desc") => query.OrderByDescending(u => u.Email),
                ("role", "asc") => query.OrderBy(u => u.Role),
                ("role", "desc") => query.OrderByDescending(u => u.Role),
                _ => query.OrderBy(u => u.Username)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            _logger.LogInformation("Users retrieved successfully");
            return new
            {
                items,
                totalCount,
                pageNumber,
                pageSize
            };
        }

        public async Task<User?> GetUser(Guid id)
        {
            _logger.LogInformation("Getting user");
            var cacheKey = $"user-{id}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return JsonSerializer.Deserialize<User>(cached);
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;
            var json = JsonSerializer.Serialize(user);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("User retrieved successfully");
            return user;
        }

        public async Task<User> CreateUser(User user)
        {
            _logger.LogInformation("Creating user");
            user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);
            user.Id = Guid.NewGuid();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await LogAudit(user.Id, user.Id, "Create", "User", null, JsonSerializer.Serialize(user));
            _logger.LogInformation("User created successfully");
            return user;
        }

        public async Task<IActionResult> UpdateUser(Guid id, User user)
        {
            if (id != user.Id) return new BadRequestResult();
            var oldUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            _context.Entry(user).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!_context.Users.Any(e => e.Id == id)) return new NotFoundResult(); else throw; }
            await LogAudit(user.Id, user.Id, "Update", "User", JsonSerializer.Serialize(oldUser), JsonSerializer.Serialize(user));
            return new NoContentResult();
        }

        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return new NotFoundResult();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await LogAudit(user.Id, user.Id, "Delete", "User", JsonSerializer.Serialize(user), null);
            return new NoContentResult();
        }

        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return new UnauthorizedObjectResult("Invalid username or password");
            }
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                return new UnauthorizedObjectResult("Invalid username or password");
            }
            return new OkObjectResult(new { user.Id, user.Username, user.Email, user.Role });
        }

        private async Task LogAudit(Guid userId, Guid entityId, string action, string entityName, string? oldValue, string? newValue)
        {
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
            try { await _httpClient.PostAsync(AuditServiceUrl, content); } catch { throw new Exception("Cannot log audit"); }
        }
    }
} 