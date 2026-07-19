using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class CustomerOrderController : Controller
    {
        private readonly RestaurantDbContext _context;

        public CustomerOrderController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: /CustomerOrder?token=xxx
        public IActionResult Index(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.ErrorMessage = "Vui lòng quét mã QR tại bàn để truy cập thực đơn.";
                return View("ErrorPage");
            }

            try
            {
                // Decode Token: yyyyMMdd|areaName|tableName
                byte[] data = Convert.FromBase64String(token);
                string decodedString = Encoding.UTF8.GetString(data);
                var parts = decodedString.Split('|');

                if (parts.Length != 3)
                {
                    ViewBag.ErrorMessage = "Mã QR không đúng định dạng.";
                    return View("ErrorPage");
                }

                string dateStr = parts[0];
                string areaName = parts[1];
                string tableName = parts[2];

                // Verify Date (Expired if not today)
                string todayStr = DateTime.Today.ToString("yyyyMMdd");
                if (dateStr != todayStr)
                {
                    ViewBag.ErrorMessage = $"Mã QR này đã HẾT HẠN phục vụ. Vui lòng liên hệ nhân viên để nhận mã QR mới của ngày hôm nay ({DateTime.Today:dd/MM/yyyy}).";
                    return View("ErrorPage");
                }

                // Find Table and Area
                var table = _context.Tables
                    .Include(t => t.Area)
                    .FirstOrDefault(t => t.TableNumber == tableName && t.Area.Name == areaName);

                if (table == null)
                {
                    ViewBag.ErrorMessage = $"Không tìm thấy {tableName} thuộc khu vực '{areaName}' trong hệ thống.";
                    return View("ErrorPage");
                }

                // Fetch Categories and active Menu Items
                var categories = _context.Categories.ToList();
                var menuItems = _context.MenuItems
                    .Include(m => m.MenuItemOptions)
                    .Where(m => m.IsActive)
                    .ToList();

                // Get active order (if table is eating)
                var activeOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.TableId == table.Id && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                ViewBag.Token = token;
                ViewBag.Table = table;
                ViewBag.Categories = categories;
                ViewBag.MenuItems = menuItems;
                ViewBag.ActiveOrder = activeOrder;

                return View();
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Lỗi giải mã Token. Mã QR không hợp lệ.";
                return View("ErrorPage");
            }
        }

        // POST: /CustomerOrder/SubmitOrder
        [HttpPost]
        public IActionResult SubmitOrder([FromBody] SubmitOrderModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Token) || model.Items == null || model.Items.Count == 0)
            {
                return Json(new { success = false, message = "Dữ liệu gửi lên không hợp lệ." });
            }

            try
            {
                // Decode and Validate Token
                byte[] data = Convert.FromBase64String(model.Token);
                string decodedString = Encoding.UTF8.GetString(data);
                var parts = decodedString.Split('|');

                if (parts.Length != 3)
                {
                    return Json(new { success = false, message = "Mã token không đúng định dạng." });
                }

                string dateStr = parts[0];
                string areaName = parts[1];
                string tableName = parts[2];

                // Verify Date
                string todayStr = DateTime.Today.ToString("yyyyMMdd");
                if (dateStr != todayStr)
                {
                    return Json(new { success = false, message = "Mã QR đã hết hạn sử dụng của ngày hôm nay." });
                }

                // Find Table
                var table = _context.Tables
                    .Include(t => t.Area)
                    .FirstOrDefault(t => t.TableNumber == tableName && t.Area.Name == areaName);

                if (table == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bàn phục vụ tương ứng." });
                }

                // Find active unpaid order, or create new one
                var activeOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == table.Id && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (activeOrder == null)
                {
                    activeOrder = new Order
                    {
                        TableId = table.Id,
                        Status = "Ordering",
                        OrderTime = DateTime.Now,
                        TotalAmount = 0,
                        DiscountAmount = 0,
                        FinalAmount = 0,
                        Note = "Khách hàng tự quét QR đặt món"
                    };
                    _context.Orders.Add(activeOrder);
                    
                    // Mark table status as serving
                    table.Status = "Serving";
                    _context.SaveChanges();
                }

                // Add Items to Order
                foreach (var submitItem in model.Items)
                {
                    var menuItem = _context.MenuItems.Find(submitItem.MenuItemId);
                    if (menuItem == null) continue;

                    // Create OrderItem, automatically set status to 'Cooking' so it pushes to kitchen screen!
                    var orderItem = new OrderItem
                    {
                        OrderId = activeOrder.Id,
                        MenuItemId = submitItem.MenuItemId,
                        Quantity = submitItem.Quantity,
                        Price = menuItem.Price,
                        Notes = submitItem.Notes ?? "",
                        Status = "Cooking", // Send straight to kitchen queue!
                        CookingStartTime = DateTime.Now
                    };
                    _context.OrderItems.Add(orderItem);
                    _context.SaveChanges(); // Save to generate OrderItemId

                    // Add Custom options if selected
                    if (submitItem.Options != null && submitItem.Options.Count > 0)
                    {
                        foreach (var opt in submitItem.Options)
                        {
                            var orderItemOption = new OrderItemOption
                            {
                                OrderItemId = orderItem.Id,
                                OptionName = opt.OptionName,
                                Price = opt.Price
                            };
                            _context.OrderItemOptions.Add(orderItemOption);
                        }
                        _context.SaveChanges();
                    }
                }

                // Recalculate totals
                RecalculateOrderTotals(activeOrder);

                return Json(new { success = true, message = "Đã gửi yêu cầu gọi món vào nhà bếp thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        private void RecalculateOrderTotals(Order order)
        {
            decimal total = 0;
            var items = _context.OrderItems
                .Include(oi => oi.OrderItemOptions)
                .Where(oi => oi.OrderId == order.Id)
                .ToList();

            foreach (var item in items)
            {
                decimal itemTotal = item.Price;
                if (item.OrderItemOptions != null)
                {
                    itemTotal += item.OrderItemOptions.Sum(o => o.Price);
                }
                total += itemTotal * item.Quantity;
            }

            order.TotalAmount = total;
            order.FinalAmount = Math.Max(0, total - order.DiscountAmount);
            _context.SaveChanges();
        }
    }

    // Models for POST binding
    public class SubmitOrderModel
    {
        public string Token { get; set; }
        public List<SubmitItemModel> Items { get; set; }
    }

    public class SubmitItemModel
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public List<SubmitOptionModel> Options { get; set; }
    }

    public class SubmitOptionModel
    {
        public string OptionName { get; set; }
        public decimal Price { get; set; }
    }
}
