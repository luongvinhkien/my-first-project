using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class PaymentController : Controller
    {
        private readonly RestaurantDbContext _context;

        public PaymentController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Checkout & Billing View
        public IActionResult Index(int? orderId)
        {
            var unpaidOrders = _context.Orders
                .Include(o => o.Table)
                .Where(o => o.Status != "Paid" && o.Status != "Cancelled" && o.OrderItems.Any())
                .ToList();

            Order selectedOrder = null;
            if (orderId.HasValue)
            {
                selectedOrder = _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemOptions)
                    .FirstOrDefault(o => o.Id == orderId.Value);
            }
            else if (unpaidOrders.Any())
            {
                // Fallback to first unpaid order
                selectedOrder = _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemOptions)
                    .FirstOrDefault(o => o.Id == unpaidOrders.First().Id);
            }

            ViewData["UnpaidOrders"] = unpaidOrders;
            ViewData["SelectedOrder"] = selectedOrder;

            return View();
        }

        // AJAX Action: Apply Voucher
        [HttpPost]
        public IActionResult ApplyVoucher(string code, decimal currentTotal)
        {
            var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == code && !v.IsUsed && v.ExpiryDate >= DateTime.Today);
            if (voucher != null)
            {
                decimal discount = 0;
                if (voucher.Type == "Percent")
                {
                    discount = currentTotal * (voucher.Value / 100);
                }
                else if (voucher.Type == "Amount")
                {
                    discount = voucher.Value;
                }

                if (discount > currentTotal) discount = currentTotal;

                return Json(new { success = true, discount = discount, voucherId = voucher.Id, message = "Áp dụng voucher thành công!" });
            }
            return Json(new { success = false, message = "Voucher không hợp lệ hoặc đã hết hạn." });
        }

        // Action: Complete payment, release table, deduct ingredients, add cashflow & CRM points
        [HttpPost]
        public IActionResult ProcessPayment(int orderId, decimal discountAmount, string paymentMethod, string voucherCode, int? customerId)
        {
            var order = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ThenInclude(m => m.Recipes)
                .ThenInclude(r => r.Ingredient)
                .FirstOrDefault(o => o.Id == orderId);

            if (order != null)
            {
                order.Status = "Paid";
                order.DiscountAmount = discountAmount;
                order.FinalAmount = order.TotalAmount - discountAmount;
                if (order.FinalAmount < 0) order.FinalAmount = 0;
                order.PaymentMethod = paymentMethod;
                order.OrderTime = DateTime.Now;

                // 1. Release Table (set to Cleaning)
                if (order.TableId.HasValue)
                {
                    var table = _context.Tables.Find(order.TableId.Value);
                    if (table != null)
                    {
                        table.Status = "Cleaning";
                    }
                }

                // 2. Consume Ingredients from Recipe BOM
                foreach (var item in order.OrderItems)
                {
                    if (item.MenuItem != null && item.MenuItem.Recipes != null)
                    {
                        foreach (var recipe in item.MenuItem.Recipes)
                        {
                            var ingredient = recipe.Ingredient;
                            if (ingredient != null)
                            {
                                // deduct quantity (QuantityNeeded * item.Quantity)
                                decimal quantityToDeduct = recipe.QuantityNeeded * item.Quantity;
                                ingredient.StockQty -= quantityToDeduct;
                                if (ingredient.StockQty < 0) ingredient.StockQty = 0; // prevent negative stock
                            }
                        }
                    }
                }

                // 3. Update Voucher Usage
                if (!string.IsNullOrEmpty(voucherCode))
                {
                    var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == voucherCode);
                    if (voucher != null)
                    {
                        voucher.IsUsed = true;
                    }
                }

                // 4. Update CRM loyalty points (1 point for each 10,000 VND spent)
                if (customerId.HasValue && customerId.Value > 0)
                {
                    var customer = _context.Customers.Find(customerId.Value);
                    if (customer != null)
                    {
                        int newPoints = (int)(order.FinalAmount / 10000);
                        customer.Points += newPoints;

                        // Tier threshold checks
                        if (customer.Points >= 3000) customer.MemberTier = "VIP";
                        else if (customer.Points >= 2000) customer.MemberTier = "Platinum";
                        else if (customer.Points >= 1000) customer.MemberTier = "Gold";
                        else if (customer.Points >= 500) customer.MemberTier = "Silver";
                    }
                }

                // 5. Add CashFlow Record
                var cf = new CashFlow
                {
                    Type = "Receipt",
                    Title = $"Thanh toán hóa đơn bàn {order.Table?.TableNumber ?? "Mang đi"} - HD{order.Id}",
                    Amount = order.FinalAmount,
                    Category = "CustomerPayment",
                    CreatedTime = DateTime.Now,
                    Description = $"Hình thức: {paymentMethod}. Voucher: {voucherCode ?? "Không"}"
                };
                _context.CashFlows.Add(cf);

                // 6. Log security audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "cashier",
                    Action = $"Thanh toán hóa đơn: Bàn {order.Table?.TableNumber ?? "Mang đi"}, Trị giá: {order.FinalAmount:N0}đ",
                    TableName = "Orders",
                    RecordId = orderId,
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();

                // Show Receipt Invoice view
                return RedirectToAction(nameof(Receipt), new { id = order.Id });
            }

            return RedirectToAction(nameof(Index));
        }

        // View Invoice print layout
        public IActionResult Receipt(int id)
        {
            var order = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.OrderItemOptions)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}
