using CategoryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CategoryService.Services
{
    public interface ICategoryService
    {
        Task<object> GetCategories(string? name, Guid? userId, string sortBy, string sortOrder, int pageNumber, int pageSize);
        Task<Category?> GetCategory(Guid id);
        Task<Category> CreateCategory(Category category);
        Task<IActionResult> UpdateCategory(Guid id, Category category);
        Task<IActionResult> DeleteCategory(Guid id);
    }
} 