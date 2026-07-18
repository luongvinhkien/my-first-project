using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quanan.Models
{
    public class Area
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public ICollection<Table> Tables { get; set; }
    }

    public class Table
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string TableNumber { get; set; }
        public int AreaId { get; set; }
        public Area Area { get; set; }
        public int Capacity { get; set; } = 4;
        [StringLength(50)]
        public string Status { get; set; } = "Empty"; // Empty, Serving, Reserved, Cleaning, Locked
    }

    public class Category
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public ICollection<MenuItem> MenuItems { get; set; }
    }

    public class MenuItem
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Code { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        [StringLength(500)]
        public string ImageUrl { get; set; }
        [StringLength(500)]
        public string VideoUrl { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatPercent { get; set; } = 0.00m;
        [StringLength(50)]
        public string Barcode { get; set; }
        [StringLength(500)]
        public string QrCode { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<MenuItemOption> MenuItemOptions { get; set; }
        public ICollection<Recipe> Recipes { get; set; }
    }

    public class MenuItemOption
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        [Required]
        [StringLength(100)]
        public string GroupName { get; set; } // Size, Sweetness, Ice, Toppings
        [Required]
        [StringLength(100)]
        public string OptionName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExtraPrice { get; set; } = 0.00m;
    }

    public class Customer
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        [Required]
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(100)]
        public string Email { get; set; }
        public DateTime? Birthdate { get; set; }
        [StringLength(10)]
        public string Gender { get; set; }
        public int Points { get; set; } = 0;
        [StringLength(50)]
        public string MemberTier { get; set; } = "Silver"; // Silver, Gold, Platinum, VIP
    }

    public class Reservation
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }
        public int? TableId { get; set; }
        public Table Table { get; set; }
        public int? AreaId { get; set; }
        public Area Area { get; set; }
        public int GuestCount { get; set; }
        public DateTime ReservationTime { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; } = 0.00m;
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, CheckedIn, Cancelled
        [StringLength(500)]
        public string Note { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string FullName { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(100)]
        public string Email { get; set; }
        [Required]
        [StringLength(50)]
        public string Role { get; set; } // Cashier, Kitchen, Manager, Admin
        [StringLength(50)]
        public string Shift { get; set; } // Sáng, Chiều, Tối
        [Required]
        [StringLength(100)]
        public string Username { get; set; }
        [Required]
        [StringLength(500)]
        public string PasswordHash { get; set; }
    }

    public class Timekeeping
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public DateTime Date { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        [StringLength(50)]
        public string Method { get; set; } = "QR"; // QR, GPS, FaceID, Fingerprint
    }

    public class Order
    {
        public int Id { get; set; }
        public int? TableId { get; set; }
        public Table Table { get; set; }
        public int? EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public int? CustomerId { get; set; }
        public Customer Customer { get; set; }
        public DateTime OrderTime { get; set; } = DateTime.Now;
        [StringLength(50)]
        public string Status { get; set; } = "Ordering"; // Ordering, Kitchen, Served, Paid, Cancelled
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0.00m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; } = 0.00m;
        [StringLength(50)]
        public string PaymentMethod { get; set; } // Cash, Transfer, Card, Wallet
        [StringLength(500)]
        public string Note { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        public int Quantity { get; set; } = 1;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [StringLength(500)]
        public string Notes { get; set; } // "Không hành", "Ít cay"
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Cooking, Finished, Served
        public DateTime? CookingStartTime { get; set; }
        public DateTime? CookingEndTime { get; set; }
        public ICollection<OrderItemOption> OrderItemOptions { get; set; }
    }

    public class OrderItemOption
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; }
        [Required]
        [StringLength(100)]
        public string OptionName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 0.00m;
    }

    public class Voucher
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Code { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }
        [Required]
        [StringLength(20)]
        public string Type { get; set; } // Percent, Amount
        public bool IsUsed { get; set; } = false;
        public DateTime ExpiryDate { get; set; }
    }

    public class Ingredient
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        [Required]
        [StringLength(50)]
        public string Unit { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal StockQty { get; set; } = 0.000m;
        [Column(TypeName = "decimal(18,3)")]
        public decimal ReorderLevel { get; set; } = 1.000m;
        public DateTime? ExpiryDate { get; set; }
    }

    public class Recipe
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityNeeded { get; set; } // in grams/ml/units
    }

    public class Supplier
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(100)]
        public string Email { get; set; }
        [StringLength(50)]
        public string TaxCode { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal DebtAmount { get; set; } = 0.00m;
    }

    public class PurchaseOrder
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Received, Paid
        public ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
    }

    public class PurchaseOrderDetail
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }

    public class InventoryAudit
    {
        public int Id { get; set; }
        public DateTime AuditDate { get; set; } = DateTime.Now;
        [Required]
        [StringLength(200)]
        public string AuditorName { get; set; }
        [StringLength(500)]
        public string DifferenceNotes { get; set; }
        public ICollection<InventoryAuditDetail> InventoryAuditDetails { get; set; }
    }

    public class InventoryAuditDetail
    {
        public int Id { get; set; }
        public int InventoryAuditId { get; set; }
        public InventoryAudit InventoryAudit { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal SystemQty { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal ActualQty { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal AdjustmentQty { get; set; }
    }

    public class CashFlow
    {
        public int Id { get; set; }
        [Required]
        [StringLength(20)]
        public string Type { get; set; } // Receipt, Payment
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [Required]
        [StringLength(100)]
        public string Category { get; set; } // Electricity, Water, Gas, Marketing, Salary, Rent, FoodSupplier, CustomerPayment, Others
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        [StringLength(500)]
        public string Description { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        [StringLength(100)]
        public string Username { get; set; }
        [Required]
        [StringLength(200)]
        public string Action { get; set; }
        [StringLength(100)]
        public string TableName { get; set; }
        public int? RecordId { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        [StringLength(50)]
        public string IpAddress { get; set; }
        [StringLength(250)]
        public string DeviceInfo { get; set; }
    }

    public class TopSellerDto
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal Revenue { get; set; }
    }
}
