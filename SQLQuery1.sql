CREATE TABLE [dbo].[Users] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL,
    [Password] NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [FullName] NVARCHAR(200) NOT NULL
);

CREATE TABLE [dbo].[Customers] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [ContactPhone] NVARCHAR(50),
    [Address] NVARCHAR(500),
    [IsEnterprise] BIT NOT NULL DEFAULT 0,
    [CustomerType] NVARCHAR(50),
    [OutstandingBalance] DECIMAL(18,2) DEFAULT 0,
    [MonthlyCreditLimit] DECIMAL(18,2) DEFAULT 0,
    [CurrentMonthDebt] DECIMAL(18,2) DEFAULT 0
);

CREATE TABLE [dbo].[Products] (
    [Id] INT NOT NULL PRIMARY KEY,
    [ProductCode] NVARCHAR(50) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [SerialNumber] NVARCHAR(100),
    [IsPallet] BIT NOT NULL DEFAULT 0,
    [PalletCode] NVARCHAR(50),
    [Quantity] INT NOT NULL DEFAULT 0,
    [Price] DECIMAL(18,2) NOT NULL,
    [Location] NVARCHAR(200),
    [CreatedAt] DATETIME NOT NULL,
    [Category] NVARCHAR(100),
    [ImageUrl] NVARCHAR(500)
);

CREATE TABLE [dbo].[Orders] (
    [Id] INT NOT NULL PRIMARY KEY,
    [OrderNumber] NVARCHAR(50) NOT NULL,
    [CustomerId] INT NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,
    [Total] DECIMAL(18,2) NOT NULL,
    [CreatedAt] DATETIME NOT NULL,
    [IsPaid] BIT NOT NULL DEFAULT 0,
    [IsPaymentConfirmed] BIT NOT NULL DEFAULT 0,
    [IsInvoiced] BIT NOT NULL DEFAULT 0,
    [PaymentMethodString] NVARCHAR(50),
    [TotalWeightKg] DECIMAL(18,2) DEFAULT 0,
    [InvoiceNumber] NVARCHAR(50)
);

CREATE TABLE [dbo].[Routes] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Destination] NVARCHAR(500),
    [Date] DATETIME NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Assigned',
    [AssignedDriverId] INT,
    [AssignedVehicleId] INT,
    [OrderIds] NVARCHAR(MAX),
    [OrderCount] INT DEFAULT 0,
    [TotalAmount] DECIMAL(18,2) DEFAULT 0,
    [ItemsCount] INT DEFAULT 0,
    [TotalMassKg] DECIMAL(18,2) DEFAULT 0,
    [SignatureData] NVARCHAR(MAX),
    [SignatureDate] DATETIME,
    [DeliveryPhotoUrl] NVARCHAR(MAX)
);

CREATE TABLE [dbo].[Returns] (
    [ReturnId] INT NOT NULL PRIMARY KEY,
    [OrderId] INT NOT NULL,
    [ReturnDate] DATETIME NOT NULL,
    [ReturnReason] NVARCHAR(200),
    [CustomerReason] NVARCHAR(500),
    [DriverPhotoUrl] NVARCHAR(MAX),
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [RefundAmount] DECIMAL(18,2),
    [ReceiverNotes] NVARCHAR(1000),
    [InspectionDate] DATETIME,
    [Condition] NVARCHAR(100),
    [StockAction] NVARCHAR(100)
);

CREATE TABLE [dbo].[ReturnItems] (
    [ReturnItemId] INT NOT NULL PRIMARY KEY,
    [ReturnId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    [Condition] NVARCHAR(100),
    [StockAction] NVARCHAR(100),
    [IsStockAdjusted] BIT DEFAULT 0
);

CREATE TABLE [dbo].[StockBatches] (
    [Id] INT NOT NULL PRIMARY KEY,
    [ProductId] INT NOT NULL,
    [BatchNumber] NVARCHAR(100) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitWeightKg] DECIMAL(18,2) NOT NULL,
    [ReceivedDate] DATETIME NOT NULL,
    [ExpiryDate] DATETIME,
    [Location] NVARCHAR(200),
    [Supplier] NVARCHAR(200),
    [CostPerUnit] DECIMAL(18,2)
);