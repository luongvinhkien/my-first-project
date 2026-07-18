using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class StaffController : Controller
    {
        private readonly RestaurantDbContext _context;

        public StaffController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Shift planning roster and security settings
        public IActionResult Index()
        {
            var staffList = _context.Employees.OrderBy(e => e.FullName).ToList();
            var timekeepings = _context.Timekeepings
                .Include(t => t.Employee)
                .Where(t => t.Date == DateTime.Today)
                .ToList();

            ViewData["StaffList"] = staffList;
            ViewData["Timekeepings"] = timekeepings;

            return View();
        }

        // Action: Create Employee
        [HttpPost]
        public IActionResult CreateEmployee(Employee emp)
        {
            if (ModelState.IsValid)
            {
                emp.PasswordHash = "123456"; // Default plaintext password
                _context.Employees.Add(emp);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Simulate Shift Clock In (Chấm công Vào)
        [HttpPost]
        public IActionResult ClockIn(int employeeId, string method)
        {
            var emp = _context.Employees.Find(employeeId);
            if (emp != null)
            {
                // Check if already checked in today
                var existing = _context.Timekeepings
                    .FirstOrDefault(t => t.EmployeeId == employeeId && t.Date == DateTime.Today && t.CheckOutTime == null);

                if (existing == null)
                {
                    _context.Timekeepings.Add(new Timekeeping
                    {
                        EmployeeId = employeeId,
                        Date = DateTime.Today,
                        CheckInTime = DateTime.Now,
                        Method = method
                    });
                    _context.SaveChanges();

                    // Log audit
                    _context.AuditLogs.Add(new AuditLog
                    {
                        Username = "system",
                        Action = $"Chấm công VÀO: Nhân viên {emp.FullName} bằng {method}",
                        TableName = "Timekeepings",
                        RecordId = employeeId,
                        Timestamp = DateTime.Now
                    });
                    _context.SaveChanges();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Simulate Shift Clock Out (Chấm công Ra)
        [HttpPost]
        public IActionResult ClockOut(int timekeepingId)
        {
            var tk = _context.Timekeepings.Include(t => t.Employee).FirstOrDefault(t => t.Id == timekeepingId);
            if (tk != null && tk.CheckOutTime == null)
            {
                tk.CheckOutTime = DateTime.Now;
                _context.SaveChanges();

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "system",
                    Action = $"Chấm công RA: Nhân viên {tk.Employee?.FullName}",
                    TableName = "Timekeepings",
                    RecordId = tk.EmployeeId,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
