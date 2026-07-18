using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Models
{
    public static class DbInitializer
    {
        public static void Initialize(RestaurantDbContext context)
        {
            // Create database schema if it doesn't exist
            context.Database.EnsureCreated();

            // Look for any categories. If true, database is already seeded.
            if (context.Categories.Any())
            {
                return;   // DB has been seeded
            }

            // 1. Seed Areas
            var area1 = new Area { Name = "Trong nhà" };
            var area2 = new Area { Name = "Ngoài trời" };
            var area3 = new Area { Name = "Phòng VIP" };
            var area4 = new Area { Name = "Lầu 1" };
            context.Areas.AddRange(area1, area2, area3, area4);
            context.SaveChanges();

            // 2. Seed Tables
            var tables = new[]
            {
                new Table { TableNumber = "Bàn 01", AreaId = area1.Id, Capacity = 4, Status = "Serving" },
                new Table { TableNumber = "Bàn 02", AreaId = area1.Id, Capacity = 4, Status = "Serving" },
                new Table { TableNumber = "Bàn 03", AreaId = area1.Id, Capacity = 2, Status = "Serving" },
                new Table { TableNumber = "Bàn 04", AreaId = area1.Id, Capacity = 6, Status = "Empty" },
                new Table { TableNumber = "Bàn 05", AreaId = area1.Id, Capacity = 4, Status = "Empty" },
                
                new Table { TableNumber = "Bàn 06", AreaId = area2.Id, Capacity = 4, Status = "Serving" },
                new Table { TableNumber = "Bàn 07", AreaId = area2.Id, Capacity = 8, Status = "Serving" },
                new Table { TableNumber = "Bàn 08", AreaId = area2.Id, Capacity = 4, Status = "Empty" },
                new Table { TableNumber = "Bàn 09", AreaId = area2.Id, Capacity = 4, Status = "Locked" },
                
                new Table { TableNumber = "Bàn 10", AreaId = area3.Id, Capacity = 10, Status = "Serving" },
                new Table { TableNumber = "Bàn 11", AreaId = area3.Id, Capacity = 10, Status = "Empty" },
                
                new Table { TableNumber = "Bàn 12", AreaId = area4.Id, Capacity = 4, Status = "Empty" }
            };
            context.Tables.AddRange(tables);
            context.SaveChanges();

            // 3. Seed Categories
            var catFood = new Category { Name = "Món ăn" };
            var catDrink = new Category { Name = "Đồ uống" };
            var catCombo = new Category { Name = "Combo" };
            var catBuffet = new Category { Name = "Buffet" };
            context.Categories.AddRange(catFood, catDrink, catCombo, catBuffet);
            context.SaveChanges();

            // 4. Seed MenuItems (with professional food photos!)
            var item1 = new MenuItem
            {
                Code = "F001",
                Name = "Phở Bò Đặc Biệt",
                Price = 65000,
                CostPrice = 25000,
                VatPercent = 8,
                Barcode = "8930000000010",
                CategoryId = catFood.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1583085209747-df170fe71a27?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_f001"
            };

            var item2 = new MenuItem
            {
                Code = "F002",
                Name = "Bún Chả Hà Nội",
                Price = 55000,
                CostPrice = 20000,
                VatPercent = 8,
                Barcode = "8930000000027",
                CategoryId = catFood.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_f002"
            };

            var item3 = new MenuItem
            {
                Code = "F003",
                Name = "Bò Lúc Lắc Khoai Tây",
                Price = 125000,
                CostPrice = 50000,
                VatPercent = 8,
                Barcode = "8930000000034",
                CategoryId = catFood.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_f003"
            };

            var item4 = new MenuItem
            {
                Code = "F004",
                Name = "Mì Xào Giòn Hải Sản",
                Price = 80000,
                CostPrice = 32000,
                VatPercent = 8,
                Barcode = "8930000000041",
                CategoryId = catFood.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1612927601601-6638404737ce?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_f004"
            };

            var item5 = new MenuItem
            {
                Code = "D001",
                Name = "Cà Phê Sữa Đá",
                Price = 29000,
                CostPrice = 8000,
                VatPercent = 8,
                Barcode = "8930000000058",
                CategoryId = catDrink.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_d001"
            };

            var item6 = new MenuItem
            {
                Code = "D002",
                Name = "Trà Đào Cam Sả",
                Price = 39000,
                CostPrice = 11000,
                VatPercent = 8,
                Barcode = "8930000000065",
                CategoryId = catDrink.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_d002"
            };

            var item7 = new MenuItem
            {
                Code = "D003",
                Name = "Nước Cam Ép Nguyên Chất",
                Price = 35000,
                CostPrice = 10000,
                VatPercent = 8,
                Barcode = "8930000000072",
                CategoryId = catDrink.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1613478223719-2ab802602423?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_d003"
            };

            var item8 = new MenuItem
            {
                Code = "C001",
                Name = "Combo Gia Đình Vui Vẻ",
                Price = 349000,
                CostPrice = 150000,
                VatPercent = 8,
                Barcode = "8930000000089",
                CategoryId = catCombo.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_c001"
            };

            var item9 = new MenuItem
            {
                Code = "B001",
                Name = "Buffet Nướng Thượng Hạng",
                Price = 299000,
                CostPrice = 130000,
                VatPercent = 10,
                Barcode = "8930000000096",
                CategoryId = catBuffet.Id,
                IsActive = true,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?w=600&auto=format&fit=crop&q=80",
                QrCode = "qr_b001"
            };

            context.MenuItems.AddRange(item1, item2, item3, item4, item5, item6, item7, item8, item9);
            context.SaveChanges();

            // 5. Seed MenuItemOptions
            context.MenuItemOptions.AddRange(
                new MenuItemOption { MenuItemId = item5.Id, GroupName = "Kích cỡ", OptionName = "Size M", ExtraPrice = 0 },
                new MenuItemOption { MenuItemId = item5.Id, GroupName = "Kích cỡ", OptionName = "Size L", ExtraPrice = 6000 },
                new MenuItemOption { MenuItemId = item5.Id, GroupName = "Độ ngọt", OptionName = "100% đường", ExtraPrice = 0 },
                new MenuItemOption { MenuItemId = item5.Id, GroupName = "Độ ngọt", OptionName = "50% đường", ExtraPrice = 0 },
                new MenuItemOption { MenuItemId = item6.Id, GroupName = "Toppings", OptionName = "Thêm Đào", ExtraPrice = 8000 }
            );

            // 6. Seed Customers (CRM)
            var customer1 = new Customer { Name = "LVK", Phone = "0903774856", Email = "luongvinhkien@gmail.com", MemberTier = "VIP", Points = 150 };
            var customer2 = new Customer { Name = "LLL", Phone = "090288374", Email = "customer2@gmail.com", MemberTier = "Silver", Points = 20 };
            context.Customers.AddRange(customer1, customer2);
            context.SaveChanges();

            // 7. Seed Employees
            context.Employees.AddRange(
                new Employee { Username = "admin", PasswordHash = "admin123", FullName = "Nguyễn Văn Admin", Role = "Admin" },
                new Employee { Username = "manager", PasswordHash = "manager123", FullName = "Trần Thị Quản Lý", Role = "Manager" },
                new Employee { Username = "staff1", PasswordHash = "staff123", FullName = "Lê Văn Phục Vụ", Role = "Staff" }
            );

            // 8. Seed Reservations
            context.Reservations.AddRange(
                new Reservation { CustomerId = customer1.Id, TableId = tables[0].Id, AreaId = area1.Id, GuestCount = 2, ReservationTime = DateTime.Today.AddHours(19), Status = "Pending", Note = "Khách VIP, xếp bàn cạnh cửa sổ" },
                new Reservation { CustomerId = customer2.Id, TableId = tables[7].Id, AreaId = area2.Id, GuestCount = 4, ReservationTime = DateTime.Today.AddHours(18), Status = "Pending", Note = "Không ăn cay" }
            );

            // 9. Seed Audit Log
            context.AuditLogs.Add(new AuditLog
            {
                Username = "system",
                Action = "Khởi tạo hệ thống dữ liệu ẩm thực Việt thành công",
                TableName = "System",
                RecordId = 1,
                Timestamp = DateTime.Now
            });

            context.SaveChanges();
        }
    }
}
