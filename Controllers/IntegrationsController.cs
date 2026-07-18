using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class IntegrationsController : Controller
    {
        private readonly RestaurantDbContext _context;

        public IntegrationsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Integrations, AI & Online view
        public IActionResult Index()
        {
            // 1. Calculate AI Forecasts
            var today = DateTime.Today;
            var pastSales = _context.Orders
                .Where(o => o.Status == "Paid" && o.OrderTime >= today.AddDays(-7))
                .Select(o => o.FinalAmount)
                .ToList();

            decimal averageDailyRevenue = pastSales.Any() ? pastSales.Average() : 450000.00m;
            decimal predictedTomorrowRevenue = averageDailyRevenue * 1.05m; // Simple statistical trend
            decimal predictedWeeklyRevenue = averageDailyRevenue * 7 * 0.98m;

            // Suggesting raw ingredient order quantities based on top menu item sales
            var topMenuItem = _context.OrderItems
                .Include(oi => oi.MenuItem)
                .GroupBy(oi => oi.MenuItem.Name)
                .Select(g => new { Name = g.Key, Qty = g.Sum(oi => oi.Quantity) })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefault();

            string recommendationNote = "Xu hướng bán hàng ổn định. Không cần nhập thêm nguyên liệu đột xuất.";
            if (topMenuItem != null && topMenuItem.Qty > 5)
            {
                recommendationNote = $"Món '{topMenuItem.Name}' đang bán chạy nhất tuần. Đề xuất tăng lượng nhập nguyên liệu liên quan thêm 15% cho tuần sau.";
            }

            // Notification logs simulator (fake database cache or just static log array)
            var notifications = new List<NotificationLog>
            {
                new NotificationLog { Time = DateTime.Now.AddMinutes(-5), Channel = "Zalo", Target = "0901234567", Content = "KiotViet F&B: Cảm ơn Nguyễn Văn A đã check-in thành công tại Bàn 05!" },
                new NotificationLog { Time = DateTime.Now.AddHours(-2), Channel = "SMS", Target = "0912345678", Content = "KiotViet F&B: Lịch đặt bàn lúc 18h ngày mai của bạn đã được xác nhận." },
                new NotificationLog { Time = DateTime.Now.AddHours(-4), Channel = "Email", Target = "a.nguyen@gmail.com", Content = "Hóa đơn điện tử HD-10 của bạn đã được lập trị giá 214,000đ. Cảm ơn quý khách!" }
            };

            ViewData["AvgDailySales"] = averageDailyRevenue;
            ViewData["ForecastTomorrow"] = predictedTomorrowRevenue;
            ViewData["ForecastWeek"] = predictedWeeklyRevenue;
            ViewData["AIRecommendation"] = recommendationNote;
            ViewData["NotificationLogs"] = notifications;

            return View();
        }

        // Action: NLP Chatbot Query Interface (Interactive Natural Language Query Simulator)
        [HttpPost]
        public IActionResult ChatbotQuery(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return Json(new { reply = "Tôi có thể giúp gì cho bạn? Hãy nhập câu hỏi." });
            }

            string reply = "";
            string cleanPrompt = prompt.ToLower().Trim();

            try
            {
                if (cleanPrompt.Contains("doanh thu") || cleanPrompt.Contains("doanh so"))
                {
                    var today = DateTime.Today;
                    var todayRevenue = _context.Orders
                        .Where(o => o.OrderTime >= today && o.Status == "Paid")
                        .Sum(o => (decimal?)o.FinalAmount) ?? 0;

                    reply = $"Doanh thu hôm nay đạt được là: **{todayRevenue:N0}đ** từ các hóa đơn đã hoàn tất thanh toán.";
                }
                else if (cleanPrompt.Contains("bàn") || cleanPrompt.Contains("phục vụ"))
                {
                    var serving = _context.Tables.Count(t => t.Status == "Serving");
                    var empty = _context.Tables.Count(t => t.Status == "Empty");
                    var reserved = _context.Tables.Count(t => t.Status == "Reserved");
                    
                    reply = $"Hiện tại có **{serving} bàn** đang phục vụ khách, **{empty} bàn trống**, và **{reserved} bàn đặt trước**.";
                }
                else if (cleanPrompt.Contains("món bán chạy") || cleanPrompt.Contains("best seller"))
                {
                    var best = _context.OrderItems
                        .Include(oi => oi.MenuItem)
                        .GroupBy(oi => oi.MenuItem.Name)
                        .Select(g => new { Name = g.Key, Qty = g.Sum(oi => oi.Quantity) })
                        .OrderByDescending(x => x.Qty)
                        .FirstOrDefault();

                    if (best != null)
                    {
                        reply = $"Món bán chạy nhất trên hệ thống là: **{best.Name}** với tổng cộng **{best.Qty} phần** đã bán ra.";
                    }
                    else
                    {
                        reply = "Hiện chưa có món ăn nào được bán ra hôm nay.";
                    }
                }
                else if (cleanPrompt.Contains("kho") || cleanPrompt.Contains("nguyên liệu"))
                {
                    var low = _context.Ingredients.Where(i => i.StockQty <= i.ReorderLevel).ToList();
                    if (low.Any())
                    {
                        reply = $"Cảnh báo hết kho! Có **{low.Count} nguyên liệu** sắp hết bao gồm: " + string.Join(", ", low.Select(l => $"{l.Name} (tồn {l.StockQty:N2} {l.Unit})")) + ". Vui lòng nhập thêm hàng.";
                    }
                    else
                    {
                        reply = "Tồn kho các nguyên liệu hiện tại đều ở mức an toàn.";
                    }
                }
                else if (cleanPrompt.Contains("nhân viên") || cleanPrompt.Contains("ca làm"))
                {
                    var active = _context.Timekeepings.Include(tk => tk.Employee).Where(tk => tk.Date == DateTime.Today && tk.CheckOutTime == null).ToList();
                    if (active.Any())
                    {
                        reply = $"Có **{active.Count} nhân viên** đang làm việc hôm nay: " + string.Join(", ", active.Select(a => $"{a.Employee.FullName} (checkin {a.CheckInTime:HH:mm})")) + ".";
                    }
                    else
                    {
                        reply = "Hiện tại chưa ghi nhận ca nhân viên nào chấm công check-in hôm nay.";
                    }
                }
                else
                {
                    reply = "Xin lỗi, tôi chưa hiểu câu hỏi của bạn. Tôi hỗ trợ tra cứu các thông tin: *doanh thu hôm nay*, *trạng thái bàn*, *món bán chạy*, *tồn kho nguyên liệu*, *nhân viên trực ca*.";
                }
            }
            catch (Exception ex)
            {
                reply = $"Lỗi xử lý truy vấn dữ liệu: {ex.Message}";
            }

            return Json(new { reply = reply });
        }

        // Action: Push Simulated Delivery / QR Order
        [HttpPost]
        public IActionResult SimulateOnlineOrder(string source)
        {
            // Sources: GrabFood, ShopeeFood, QR Order (Table 1)
            var table = _context.Tables.FirstOrDefault(t => t.TableNumber == "Bàn 01");
            var item = _context.MenuItems.FirstOrDefault(m => m.Code == "F001"); // Pho Bo

            if (item != null)
            {
                // Create order status Kitchen directly
                var order = new Order
                {
                    TableId = source == "QR Order" ? (table != null ? (int?)table.Id : null) : null,
                    OrderTime = DateTime.Now,
                    Status = "Kitchen",
                    TotalAmount = item.Price,
                    FinalAmount = item.Price,
                    Note = $"Đơn hàng online từ nguồn: {source}"
                };
                _context.Orders.Add(order);
                _context.SaveChanges();

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = item.Id,
                    Quantity = 1,
                    Price = item.Price,
                    Notes = $"Simulated {source} order",
                    Status = "Pending"
                };
                _context.OrderItems.Add(orderItem);

                if (source == "QR Order" && table != null)
                {
                    table.Status = "Serving";
                }

                _context.SaveChanges();

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "system",
                    Action = $"Nhận đơn online từ nguồn {source}, Order ID: {order.Id}",
                    TableName = "Orders",
                    RecordId = order.Id,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();

                return Json(new { success = true, orderId = order.Id });
            }

            return Json(new { success = false, message = "Không tìm thấy món ăn F001 để mô phỏng." });
        }
    }

    public class NotificationLog
    {
        public DateTime Time { get; set; }
        public string Channel { get; set; }
        public string Target { get; set; }
        public string Content { get; set; }
    }
}
