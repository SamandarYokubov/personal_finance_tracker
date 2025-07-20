using FinanceService.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinanceService.Services
{
    public interface ITransactionService
    {
        Task<object> GetTransactions(Guid? userId, Guid? categoryId, TransactionType? type, DateTime? dateFrom, DateTime? dateTo, string sortBy, string sortOrder, int pageNumber, int pageSize);
        Task<Transaction?> GetTransaction(Guid id);
        Task<object> GetMonthlySummary(Guid userId, int year, int month);
        Task<Transaction> CreateTransaction(Transaction transaction);
        Task<IActionResult> UpdateTransaction(Guid id, Transaction transaction);
        Task<IActionResult> DeleteTransaction(Guid id);
    }
} 