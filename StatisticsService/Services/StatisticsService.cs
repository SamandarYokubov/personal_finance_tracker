using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using StatisticsService.DTOs;
using Microsoft.Extensions.Logging;

namespace StatisticsService.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<StatisticsService> _logger;
        private const string CategoryServiceUrl = "http://category_service:80/api/Category";
        private const string TransactionServiceUrl = "http://finance_service:80/api/Transaction";

        public StatisticsService(IDistributedCache cache, ILogger<StatisticsService> logger)
        {
            _httpClient = new HttpClient();
            _cache = cache;
            _logger = logger;
        }

        public async Task<IActionResult> GetCategoriesByUser()
        {
            _logger.LogInformation("Getting categories by user");
            var cacheKey = "categories-by-user";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return new ContentResult { Content = cached, ContentType = "application/json" };
            var categories = await _httpClient.GetStringAsync(CategoryServiceUrl);
            var transactions = await _httpClient.GetStringAsync(TransactionServiceUrl);
            var categoryList = JsonSerializer.Deserialize<List<CategoryDto>>(categories) ?? new();
            var transactionList = JsonSerializer.Deserialize<List<TransactionDto>>(transactions) ?? new();

            var result = transactionList
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Categories = g.Select(t => categoryList.FirstOrDefault(c => c.Id == t.CategoryId)?.Name)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToList()
                })
                .ToList();
            var json = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("Categories by user retrieved successfully");
            return new ContentResult { Content = json, ContentType = "application/json" };
        }

        public async Task<IActionResult> GetCategoriesByUserExcel()
        {
            _logger.LogInformation("Getting categories by user excel");
            var cacheKey = "categories-by-user-excel";
            var cached = await _cache.GetAsync(cacheKey);
            if (cached != null)
                return new FileContentResult(cached, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = "categories_by_user.xlsx" };
            var categories = await _httpClient.GetStringAsync(CategoryServiceUrl);
            var transactions = await _httpClient.GetStringAsync(TransactionServiceUrl);
            var categoryList = JsonSerializer.Deserialize<List<CategoryDto>>(categories) ?? new();
            var transactionList = JsonSerializer.Deserialize<List<TransactionDto>>(transactions) ?? new();

            var result = transactionList
                .GroupBy(t => t.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Categories = g.Select(t => categoryList.FirstOrDefault(c => c.Id == t.CategoryId)?.Name)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToList()
                })
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("CategoriesByUser");
            worksheet.Cell(1, 1).Value = "UserId";
            worksheet.Cell(1, 2).Value = "Categories";
            int row = 2;
            foreach (var item in result)
            {
                worksheet.Cell(row, 1).Value = item.UserId.ToString();
                worksheet.Cell(row, 2).Value = string.Join(", ", item.Categories);
                row++;
            }
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var bytes = stream.ToArray();
            await _cache.SetAsync(cacheKey, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("Categories by user excel retrieved successfully");
            return new FileContentResult(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") { FileDownloadName = "categories_by_user.xlsx" };
        }

        public async Task<IActionResult> GetUserExpenditureTrend(Guid userId)
        {
            _logger.LogInformation("Getting user expenditure trend");
            var cacheKey = $"user-expenditure-trend-{userId}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return new ContentResult { Content = cached, ContentType = "application/json" };
            var transactionsJson = await _httpClient.GetStringAsync(TransactionServiceUrl);
            var transactionList = JsonSerializer.Deserialize<List<TransactionDto>>(transactionsJson) ?? new();
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var thisMonthTotal = transactionList
                .Where(t => t.UserId == userId && !t.IsDeleted && t.Type == 1 /* Expense */ && t.CreatedAt >= thisMonth)
                .Sum(t => t.Amount);
            var lastMonthTotal = transactionList
                .Where(t => t.UserId == userId && !t.IsDeleted && t.Type == 1 /* Expense */ && t.CreatedAt >= lastMonth && t.CreatedAt < thisMonth)
                .Sum(t => t.Amount);

            string trend = thisMonthTotal > lastMonthTotal ? "increased" :
                            thisMonthTotal < lastMonthTotal ? "decreased" : "no change";
            var resultObj = new
            {
                UserId = userId,
                ThisMonthTotal = thisMonthTotal,
                LastMonthTotal = lastMonthTotal,
                Trend = trend
            };
            var json = JsonSerializer.Serialize(resultObj);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            _logger.LogInformation("User expenditure trend retrieved successfully");
            return new ContentResult { Content = json, ContentType = "application/json" };
        }
    }
} 