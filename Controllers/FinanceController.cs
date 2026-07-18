using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class FinanceController : Controller
    {
        private readonly RestaurantDbContext _context;

        public FinanceController(RestaurantDbContext context)
        {
            _context = context;
        }

        // CashFlow vouchers ledger & Audit logs
        public IActionResult Index()
        {
            var cashflows = _context.CashFlows.OrderByDescending(c => c.CreatedTime).ToList();
            var auditLogs = _context.AuditLogs.OrderByDescending(a => a.Timestamp).ToList();

            // Calculate totals
            decimal totalReceipts = cashflows.Where(c => c.Type == "Receipt").Sum(c => c.Amount);
            decimal totalPayments = cashflows.Where(c => c.Type == "Payment").Sum(c => c.Amount);
            decimal balance = totalReceipts - totalPayments;

            ViewData["CashFlows"] = cashflows;
            ViewData["AuditLogs"] = auditLogs;
            ViewData["TotalReceipts"] = totalReceipts;
            ViewData["TotalPayments"] = totalPayments;
            ViewData["Balance"] = balance;

            return View();
        }

        // Action: Create CashFlow Voucher (Lập phiếu Thu/Chi)
        [HttpPost]
        public IActionResult CreateCashFlow(CashFlow cf)
        {
            if (ModelState.IsValid)
            {
                cf.CreatedTime = DateTime.Now;
                _context.CashFlows.Add(cf);
                _context.SaveChanges();

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "manager",
                    Action = $"Lập phiếu {cf.Type}: {cf.Title}, Số tiền: {cf.Amount:N0}đ",
                    TableName = "CashFlows",
                    RecordId = cf.Id,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
