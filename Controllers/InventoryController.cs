using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class InventoryController : Controller
    {
        private readonly RestaurantDbContext _context;

        public InventoryController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Warehouse Stock and Recipe lists
        public IActionResult Index(int? selectedMenuItemId)
        {
            var ingredients = _context.Ingredients.OrderBy(i => i.Name).ToList();
            var menuItems = _context.MenuItems.OrderBy(m => m.Name).ToList();

            MenuItem selectedItem = null;
            if (selectedMenuItemId.HasValue)
            {
                selectedItem = _context.MenuItems
                    .Include(m => m.Recipes)
                    .ThenInclude(r => r.Ingredient)
                    .FirstOrDefault(m => m.Id == selectedMenuItemId.Value);
            }
            else if (menuItems.Any())
            {
                selectedItem = _context.MenuItems
                    .Include(m => m.Recipes)
                    .ThenInclude(r => r.Ingredient)
                    .FirstOrDefault(m => m.Id == menuItems.First().Id);
            }

            ViewData["Ingredients"] = ingredients;
            ViewData["MenuItems"] = menuItems;
            ViewData["SelectedItem"] = selectedItem;

            return View();
        }

        // Action: Create Ingredient
        [HttpPost]
        public IActionResult CreateIngredient(Ingredient ing)
        {
            if (ModelState.IsValid)
            {
                _context.Ingredients.Add(ing);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Create/Update Recipe Map
        [HttpPost]
        public IActionResult CreateRecipe(int menuItemId, int ingredientId, decimal quantityNeeded)
        {
            // Check if mapping already exists, if so update quantity, else add
            var existing = _context.Recipes
                .FirstOrDefault(r => r.MenuItemId == menuItemId && r.IngredientId == ingredientId);

            if (existing != null)
            {
                existing.QuantityNeeded = quantityNeeded;
            }
            else
            {
                _context.Recipes.Add(new Recipe
                {
                    MenuItemId = menuItemId,
                    IngredientId = ingredientId,
                    QuantityNeeded = quantityNeeded
                });
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index), new { selectedMenuItemId = menuItemId });
        }

        // Action: Delete Recipe mapping
        [HttpPost]
        public IActionResult DeleteRecipe(int recipeId)
        {
            var recipe = _context.Recipes.Find(recipeId);
            int itemId = 0;
            if (recipe != null)
            {
                itemId = recipe.MenuItemId;
                _context.Recipes.Remove(recipe);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index), new { selectedMenuItemId = itemId > 0 ? (int?)itemId : null });
        }

        // Action: Perform Warehouse Count Audit (Điều chỉnh kho)
        [HttpPost]
        public IActionResult AuditInventory(int ingredientId, decimal actualQty, string notes)
        {
            var ing = _context.Ingredients.Find(ingredientId);
            if (ing != null)
            {
                decimal systemQty = ing.StockQty;
                decimal adjustment = actualQty - systemQty;

                ing.StockQty = actualQty;

                // Create Inventory Audit Header & Details
                var audit = new InventoryAudit
                {
                    AuditDate = DateTime.Now,
                    AuditorName = "Manager",
                    DifferenceNotes = notes
                };
                _context.InventoryAudits.Add(audit);
                _context.SaveChanges();

                var auditDetail = new InventoryAuditDetail
                {
                    InventoryAuditId = audit.Id,
                    IngredientId = ingredientId,
                    SystemQty = systemQty,
                    ActualQty = actualQty,
                    AdjustmentQty = adjustment
                };
                _context.InventoryAuditDetails.Add(auditDetail);
                _context.SaveChanges();

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "manager",
                    Action = $"Kiểm kê điều chỉnh kho: {ing.Name}. Hệ thống: {systemQty:N2}, Thực tế: {actualQty:N2}, Lệch: {adjustment:N2}",
                    TableName = "Ingredients",
                    RecordId = ingredientId,
                    Timestamp = DateTime.Now
                });
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
