USE master;
GO

SELECT 
    session_id, 
    login_name, 
    host_name, 
    program_name
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('TourBookingSystem');

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += 'KILL ' + CAST(session_id AS NVARCHAR(10)) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('TourBookingSystem');

EXEC(@sql);

ALTER DATABASE TourBookingSystem SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

DROP DATABASE TourBookingSystem;
GO


CREATE DATABASE TourBookingSystem;
GO
USE TourBookingSystem;
GO

-- Tạo bảng người dùng, phân quyền và seed roles
CREATE TABLE dbo.Roles (
  RoleID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Roles PRIMARY KEY DEFAULT NEWID(),
  RoleName NVARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE,
  Description NVARCHAR(250) NULL
);

CREATE TABLE dbo.Users (
  UserID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY NONCLUSTERED DEFAULT NEWID(),
  Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
  PasswordHash VARBINARY(512) NOT NULL,
  PasswordSalt VARBINARY(64) NOT NULL,
  PasswordAlgo NVARCHAR(100) NOT NULL,
  Email NVARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
  Phone NVARCHAR(20) NULL,
  FullName NVARCHAR(100) NULL,
  Address NVARCHAR(255) NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.UserRoles (
  UserID UNIQUEIDENTIFIER NOT NULL,
  RoleID UNIQUEIDENTIFIER NOT NULL,
  AssignedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  AssignedBy UNIQUEIDENTIFIER NULL,
  CONSTRAINT PK_UserRoles PRIMARY KEY NONCLUSTERED (UserID, RoleID),
  CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID),
  CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
  CONSTRAINT FK_UserRoles_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES dbo.Users(UserID)
);

-- Seed roles (chạy trong cùng batch)
INSERT INTO dbo.Roles (RoleID, RoleName, Description) VALUES (NEWID(), N'Customer', N'Customer / Guest');
INSERT INTO dbo.Roles (RoleID, RoleName, Description) VALUES (NEWID(), N'AdministrativeStaff', N'Tour guide');

-- Tạo clustered index cho Users và UserRoles, index phụ
CREATE CLUSTERED INDEX CX_Users_CreatedAt_UserID ON dbo.Users (CreatedAt, UserID);

CREATE CLUSTERED INDEX CX_UserRoles_AssignedAt_UserID ON dbo.UserRoles (AssignedAt, UserID);

CREATE NONCLUSTERED INDEX IX_UserRoles_RoleID ON dbo.UserRoles(RoleID);

-- Tạo bảng địa điểm
-- Countries (quốc gia, dùng UUID)
CREATE TABLE dbo.Countries (
  CountryID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Countries PRIMARY KEY DEFAULT NEWID(),
  ISO2 NVARCHAR(2) NULL CONSTRAINT UQ_Countries_ISO2 UNIQUE,
  ISO3 NVARCHAR(3) NULL CONSTRAINT UQ_Countries_ISO3 UNIQUE,
  CountryName NVARCHAR(200) NOT NULL CONSTRAINT UQ_Countries_Name UNIQUE,
  CurrencyCode NVARCHAR(10) NULL,
  PhoneCode NVARCHAR(20) NULL,
  Timezone NVARCHAR(100) NULL,
  Region NVARCHAR(100) NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE NONCLUSTERED INDEX IX_Countries_CountryName ON dbo.Countries(CountryName);

-- Locations (địa điểm, tham chiếu CountryID)
CREATE TABLE dbo.Locations (
  LocationID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Locations PRIMARY KEY DEFAULT NEWID(),
  LocationName NVARCHAR(200) NOT NULL,
  CountryID UNIQUEIDENTIFIER NULL,
  City NVARCHAR(100) NULL,
  Address NVARCHAR(500) NULL,
  Latitude DECIMAL(9,6) NULL,
  Longitude DECIMAL(9,6) NULL,
  Description NVARCHAR(MAX) NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  IsDeleted BIT NOT NULL DEFAULT 0,
  CONSTRAINT FK_Locations_Countries FOREIGN KEY (CountryID) REFERENCES dbo.Countries(CountryID)
);

CREATE NONCLUSTERED INDEX IX_Locations_CountryID ON dbo.Locations(CountryID);
CREATE NONCLUSTERED INDEX IX_Locations_City ON dbo.Locations(City);
CREATE NONCLUSTERED INDEX IX_Locations_LatLong ON dbo.Locations(Latitude, Longitude);


-- Tạo bảng hạng mục tour - ví dụ: du lịch biển, núi, văn hóa
CREATE TABLE dbo.Categories (
  CategoryID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Categories PRIMARY KEY DEFAULT NEWID(),
  CategoryName NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_CategoryName UNIQUE,
  Description NVARCHAR(MAX) NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Du lịch biển
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch biển', N'Tour nghỉ dưỡng tại các bãi biển nổi tiếng, hoạt động lặn biển, chèo thuyền, tiệc BBQ hải sản.');

-- Du lịch núi
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch núi', N'Khám phá núi rừng, trekking, leo núi, cắm trại, trải nghiệm thiên nhiên hoang sơ.');

-- Du lịch văn hóa
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch văn hóa', N'Tìm hiểu phong tục tập quán, lễ hội truyền thống, tham quan di tích lịch sử và bảo tàng.');

-- Du lịch sinh thái
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch sinh thái', N'Tour tham quan rừng ngập mặn, vườn quốc gia, trải nghiệm thiên nhiên gắn với bảo tồn môi trường.');

-- Du lịch tâm linh
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch tâm linh', N'Hành hương đến các chùa, đền, nhà thờ, trải nghiệm không gian tôn giáo và tín ngưỡng.');

-- Du lịch nghỉ dưỡng cao cấp
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch nghỉ dưỡng cao cấp', N'Tour tại resort 5 sao, spa, golf, dịch vụ sang trọng dành cho khách hàng cao cấp.');

-- Du lịch mạo hiểm
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch mạo hiểm', N'Hoạt động thể thao mạo hiểm như nhảy dù, leo vách đá, lặn biển sâu, khám phá hang động.');

-- Du lịch ẩm thực
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch ẩm thực', N'Tour trải nghiệm đặc sản vùng miền, lớp học nấu ăn, tham quan chợ truyền thống và nhà hàng nổi tiếng.');

-- Du lịch nông thôn
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch nông thôn', N'Trải nghiệm cuộc sống làng quê, tham gia sản xuất nông nghiệp, thưởng thức đặc sản dân dã.');

-- Du lịch kết hợp hội nghị (MICE)
INSERT INTO dbo.Categories (CategoryName, Description)
VALUES (N'Du lịch MICE', N'Tour kết hợp hội nghị, hội thảo, triển lãm và du lịch, phục vụ doanh nghiệp và tổ chức.');


-- Tạo bảng Tours (Tour du lịch - lõi của hệ thống, hỗ trợ quản lý tour) 
CREATE TABLE dbo.Tours (
  TourID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tours PRIMARY KEY NONCLUSTERED DEFAULT NEWID(),
  TourName NVARCHAR(200) NOT NULL,
  Slug NVARCHAR(250) NULL,
  ShortDescription NVARCHAR(500) NULL,
  Description NVARCHAR(MAX) NULL,
  LocationID UNIQUEIDENTIFIER NULL,
  CategoryID UNIQUEIDENTIFIER NULL,
  DefaultGuideID UNIQUEIDENTIFIER NULL,
  IsPublished BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  IsDeleted BIT NOT NULL DEFAULT 0,
  CONSTRAINT FK_Tours_Locations FOREIGN KEY (LocationID) REFERENCES dbo.Locations(LocationID),
  CONSTRAINT FK_Tours_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(CategoryID),
  CONSTRAINT FK_Tours_DefaultGuide FOREIGN KEY (DefaultGuideID) REFERENCES dbo.Users(UserID)
);

CREATE CLUSTERED INDEX CX_Tours_CreatedAt_TourID ON dbo.Tours (CreatedAt, TourID);

CREATE TABLE dbo.TourInstances (
  InstanceID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TourInstances PRIMARY KEY NONCLUSTERED DEFAULT NEWID(),
  TourID UNIQUEIDENTIFIER NOT NULL,
  StartDate DATETIME2(3) NOT NULL,
  EndDate DATETIME2(3) NOT NULL,
  Capacity INT NOT NULL CHECK (Capacity >= 0),
  SeatsBooked INT NOT NULL DEFAULT 0 CHECK (SeatsBooked >= 0),
  SeatsHeld INT NOT NULL DEFAULT 0 CHECK (SeatsHeld >= 0),     -- số chỗ đang hold tạm
  HoldExpires DATETIME2(3) NULL,                                -- thời điểm hold sẽ hết hạn (nếu có)
  Status NVARCHAR(50) NOT NULL DEFAULT N'Open',
  PriceBase DECIMAL(18,2) NOT NULL,
  Currency NVARCHAR(10) NOT NULL DEFAULT N'USD',
  GuideID UNIQUEIDENTIFIER NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  CONSTRAINT FK_TourInstances_Tours FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID),
  CONSTRAINT FK_TourInstances_Guide FOREIGN KEY (GuideID) REFERENCES dbo.Users(UserID)
);

CREATE CLUSTERED INDEX CX_TourInstances_StartDate_InstanceID ON dbo.TourInstances (StartDate, InstanceID);
CREATE NONCLUSTERED INDEX IX_TourInstances_TourID_StartDate ON dbo.TourInstances (TourID, StartDate);

-- Part 4: TourPrices, Itineraries, TourImages
CREATE TABLE dbo.TourPrices (
  TourPriceID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TourPrices PRIMARY KEY DEFAULT NEWID(),
  InstanceID UNIQUEIDENTIFIER NULL,
  TourID UNIQUEIDENTIFIER NULL,
  PriceType NVARCHAR(50) NOT NULL,
  Amount DECIMAL(18,2) NOT NULL,
  Currency NVARCHAR(10) NOT NULL DEFAULT N'USD',
  CONSTRAINT FK_TourPrices_Instance FOREIGN KEY (InstanceID) REFERENCES dbo.TourInstances(InstanceID),
  CONSTRAINT FK_TourPrices_Tour FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID)
);
CREATE NONCLUSTERED INDEX IX_TourPrices_InstanceID ON dbo.TourPrices(InstanceID);
CREATE NONCLUSTERED INDEX IX_TourPrices_TourID ON dbo.TourPrices(TourID);

CREATE TABLE dbo.Itineraries (
  ItineraryID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Itineraries PRIMARY KEY DEFAULT NEWID(),
  TourID UNIQUEIDENTIFIER NOT NULL,
  DayIndex INT NOT NULL,
  Title NVARCHAR(250) NULL,
  Description NVARCHAR(MAX) NULL,
  CONSTRAINT FK_Itineraries_Tours FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID)
);

CREATE TABLE dbo.TourImages (
  ImageID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TourImages PRIMARY KEY DEFAULT NEWID(),
  TourID UNIQUEIDENTIFIER NOT NULL,
  Provider NVARCHAR(50) NOT NULL DEFAULT N'AzureBlob',
  Url NVARCHAR(1000) NOT NULL,
  Path NVARCHAR(1000) NULL,
  FileName NVARCHAR(255) NULL,
  MimeType NVARCHAR(100) NULL,
  FileSize INT NULL,
  Width INT NULL,
  Height INT NULL,
  IsPrimary BIT NOT NULL DEFAULT 0,
  SortOrder INT NOT NULL DEFAULT 0,
  AltText NVARCHAR(500) NULL,
  Checksum NVARCHAR(128) NULL,
  UploadedBy UNIQUEIDENTIFIER NULL,
  UploadedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  IsPublic BIT NOT NULL DEFAULT 1,
  CONSTRAINT FK_TourImages_Tours FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID),
  CONSTRAINT FK_TourImages_Users FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(UserID)
);

-- 6. Promotions (Khuyến mãi - hỗ trợ áp dụng giảm giá) 
-- Promotions: chiến dịch cơ bản
CREATE TABLE dbo.Promotions (
  PromotionID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Promotions PRIMARY KEY DEFAULT NEWID(),
  Name NVARCHAR(200) NOT NULL,
  Slug NVARCHAR(200) NULL,
  Description NVARCHAR(MAX) NULL,
  PromotionType NVARCHAR(50) NOT NULL,    -- e.g., 'Coupon','Automatic','FlashSale'
  StartAt DATETIME2(3) NULL,
  EndAt DATETIME2(3) NULL,
  IsActive BIT NOT NULL DEFAULT 1,
  Priority INT NOT NULL DEFAULT 100,       -- nhỏ hơn = ưu tiên trước
  AllowStack BIT NOT NULL DEFAULT 0,       -- có cho phép cộng khuyến mãi khác không
  MaxGlobalUses INT NULL,                  -- NULL = unlimited
  MaxUsesPerUser INT NULL,                 -- NULL = unlimited
  MinTotalAmount DECIMAL(18,2) NULL,       -- điều kiện tối thiểu
  MinSeats INT NULL,                       -- điều kiện tối thiểu số ghế
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL
);

CREATE NONCLUSTERED INDEX IX_Promotions_IsActive_StartEnd ON dbo.Promotions(IsActive, StartAt, EndAt);

-- Part B1: Services (dịch vụ độc lập, UUID PK) - Moved before Bookings to satisfy FK constraint
CREATE TABLE dbo.Services (
  ServiceID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Services PRIMARY KEY DEFAULT NEWID(),
  ServiceName NVARCHAR(200) NOT NULL,
  Code NVARCHAR(50) NULL,                     -- mã nội bộ
  Description NVARCHAR(MAX) NULL,
  Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
  Currency NVARCHAR(10) NOT NULL DEFAULT N'USD',
  IsActive BIT NOT NULL DEFAULT 1,
  IsTaxable BIT NOT NULL DEFAULT 1,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE NONCLUSTERED INDEX IX_Services_ServiceName ON dbo.Services(ServiceName);

-- Tạo bảng Bookings (Đặt tour - liên kết khách và tour, hỗ trợ đặt/hủy) 
CREATE TABLE dbo.Bookings (
  BookingID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY NONCLUSTERED DEFAULT NEWID(),
  InstanceID UNIQUEIDENTIFIER NOT NULL,
  UserID UNIQUEIDENTIFIER NOT NULL,
  BookingRef NVARCHAR(50) NOT NULL CONSTRAINT UQ_Bookings_BookingRef UNIQUE,
  Seats INT NOT NULL CHECK (Seats > 0),
  TotalAmount DECIMAL(14,2) NOT NULL,
  Currency NVARCHAR(10) NOT NULL,
  Status NVARCHAR(50) NOT NULL DEFAULT N'Pending',
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt DATETIME2(3) NULL,
  CONSTRAINT FK_Bookings_Instance FOREIGN KEY (InstanceID) REFERENCES dbo.TourInstances(InstanceID),
  CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
CREATE CLUSTERED INDEX CX_Bookings_CreatedAt_BookingID ON dbo.Bookings (CreatedAt, BookingID);
CREATE NONCLUSTERED INDEX IX_Bookings_InstanceID ON dbo.Bookings(InstanceID);
CREATE NONCLUSTERED INDEX IX_Bookings_UserID ON dbo.Bookings(UserID);

CREATE TABLE dbo.BookingServices (
      BookingServiceID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BookingServices PRIMARY KEY DEFAULT NEWID(),
      BookingID UNIQUEIDENTIFIER NOT NULL,
      ServiceID UNIQUEIDENTIFIER NOT NULL,
      Quantity INT NOT NULL DEFAULT 1 CHECK (Quantity >= 1),
      PriceAtBooking DECIMAL(18,2) NOT NULL,
      Currency NVARCHAR(10) NOT NULL DEFAULT N'VND',
      CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
      CONSTRAINT FK_BookingServices_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID) ON DELETE CASCADE,
      CONSTRAINT FK_BookingServices_Services FOREIGN KEY (ServiceID) REFERENCES dbo.Services(ServiceID)
    );

CREATE NONCLUSTERED INDEX IX_BookingServices_BookingID ON dbo.BookingServices(BookingID);
CREATE NONCLUSTERED INDEX IX_BookingServices_ServiceID ON dbo.BookingServices(ServiceID);
ALTER TABLE dbo.Bookings ADD SpecialRequests NVARCHAR(MAX) NULL;

-- PromotionRules: chi tiết cách tính giảm (tách logic cho linh hoạt)
CREATE TABLE dbo.PromotionRules (
  PromotionRuleID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PromotionRules PRIMARY KEY DEFAULT NEWID(),
  PromotionID UNIQUEIDENTIFIER NOT NULL,
  RuleType NVARCHAR(50) NOT NULL,          -- 'Percent','Fixed','FreeSeat','BuyXGetY','FreeService'
  Value DECIMAL(18,6) NOT NULL,            -- percent as 10.00 = 10%, or fixed amount in currency
  Currency NVARCHAR(10) NULL,              -- nếu RuleType = Fixed
  MaxDiscountAmount DECIMAL(18,2) NULL,    -- cap per application
  AppliesToSeatType NVARCHAR(50) NULL,     -- optional seat type
  CONSTRAINT FK_PromotionRules_Promotions FOREIGN KEY (PromotionID) REFERENCES dbo.Promotions(PromotionID)
);

CREATE NONCLUSTERED INDEX IX_PromotionRules_PromotionID ON dbo.PromotionRules(PromotionID);

-- PromotionTargets: áp dụng cho Tour / Instance / Category / All
CREATE TABLE dbo.PromotionTargets (
  PromotionTargetID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PromotionTargets PRIMARY KEY DEFAULT NEWID(),
  PromotionID UNIQUEIDENTIFIER NOT NULL,
  TargetType NVARCHAR(50) NOT NULL,        -- 'All','Tour','Instance','Category'
  TargetID UNIQUEIDENTIFIER NULL,          -- NULL if TargetType = 'All'
  CONSTRAINT FK_PromotionTargets_Promotions FOREIGN KEY (PromotionID) REFERENCES dbo.Promotions(PromotionID)
);
CREATE NONCLUSTERED INDEX IX_PromotionTargets_PromotionID ON dbo.PromotionTargets(PromotionID);
CREATE NONCLUSTERED INDEX IX_PromotionTargets_TargetType_TargetID ON dbo.PromotionTargets(TargetType, TargetID);

-- Coupons: mã coupon cụ thể (0..n mã cho 1 Promotion)
CREATE TABLE dbo.Coupons (
  CouponID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Coupons PRIMARY KEY DEFAULT NEWID(),
  PromotionID UNIQUEIDENTIFIER NOT NULL,
  Code NVARCHAR(100) NOT NULL CONSTRAINT UQ_Coupons_Code UNIQUE,
  Description NVARCHAR(500) NULL,
  IsActive BIT NOT NULL DEFAULT 1,
  StartsAt DATETIME2(3) NULL,
  ExpiresAt DATETIME2(3) NULL,
  MaxUses INT NULL,                        -- override promotion global uses if set
  MaxUsesPerUser INT NULL,                 -- override promotion per-user
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Coupons_Promotions FOREIGN KEY (PromotionID) REFERENCES dbo.Promotions(PromotionID)
);
CREATE NONCLUSTERED INDEX IX_Coupons_Code ON dbo.Coupons(Code);

-- PromotionRedemptions: mỗi lần apply coupon/promotion, audit và enforce
CREATE TABLE dbo.PromotionRedemptions (
  RedemptionID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PromotionRedemptions PRIMARY KEY DEFAULT NEWID(),
  PromotionID UNIQUEIDENTIFIER NOT NULL,
  CouponID UNIQUEIDENTIFIER NULL,
  BookingID UNIQUEIDENTIFIER NULL,
  UserID UNIQUEIDENTIFIER NULL,
  InstanceID UNIQUEIDENTIFIER NULL,
  AppliedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  DiscountAmount DECIMAL(18,2) NOT NULL,
  Currency NVARCHAR(10) NOT NULL,
  Status NVARCHAR(50) NOT NULL DEFAULT N'Applied', -- Applied, Voided, Expired
  Notes NVARCHAR(1000) NULL,
  CONSTRAINT FK_PromotionRedemptions_Promotions FOREIGN KEY (PromotionID) REFERENCES dbo.Promotions(PromotionID),
  CONSTRAINT FK_PromotionRedemptions_Coupons FOREIGN KEY (CouponID) REFERENCES dbo.Coupons(CouponID),
  CONSTRAINT FK_PromotionRedemptions_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID),
  CONSTRAINT FK_PromotionRedemptions_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
CREATE NONCLUSTERED INDEX IX_PromotionRedemptions_Promo ON dbo.PromotionRedemptions(PromotionID);
CREATE NONCLUSTERED INDEX IX_PromotionRedemptions_User ON dbo.PromotionRedemptions(UserID);

-- Optional: PromotionUsage aggregate counters (keeps simple counters, update in tx)
CREATE TABLE dbo.PromotionUsage (
  PromotionID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PromotionUsage PRIMARY KEY,
  TotalUses INT NOT NULL DEFAULT 0,
  LastUpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_PromotionUsage_Promotions FOREIGN KEY (PromotionID) REFERENCES dbo.Promotions(PromotionID)
);

-- 8. Payments (Thanh toán - hỗ trợ thanh toán cho booking) 
CREATE TABLE dbo.Payments (
  PaymentID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY DEFAULT NEWID(),
  BookingID UNIQUEIDENTIFIER NOT NULL,
  PaymentMethod NVARCHAR(50) NOT NULL,
  Amount DECIMAL(14,2) NOT NULL,
  Currency NVARCHAR(10) NOT NULL,
  TransactionRef NVARCHAR(200) NULL,
  Status NVARCHAR(50) NOT NULL DEFAULT N'Completed',
  PaidAt DATETIME2(3) NULL,
  CONSTRAINT FK_Payments_Bookings FOREIGN KEY (BookingID) REFERENCES dbo.Bookings(BookingID)
);
CREATE NONCLUSTERED INDEX IX_Payments_BookingID ON dbo.Payments(BookingID);

-- 9. Reviews (Đánh giá - hỗ trợ đánh giá sau tour) 
CREATE TABLE dbo.Reviews (
  ReviewID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY DEFAULT NEWID(),
  TourID UNIQUEIDENTIFIER NOT NULL,
  UserID UNIQUEIDENTIFIER NOT NULL,
  Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
  Comment NVARCHAR(MAX) NULL,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Reviews_Tours FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID),
  CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);
CREATE NONCLUSTERED INDEX IX_Reviews_TourID ON dbo.Reviews(TourID);

-- 10. Notifications (Thông báo - hỗ trợ gửi thông báo cho người dùng) 
-- Part A: Notifications (UUID PK, tham chiếu Users.UserID)
CREATE TABLE dbo.Notifications (
  NotificationID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY DEFAULT NEWID(),
  UserID UNIQUEIDENTIFIER NOT NULL,
  Title NVARCHAR(200) NOT NULL,
  Message NVARCHAR(MAX) NOT NULL,
  Payload NVARCHAR(MAX) NULL,                -- thêm JSON payload nếu cần dữ liệu bổ sung
  NotificationType NVARCHAR(50) NULL,        -- e.g., 'Info','Alert','Promotion','System'
  Channel NVARCHAR(50) NULL,                 -- e.g., 'Email','Push','SMS','InApp'
  SentAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  IsRead BIT NOT NULL DEFAULT 0,
  ReadAt DATETIME2(3) NULL,
  IsDeleted BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserID) REFERENCES dbo.Users(UserID)
);

CREATE NONCLUSTERED INDEX IX_Notifications_UserID_SentAt ON dbo.Notifications(UserID, SentAt);
CREATE NONCLUSTERED INDEX IX_Notifications_IsRead_SentAt ON dbo.Notifications(IsRead, SentAt);


-- Part B2: TourServices (liên kết many to many giữa Tours và Services)
CREATE TABLE dbo.TourServices (
  TourServiceID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TourServices PRIMARY KEY DEFAULT NEWID(),
  TourID UNIQUEIDENTIFIER NOT NULL,
  ServiceID UNIQUEIDENTIFIER NOT NULL,
  PriceOverride DECIMAL(18,2) NULL,           -- nếu muốn giá riêng cho tour
  Currency NVARCHAR(10) NULL,
  IsIncluded BIT NOT NULL DEFAULT 0,          -- 1 = dịch vụ đã bao gồm trong tour
  SortOrder INT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT FK_TourServices_Tours FOREIGN KEY (TourID) REFERENCES dbo.Tours(TourID),
  CONSTRAINT FK_TourServices_Services FOREIGN KEY (ServiceID) REFERENCES dbo.Services(ServiceID)
);

CREATE NONCLUSTERED INDEX IX_TourServices_TourID ON dbo.TourServices(TourID);
CREATE NONCLUSTERED INDEX IX_TourServices_ServiceID ON dbo.TourServices(ServiceID);
CREATE UNIQUE NONCLUSTERED INDEX UQ_TourServices_Tour_Service ON dbo.TourServices(TourID, ServiceID);

-- Part C: Indexes bổ sung (chạy sau khi các bảng tồn tại)
CREATE NONCLUSTERED INDEX IDX_Tours_TourName ON dbo.Tours(TourName);
CREATE NONCLUSTERED INDEX IDX_Users_Email ON dbo.Users(Email);

DECLARE 
  @plain NVARCHAR(4000) = N'AdminPassword123!',   -- thay mật khẩu an toàn trước khi production
  @salt VARBINARY(32) = CRYPT_GEN_RANDOM(32),
  @hash VARBINARY(MAX),
  @i INT = 0,
  @iterations INT = 1000,
  @adminUserID UNIQUEIDENTIFIER,
  @adminRoleID UNIQUEIDENTIFIER;

SET @hash = CONVERT(VARBINARY(MAX), @plain) + @salt;

WHILE @i < @iterations
BEGIN
  SET @hash = HASHBYTES('SHA2_512', @hash);
  SET @i += 1;
END

SET @adminUserID = NEWID();

INSERT INTO dbo.Users (UserID, Username, Email, PasswordHash, PasswordSalt, PasswordAlgo, CreatedAt, FullName)
VALUES (
  @adminUserID,
  N'admin',
  N'ThanhLuanAdmin@gmail.com',
  @hash,
  @salt,
  N'SHA2_512+iter1000',
  SYSUTCDATETIME(),
  N'Administrator'
);

SELECT @adminRoleID = RoleID FROM dbo.Roles WHERE RoleName = N'AdministrativeStaff';

INSERT INTO dbo.UserRoles (UserID, RoleID, AssignedAt, AssignedBy)
VALUES (@adminUserID, @adminRoleID, SYSUTCDATETIME(), NULL);

-- Performance Optimization Indexes for Overbooking System
-- Run this SQL script to add indexes for faster queries

-- Index for finding pending bookings by instance (used in soft limit checks)
CREATE INDEX IX_Bookings_InstanceID_Status_CreatedAt 
ON Bookings(InstanceID, Status, CreatedAt)
INCLUDE (Seats);

-- Index for user pending booking checks
CREATE INDEX IX_Bookings_UserID_Status
ON Bookings(UserID, Status);

-- Composite index for tour instance lookups
CREATE INDEX IX_TourInstances_Status_StartDate
ON TourInstances(Status, StartDate);

PRINT 'Performance indexes created successfully';

select * from users;
select * from userroles;
select * from roles;

select * from Countries;
select * from Locations;
select * from Tours;
select * from TourInstances;
select * from TourPrices;
select * from Itineraries;
select * from TourImages;

select * from Services;
select * from TourServices;

select * from Bookings;
select * from BookingServices;