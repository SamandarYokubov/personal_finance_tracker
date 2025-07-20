using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using StatisticsService.Services;

namespace statistics_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("categories-by-user")]
        public async Task<IActionResult> GetCategoriesByUser()
        {
            return await _statisticsService.GetCategoriesByUser();
        }

        [HttpGet("categories-by-user/excel")]
        public async Task<IActionResult> GetCategoriesByUserExcel()
        {
            return await _statisticsService.GetCategoriesByUserExcel();
        }

        [HttpGet("user-expenditure-trend/{userId}")]
        public async Task<IActionResult> GetUserExpenditureTrend(Guid userId)
        {
            return await _statisticsService.GetUserExpenditureTrend(userId);
        }
    }
} 