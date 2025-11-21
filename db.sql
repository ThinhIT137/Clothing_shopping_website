 -- Phần admin 
-- -Login(có kiểm tra role)
-- -Quản lý category(thêm,sửa,xóa tạm - khôi phục , xóa ,ẩn-hiện)
-- -Quản lý sản phẩm(thêm,sửa,xóa tạm - khôi phục , xóa ,ẩn-hiện)
-- - Quản lý người dùng(Khóa mở, xóa) 
-- -Quản lý đơn hàng(chỉnh trạng thái đơn, xem chi tiết)
-- -Quản lý tin tức(thêm,sửa,xóa tạm - khôi phục , xóa ,ẩn-hiện)
-- Phần người dùng:
-- -Đăng kí ,đăng nhập
-- -Xem sản phẩm 
-- -Đánh giá sản phẩm
-- -Quản lý giỏ hàng - đặt hàng
-- -Đổi mật khẩu
-- -Đổi thông tin cá nhân.
-- Phần trang chủ:
-- -Đổ dữ liệu category từ database
-- -Đổ sản phẩm mới ,sản phẩm sale, tin tức từ database

-- 1. Users (Người dùng) - dùng UUID v4 (uniqueidentifier) do SQL sinh mặc định NEWID()
CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- UUID v4, tự sinh nếu không cung cấp
    Email NVARCHAR(255) NOT NULL UNIQUE,                 -- email dùng để đăng nhập
    PasswordHash NVARCHAR(512) NOT NULL,                 -- lưu hash mật khẩu (bcrypt/argon2 ở tầng app)
    FullName NVARCHAR(200) NULL,                         -- họ và tên
    Phone NVARCHAR(20) NULL,                             -- số điện thoại
    Role NVARCHAR(20) NOT NULL DEFAULT 'Customer',       -- 'Admin', 'Staff', 'Customer', 'shipper'
    IsLocked BIT NOT NULL DEFAULT 0,                     -- 1 = đang hoạt động, 0 = chưa động
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- thời gian tạo
    UpdatedAt DATETIME2 NULL,                            -- thời gian cập nhật gần nhất
    IsDeleted BIT NOT NULL DEFAULT 0,                    -- xóa mềm (soft delete)
    DeletedAt DATETIME2 NULL,
	gender nvarchar(10) NOT NULL,
	Birthday date NOT NULL,
	Address nvarchar(max) Null
);
CREATE INDEX IX_Users_Email ON Users(Email);

-- 2. Categories (Danh mục sản phẩm)
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,            -- giữ INT cho danh mục để nhẹ
    Name NVARCHAR(150) NOT NULL,                         -- tên danh mục
    Slug NVARCHAR(200) NOT NULL UNIQUE,                  -- slug dùng cho URL
    Description NVARCHAR(1000) NULL,                     -- mô tả ngắn
    SortOrder INT NOT NULL DEFAULT 0,                    -- thứ tự hiển thị
    IsHidden BIT NOT NULL DEFAULT 0,                     -- 1 = ẩn, 0 = hiện
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL
);

-- 3. Products (Sản phẩm - thông tin chung)
-- LƯU Ý: không lưu Stock ở đây; tồn kho quản lý theo ProductVariants
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,                             -- FK -> Categories
    Name NVARCHAR(250) NOT NULL,                         -- tên sản phẩm
    Slug NVARCHAR(300) NOT NULL UNIQUE,                  -- slug cho URL
    ShortDesc NVARCHAR(Max) NULL,                        -- mô tả ngắn
    FullDesc NVARCHAR(MAX) NULL,                         -- mô tả chi tiết
	Video nvarchar(max) null,
    Images NVARCHAR(MAX) NULL,                           -- ảnh chung (JSON/CSV), biến thể có thể có ảnh riêng
    IsHidden BIT NOT NULL DEFAULT 0,                     -- ẩn/hiện
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
	TargetGroup int NULL,
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_Slug ON Products(Slug);

-- 4. ProductVariants (Biến thể: size, màu, sku, tồn kho riêng)
CREATE TABLE ProductVariants (
    ProductVariantId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,                              -- FK -> Products
    Size NVARCHAR(50) NULL,                              -- S, M, L, XL hoặc 38, 40...
    Color NVARCHAR(100) NULL,                            -- màu
    Price DECIMAL(12,2) NULL,                            -- giá riêng cho variant (nếu NULL => dùng Products.Price)
    SalePrice DECIMAL(12,2) NULL,                        -- giá sale cho variant (nếu có)
    Stock INT NOT NULL DEFAULT 0,                        -- tồn kho theo biến thể
    IsHidden BIT NOT NULL DEFAULT 0,                     -- ẩn/hiện biến thể
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    CONSTRAINT FK_ProductVariants_Product FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);
CREATE INDEX IX_ProductVariants_ProductId ON ProductVariants(ProductId);

-- 5. News (Tin tức / Bài viết)
CREATE TABLE News (
    NewsId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(300) NOT NULL,                        -- tiêu đề
    ShortDesc NVARCHAR(500) NULL,                        -- mô tả ngắn
    Content NVARCHAR(MAX) NULL,                          -- nội dung
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
);

-- 6. Notifications (Gửi cho người dùng chỉ định Vd: gửi người dùng nhận được đơn hàng)
CREATE TABLE Notifications (
    NotificationId BIGINT IDENTITY(1,1) PRIMARY KEY,
    NewsId INT Not Null,
    ReceiverId UNIQUEIDENTIFIER Null,		   -- NULL = gửi cho tất cả
	OrderId INT Null,                          -- liên kết đơn hàng (nếu là thông báo đơn hàng)
    IsRead BIT DEFAULT 0,                      -- false chưa đọc, true đã đọc
	CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserNews_News FOREIGN KEY (NewsId) REFERENCES News(NewsId),
    CONSTRAINT FK_UserNews_Receiver FOREIGN KEY (ReceiverId) REFERENCES Users(UserId),
	CONSTRAINT FK_UserNews_OrderId FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
);

-- 7. Orders (Đơn hàng - header)
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,                     -- người đặt, FK -> Users(UserId)
    TotalAmount DECIMAL(14,2) NOT NULL,                  -- tổng tiền
    ShippingAddress NVARCHAR(Max) NULL,                 -- địa chỉ giao hàng
    Phone NVARCHAR(20) NULL,                             -- số liên hệ
    Email NVARCHAR(255) NULL,                            -- email người nhận
    OrderStatus NVARCHAR(30) NOT NULL DEFAULT 'Pending', -- trạng thái đơn
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Orders_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_OrderStatus ON Orders(OrderStatus);

-- 8. OrderItems (Chi tiết đơn, lưu snapshot thông tin sản phẩm + biến thể)
CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,                                -- FK -> Orders
    ProductVariantId INT NULL,                           -- tham chiếu sang variant (nullable)
    UnitPrice DECIMAL(12,2) NOT NULL,                    -- snapshot giá lúc đặt (theo variant nếu có)
    Quantity INT NOT NULL,                               -- số lượng
    TotalMoney AS (UnitPrice * Quantity) PERSISTED,       -- tổng dòng
    CONSTRAINT FK_OrderItems_Order FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    CONSTRAINT FK_OrderItems_Variant FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId)
);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);

-- 9. CartItems (Giỏ hàng tạm của user) - tham chiếu ProductVariantId
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,                     -- người dùng (FK -> Users)
    ProductVariantId INT NOT NULL,                        -- biến thể sản phẩm (FK -> ProductVariants)
    Quantity INT NOT NULL DEFAULT 1,                      -- số lượng
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CartItems_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_CartItems_Variant FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId)
);
CREATE INDEX IX_CartItems_UserId ON CartItems(UserId);
CREATE INDEX IX_CartItems_User_Variant ON CartItems(UserId, ProductVariantId);

-- 10 Reviews (Đánh giá & bình luận)
CREATE TABLE Reviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    ProductVariantId INT NULL,          -- tham chiếu biến thể (nullable)
    UserId UNIQUEIDENTIFIER NOT NULL,   -- ai đánh giá (FK -> Users)
    Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5), -- sao 1..5
    Title NVARCHAR(200) NULL,           -- tiêu đề review (tuỳ chọn)
    Content NVARCHAR(MAX) NULL,         -- nội dung/bình luận
    IsApproved BIT NOT NULL DEFAULT 0,  -- 0 = chờ duyệt, 1 = hiển thị
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
	CONSTRAINT FK_Reviews_CartItemsVariant FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId),
	CONSTRAINT FK_Reviews_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
);

-- 11 Favorite (Sản phẩm yêu thích)
CREATE TABLE FavoriteItems (
    FavoriteItemsId INT IDENTITY(1,1) PRIMARY KEY,     
    UserId UNIQUEIDENTIFIER NOT NULL,                     -- người dùng (FK -> Users)
    ProductVariantId INT NOT NULL,                        -- biến thể sản phẩm (FK -> ProductVariants)
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_FavoriteItems_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_FavoriteItems_Variant FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId)
);

-- 12. Vouchers (Mã giảm giá / Phiếu mua hàng)
CREATE TABLE VOUCHERS (
    VoucherId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,                -- Tên voucher (VD: Giảm 20% cho đơn hàng Tết)
    Description NVARCHAR(500) NULL,             -- Mô tả chi tiết voucher
    VoucherType NVARCHAR(20) NOT NULL,          -- Loại giảm giá: 'Percentage' (%), 'FixedAmount' (giá trị cố định), 'FreeShipping'
    StartDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), -- Ngày bắt đầu         
    EndDate DATETIME2 NULL,                     -- Ngày hết hạn (NULL nếu không hết hạn) 
    IsActive BIT NOT NULL DEFAULT 1,            -- Trạng thái: 1 = hoạt động, 0 = không hoạt động 
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL
);

-- 13. VoucerUsers (Người dùng được dùng bao nhiêu voucher)
CREATE TABLE VOUCHERUSERS (
	VoucherId INT NOT NULL,   
	UserId UNIQUEIDENTIFIER NOT NULL,      
	UsedCount INT NOT NULL DEFAULT 0,                
	LastUsedAt DATETIME2 NULL,
	PRIMARY KEY (VoucherId, UserId),
    CONSTRAINT FK_VoucherUsers_Voucher FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId),
    CONSTRAINT FK_VoucherUsers_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

/* =================================
	Users 
 ===================================*/ 

 /* =================================
	Categories 
 ===================================*/ 
 INSERT INTO Categories (Name, Slug, Description, SortOrder, IsHidden)
VALUES
-- Áo
(N'Áo phông', N'ao-phong', N'Áo phông thời trang, dễ phối đồ.', 1, 0), -- 10
(N'Áo sơ mi', N'ao-so-mi', N'Áo sơ mi công sở, thanh lịch.', 2, 0), -- 10
(N'Áo hoodie', N'ao-hoodie', N'Áo hoodie cá tính, phong cách đường phố.', 3, 0), --10
(N'Áo khoác', N'ao-khoac', N'Áo khoác giữ ấm, nhiều kiểu dáng.', 4, 0), --10
(N'Áo phao', N'ao-phao', N'Áo phao mùa đông, dày dặn, ấm áp.', 5, 0), --10
(N'Áo dài', N'ao-dai', N'Áo dài truyền thống Việt Nam.', 6, 0), --10
-- Quần
(N'Quần jean', N'quan-jean', N'Quần jean phong cách, bền đẹp.', 7, 0), --10
(N'Quần short', N'quan-short', N'Quần short năng động, thoải mái.', 8, 0), --10
(N'Quần dài', N'quan-dai', N'Quần dài công sở, lịch sự.', 9, 0), --10
-- Váy / Đầm
(N'Chân váy', N'chan-vay', N'Chân váy nữ tính, thời trang.', 10, 0), --10
(N'Đầm', N'dam', N'Đầm dự tiệc, công sở, dạo phố.', 11, 0), --10
-- Giày
(N'Giày Nike', N'giay-nike', N'Giày thể thao Nike chính hãng.', 12, 0), --10
(N'Giày Adidas', N'giay-adidas', N'Giày thể thao Adidas chính hãng.', 13, 0), --10
(N'Giày cao gót', N'giay-cao-got', N'Giày cao gót nữ tính, sang trọng.', 14, 0), --10
(N'Giày bốt', N'giay-bot', N'Giày bốt cổ cao, phong cách cá tính.', 15, 0), --10
-- Dép
(N'Dép tông', N'dep-tong', N'Dép tông tiện dụng, thoáng mát.', 16, 0), --10
(N'Dép sục', N'dep-suc', N'Dép sục thời trang, dễ mang.', 17, 0), --10
(N'Dép tổ ong', N'dep-to-ong', N'Dép tổ ong truyền thống, bền bỉ.', 18, 0); --10

 /* =================================
	Products 
 ===================================*/ 
 update Products set ShortDesc = N'[
  "Thoải mái, bền bỉ và vượt thời gian—là lựa chọn số một không phải tự nhiên mà có. Thiết kế cổ điển của thập niên 80 kết hợp với các chi tiết táo bạo tạo nên phong cách phù hợp dù bạn đang ở trên sân hay đang di chuyển.",
  {
    "Chi tiết sản phẩm": [
      "Màu sắc hiển thị: Trắng/Trắng/Xanh băng hà/Trắng",
      "Kiểu dáng: IB8875-111",
      "Quốc gia/Khu vực xuất xứ: Việt Nam"
    ]
  }
]'
where ProductId = 1
select * from Products
update Products set TargetGroup = 10 where ProductId = 7;

 /* =================================
	ProductVariants 
 ===================================*/ 
INSERT INTO ProductVariants (ProductId, Size, Color, Price, Stock)
VALUES
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'38', N'white', 3519000, 10),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'39', N'white', 3519000, 8),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'40', N'white', 3519000, 12),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'41', N'white', 3519000, 9),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'42', N'white', 3519000, 7),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'43', N'white', 3519000, 7),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'44', N'white', 3519000, 7),
((SELECT ProductId FROM Products WHERE Slug = N'nike-air-force-1-07-lx'), N'45', N'white', 3519000, 7)

 /* =================================
	News 
 ===================================*/ 
select * from News

 /* =================================
	Notifications 
 ===================================*/ 
INSERT INTO Notifications (NotificationId, NewsId, ReceiverId, OrderId, IsRead, CreatedAt)
VALUES
(1, 1, '8458B6FB-292C-4123-9CD1-77D5C21C011C', 2, 0, '2025-10-27 11:04:27.0750280'),
(2, 1, '8458B6FB-292C-4123-9CD1-77D5C21C011C', 2, 0, '2025-10-27 11:32:01.1515092');

 /* =================================
	Orders 
 ===================================*/ 
 INSERT INTO Orders (UserId, TotalAmount, ShippingAddress, Phone, Email, OrderStatus, CreatedAt)
VALUES ('8458B6FB-292C-4123-9CD1-77D5C21C011C', 3519000, N'Hà Nội', '0987654321', 'tranthinh130720052@gmail.com', N'Completed', GETDATE());

 /* =================================
	OrderItems 
 ===================================*/
INSERT INTO OrderItems (OrderId, ProductVariantId, UnitPrice, Quantity)
VALUES (2, 2, 3519000, 1);


 /* =================================
	CartItems 
 ===================================*/ 
INSERT INTO CartItems (UserId, ProductVariantId, Quantity, AddedAt)
VALUES ('8458B6FB-292C-4123-9CD1-77D5C21C011C', 2, 1, GETDATE());

 /* =================================
	Reviews 
 ===================================*/ 
INSERT INTO Reviews (ProductVariantId, UserId, Rating, Title, [Content], IsApproved, CreatedAt)
VALUES (2, '8458B6FB-292C-4123-9CD1-77D5C21C011C', 5, N'Hàng đẹp chuẩn', N'Giày đẹp, đúng mô tả, đi rất êm', 1, GETDATE());

 /* =================================
	FavoriteItems 
 ===================================*/ 
INSERT INTO FavoriteItems (UserId, ProductVariantId)
VALUES ('8458B6FB-292C-4123-9CD1-77D5C21C011C', 2);

/* =================================
	VOUCHERS 
 ===================================*/ 
INSERT INTO VOUCHERS (Name, Description, VoucherType, StartDate, EndDate, IsActive, CreatedAt)
VALUES (N'Giảm 10%', N'Giảm 10% cho đơn hàng từ 500K', 'DiscountPercent', '2025-10-20', '2025-12-31', 1, GETDATE());

 /* =================================
	VOUCHERUSERS 
 ===================================*/ 
INSERT INTO VOUCHERUSERS (VoucherId, UserId, UsedCount)
VALUES (1, '8458B6FB-292C-4123-9CD1-77D5C21C011C', 3);