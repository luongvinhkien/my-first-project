using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class CrmController : Controller
    {
        private readonly RestaurantDbContext _context;

        public CrmController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Customer profiles list
        public IActionResult Index(string search, int? selectedCustomerId)
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search));
            }

            var customers = query.OrderByDescending(c => c.Points).ToList();
            
            Customer selectedCust = null;
            if (selectedCustomerId.HasValue)
            {
                selectedCust = _context.Customers.Find(selectedCustomerId.Value);
            }
            else if (customers.Any())
            {
                selectedCust = customers.First();
            }

            // Fetch order history of selected customer
            var orderHistory = selectedCust != null
                ? _context.Orders
                    .Include(o => o.Table)
                    .Where(o => o.CustomerId == selectedCust.Id)
                    .OrderByDescending(o => o.OrderTime)
                    .ToList()
                : null;

            ViewData["Customers"] = customers;
            ViewData["SelectedCustomer"] = selectedCust;
            ViewData["OrderHistory"] = orderHistory;
            ViewData["SearchQuery"] = search;

            return View();
        }

        // Action: Create customer
        [HttpPost]
        public IActionResult CreateCustomer(Customer cust)
        {
            if (ModelState.IsValid)
            {
                cust.Points = 0;
                cust.MemberTier = "Silver";
                _context.Customers.Add(cust);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Redeem points
        [HttpPost]
        public IActionResult RedeemGift(int customerId, string giftType, int pointsCost)
        {
            var cust = _context.Customers.Find(customerId);
            if (cust != null && cust.Points >= pointsCost)
            {
                cust.Points -= pointsCost;
                
                // Downgrade tier check if necessary (usually tier stays or drops based on points)
                if (cust.Points >= 3000) cust.MemberTier = "VIP";
                else if (cust.Points >= 2000) cust.MemberTier = "Platinum";
                else if (cust.Points >= 1000) cust.MemberTier = "Gold";
                else if (cust.Points >= 500) cust.MemberTier = "Silver";
                else cust.MemberTier = "Silver";

                // Seed a voucher as reward
                string voucherCode = "REDEEM-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                decimal voucherVal = giftType == "Voucher 50k" ? 50000.00m : 100000.00m;
                
                _context.Vouchers.Add(new Voucher
                {
                    Code = voucherCode,
                    Value = voucherVal,
                    Type = "Amount",
                    IsUsed = false,
                    ExpiryDate = DateTime.Today.AddMonths(1)
                });

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "cashier",
                    Action = $"Đổi quà: Khách {cust.Name} đổi {pointsCost} điểm lấy {giftType}. Code: {voucherCode}",
                    TableName = "Customers",
                    RecordId = customerId,
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index), new { selectedCustomerId = customerId });
        }
    }
}
