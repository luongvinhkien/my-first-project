using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class OrderController : Controller
    {
        private readonly RestaurantDbContext _context;

        public OrderController(RestaurantDbContext context)
        {
            _context = context;
        }

        // POS Screen View
        public IActionResult Index(int? tableId)
        {
            var categories = _context.Categories.ToList();
            var menuItems = _context.MenuItems.Include(m => m.MenuItemOptions).Where(m => m.IsActive).ToList();
            var activeTables = _context.Tables.Where(t => t.Status == "Serving").ToList();
            var areas = _context.Areas.Include(a => a.Tables).ToList();
            ViewData["Areas"] = areas;

            Order activeOrder = null;
            Table currentTable = null;

            if (tableId.HasValue)
            {
                currentTable = _context.Tables.Find(tableId.Value);
                if (currentTable != null)
                {
                    // Find active unpaid order
                    activeOrder = _context.Orders
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.OrderItemOptions)
                        .Where(o => o.TableId == tableId.Value && o.Status != "Paid" && o.Status != "Cancelled")
                        .OrderByDescending(o => o.OrderTime)
                        .FirstOrDefault();

                    // If table is serving but has no order, create one
                    if (activeOrder == null)
                    {
                        activeOrder = new Order
                        {
                            TableId = tableId.Value,
                            OrderTime = DateTime.Now,
                            Status = "Ordering",
                            TotalAmount = 0,
                            FinalAmount = 0
                        };
                        _context.Orders.Add(activeOrder);
                        _context.SaveChanges();
                    }
                }
            }

            ViewData["Categories"] = categories;
            ViewData["MenuItems"] = menuItems;
            ViewData["ActiveTables"] = activeTables;
            ViewData["CurrentTable"] = currentTable;
            ViewData["ActiveOrder"] = activeOrder;

            return View();
        }

        // AJAX Action: Add item to order
        [HttpPost]
        public IActionResult AddItem(int orderId, int menuItemId, int quantity, string notes, string selectedOptions)
        {
            var order = _context.Orders.Find(orderId);
            var menuItem = _context.MenuItems.Include(m => m.MenuItemOptions).FirstOrDefault(m => m.Id == menuItemId);

            if (order != null && menuItem != null)
            {
                decimal itemPrice = menuItem.Price;
                var optionList = new List<OrderItemOption>();

                // Parse selected options (format: "Option1:price1,Option2:price2")
                if (!string.IsNullOrEmpty(selectedOptions))
                {
                    var optChunks = selectedOptions.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var chunk in optChunks)
                      {
                        var parts = chunk.Split(':');
                        if (parts.Length == 2)
                        {
                            var optName = parts[0];
                            if (decimal.TryParse(parts[1], out decimal optPrice))
                            {
                                itemPrice += optPrice;
                                optionList.Add(new OrderItemOption
                                {
                                    OptionName = optName,
                                    Price = optPrice
                                });
                            }
                        }
                    }
                }

                // Check if identical item (same menuitem, notes and options) exists
                var existingItem = _context.OrderItems
                    .Include(oi => oi.OrderItemOptions)
                    .Where(oi => oi.OrderId == orderId && oi.MenuItemId == menuItemId && oi.Notes == notes && oi.Status == "Pending")
                    .ToList()
                    .FirstOrDefault(oi => 
                        oi.OrderItemOptions.Count == optionList.Count && 
                        oi.OrderItemOptions.All(oio => optionList.Any(ol => ol.OptionName == oio.OptionName))
                    );

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    var newItem = new OrderItem
                    {
                        OrderId = orderId,
                        MenuItemId = menuItemId,
                        Quantity = quantity,
                        Price = itemPrice,
                        Notes = notes,
                        Status = "Pending", // Wait to be sent to kitchen
                        OrderItemOptions = optionList
                    };
                    _context.OrderItems.Add(newItem);
                }

                _context.SaveChanges();
                RecalculateOrderTotals(orderId);
                
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy Order hoặc Món ăn" });
        }

        // AJAX Action: Update quantity of order item
        [HttpPost]
        public IActionResult UpdateItemQty(int orderItemId, int quantity)
        {
            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem != null)
            {
                if (quantity <= 0)
                {
                    _context.OrderItems.Remove(orderItem);
                }
                else
                {
                    orderItem.Quantity = quantity;
                }
                _context.SaveChanges();
                RecalculateOrderTotals(orderItem.OrderId);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // AJAX Action: Remove item from order
        [HttpPost]
        public IActionResult RemoveItem(int orderItemId)
        {
            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem != null)
            {
                int orderId = orderItem.OrderId;
                _context.OrderItems.Remove(orderItem);
                _context.SaveChanges();
                RecalculateOrderTotals(orderId);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // Action: Send Order to Kitchen (Status Ordering/Pending -> Kitchen/Cooking)
        [HttpPost]
        public IActionResult SendToKitchen(int orderId)
        {
            var order = _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.Status = "Kitchen";

                foreach (var item in order.OrderItems)
                {
                    if (item.Status == "Pending")
                    {
                        item.Status = "Cooking";
                        item.CookingStartTime = DateTime.Now;
                    }
                }

                _context.SaveChanges();

                // Log audit trail
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "cashier",
                    Action = $"Gửi đơn chế biến nhà bếp, Order ID: {orderId}",
                    TableName = "Orders",
                    RecordId = orderId,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
            }
            
            return RedirectToAction(nameof(Index), new { tableId = order?.TableId });
        }

        private void RecalculateOrderTotals(int orderId)
        {
            var order = _context.Orders.Include(o => o.OrderItems).FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                order.TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.Price);
                order.FinalAmount = order.TotalAmount - order.DiscountAmount;
                _context.SaveChanges();
            }
        }
    }
}
