using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class MenuController : Controller
    {
        private readonly RestaurantDbContext _context;

        public MenuController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Menu Management List
        public IActionResult Index(int? categoryId)
        {
            var categories = _context.Categories.ToList();
            var query = _context.MenuItems.Include(m => m.Category).AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            var items = query.OrderBy(m => m.Code).ToList();

            ViewData["Categories"] = categories;
            ViewData["MenuItems"] = items;
            ViewData["SelectedCategoryId"] = categoryId ?? 0;

            return View();
        }

        private string SaveUploadedFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            
            return $"/uploads/{folder}/{uniqueFileName}";
        }

        // Action: Create Menu Item
        [HttpPost]
        public IActionResult CreateMenuItem(
            string code,
            string name,
            decimal price,
            decimal costPrice,
            decimal vatPercent,
            string barcode,
            int categoryId,
            string imageUrl,
            string videoUrl,
            IFormFile imageFile,
            IFormFile videoFile)
        {
            var item = new MenuItem
            {
                Code = code,
                Name = name,
                Price = price,
                CostPrice = costPrice,
                VatPercent = vatPercent,
                Barcode = barcode,
                CategoryId = categoryId,
                IsActive = true,
                QrCode = "qr_" + (code ?? "").ToLower()
            };

            if (imageFile != null)
            {
                item.ImageUrl = SaveUploadedFile(imageFile, "images");
            }
            else
            {
                item.ImageUrl = imageUrl;
            }

            if (videoFile != null)
            {
                item.VideoUrl = SaveUploadedFile(videoFile, "videos");
            }
            else
            {
                item.VideoUrl = videoUrl;
            }

            _context.MenuItems.Add(item);
            _context.SaveChanges();

            // Log audit
            _context.AuditLogs.Add(new AuditLog
            {
                Username = "manager",
                Action = $"Thêm món mới: {name} ({code}), Giá: {price:N0}đ",
                TableName = "MenuItems",
                RecordId = item.Id,
                Timestamp = System.DateTime.Now
            });
            _context.SaveChanges();

            return RedirectToAction(nameof(Index), new { categoryId = categoryId });
        }

        [HttpPost]
        public IActionResult UpdateMenuItem(
            int id, 
            string name, 
            decimal price, 
            decimal costPrice, 
            decimal vatPercent, 
            string barcode, 
            int categoryId, 
            string imageUrl, 
            string videoUrl, 
            IFormFile editImageFile, 
            IFormFile editVideoFile)
        {
            var dbItem = _context.MenuItems.Find(id);
            if (dbItem != null)
            {
                dbItem.Name = name;
                dbItem.Price = price;
                dbItem.CostPrice = costPrice;
                dbItem.VatPercent = vatPercent;
                dbItem.Barcode = barcode;
                dbItem.CategoryId = categoryId;

                if (editImageFile != null)
                {
                    dbItem.ImageUrl = SaveUploadedFile(editImageFile, "images");
                }
                else
                {
                    dbItem.ImageUrl = imageUrl;
                }

                if (editVideoFile != null)
                {
                    dbItem.VideoUrl = SaveUploadedFile(editVideoFile, "videos");
                }
                else
                {
                    dbItem.VideoUrl = videoUrl;
                }

                _context.SaveChanges();

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "manager",
                    Action = $"Sửa món: {name} ({dbItem.Code})",
                    TableName = "MenuItems",
                    RecordId = id,
                    Timestamp = System.DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index), new { categoryId = categoryId });
        }

        // Action: Toggle Active Status
        [HttpPost]
        public IActionResult ToggleStatus(int itemId)
        {
            var item = _context.MenuItems.Find(itemId);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Delete Menu Item
        [HttpPost]
        public IActionResult DeleteMenuItem(int itemId)
        {
            var item = _context.MenuItems.Find(itemId);
            if (item != null)
            {
                int catId = item.CategoryId;
                _context.MenuItems.Remove(item);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index), new { categoryId = catId });
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Create Category
        [HttpPost]
        public IActionResult CreateCategory(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _context.Categories.Add(new Category { Name = name });
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
