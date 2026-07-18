using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class HomeController : Controller
    {
        private readonly RestaurantDbContext _context;

        public HomeController(RestaurantDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var today = DateTime.Today;

            // Today's Revenue
            var todayRevenue = _context.Orders
                .Where(o => o.OrderTime >= today && o.Status == "Paid")
                .Sum(o => (decimal?)o.FinalAmount) ?? 0.00m;

            // Active Tables Serving
            var servingTables = _context.Tables.Count(t => t.Status == "Serving");

            // Empty Tables
            var emptyTables = _context.Tables.Count(t => t.Status == "Empty");

            // Guests Count (from reservations today or active serving tables)
            var activeGuests = _context.Reservations
                .Where(r => r.ReservationTime >= today && r.ReservationTime < today.AddDays(1) && r.Status == "CheckedIn")
                .Sum(r => (int?)r.GuestCount) ?? 0;
            if (activeGuests == 0)
            {
                activeGuests = servingTables * 3; // Fallback estimate
            }

            // Top Selling Items (all time or today)
            var topSellers = _context.OrderItems
                .Include(oi => oi.MenuItem)
                .GroupBy(oi => oi.MenuItem.Name)
                .Select(g => new TopSellerDto {
                    Name = g.Key,
                    Qty = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.Price)
                })
                .OrderByDescending(x => x.Qty)
                .Take(5)
                .ToList();

            // Orders status
            var cookingOrders = _context.Orders.Count(o => o.Status == "Kitchen");
            var pendingPaymentOrders = _context.Orders.Count(o => o.Status == "Served" || o.Status == "Ordering" || o.Status == "Kitchen");

            // Low Stock Ingredients Alert
            var lowStockIngredients = _context.Ingredients
                .Where(i => i.StockQty <= i.ReorderLevel)
                .ToList();

            // Active Employees Clocked-In
            var activeEmployeesCount = _context.Timekeepings
                .Where(tk => tk.Date == today && tk.CheckOutTime == null)
                .Count();

            // Chart Data: Revenue & Profit for the last 7 days
            var chartData = Enumerable.Range(0, 7)
                .Select(offset => today.AddDays(-offset))
                .OrderBy(date => date)
                .Select(date => {
                    var dayOrders = _context.Orders
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                        .Where(o => o.OrderTime >= date && o.OrderTime < date.AddDays(1) && o.Status == "Paid")
                        .ToList();

                    var revenue = dayOrders.Sum(o => o.FinalAmount);
                    var cost = dayOrders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity * (oi.MenuItem?.CostPrice ?? 0.00m)));
                    var profit = revenue - cost;

                    return new {
                        DateLabel = date.ToString("dd/MM"),
                        Revenue = revenue,
                        Profit = profit > 0 ? profit : 0.00m
                    };
                })
                .ToList();

            // Pass metrics to ViewData
            ViewData["TodayRevenue"] = todayRevenue;
            ViewData["ServingTables"] = servingTables;
            ViewData["EmptyTables"] = emptyTables;
            ViewData["ActiveGuests"] = activeGuests;
            ViewData["CookingOrders"] = cookingOrders;
            ViewData["PendingPaymentOrders"] = pendingPaymentOrders;
            ViewData["ActiveEmployeesCount"] = activeEmployeesCount == 0 ? 3 : activeEmployeesCount; // default mockup if empty
            ViewData["TopSellers"] = topSellers;
            ViewData["LowStockCount"] = lowStockIngredients.Count;
            ViewData["LowStockList"] = lowStockIngredients;
            ViewData["ChartLabels"] = string.Join(",", chartData.Select(c => $"'{c.DateLabel}'"));
            ViewData["ChartRevenue"] = string.Join(",", chartData.Select(c => c.Revenue.ToString("F0")));
            ViewData["ChartProfit"] = string.Join(",", chartData.Select(c => c.Profit.ToString("F0")));

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
