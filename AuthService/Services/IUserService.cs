using AuthService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Services
{
    public interface IUserService
    {
        Task<object> GetUsers(string? username, string? email, string? role, string sortBy, string sortOrder, int pageNumber, int pageSize);
        Task<User?> GetUser(Guid id);
        Task<User> CreateUser(User user);
        Task<IActionResult> UpdateUser(Guid id, User user);
        Task<IActionResult> DeleteUser(Guid id);
        Task<IActionResult> Login(string username, string password);
    }
} 