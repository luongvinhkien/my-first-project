using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiOrderController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public ApiOrderController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/ApiOrder/table/{tableId}
        [HttpGet("table/{tableId}")]
        public IActionResult GetActiveOrder(int tableId)
        {
            try
            {
                var table = _context.Tables.Find(tableId);
                if (table == null)
                    return NotFound(new { message = "Table not found" });

                var activeOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.OrderItemOptions)
                    .Where(o => o.TableId == tableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (activeOrder == null)
                {
                    // Auto-create active order just like Web controller
                    activeOrder = new Order
                    {
                        TableId = tableId,
                        Status = "Ordering",
                        OrderTime = DateTime.Now,
                        TotalAmount = 0,
                        DiscountAmount = 0,
                        FinalAmount = 0
                    };
                    _context.Orders.Add(activeOrder);
                    table.Status = "Serving";
                    _context.SaveChanges();
                }

                return Ok(FormatOrder(activeOrder));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/add-item
        [HttpPost("add-item")]
        public IActionResult AddItemToOrder([FromBody] AddItemModel model)
        {
            if (model == null || model.TableId <= 0 || model.MenuItemId <= 0 || model.Quantity <= 0)
                return BadRequest(new { message = "Invalid input parameters" });

            try
            {
                // Find active order
                var activeOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == model.TableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (activeOrder == null)
                {
                    activeOrder = new Order
                    {
                        TableId = model.TableId,
                        Status = "Ordering",
                        OrderTime = DateTime.Now
                    };
                    _context.Orders.Add(activeOrder);
                    
                    var table = _context.Tables.Find(model.TableId);
                    if (table != null) table.Status = "Serving";
                    
                    _context.SaveChanges();
                }

                var menuItem = _context.MenuItems.Find(model.MenuItemId);
                if (menuItem == null)
                    return NotFound(new { message = "Menu item not found" });

                // Create OrderItem
                var orderItem = new OrderItem
                {
                    OrderId = activeOrder.Id,
                    MenuItemId = model.MenuItemId,
                    Quantity = model.Quantity,
                    Price = menuItem.Price,
                    Notes = model.Notes,
                    Status = "Pending"
                };
                _context.OrderItems.Add(orderItem);
                _context.SaveChanges();

                // Add options if any
                decimal optionsTotal = 0;
                if (model.Options != null && model.Options.Count > 0)
                {
                    foreach (var opt in model.Options)
                    {
                        var orderItemOption = new OrderItemOption
                        {
                            OrderItemId = orderItem.Id,
                            OptionName = opt.OptionName,
                            Price = opt.Price
                        };
                        _context.OrderItemOptions.Add(orderItemOption);
                        optionsTotal += opt.Price;
                    }
                    _context.SaveChanges();
                }

                // Update order amounts
                RecalculateOrder(activeOrder);

                return Ok(new { message = "Item added successfully", order = FormatOrder(activeOrder) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/update-qty
        [HttpPost("update-qty")]
        public IActionResult UpdateQuantity([FromBody] UpdateQtyModel model)
        {
            if (model == null || model.OrderItemId <= 0 || model.Quantity <= 0)
                return BadRequest(new { message = "Invalid parameters" });

            try
            {
                var orderItem = _context.OrderItems.Find(model.OrderItemId);
                if (orderItem == null)
                    return NotFound(new { message = "Order item not found" });

                orderItem.Quantity = model.Quantity;
                _context.SaveChanges();

                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.Id == orderItem.OrderId);
                
                if (order != null)
                {
                    RecalculateOrder(order);
                }

                return Ok(new { message = "Quantity updated", order = FormatOrder(order) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/remove-item
        [HttpPost("remove-item")]
        public IActionResult RemoveItem([FromBody] RemoveItemModel model)
        {
            if (model == null || model.OrderItemId <= 0)
                return BadRequest(new { message = "Invalid parameters" });

            try
            {
                var orderItem = _context.OrderItems
                    .Include(oi => oi.OrderItemOptions)
                    .FirstOrDefault(oi => oi.Id == model.OrderItemId);

                if (orderItem == null)
                    return NotFound(new { message = "Order item not found" });

                var orderId = orderItem.OrderId;

                // Delete options first due to foreign keys
                if (orderItem.OrderItemOptions != null)
                {
                    _context.OrderItemOptions.RemoveRange(orderItem.OrderItemOptions);
                }
                _context.OrderItems.Remove(orderItem);
                _context.SaveChanges();

                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefault(o => o.Id == orderId);
                
                if (order != null)
                {
                    RecalculateOrder(order);
                }

                return Ok(new { message = "Item removed", order = FormatOrder(order) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/send-to-kitchen
        [HttpPost("send-to-kitchen")]
        public IActionResult SendToKitchen([FromBody] SendKitchenModel model)
        {
            if (model == null || model.TableId <= 0)
                return BadRequest(new { message = "Invalid parameters" });

            try
            {
                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == model.TableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (order == null)
                    return NotFound(new { message = "Active order not found" });

                order.Status = "Kitchen";
                foreach (var item in order.OrderItems)
                {
                    if (item.Status == "Pending")
                    {
                        item.Status = "Cooking";
                    }
                }
                _context.SaveChanges();

                return Ok(new { message = "Order sent to kitchen", order = FormatOrder(order) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/pay
        [HttpPost("pay")]
        public IActionResult ProcessPayment([FromBody] PayModel model)
        {
            if (model == null || model.TableId <= 0)
                return BadRequest(new { message = "Invalid parameters" });

            try
            {
                var order = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == model.TableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (order == null)
                    return NotFound(new { message = "Active order not found" });

                order.Status = "Paid";
                order.PaymentMethod = string.IsNullOrEmpty(model.PaymentMethod) ? "Cash" : model.PaymentMethod;
                order.DiscountAmount = model.DiscountAmount >= 0 ? model.DiscountAmount : 0;
                
                // Recalculate with discount
                order.FinalAmount = Math.Max(0, order.TotalAmount - order.DiscountAmount);

                // Set table back to empty
                var table = _context.Tables.Find(model.TableId);
                if (table != null)
                {
                    table.Status = "Empty";
                }

                // Add to CashFlow logs
                var cashFlow = new CashFlow
                {
                    Type = "Receipt",
                    Title = $"Thanh toán bàn {table?.TableNumber ?? order.TableId.ToString()}",
                    Amount = order.FinalAmount,
                    Category = "CustomerPayment",
                    Description = $"Thanh toán hóa đơn bàn {table?.TableNumber ?? order.TableId.ToString()} (Mobile)",
                    CreatedTime = DateTime.Now
                };
                _context.CashFlows.Add(cashFlow);

                _context.SaveChanges();

                return Ok(new { message = "Payment successful", order = FormatOrder(order) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/ApiOrder/kitchen-queue
        [HttpGet("kitchen-queue")]
        public IActionResult GetKitchenQueue()
        {
            try
            {
                var items = _context.OrderItems
                    .Include(oi => oi.MenuItem)
                    .Include(oi => oi.Order)
                    .ThenInclude(o => o.Table)
                    .Include(oi => oi.OrderItemOptions)
                    .Where(oi => oi.Status == "Cooking" || oi.Status == "Pending")
                    .OrderBy(oi => oi.Order.OrderTime)
                    .Select(oi => new
                    {
                        oi.Id,
                        oi.OrderId,
                        TableName = oi.Order.Table.TableNumber,
                        FoodName = oi.MenuItem.Name,
                        oi.Quantity,
                        oi.Notes,
                        oi.Status,
                        OrderTime = oi.Order.OrderTime.ToString("HH:mm")
                    }).ToList();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiOrder/kitchen-finish
        [HttpPost("kitchen-finish")]
        public IActionResult FinishCooking([FromBody] FinishCookingModel model)
        {
            if (model == null || model.OrderItemId <= 0)
                return BadRequest(new { message = "Invalid parameters" });

            try
            {
                var item = _context.OrderItems.Find(model.OrderItemId);
                if (item == null)
                    return NotFound(new { message = "Order item not found" });

                item.Status = "Finished";
                _context.SaveChanges();

                return Ok(new { message = "Item marked as finished cooking" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private void RecalculateOrder(Order order)
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

        private object FormatOrder(Order order)
        {
            if (order == null) return null;

            return new
            {
                order.Id,
                order.TableId,
                order.Status,
                order.OrderTime,
                order.TotalAmount,
                order.DiscountAmount,
                order.FinalAmount,
                order.PaymentMethod,
                order.Note,
                Items = order.OrderItems?.Select(oi => new
                {
                    oi.Id,
                    oi.MenuItemId,
                    FoodName = oi.MenuItem?.Name ?? "N/A",
                    oi.Quantity,
                    oi.Price,
                    Subtotal = (oi.Price + (oi.OrderItemOptions?.Sum(o => o.Price) ?? 0)) * oi.Quantity,
                    oi.Notes,
                    oi.Status,
                    Options = oi.OrderItemOptions?.Select(o => new
                    {
                        o.Id,
                        o.OptionName,
                        o.Price
                    }).ToList()
                }).ToList()
            };
        }

        public class AddItemModel
        {
            public int TableId { get; set; }
            public int MenuItemId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
            public List<OptionModel> Options { get; set; }
        }

        public class OptionModel
        {
            public string OptionName { get; set; }
            public decimal Price { get; set; }
        }

        public class UpdateQtyModel
        {
            public int OrderItemId { get; set; }
            public int Quantity { get; set; }
        }

        public class RemoveItemModel
        {
            public int OrderItemId { get; set; }
        }

        public class SendKitchenModel
        {
            public int TableId { get; set; }
        }

        public class PayModel
        {
            public int TableId { get; set; }
            public string PaymentMethod { get; set; }
            public decimal DiscountAmount { get; set; }
        }

        public class FinishCookingModel
        {
            public int OrderItemId { get; set; }
        }
    }
}
