using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    public class ProcurementController : Controller
    {
        private readonly RestaurantDbContext _context;

        public ProcurementController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Supplier lists & purchase order history
        public IActionResult Index(int? selectedSupplierId)
        {
            var suppliers = _context.Suppliers.OrderBy(s => s.Name).ToList();
            var ingredients = _context.Ingredients.OrderBy(i => i.Name).ToList();

            Supplier selectedSupp = null;
            if (selectedSupplierId.HasValue)
            {
                selectedSupp = _context.Suppliers.Find(selectedSupplierId.Value);
            }
            else if (suppliers.Any())
            {
                selectedSupp = suppliers.First();
            }

            // Fetch PO history of selected supplier
            var purchaseHistory = selectedSupp != null
                ? _context.PurchaseOrders
                    .Where(p => p.SupplierId == selectedSupp.Id)
                    .OrderByDescending(p => p.OrderDate)
                    .ToList()
                : null;

            ViewData["Suppliers"] = suppliers;
            ViewData["Ingredients"] = ingredients;
            ViewData["SelectedSupplier"] = selectedSupp;
            ViewData["PurchaseHistory"] = purchaseHistory;

            return View();
        }

        // Action: Register Supplier
        [HttpPost]
        public IActionResult CreateSupplier(Supplier supp)
        {
            if (ModelState.IsValid)
            {
                supp.DebtAmount = 0.00m;
                _context.Suppliers.Add(supp);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        // Action: Purchase Order (replenish stock and record debt)
        [HttpPost]
        public IActionResult CreatePurchase(int supplierId, int ingredientId, decimal quantity, decimal unitPrice)
        {
            var supplier = _context.Suppliers.Find(supplierId);
            var ingredient = _context.Ingredients.Find(ingredientId);

            if (supplier != null && ingredient != null)
            {
                decimal totalCost = quantity * unitPrice;

                // 1. Create Purchase Order
                var po = new PurchaseOrder
                {
                    SupplierId = supplierId,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalCost,
                    Status = "Received"
                };
                _context.PurchaseOrders.Add(po);
                _context.SaveChanges();

                // 2. Details
                var pod = new PurchaseOrderDetail
                {
                    PurchaseOrderId = po.Id,
                    IngredientId = ingredientId,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                };
                _context.PurchaseOrderDetails.Add(pod);

                // 3. Increase stock in warehouse
                ingredient.StockQty += quantity;

                // 4. Increase supplier debt
                supplier.DebtAmount += totalCost;

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "manager",
                    Action = $"Nhập hàng: {ingredient.Name} (SL: {quantity:N2}), Tổng tiền: {totalCost:N0}đ ghi nợ NCC {supplier.Name}",
                    TableName = "PurchaseOrders",
                    RecordId = po.Id,
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index), new { selectedSupplierId = supplierId });
        }

        // Action: Settle Supplier Debt (Trả nợ NCC)
        [HttpPost]
        public IActionResult PaySupplierDebt(int supplierId, decimal paymentAmount)
        {
            var supplier = _context.Suppliers.Find(supplierId);
            if (supplier != null && paymentAmount > 0)
            {
                // Cap payment
                if (paymentAmount > supplier.DebtAmount) paymentAmount = supplier.DebtAmount;

                supplier.DebtAmount -= paymentAmount;

                // Create a Payment CashFlow voucher
                var cf = new CashFlow
                {
                    Type = "Payment",
                    Title = $"Trả nợ nhà cung cấp: {supplier.Name}",
                    Amount = paymentAmount,
                    Category = "FoodSupplier",
                    CreatedTime = DateTime.Now,
                    Description = $"Thanh toán công nợ NCC {supplier.Name} bằng chuyển khoản."
                };
                _context.CashFlows.Add(cf);

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    Username = "manager",
                    Action = $"Thanh toán công nợ NCC: {supplier.Name}, Số tiền: {paymentAmount:N0}đ",
                    TableName = "Suppliers",
                    RecordId = supplierId,
                    Timestamp = DateTime.Now
                });

                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index), new { selectedSupplierId = supplierId });
        }
    }
}
