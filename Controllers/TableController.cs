using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class TableController : Controller
    {
        private readonly RestaurantDbContext _context;

        public TableController(RestaurantDbContext context)
        {
            _context = context;
        }

        // View Table Map & Reservations
        public IActionResult Index(DateTime? date)
        {
            var filterDate = date ?? DateTime.Today;
            ViewData["SelectedDate"] = filterDate.ToString("yyyy-MM-dd");
            var isToday = filterDate.Date == DateTime.Today;

            var areas = _context.Areas.Include(a => a.Tables).ToList();
            
            // Filter reservations for the selected date
            var startOfDay = filterDate.Date;
            var endOfDay = startOfDay.AddDays(1);
            var reservations = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Include(r => r.Area)
                .Where(r => r.ReservationTime >= startOfDay && r.ReservationTime < endOfDay)
                .OrderBy(r => r.ReservationTime)
                .ToList();

            // Build a set of table IDs that have pending reservations on this date
            var reservedTableIds = reservations
                .Where(r => r.Status == "Pending" && r.TableId.HasValue)
                .Select(r => r.TableId.Value)
                .ToHashSet();

            // Dynamic status override for map display
            foreach (var area in areas)
            {
                foreach (var table in area.Tables)
                {
                    if (table.Status == "Locked") continue;

                    if (reservedTableIds.Contains(table.Id))
                    {
                        table.Status = "Reserved";
                    }
                    else
                    {
                        if (isToday)
                        {
                            if (table.Status == "Reserved")
                            {
                                table.Status = "Empty";
                            }
                        }
                        else
                        {
                            table.Status = "Empty";
                        }
                    }
                }
            }

            var emptyTables = _context.Tables.Where(t => t.Status == "Empty").ToList();
            var servingTables = _context.Tables.Where(t => t.Status == "Serving").ToList();

            ViewData["Areas"] = areas;
            ViewData["Reservations"] = reservations;
            ViewData["EmptyTables"] = emptyTables;
            ViewData["ServingTables"] = servingTables;

            return View();
        }

        [HttpPost]
        public IActionResult CreateArea(string areaName)
        {
            if (!string.IsNullOrEmpty(areaName))
            {
                var area = new Area { Name = areaName.Trim() };
                _context.Areas.Add(area);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CreateTable(string tableNumber, int areaId, int capacity)
        {
            if (!string.IsNullOrEmpty(tableNumber) && areaId > 0 && capacity > 0)
            {
                var table = new Table {
                    TableNumber = tableNumber.Trim(),
                    AreaId = areaId,
                    Capacity = capacity,
                    Status = "Empty"
                };
                _context.Tables.Add(table);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Action: Open a Table (Status Empty -> Serving)
        [HttpPost]
        public IActionResult OpenTable(int tableId)
        {
            var table = _context.Tables.Find(tableId);
            if (table != null && table.Status == "Empty")
            {
                table.Status = "Serving";
                
                // Create an active order for this table
                var order = new Order
                {
                    TableId = tableId,
                    OrderTime = DateTime.Now,
                    Status = "Ordering",
                    TotalAmount = 0,
                    FinalAmount = 0
                };
                _context.Orders.Add(order);
                _context.SaveChanges();

                // Log audit trail
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "cashier",
                    Action = $"Mở bàn: {table.TableNumber}",
                    TableName = "Tables",
                    RecordId = tableId,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Clean Table (Status Cleaning -> Empty)
        [HttpPost]
        public IActionResult CleanTable(int tableId)
        {
            var table = _context.Tables.Find(tableId);
            if (table != null && table.Status == "Cleaning")
            {
                table.Status = "Empty";
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Toggle Lock Table (Empty <-> Locked)
        [HttpPost]
        public IActionResult ToggleLockTable(int tableId)
        {
            var table = _context.Tables.Find(tableId);
            if (table != null)
            {
                if (table.Status == "Locked")
                {
                    table.Status = "Empty";
                }
                else if (table.Status == "Empty")
                {
                    table.Status = "Locked";
                }
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Transfer Table (Chuyển bàn)
        [HttpPost]
        public IActionResult TransferTable(int fromTableId, int toTableId)
        {
            var fromTable = _context.Tables.Find(fromTableId);
            var toTable = _context.Tables.Find(toTableId);

            if (fromTable != null && toTable != null && fromTable.Status == "Serving" && toTable.Status == "Empty")
            {
                // Find active order of fromTable
                var activeOrder = _context.Orders
                    .Where(o => o.TableId == fromTableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .OrderByDescending(o => o.OrderTime)
                    .FirstOrDefault();

                if (activeOrder != null)
                {
                    activeOrder.TableId = toTableId;
                    fromTable.Status = "Empty";
                    toTable.Status = "Serving";
                    _context.SaveChanges();

                    // Log audit
                    _context.AuditLogs.Add(new AuditLog
                    {
                        Username = "cashier",
                        Action = $"Chuyển bàn từ {fromTable.TableNumber} sang {toTable.TableNumber}",
                        TableName = "Orders",
                        RecordId = activeOrder.Id,
                        Timestamp = DateTime.Now
                    });
                    _context.SaveChanges();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Merge Tables (Gộp bàn)
        [HttpPost]
        public IActionResult MergeTables(int sourceTableId, int targetTableId)
        {
            var sourceTable = _context.Tables.Find(sourceTableId);
            var targetTable = _context.Tables.Find(targetTableId);

            if (sourceTable != null && targetTable != null && sourceTable.Status == "Serving" && targetTable.Status == "Serving")
            {
                var sourceOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == sourceTableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .FirstOrDefault();

                var targetOrder = _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.TableId == targetTableId && o.Status != "Paid" && o.Status != "Cancelled")
                    .FirstOrDefault();

                if (sourceOrder != null && targetOrder != null)
                {
                    // Move all items from sourceOrder to targetOrder
                    foreach (var item in sourceOrder.OrderItems)
                    {
                        // Check if item already exists in targetOrder, merge quantity
                        var existingItem = targetOrder.OrderItems
                            .FirstOrDefault(oi => oi.MenuItemId == item.MenuItemId && oi.Notes == item.Notes && oi.Status == item.Status);

                        if (existingItem != null)
                        {
                            existingItem.Quantity += item.Quantity;
                        }
                        else
                        {
                            item.OrderId = targetOrder.Id;
                        }
                    }

                    // Recalculate target order totals
                    targetOrder.TotalAmount = targetOrder.OrderItems.Sum(oi => oi.Quantity * oi.Price);
                    targetOrder.FinalAmount = targetOrder.TotalAmount - targetOrder.DiscountAmount;

                    // Delete source order and make source table Empty/Cleaning
                    _context.Orders.Remove(sourceOrder);
                    sourceTable.Status = "Cleaning";
                    _context.SaveChanges();

                    // Log audit
                    _context.AuditLogs.Add(new AuditLog
                    {
                        Username = "cashier",
                        Action = $"Gộp bàn {sourceTable.TableNumber} vào {targetTable.TableNumber}",
                        TableName = "Orders",
                        RecordId = targetOrder.Id,
                        Timestamp = DateTime.Now
                    });
                    _context.SaveChanges();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Reservation Booking (Đặt bàn trước)
        [HttpPost]
        public IActionResult CreateReservation(string customerName, string customerPhone, string customerEmail, int guestCount, DateTime reservationTime, int? tableId, int? areaId, decimal deposit, string note)
        {
            // 1. Find or create customer
            var customer = _context.Customers.FirstOrDefault(c => c.Phone == customerPhone);
            if (customer == null)
            {
                customer = new Customer
                {
                    Name = customerName,
                    Phone = customerPhone,
                    Email = customerEmail,
                    MemberTier = "Silver"
                };
                _context.Customers.Add(customer);
                _context.SaveChanges();
            }

            // 2. Create reservation
            var res = new Reservation
            {
                CustomerId = customer.Id,
                TableId = tableId == 0 ? null : tableId,
                AreaId = areaId == 0 ? null : areaId,
                GuestCount = guestCount,
                ReservationTime = reservationTime,
                DepositAmount = deposit,
                Status = "Pending",
                Note = note
            };

            // If table was selected, mark it as Reserved
            if (tableId.HasValue && tableId.Value > 0)
            {
                var table = _context.Tables.Find(tableId.Value);
                if (table != null)
                {
                    table.Status = "Reserved";
                }
            }

            _context.Reservations.Add(res);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // Action: Check-in Reservation (Khách đến check-in)
        [HttpPost]
        public IActionResult CheckInReservation(int reservationId)
        {
            var res = _context.Reservations.Include(r => r.Table).FirstOrDefault(r => r.Id == reservationId);
            if (res != null && res.Status == "Pending")
            {
                res.Status = "CheckedIn";
                
                // If table is associated, make it Serving and create Order
                if (res.TableId.HasValue)
                {
                    var table = _context.Tables.Find(res.TableId.Value);
                    if (table != null)
                    {
                        table.Status = "Serving";

                        // Create order
                        var order = new Order
                        {
                            TableId = table.Id,
                            CustomerId = res.CustomerId,
                            OrderTime = DateTime.Now,
                            Status = "Ordering",
                            TotalAmount = 0,
                            FinalAmount = 0,
                            Note = $"Checked in from reservation. Deposit: {res.DepositAmount:N0}đ"
                        };
                        _context.Orders.Add(order);
                    }
                }
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Cancel Reservation
        [HttpPost]
        public IActionResult CancelReservation(int reservationId)
        {
            var res = _context.Reservations.Include(r => r.Table).FirstOrDefault(r => r.Id == reservationId);
            if (res != null && res.Status == "Pending")
            {
                res.Status = "Cancelled";
                if (res.TableId.HasValue)
                {
                    var table = _context.Tables.Find(res.TableId.Value);
                    if (table != null && table.Status == "Reserved")
                    {
                        table.Status = "Empty";
                    }
                }
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
