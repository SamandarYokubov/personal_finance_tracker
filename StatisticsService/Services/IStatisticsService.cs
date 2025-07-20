using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace StatisticsService.Services
{
    public interface IStatisticsService
    {
        Task<IActionResult> GetCategoriesByUser();
        Task<IActionResult> GetCategoriesByUserExcel();
        Task<IActionResult> GetUserExpenditureTrend(Guid userId);
    }
} 