using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceService.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using FinanceService.Services;

namespace FinanceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetTransactions(
            Guid? userId = null,
            Guid? categoryId = null,
            TransactionType? type = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int pageNumber = 1,
            int pageSize = 20)
        {
            var result = await _transactionService.GetTransactions(userId, categoryId, type, dateFrom, dateTo, sortBy, sortOrder, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(Guid id)
        {
            var transaction = await _transactionService.GetTransaction(id);
            if (transaction == null) return NotFound();
            return Ok(transaction);
        }

        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetMonthlySummary(Guid userId, int year, int month)
        {
            var summary = await _transactionService.GetMonthlySummary(userId, year, month);
            return Ok(summary);
        }

        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            var created = await _transactionService.CreateTransaction(transaction);
            return CreatedAtAction(nameof(GetTransaction), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, Transaction transaction)
        {
            return await _transactionService.UpdateTransaction(id, transaction);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            return await _transactionService.DeleteTransaction(id);
        }
    }
} 