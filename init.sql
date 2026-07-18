-- SQL Server Database Initialization and Mock Data Seeding Script for Quanan
USE master;
GO

-- Recreate database
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'Quanan')
BEGIN
    ALTER DATABASE Quanan SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Quanan;
END
GO

CREATE DATABASE Quanan;
GO

USE Quanan;
GO

-- Create Areas
CREATE TABLE Areas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

-- Create Tables
CREATE TABLE Tables (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TableNumber NVARCHAR(50) NOT NULL,
    AreaId INT FOREIGN KEY REFERENCES Areas(Id),
    Capacity INT DEFAULT 4,
    Status NVARCHAR(50) DEFAULT 'Empty' -- Empty, Serving, Reserved, Cleaning, Locked
);

-- Create Categories
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

-- Create MenuItems
CREATE TABLE MenuItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) UNIQUE NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    VideoUrl NVARCHAR(500) NULL,
    Price DECIMAL(18,2) NOT NULL,
    CostPrice DECIMAL(18,2) NOT NULL,
    VatPercent DECIMAL(5,2) DEFAULT 0.0,
    Barcode NVARCHAR(50) NULL,
    QrCode NVARCHAR(500) NULL,
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id),
    IsActive BIT DEFAULT 1
);

-- Create MenuItemOptions
CREATE TABLE MenuItemOptions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemId INT FOREIGN KEY REFERENCES MenuItems(Id) ON DELETE CASCADE,
    GroupName NVARCHAR(100) NOT NULL, -- Size, Sweetness, Ice, Toppings
    OptionName NVARCHAR(100) NOT NULL,
    ExtraPrice DECIMAL(18,2) DEFAULT 0.0
);

-- Create Customers (CRM)
CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(20) UNIQUE NOT NULL,
    Email NVARCHAR(100) NULL,
    Birthdate DATE NULL,
    Gender NVARCHAR(10) NULL,
    Points INT DEFAULT 0,
    MemberTier NVARCHAR(50) DEFAULT 'Silver' -- Silver, Gold, Platinum, VIP
);

-- Create Reservations
CREATE TABLE Reservations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT FOREIGN KEY REFERENCES Customers(Id),
    TableId INT NULL FOREIGN KEY REFERENCES Tables(Id),
    AreaId INT NULL FOREIGN KEY REFERENCES Areas(Id),
    GuestCount INT NOT NULL,
    ReservationTime DATETIME NOT NULL,
    DepositAmount DECIMAL(18,2) DEFAULT 0.0,
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, CheckedIn, Cancelled
    Note NVARCHAR(500) NULL
);

-- Create Employees
CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Role NVARCHAR(50) NOT NULL, -- Cashier, Kitchen, Manager, Admin
    Shift NVARCHAR(50) NULL, -- Sáng, Chiều, Tối
    Username NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL
);

-- Create Timekeepings
CREATE TABLE Timekeepings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT FOREIGN KEY REFERENCES Employees(Id) ON DELETE CASCADE,
    Date DATE NOT NULL,
    CheckInTime DATETIME NOT NULL,
    CheckOutTime DATETIME NULL,
    Method NVARCHAR(50) DEFAULT 'QR' -- QR, GPS, FaceID, Fingerprint
);

-- Create Orders
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TableId INT NULL FOREIGN KEY REFERENCES Tables(Id),
    EmployeeId INT NULL FOREIGN KEY REFERENCES Employees(Id),
    CustomerId INT NULL FOREIGN KEY REFERENCES Customers(Id),
    OrderTime DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50) DEFAULT 'Ordering', -- Ordering, Kitchen, Served, Paid, Cancelled
    TotalAmount DECIMAL(18,2) DEFAULT 0.0,
    DiscountAmount DECIMAL(18,2) DEFAULT 0.0,
    FinalAmount DECIMAL(18,2) DEFAULT 0.0,
    PaymentMethod NVARCHAR(50) NULL, -- Cash, Transfer, Card, Wallet
    Note NVARCHAR(500) NULL
);

-- Create OrderItems
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT FOREIGN KEY REFERENCES Orders(Id) ON DELETE CASCADE,
    MenuItemId INT FOREIGN KEY REFERENCES MenuItems(Id),
    Quantity INT NOT NULL DEFAULT 1,
    Price DECIMAL(18,2) NOT NULL,
    Notes NVARCHAR(500) NULL, -- "Không hành", "Ít cay"
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Cooking, Finished, Served
    CookingStartTime DATETIME NULL,
    CookingEndTime DATETIME NULL
);

-- Create OrderItemOptions
CREATE TABLE OrderItemOptions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderItemId INT FOREIGN KEY REFERENCES OrderItems(Id) ON DELETE CASCADE,
    OptionName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) DEFAULT 0.0
);

-- Create Vouchers
CREATE TABLE Vouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) UNIQUE NOT NULL,
    Value DECIMAL(18,2) NOT NULL,
    Type NVARCHAR(20) NOT NULL, -- Percent, Amount
    IsUsed BIT DEFAULT 0,
    ExpiryDate DATE NOT NULL
);

-- Create Ingredients (Inventory)
CREATE TABLE Ingredients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Unit NVARCHAR(50) NOT NULL,
    StockQty DECIMAL(18,3) DEFAULT 0.0,
    ReorderLevel DECIMAL(18,3) DEFAULT 1.0,
    ExpiryDate DATE NULL
);

-- Create Recipes (BOM)
CREATE TABLE Recipes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MenuItemId INT FOREIGN KEY REFERENCES MenuItems(Id) ON DELETE CASCADE,
    IngredientId INT FOREIGN KEY REFERENCES Ingredients(Id) ON DELETE CASCADE,
    QuantityNeeded DECIMAL(18,3) NOT NULL -- in grams/ml/units
);

-- Create Suppliers
CREATE TABLE Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    TaxCode NVARCHAR(50) NULL,
    DebtAmount DECIMAL(18,2) DEFAULT 0.0
);

-- Create PurchaseOrders
CREATE TABLE PurchaseOrders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId INT FOREIGN KEY REFERENCES Suppliers(Id),
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Pending' -- Pending, Received, Paid
);

-- Create PurchaseOrderDetails
CREATE TABLE PurchaseOrderDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseOrderId INT FOREIGN KEY REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
    IngredientId INT FOREIGN KEY REFERENCES Ingredients(Id),
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);

-- Create InventoryAudits
CREATE TABLE InventoryAudits (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AuditDate DATETIME DEFAULT GETDATE(),
    AuditorName NVARCHAR(200) NOT NULL,
    DifferenceNotes NVARCHAR(500) NULL
);

-- Create InventoryAuditDetails
CREATE TABLE InventoryAuditDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InventoryAuditId INT FOREIGN KEY REFERENCES InventoryAudits(Id) ON DELETE CASCADE,
    IngredientId INT FOREIGN KEY REFERENCES Ingredients(Id),
    SystemQty DECIMAL(18,3) NOT NULL,
    ActualQty DECIMAL(18,3) NOT NULL,
    AdjustmentQty DECIMAL(18,3) NOT NULL
);

-- Create CashFlows
CREATE TABLE CashFlows (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Type NVARCHAR(20) NOT NULL, -- Receipt, Payment
    Title NVARCHAR(200) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Category NVARCHAR(100) NOT NULL, -- Electricity, Water, Gas, Marketing, Salary, Rent, FoodSupplier, CustomerPayment, Others
    CreatedTime DATETIME DEFAULT GETDATE(),
    Description NVARCHAR(500) NULL
);

-- Create AuditLogs
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    Username NVARCHAR(100) NULL,
    Action NVARCHAR(200) NOT NULL,
    TableName NVARCHAR(100) NULL,
    RecordId INT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    Timestamp DATETIME DEFAULT GETDATE(),
    IpAddress NVARCHAR(50) NULL,
    DeviceInfo NVARCHAR(250) NULL
);
GO

-- ==========================================================
-- SEED MOCK DATA
-- ==========================================================

-- Seed Areas
INSERT INTO Areas (Name) VALUES (N'Trong nhà'), (N'Ngoài trời'), (N'Phòng VIP'), (N'Lầu 1');
GO

-- Seed Tables
INSERT INTO Tables (TableNumber, AreaId, Capacity, Status) VALUES
('Bàn 01', 1, 4, 'Empty'),
('Bàn 02', 1, 4, 'Serving'),
('Bàn 03', 1, 2, 'Empty'),
('Bàn 04', 1, 6, 'Cleaning'),
('Bàn 05', 1, 4, 'Reserved'),
('Bàn 06', 2, 4, 'Empty'),
('Bàn 07', 2, 8, 'Serving'),
('Bàn 08', 2, 4, 'Empty'),
('Bàn 09', 2, 4, 'Locked'),
('Bàn 10', 3, 10, 'Serving'),
('Bàn 11', 3, 10, 'Empty'),
('Lầu 1 - 01', 4, 4, 'Empty'),
('Lầu 1 - 02', 4, 4, 'Empty'),
('Lầu 1 - 03', 4, 6, 'Serving');
GO

-- Seed Categories
INSERT INTO Categories (Name) VALUES (N'Món ăn'), (N'Đồ uống'), (N'Combo'), (N'Buffet');
GO

-- Seed MenuItems
INSERT INTO MenuItems (Code, Name, ImageUrl, Price, CostPrice, VatPercent, Barcode, QrCode, CategoryId, IsActive) VALUES
('F001', N'Phở Bò Đặc Biệt', 'https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=400', 65000.00, 25000.00, 8.0, '8930000000010', 'qr_pho', 1, 1),
('F002', N'Bún Chả Hà Nội', 'https://images.unsplash.com/photo-1596797038530-2c107229654b?w=400', 55000.00, 20000.00, 8.0, '8930000000027', 'qr_buncha', 1, 1),
('F003', N'Bò Lúc Lắc Khoai Tây', 'https://images.unsplash.com/photo-1600891964599-f61ba0e24092?w=400', 125000.00, 50000.00, 8.0, '8930000000034', 'qr_boluclac', 1, 1),
('F004', N'Mì Xào Giòn Hải Sản', 'https://images.unsplash.com/photo-1585032226651-759b368d7246?w=400', 80000.00, 32000.00, 8.0, '8930000000041', 'qr_mixao', 1, 1),
('D001', N'Cà Phê Sữa Đá', 'https://images.unsplash.com/photo-1541167760496-1628856ab772?w=400', 29000.00, 8000.00, 8.0, '8930000000058', 'qr_cfsuada', 2, 1),
('D002', N'Trà Đào Cam Sả', 'https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400', 39000.00, 11000.00, 8.0, '8930000000065', 'qr_tradao', 2, 1),
('D003', N'Nước Cam Ép Nguyên Chất', 'https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400', 35000.00, 10000.00, 8.0, '8930000000072', 'qr_nuoccam', 2, 1),
('C001', N'Combo Gia Đình Vui Vẻ', 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400', 349000.00, 150000.00, 8.0, '8930000000089', 'qr_combogiadinh', 3, 1),
('B001', N'Buffet Nướng Thượng Hạng', 'https://images.unsplash.com/photo-1544025162-d76694265947?w=400', 299000.00, 130000.00, 10.0, '8930000000096', 'qr_buffetnuong', 4, 1);
GO

-- Seed MenuItemOptions
-- Size options for Coffee
INSERT INTO MenuItemOptions (MenuItemId, GroupName, OptionName, ExtraPrice) VALUES
(5, 'Size', 'Size S', 0.00),
(5, 'Size', 'Size M', 5000.00),
(5, 'Size', 'Size L', 10000.00),
(5, 'Sweetness', '30% đường', 0.00),
(5, 'Sweetness', '50% đường', 0.00),
(5, 'Sweetness', '70% đường', 0.00),
(5, 'Sweetness', '100% đường', 0.00),
(5, 'Ice', 'Không đá', 0.00),
(5, 'Ice', 'Ít đá', 0.00),
(5, 'Ice', 'Bình thường', 0.00),
(5, 'Toppings', 'Kem cheese', 10000.00),
(5, 'Toppings', 'Trân châu đen', 8000.00);

-- Size options for Tea
INSERT INTO MenuItemOptions (MenuItemId, GroupName, OptionName, ExtraPrice) VALUES
(6, 'Size', 'Size M', 0.00),
(6, 'Size', 'Size L', 8000.00),
(6, 'Sweetness', '70% đường', 0.00),
(6, 'Sweetness', '100% đường', 0.00),
(6, 'Ice', 'Bình thường', 0.00),
(6, 'Toppings', 'Thêm thạch đào', 8000.00);
GO

-- Seed Customers
INSERT INTO Customers (Name, Phone, Email, Birthdate, Gender, Points, MemberTier) VALUES
(N'Nguyễn Văn A', '0901234567', 'a.nguyen@gmail.com', '1990-05-15', N'Nam', 1250, 'Gold'),
(N'Trần Thị B', '0912345678', 'b.tran@yahoo.com', '1995-10-20', N'Nữ', 600, 'Silver'),
(N'Lê Hoàng C', '0987654321', 'c.le@outlook.com', '1988-02-10', N'Nam', 3500, 'VIP'),
(N'Phạm Minh D', '0933334444', 'd.pham@hotmail.com', '2001-08-25', N'Nam', 150, 'Silver');
GO

-- Seed Reservations
INSERT INTO Reservations (CustomerId, TableId, AreaId, GuestCount, ReservationTime, DepositAmount, Status, Note) VALUES
(1, 5, 1, 4, DATEADD(hour, 4, GETDATE()), 200000.00, 'Pending', N'Sinh nhật anh A, cần setup nến'),
(2, 8, 2, 2, DATEADD(day, 1, GETDATE()), 0.00, 'Pending', N'Ngồi gần cửa sổ');
GO

-- Seed Employees
INSERT INTO Employees (FullName, Phone, Email, Role, Shift, Username, PasswordHash) VALUES
(N'Lê Quản Lý', '0909090901', 'manager@restaurant.com', 'Manager', N'Tối', 'manager', '123456'),
(N'Trần Thu Ngân', '0909090902', 'cashier@restaurant.com', 'Cashier', N'Sáng', 'cashier', '123456'),
(N'Nguyễn Đầu Bếp', '0909090903', 'kitchen@restaurant.com', 'Kitchen', N'Chiều', 'kitchen', '123456'),
(N'Phan Quản Trị', '0909090904', 'admin@restaurant.com', 'Admin', N'Sáng', 'admin', '123456');
GO

-- Seed Timekeepings
INSERT INTO Timekeepings (EmployeeId, Date, CheckInTime, CheckOutTime, Method) VALUES
(2, CAST(GETDATE() AS DATE), DATEADD(hour, -5, GETDATE()), NULL, 'QR'),
(3, CAST(GETDATE() AS DATE), DATEADD(hour, -4, GETDATE()), NULL, 'FaceID'),
(1, CAST(GETDATE() AS DATE), DATEADD(hour, -2, GETDATE()), NULL, 'GPS');
GO

-- Seed Vouchers
INSERT INTO Vouchers (Code, Value, Type, IsUsed, ExpiryDate) VALUES
('GIAM20K', 20000.00, 'Amount', 0, DATEADD(month, 3, GETDATE())),
('KM10PHANTRAM', 10.00, 'Percent', 0, DATEADD(month, 3, GETDATE())),
('VOUCHERFREE50K', 50000.00, 'Amount', 0, DATEADD(month, 1, GETDATE()));
GO

-- Seed Ingredients (Inventory)
INSERT INTO Ingredients (Name, Unit, StockQty, ReorderLevel, ExpiryDate) VALUES
(N'Thịt Bò Mỹ', 'kg', 25.500, 5.000, DATEADD(day, 7, GETDATE())),
(N'Thịt Heo Rừng', 'kg', 15.000, 3.000, DATEADD(day, 5, GETDATE())),
(N'Bánh Phở Tươi', 'kg', 40.000, 10.000, DATEADD(day, 2, GETDATE())),
(N'Đào ngâm hộp', 'lon', 30.000, 5.000, DATEADD(month, 6, GETDATE())),
(N'Hành lá', 'kg', 5.000, 1.000, DATEADD(day, 4, GETDATE())),
(N'Gừng tươi', 'kg', 3.000, 0.500, DATEADD(day, 10, GETDATE())),
(N'Cà phê hạt', 'kg', 12.000, 2.000, DATEADD(month, 3, GETDATE())),
(N'Sữa đặc Ngôi Sao', 'lon', 48.000, 10.000, DATEADD(month, 8, GETDATE())),
(N'Nước Lèo Phở', 'lít', 50.000, 10.000, DATEADD(day, 1, GETDATE()));
GO

-- Seed Recipes (BOM)
-- Recipe for Phở Bò Đặc Biệt (F001)
INSERT INTO Recipes (MenuItemId, IngredientId, QuantityNeeded) VALUES
(1, 1, 0.150), -- 150g Thịt Bò Mỹ
(1, 3, 0.200), -- 200g Bánh Phở
(1, 5, 0.010), -- 10g Hành
(1, 6, 0.005), -- 5g Gừng
(1, 9, 0.400); -- 400ml Nước lèo

-- Recipe for Cà Phê Sữa Đá (D001)
INSERT INTO Recipes (MenuItemId, IngredientId, QuantityNeeded) VALUES
(5, 7, 0.025), -- 25g Cà phê hạt
(5, 8, 0.080); -- 80g Sữa đặc

-- Recipe for Trà Đào Cam Sả (D002)
INSERT INTO Recipes (MenuItemId, IngredientId, QuantityNeeded) VALUES
(6, 4, 0.250); -- 0.25 lon Đào ngâm
GO

-- Seed Suppliers
INSERT INTO Suppliers (Name, Phone, Email, TaxCode, DebtAmount) VALUES
(N'Công ty TNHH Thực Phẩm Sạch Hà Nội', '0243123456', 'info@hanoifood.vn', '0101234567', 15000000.00),
(N'Tổng đại lý Nước giải khát & Cà phê Trung Nguyên', '0283987654', 'coffee@trungnguyen.com.vn', '0309876543', 8500000.00);
GO

-- Seed Orders & OrderItems (Completed, Paid history to feed Reports and Dashboard)
-- Order 1: Table 2 (Serving)
INSERT INTO Orders (TableId, EmployeeId, CustomerId, OrderTime, Status, TotalAmount, DiscountAmount, FinalAmount, PaymentMethod, Note) VALUES
(2, 2, 1, DATEADD(minute, -45, GETDATE()), 'Kitchen', 214000.00, 0.00, 214000.00, NULL, N'Không hành phở bò');

INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, Price, Notes, Status, CookingStartTime, CookingEndTime) VALUES
(1, 1, 2, 65000.00, N'Không hành', 'Cooking', DATEADD(minute, -10, GETDATE()), NULL),
(1, 5, 2, 29000.00, NULL, 'Finished', DATEADD(minute, -12, GETDATE()), DATEADD(minute, -8, GETDATE())),
(1, 6, 1, 39000.00, NULL, 'Served', DATEADD(minute, -15, GETDATE()), DATEADD(minute, -10, GETDATE()));

-- Order 2: Table 7 (Serving)
INSERT INTO Orders (TableId, EmployeeId, CustomerId, OrderTime, Status, TotalAmount, DiscountAmount, FinalAmount, PaymentMethod, Note) VALUES
(7, 2, 2, DATEADD(minute, -90, GETDATE()), 'Served', 198000.00, 10000.00, 188000.00, NULL, NULL);

INSERT INTO OrderItems (OrderId, MenuItemId, Quantity, Price, Notes, Status, CookingStartTime, CookingEndTime) VALUES
(2, 2, 2, 55000.00, NULL, 'Served', DATEADD(minute, -80, GETDATE()), DATEADD(minute, -65, GETDATE())),
(2, 7, 2, 35000.00, NULL, 'Served', DATEADD(minute, -85, GETDATE()), DATEADD(minute, -80, GETDATE())),
(2, 5, 1, 29000.00, N'Ít đá', 'Served', DATEADD(minute, -75, GETDATE()), DATEADD(minute, -70, GETDATE()));

-- Seed some historical PAID orders for past 7 days to generate beautiful charts
DECLARE @i INT = 7;
WHILE @i > 0
BEGIN
    -- Insert 3-4 orders per day with random values
    INSERT INTO Orders (TableId, EmployeeId, CustomerId, OrderTime, Status, TotalAmount, DiscountAmount, FinalAmount, PaymentMethod, Note) VALUES
    (1, 2, 1, DATEADD(day, -@i, DATEADD(hour, 12, GETDATE())), 'Paid', 245000.00, 20000.00, 225000.00, 'Transfer', N'Mở bàn lúc trưa'),
    (3, 2, NULL, DATEADD(day, -@i, DATEADD(hour, 18, GETDATE())), 'Paid', 450000.00, 0.00, 450000.00, 'Cash', NULL),
    (6, 2, 3, DATEADD(day, -@i, DATEADD(hour, 20, GETDATE())), 'Paid', 890000.00, 50000.00, 840000.00, 'Card', N'Bàn VIP sinh nhật');

    SET @i = @i - 1;
END
GO

-- Seed CashFlows
INSERT INTO CashFlows (Type, Title, Amount, Category, CreatedTime, Description) VALUES
('Receipt', N'Doanh thu bán lẻ ngày hôm qua', 1515000.00, 'CustomerPayment', DATEADD(day, -1, GETDATE()), N'Tổng kết ca tối'),
('Payment', N'Tiền điện tháng này', 2500000.00, 'Electricity', DATEADD(day, -10, GETDATE()), N'Thanh toán điện lực EVN'),
('Payment', N'Tiền nước tháng này', 600000.00, 'Water', DATEADD(day, -10, GETDATE()), N'Thanh toán nước sạch'),
('Payment', N'Nhập hàng gia vị & Rau củ', 1200000.00, 'FoodSupplier', DATEADD(day, -3, GETDATE()), N'Nhập chợ đầu mối'),
('Payment', N'Lương nhân viên tháng trước', 18000000.00, 'Salary', DATEADD(day, -12, GETDATE()), N'Thanh toán lương ca sáng và tối');
GO

-- Seed AuditLogs
INSERT INTO AuditLogs (UserId, Username, Action, TableName, RecordId, OldValues, NewValues, Timestamp, IpAddress, DeviceInfo) VALUES
(4, 'admin', N'Đăng nhập hệ thống', 'Employees', 4, NULL, NULL, DATEADD(hour, -5, GETDATE()), '192.168.1.10', 'Windows PC - VS Code'),
(1, 'manager', N'Cập nhật giá bán món Phở Bò', 'MenuItems', 1, '{"Price":60000.00}', '{"Price":65000.00}', DATEADD(day, -2, GETDATE()), '192.168.1.11', 'MacBook Air - Chrome');
GO
