using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class KitchenController : Controller
    {
        private readonly RestaurantDbContext _context;

        public KitchenController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Kitchen Display Screen
        public IActionResult Index(string section = "Bếp")
        {
            // Sections: "Bếp" (for food categories), "Bar" (for drink category)
            // Food category ID = 1 (Món ăn), 3 (Combo), 4 (Buffet)
            // Drink category ID = 2 (Đồ uống)

            var query = _context.OrderItems
                .Include(oi => oi.MenuItem)
                .ThenInclude(m => m.Category)
                .Include(oi => oi.OrderItemOptions)
                .Include(oi => oi.Order)
                .ThenInclude(o => o.Table)
                .Where(oi => oi.Order.Status == "Kitchen" && oi.Status != "Served");

            if (section == "Bar")
            {
                // Beverages
                query = query.Where(oi => oi.MenuItem.CategoryId == 2);
            }
            else
            {
                // Food & Combos
                query = query.Where(oi => oi.MenuItem.CategoryId != 2);
            }

            var items = query.OrderBy(oi => oi.Order.OrderTime).ToList();

            ViewData["ActiveSection"] = section;
            ViewData["OrderItems"] = items;

            return View();
        }

        // Action: Start cooking (transitions status to Cooking)
        [HttpPost]
        public IActionResult StartCooking(int orderItemId)
        {
            var item = _context.OrderItems.Find(orderItemId);
            if (item != null && item.Status == "Pending")
            {
                item.Status = "Cooking";
                item.CookingStartTime = DateTime.Now;
                _context.SaveChanges();
            }
            // Redirect back retaining section query string if possible
            string section = Request.Form["section"].ToString();
            return RedirectToAction(nameof(Index), new { section = string.IsNullOrEmpty(section) ? "Bếp" : section });
        }

        // Action: Finish cooking (transitions status to Finished)
        [HttpPost]
        public IActionResult FinishCooking(int orderItemId)
        {
            var item = _context.OrderItems.Find(orderItemId);
            if (item != null && item.Status == "Cooking")
            {
                item.Status = "Finished";
                item.CookingEndTime = DateTime.Now;
                _context.SaveChanges();
            }
            string section = Request.Form["section"].ToString();
            return RedirectToAction(nameof(Index), new { section = string.IsNullOrEmpty(section) ? "Bếp" : section });
        }

        // Action: Mark as Served (transitions status to Served)
        [HttpPost]
        public IActionResult MarkAsServed(int orderItemId)
        {
            var item = _context.OrderItems.Include(oi => oi.Order).FirstOrDefault(oi => oi.Id == orderItemId);
            if (item != null && item.Status == "Finished")
            {
                item.Status = "Served";
                _context.SaveChanges();

                // If all items in this order are served, update order status to Served
                var order = item.Order;
                var allItems = _context.OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
                if (allItems.All(oi => oi.Status == "Served"))
                {
                    order.Status = "Served";
                    _context.SaveChanges();
                }
            }
            string section = Request.Form["section"].ToString();
            return RedirectToAction(nameof(Index), new { section = string.IsNullOrEmpty(section) ? "Bếp" : section });
        }
    }
}
