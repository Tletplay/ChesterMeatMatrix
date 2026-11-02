using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChesterMeatMatrix.Data
{
    // ========================================
    // ORDER NUMBER COUNTER (Global Counter)
    // ========================================
    public static class OrderNumberCounter
    {
        private static int _currentCounter = 0;
        private static readonly object _lock = new object();

        public static int GetNextCounter()
        {
            lock (_lock)
            {
                _currentCounter++;
                return _currentCounter;
            }
        }

        public static void InitializeCounter(int lastValue)
        {
            lock (_lock)
            {
                _currentCounter = lastValue;
            }
        }

        public static int GetCurrentCounter()
        {
            lock (_lock)
            {
                return _currentCounter;
            }
        }
    }

    // ========================================
    // MAIN IN-MEMORY DATABASE
    // ========================================
    public static class InMemoryDatabase
    {
        public static List<Product> Products { get; } = new List<Product>();
        public static List<Customer> Customers { get; } = new List<Customer>();
        public static List<Order> Orders { get; } = new List<Order>();
        public static List<User> Users { get; } = new List<User>();
        public static List<Route> Routes { get; } = new List<Route>();
        public static List<Vehicle> Vehicles { get; } = new List<Vehicle>();

        // ========================================
        // STOCK MANAGEMENT LISTS (Existing)
        // ========================================
        public static List<StockBatch> StockBatches { get; } = new List<StockBatch>();
        public static List<StockMovement> StockMovements { get; } = new List<StockMovement>();

        // ========================================
        // NEW STOCK MANAGEMENT LISTS (from merge)
        // ========================================
        public static List<StockLocation> StockLocations { get; } = new List<StockLocation>();
        public static List<ExpectedStock> ExpectedStocks { get; } = new List<ExpectedStock>();
        public static List<LowStockAlert> LowStockAlerts { get; } = new List<LowStockAlert>();
        public static List<ProductStockThreshold> ProductStockThresholds { get; } = new List<ProductStockThreshold>();
        public static List<BatchItemQrCode> BatchItemQrCodes { get; } = new List<BatchItemQrCode>();
        public static List<Pallet> Pallets { get; } = new List<Pallet>();
        public static List<PalletStockBatch> PalletStockBatches { get; } = new List<PalletStockBatch>();
        public static List<EnterpriseCart> EnterpriseCarts { get; } = new List<EnterpriseCart>();
        public static List<EnterpriseCartItem> EnterpriseCartItems { get; } = new List<EnterpriseCartItem>();
        public static List<CreditNote> CreditNotes { get; } = new List<CreditNote>();
        public static List<Truck> Trucks { get; } = new List<Truck>();
        public static List<StockAdjustment> StockAdjustments { get; } = new List<StockAdjustment>();
        public static List<Return> Returns { get; } = new List<Return>();
        public static List<ReturnItem> ReturnItems { get; } = new List<ReturnItem>();
        public static List<Claim> Claims { get; } = new List<Claim>();
        public static List<SupplierReturn> SupplierReturns { get; } = new List<SupplierReturn>();
        public static List<PalletGroup> PalletGroups { get; } = new List<PalletGroup>();
        public static List<Notification> Notifications { get; } = new List<Notification>();
        public static List<WorkTask> WorkTasks { get; } = new List<WorkTask>();
        public static List<TaskAssignment> TaskAssignments { get; } = new List<TaskAssignment>();
        public static List<Shift> Shifts { get; } = new List<Shift>();
        public static List<ShiftAssignment> ShiftAssignments { get; } = new List<ShiftAssignment>();
        public static List<Attendance> Attendances { get; } = new List<Attendance>();
        public static List<Performance> Performances { get; } = new List<Performance>();
        public static List<LeaveRequest> LeaveRequests { get; } = new List<LeaveRequest>();
        public static List<StockTake> StockTakes { get; } = new List<StockTake>();
        public static List<StockTakeAssignment> StockTakeAssignments { get; } = new List<StockTakeAssignment>();
        public static List<StockCountItem> StockCountItems { get; } = new List<StockCountItem>();

        static InMemoryDatabase()
        {
            Seed();
            InitializeOrderCounter();
        }

        public static void InitializeOrderCounter()
        {
            if (Orders.Any())
            {
                var maxCounter = Orders
                    .Where(o => !string.IsNullOrEmpty(o.OrderNumber))
                    .Select(o =>
                    {
                        var parts = o.OrderNumber.Split('-');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int counter))
                            return counter;
                        return 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                OrderNumberCounter.InitializeCounter(maxCounter);
            }
        }

        public static void Seed()
        {
            if (Users.Any()) return;

            // ========================================
            // SEED USERS
            // ========================================
            Users.AddRange(new[]
            {
                new User { Id = 100, Username = "Adminchester@.ac.za", Password = "$$Admin", Role = "Admin", FullName = "Chester Admin" },
                new User { Id = 1, Username = "checker1", Password = "password", Role = "Checker", FullName = "Checker One", EmployeeId = "222408030", FaceData = "" },
                new User { Id = 2, Username = "warehouse1", Password = "password", Role = "Warehouse", FullName = "Warehouse One", EmployeeId = "222408030", FaceData = "" },
                new User { Id = 3, Username = "sales1", Password = "password", Role = "SalesRep", FullName = "Sales Rep" },
                new User { Id = 4, Username = "router1", Password = "password", Role = "Router", FullName = "Router One" },
                new User { Id = 5, Username = "driver1", Password = "password", Role = "Driver", FullName = "Driver One", EmployeeId = "222408030" },
                new User { Id = 6, Username = "olwethu.jozela", Password = "password", Role = "Driver", FullName = "Olwethu Jozela" },
                new User { Id = 7, Username = "vusi.majola", Password = "password", Role = "Driver", FullName = "Vusi Majola" },
                new User { Id = 8, Username = "thando.moore", Password = "password", Role = "Driver", FullName = "Thando Moore" },
                new User { Id = 9, Username = "sibongile.khumalo", Password = "password", Role = "Driver", FullName = "Sibongile Khumalo" },
                new User { Id = 10, Username = "lwazi.ndlovu", Password = "password", Role = "Driver", FullName = "Lwazi Ndlovu" },
                new User { Id = 11, Username = "tahirah.meyer", Password = "password", Role = "Driver", FullName = "Tahirah Meyer" },
                new User { Id = 12, Username = "ndaba.smith", Password = "password", Role = "Driver", FullName = "Ndaba Smith" },
                new User { Id = 13, Username = "karen.louw", Password = "password", Role = "Driver", FullName = "Karen Louw" },
                new User { Id = 14, Username = "musa.zulu", Password = "password", Role = "Driver", FullName = "Musa Zulu" },
                new User { Id = 15, Username = "lerato.mokoena", Password = "password", Role = "Driver", FullName = "Lerato Mokoena" },
                new User { Id = 16, Username = "siphiwe.radebe", Password = "password", Role = "Driver", FullName = "Siphiwe Radebe" },
                new User { Id = 17, Username = "anita.botha", Password = "password", Role = "Driver", FullName = "Anita Botha" },
                new User { Id = 18, Username = "peter.kekana", Password = "password", Role = "Driver", FullName = "Peter Kekana" },
                new User { Id = 19, Username = "joyce.makgoba", Password = "password", Role = "Driver", FullName = "Joyce Makgoba" },
                new User { Id = 20, Username = "nelson.mashaba", Password = "password", Role = "Driver", FullName = "Nelson Mashaba" },
                new User { Id = 21, Username = "marcellus.phiri", Password = "password", Role = "Driver", FullName = "Marcellus Phiri" },
                new User { Id = 23, Username = "warehouse.manager", Password = "password", Role = "WarehouseManager", FullName = "Warehouse Manager" },
                new User { Id = 22, Username = "dumisani.magubane", Password = "password", Role = "Driver", FullName = "Dumisani Magubane" },
                new User { Id = 24, Username = "receiver1", Password = "password", Role = "Receiver", FullName = "Receiver One" },
                new User { Id = 30, Username = "testdriver", Password = "driver123", Role = "Driver", FullName = "Test Driver", EmployeeId = "222408030" },
                new User { Id = 31, Username = "receiver", Password = "receiver123", Role = "Receiver", FullName = "Test Receiver" },
            });

            // ========================================
            // SEED CUSTOMERS (with CustomerType)
            // ========================================
            Customers.AddRange(new[]
            {
                new Customer
                {
                    Id = 1,
                    Name = "Instore Customer",
                    IsEnterprise = false,
                    CustomerType = CustomerTypeEnum.Instore,
                    ContactPhone = "031 555 1234",
                    Address = "Chester Store - 45 Umbilo Road, Durban",
                    OutstandingBalance = 0m,
                    MonthlyCreditLimit = 0m,
                    CurrentMonthDebt = 0m
                },
                new Customer
                {
                    Id = 2,
                    Name = "Standard Customer",
                    IsEnterprise = false,
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "082 123 4567",
                    Address = "123 Main Street, Durban",
                    OutstandingBalance = 0m,
                    MonthlyCreditLimit = 0m,
                    CurrentMonthDebt = 0m
                }
            });

            // ========================================
            // SEED VEHICLES (existing - keep as is)
            // ========================================
            var vehicles = new List<Vehicle>();
            int vid = 3;
            for (int i = 1; i <= 20; i++)
            {
                vehicles.Add(new Vehicle
                {
                    Id = vid++,
                    Registration = $"CHT{i:000}",
                    Model = "Chester Truck",
                    Capacity = 2000 + (i % 5) * 200
                });
            }
            Vehicles.AddRange(vehicles);

            // ========================================
            // SEED PRODUCTS - SHORT CATALOG (8 products)
            // ========================================
            int nextId = 1;
            var products = new List<Product>();

            var catalogProducts = new[]
            {
                new { Code = "BEEF-001", Name = "Fresh Beef Chuck (1 kg)", Price = 89.99m, Qty = 100, Location = "Warehouse Floor 175", Category = "Beef", Image = "/Content/Images/Products/beef-chuck.jpg" },
                new { Code = "BEEF-003", Name = "Beef Mince (1 kg)", Price = 69.99m, Qty = 100, Location = "Warehouse Floor 175", Category = "Beef", Image = "/Content/Images/Products/beef-mince.jpg" },
                new { Code = "BEEF-010", Name = "Biltong (500g)", Price = 199.99m, Qty = 100, Location = "Warehouse Floor 191", Category = "Beef", Image = "/Content/Images/Products/biltong.jpg" },
                new { Code = "BEEF-005", Name = "Braai Wors / Boerewors (1 kg)", Price = 84.99m, Qty = 100, Location = "Warehouse Floor 175", Category = "Wors & Sausages", Image = "/Content/Images/Products/braai-wors.jpg" },
                new { Code = "PORK-003", Name = "Pork Sausages (1 kg)", Price = 64.99m, Qty = 100, Location = "Warehouse Floor 191", Category = "Wors & Sausages", Image = "/Content/Images/Products/pork-sausages.jpg" },
                new { Code = "CHICK-005", Name = "Chicken Wings (2 kg)", Price = 59.99m, Qty = 100, Location = "Freezer 180", Category = "Chicken", Image = "/Content/Images/Products/chicken-wings.jpg" },
                new { Code = "CHICK-003", Name = "Chicken Leg Quarters (5 kg)", Price = 79.99m, Qty = 100, Location = "Freezer 180", Category = "Chicken", Image = "/Content/Images/Products/chicken-leg-quarters.jpg" },
                new { Code = "CHICK-017", Name = "Fresh Whole Chicken (each ~1.8kg)", Price = 59.99m, Qty = 100, Location = "Freezer 180", Category = "Chicken", Image = "/Content/Images/Products/whole-chicken.jpg" }
            };

            foreach (var p in catalogProducts)
            {
                products.Add(new Product
                {
                    Id = nextId++,
                    ProductCode = p.Code,
                    Name = p.Name,
                    SerialNumber = $"SN{nextId}{DateTime.UtcNow.Ticks}",
                    IsPallet = false,
                    PalletCode = "",
                    Quantity = p.Qty,
                    Price = p.Price,
                    Location = p.Location,
                    CreatedAt = DateTime.UtcNow,
                    Category = p.Category,
                    ImageUrl = p.Image
                });
            }

           

            Products.AddRange(products);

            // ========================================
            // CREATE PALLET HEADER RECORDS FOR SEEDED PRODUCTS
            // ========================================
            int palletIdCounter = products.Max(p => p.Id) + 1;
            int palletCodeCounter = 8000;
            const int productsPerPallet = 15;
            int itemsInCurrentPallet = 0;
            string currentPalletCode = "";

            foreach (var product in products.Where(p => !p.IsPallet).OrderBy(p => p.Location))
            {
                if (itemsInCurrentPallet == 0)
                {
                    currentPalletCode = palletCodeCounter.ToString();
                    palletCodeCounter++;

                    var palletHeader = new Product
                    {
                        Id = palletIdCounter++,
                        ProductCode = "PAL-" + currentPalletCode,
                        Name = "Pallet - " + (product.Category ?? "Mixed Products"),
                        SerialNumber = "",
                        IsPallet = true,
                        PalletCode = currentPalletCode,
                        Quantity = 0,
                        Price = product.Price,
                        Location = product.Location,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        Category = "Pallet",
                        ImageUrl = "/Content/Images/placeholders/pallet.jpg"
                    };
                    Products.Add(palletHeader);
                }

                product.PalletCode = currentPalletCode;
                itemsInCurrentPallet++;
                if (itemsInCurrentPallet >= productsPerPallet)
                {
                    itemsInCurrentPallet = 0;
                }

                var pallet = Products.FirstOrDefault(p => p.IsPallet && p.PalletCode == currentPalletCode);
                if (pallet != null)
                {
                    pallet.Quantity = Products.Count(p => !p.IsPallet && p.PalletCode == currentPalletCode);
                }
            }

            // ========================================
            // SEED STOCK BATCHES
            // ========================================
            int batchId = 1;
            int movementId = 1;
            var stockBatches = new List<StockBatch>();
            var stockMovements = new List<StockMovement>();

            foreach (var product in products)
            {
                if (product.Quantity > 0)
                {
                    var batch1 = new StockBatch
                    {
                        Id = batchId++,
                        ProductId = product.Id,
                        Product = product,
                        BatchNumber = $"BATCH-{product.ProductCode}-001",
                        Quantity = product.Quantity / 2,
                        UnitWeightKg = 1.0m,
                        ReceivedDate = DateTime.UtcNow.AddDays(-30),
                        ExpiryDate = DateTime.UtcNow.AddMonths(6),
                        Location = product.Location,
                        Supplier = "Initial Supplier A",
                        CostPerUnit = product.Price * 0.65m
                    };
                    stockBatches.Add(batch1);

                    stockMovements.Add(new StockMovement
                    {
                        Id = movementId++,
                        ProductId = product.Id,
                        Product = product,
                        MovementType = "IN",
                        Quantity = batch1.Quantity,
                        Reference = batch1.BatchNumber,
                        MovementDate = batch1.ReceivedDate,
                        PerformedBy = "System",
                        Notes = "Initial stock seeding - Batch 1",
                        BatchId = batch1.Id
                    });

                    if (product.Quantity - (product.Quantity / 2) > 0)
                    {
                        var batch2 = new StockBatch
                        {
                            Id = batchId++,
                            ProductId = product.Id,
                            Product = product,
                            BatchNumber = $"BATCH-{product.ProductCode}-002",
                            Quantity = product.Quantity - (product.Quantity / 2),
                            UnitWeightKg = 1.0m,
                            ReceivedDate = DateTime.UtcNow.AddDays(-7),
                            ExpiryDate = DateTime.UtcNow.AddMonths(8),
                            Location = product.Location,
                            Supplier = "Initial Supplier B",
                            CostPerUnit = product.Price * 0.68m
                        };
                        stockBatches.Add(batch2);

                        stockMovements.Add(new StockMovement
                        {
                            Id = movementId++,
                            ProductId = product.Id,
                            Product = product,
                            MovementType = "IN",
                            Quantity = batch2.Quantity,
                            Reference = batch2.BatchNumber,
                            MovementDate = batch2.ReceivedDate,
                            PerformedBy = "System",
                            Notes = "Initial stock seeding - Batch 2",
                            BatchId = batch2.Id
                        });
                    }
                }
            }

            StockBatches.AddRange(stockBatches);
            StockMovements.AddRange(stockMovements);

            // ========================================
            // SEED STOCK LOCATIONS
            // ========================================
            if (!StockLocations.Any())
            {
                StockLocations.AddRange(new[]
                {
                    new StockLocation { StockLocationId = 1, Name = "Warehouse Floor 175", Warehouse = "Main Warehouse" },
                    new StockLocation { StockLocationId = 2, Name = "Warehouse Floor 191", Warehouse = "Main Warehouse" },
                    new StockLocation { StockLocationId = 3, Name = "Picking Freezer 180", Warehouse = "Cold Storage" },
                    new StockLocation { StockLocationId = 4, Name = "Mobile Freezer 170", Warehouse = "Cold Storage" },
                    new StockLocation { StockLocationId = 5, Name = "Congeller Freezer 200", Warehouse = "Cold Storage" },
                    new StockLocation { StockLocationId = 6, Name = "Receiving Bay", Warehouse = "Main Warehouse" }
                });
            }

            // ========================================
            // SEED TRUCKS
            // ========================================
            if (!Trucks.Any())
            {
                Trucks.AddRange(new[]
                {
                    new Truck { TruckId = 1, RegistrationNumber = "ND-45231", Capacity = 3500m, IsAvailable = true, CreatedDate = DateTime.UtcNow },
                    new Truck { TruckId = 2, RegistrationNumber = "ND-98314", Capacity = 5000m, IsAvailable = true, CreatedDate = DateTime.UtcNow },
                    new Truck { TruckId = 3, RegistrationNumber = "ND-77420", Capacity = 2800m, IsAvailable = true, CreatedDate = DateTime.UtcNow },
                    new Truck { TruckId = 4, RegistrationNumber = "ND-63951", Capacity = 4200m, IsAvailable = true, CreatedDate = DateTime.UtcNow }
                });
            }

            // ========================================
            // SEED PRODUCT STOCK THRESHOLDS
            // ========================================
            if (!ProductStockThresholds.Any())
            {
                int thresholdId = 1;
                foreach (var product in Products.Where(p => !p.IsPallet))
                {
                    ProductStockThresholds.Add(new ProductStockThreshold
                    {
                        ProductStockThresholdId = thresholdId++,
                        ProductId = product.Id,
                        MinimumStockLevel = 50,
                        ReorderQuantity = 100,
                        AutoCreateAlert = true,
                        DefaultBatchSize = 25,
                        DefaultUnitWeightKg = 1.0m,
                        LastUpdated = DateTime.UtcNow,
                        UpdatedBy = "System"
                    });
                }
            }

            // ========================================
            // DECLARE nextItemId and nextOrderId ONCE at the beginning of order seeding
            // ========================================
            int nextItemId = Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Any()
                ? Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Max(i => i.Id) + 1
                : 1;
            int nextOrderId = Orders.Any() ? Orders.Max(o => o.Id) + 1 : 1000;

            // ========================================
            // SEED TEST ORDERS
            // ========================================

            // Get or create test customer
            var testCardCustomer = Customers.FirstOrDefault(c => c.Name == "Test Card Customer");
            if (testCardCustomer == null)
            {
                testCardCustomer = new Customer
                {
                    Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 200,
                    Name = "Test Card Customer",
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "0821112222",
                    Address = "456 Ocean Drive, Durban North",
                    OutstandingBalance = 0,
                    MonthlyCreditLimit = 50000,
                    CurrentMonthDebt = 0
                };
                Customers.Add(testCardCustomer);
            }

            // Get or create products
            var beefMince = Products.FirstOrDefault(p => p.ProductCode == "BEEF-003");
            var chickenLegs = Products.FirstOrDefault(p => p.ProductCode == "CHICK-003");
            var porkChops = Products.FirstOrDefault(p => p.ProductCode == "PORK-001");
            var beefChuck = Products.FirstOrDefault(p => p.ProductCode == "BEEF-001");

            // Get or create checker customer
            var checkerCustomer = Customers.FirstOrDefault(c => c.Name == "Checker Test Customer");
            if (checkerCustomer == null)
            {
                checkerCustomer = new Customer
                {
                    Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 300,
                    Name = "Checker Test Customer",
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "0831234567",
                    Address = "789 Main Road, Durban",
                    OutstandingBalance = 0,
                    MonthlyCreditLimit = 50000,
                    CurrentMonthDebt = 0
                };
                Customers.Add(checkerCustomer);
            }

            // Get or create router customer
            var routerCustomer = Customers.FirstOrDefault(c => c.Name == "Router Test Customer");
            if (routerCustomer == null)
            {
                routerCustomer = new Customer
                {
                    Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 400,
                    Name = "Router Test Customer",
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "0841234567",
                    Address = "123 Durban Street, Durban",
                    OutstandingBalance = 0,
                    MonthlyCreditLimit = 50000,
                    CurrentMonthDebt = 0
                };
                Customers.Add(routerCustomer);
            }

            // ORDER: TEST-CARD-001
            if (!Orders.Any(o => o.OrderNumber == "TEST-CARD-001") && beefChuck != null && testCardCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = beefChuck.Id, Product = beefChuck, Quantity = 2, UnitPrice = beefChuck.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "TEST-CARD-001",
                    CustomerId = testCardCustomer.Id,
                    Customer = testCardCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    Items = items,
                    Total = beefChuck.Price * 2,
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-3),
                    PaymentMethodString = "Card",
                    IsInvoiced = false,
                    TotalWeightKg = 2
                });
            }

            // ORDER: TEST-CARD-002
            if (!Orders.Any(o => o.OrderNumber == "TEST-CARD-002") && chickenLegs != null && testCardCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = chickenLegs.Id, Product = chickenLegs, Quantity = 2, UnitPrice = chickenLegs.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "TEST-CARD-002",
                    CustomerId = testCardCustomer.Id,
                    Customer = testCardCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Items = items,
                    Total = chickenLegs.Price * 2,
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-2),
                    PaymentMethodString = "Card",
                    IsInvoiced = false,
                    TotalWeightKg = 10
                });
            }

            // ORDER: CHECKER-READY-001
            if (!Orders.Any(o => o.OrderNumber == "CHECKER-READY-001") && beefMince != null && checkerCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = beefMince.Id, Product = beefMince, Quantity = 5, UnitPrice = beefMince.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "CHECKER-READY-001",
                    CustomerId = checkerCustomer.Id,
                    Customer = checkerCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    Items = items,
                    Total = beefMince.Price * 5,
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-5),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-CHECKER-001",
                    InvoiceDate = DateTime.UtcNow.AddHours(-5),
                    TotalWeightKg = 0
                });
            }

            // ORDER: CHECKER-READY-002
            if (!Orders.Any(o => o.OrderNumber == "CHECKER-READY-002") && chickenLegs != null && checkerCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = chickenLegs.Id, Product = chickenLegs, Quantity = 3, UnitPrice = chickenLegs.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "CHECKER-READY-002",
                    CustomerId = checkerCustomer.Id,
                    Customer = checkerCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-4),
                    Items = items,
                    Total = chickenLegs.Price * 3,
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-4),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-CHECKER-002",
                    InvoiceDate = DateTime.UtcNow.AddHours(-4),
                    TotalWeightKg = 0
                });
            }

            // ORDER: CHECKER-READY-003
            if (!Orders.Any(o => o.OrderNumber == "CHECKER-READY-003") && beefMince != null && porkChops != null && checkerCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = beefMince.Id, Product = beefMince, Quantity = 2, UnitPrice = beefMince.Price },
                    new OrderItem { Id = nextItemId++, ProductId = porkChops.Id, Product = porkChops, Quantity = 4, UnitPrice = porkChops.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "CHECKER-READY-003",
                    CustomerId = checkerCustomer.Id,
                    Customer = checkerCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    Items = items,
                    Total = (beefMince.Price * 2) + (porkChops.Price * 4),
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-3),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-CHECKER-003",
                    InvoiceDate = DateTime.UtcNow.AddHours(-3),
                    TotalWeightKg = 0
                });
            }

            // ========================================
            // SEED VEHICLES WITH WEIGHT CAPABILITIES
            // ========================================
            if (!Vehicles.Any(v => v.Registration == "TEST-TRUCK-001"))
            {
                Vehicles.Add(new Vehicle
                {
                    Id = Vehicles.Any() ? Vehicles.Max(v => v.Id) + 1 : 100,
                    Registration = "TEST-TRUCK-001",
                    Model = "Hino 300",
                    Capacity = 3500,
                    EmptyWeightKg = 2800,
                    MaxLoadCapacityKg = 3500,
                    CurrentLoadWeightKg = 0
                });

                Vehicles.Add(new Vehicle
                {
                    Id = Vehicles.Any() ? Vehicles.Max(v => v.Id) + 1 : 101,
                    Registration = "TEST-TRUCK-002",
                    Model = "Isuzu NQR",
                    Capacity = 5000,
                    EmptyWeightKg = 3200,
                    MaxLoadCapacityKg = 5000,
                    CurrentLoadWeightKg = 0
                });
            }

            // ========================================
            // ORDER: ROUTER-READY-001 (Already weighed, status = Processing)
            // ========================================
            if (!Orders.Any(o => o.OrderNumber == "ROUTER-READY-001") && beefChuck != null && routerCustomer != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = nextItemId++, ProductId = beefChuck.Id, Product = beefChuck, Quantity = 10, UnitPrice = beefChuck.Price }
                };
                Orders.Add(new Order
                {
                    Id = nextOrderId++,
                    OrderNumber = "ROUTER-READY-001",
                    CustomerId = routerCustomer.Id,
                    Customer = routerCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Items = items,
                    Total = beefChuck.Price * 10,
                    Status = OrderStatusEnum.Processing,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-1),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    TotalWeightKg = 10
                });
            }


            // ========================================
            // SEED: TEST DRIVER ROUTE ORDERS (for driver testing)
            // ========================================

            // Get the existing Test Driver user (DO NOT CREATE NEW)
            var testDriver = Users.FirstOrDefault(u => u.Username == "testdriver");

            if (testDriver != null)
            {
                // Get or create test customers for orders
                var driverCustomer1 = Customers.FirstOrDefault(c => c.Name == "Driver Test Customer 1");
                if (driverCustomer1 == null)
                {
                    driverCustomer1 = new Customer
                    {
                        Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 500,
                        Name = "Driver Test Customer 1",
                        CustomerType = CustomerTypeEnum.Standard,
                        ContactPhone = "0711111111",
                        Address = "123 Main Street, Durban",
                        OutstandingBalance = 0,
                        MonthlyCreditLimit = 50000,
                        CurrentMonthDebt = 0
                    };
                    Customers.Add(driverCustomer1);
                }

                var driverCustomer2 = Customers.FirstOrDefault(c => c.Name == "Driver Test Customer 2");
                if (driverCustomer2 == null)
                {
                    driverCustomer2 = new Customer
                    {
                        Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 501,
                        Name = "Driver Test Customer 2",
                        CustomerType = CustomerTypeEnum.Standard,
                        ContactPhone = "0722222222",
                        Address = "456 Beach Road, Umhlanga",
                        OutstandingBalance = 0,
                        MonthlyCreditLimit = 50000,
                        CurrentMonthDebt = 0
                    };
                    Customers.Add(driverCustomer2);
                }

                var driverCustomer3 = Customers.FirstOrDefault(c => c.Name == "Driver Test Customer 3");
                if (driverCustomer3 == null)
                {
                    driverCustomer3 = new Customer
                    {
                        Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 502,
                        Name = "Driver Test Customer 3",
                        CustomerType = CustomerTypeEnum.Standard,
                        ContactPhone = "0733333333",
                        Address = "789 Westville Road, Westville",
                        OutstandingBalance = 0,
                        MonthlyCreditLimit = 50000,
                        CurrentMonthDebt = 0
                    };
                    Customers.Add(driverCustomer3);
                }

                // Get products
                beefMince = Products.FirstOrDefault(p => p.ProductCode == "BEEF-003");
                chickenLegs = Products.FirstOrDefault(p => p.ProductCode == "CHICK-003");
                beefChuck = Products.FirstOrDefault(p => p.ProductCode == "BEEF-001");

                // Get next IDs
                int driverNextItemId = Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Any()
                    ? Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Max(i => i.Id) + 1
                    : 1;
                int driverNextOrderId = Orders.Any() ? Orders.Max(o => o.Id) + 1 : 5000;

                var driverOrderIds = new List<int>();

                // ORDER 1: Beef Mince x3 for Customer 1 (3 kg)
                if (!Orders.Any(o => o.OrderNumber == "DRIVER-ORDER-001") && beefMince != null)
                {
                    var items = new List<OrderItem>
                    {
                        new OrderItem { Id = driverNextItemId++, ProductId = beefMince.Id, Product = beefMince, Quantity = 3, UnitPrice = beefMince.Price }
                    };
                    var order1 = new Order
                    {
                        Id = driverNextOrderId++,
                        OrderNumber = "DRIVER-ORDER-001",
                        CustomerId = driverCustomer1.Id,
                        Customer = driverCustomer1,
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        Items = items,
                        Total = beefMince.Price * 3,
                        Status = OrderStatusEnum.Confirmed,
                        IsPaid = true,
                        IsPaymentConfirmed = true,
                        PaymentDate = DateTime.UtcNow.AddHours(-2),
                        PaymentMethodString = "Card",
                        IsInvoiced = true,
                        InvoiceNumber = "INV-DRIVER-001",
                        InvoiceDate = DateTime.UtcNow.AddHours(-2),
                        TotalWeightKg = 3
                    };
                    Orders.Add(order1);
                    driverOrderIds.Add(order1.Id);
                }

                // ORDER 2: Chicken Legs x2 for Customer 2 (10 kg)
                if (!Orders.Any(o => o.OrderNumber == "DRIVER-ORDER-002") && chickenLegs != null)
                {
                    var items = new List<OrderItem>
                    {
                        new OrderItem { Id = driverNextItemId++, ProductId = chickenLegs.Id, Product = chickenLegs, Quantity = 2, UnitPrice = chickenLegs.Price }
                    };
                    var order2 = new Order
                    {
                        Id = driverNextOrderId++,
                        OrderNumber = "DRIVER-ORDER-002",
                        CustomerId = driverCustomer2.Id,
                        Customer = driverCustomer2,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        Items = items,
                        Total = chickenLegs.Price * 2,
                        Status = OrderStatusEnum.Confirmed,
                        IsPaid = true,
                        IsPaymentConfirmed = true,
                        PaymentDate = DateTime.UtcNow.AddHours(-1),
                        PaymentMethodString = "Card",
                        IsInvoiced = true,
                        InvoiceNumber = "INV-DRIVER-002",
                        InvoiceDate = DateTime.UtcNow.AddHours(-1),
                        TotalWeightKg = 10
                    };
                    Orders.Add(order2);
                    driverOrderIds.Add(order2.Id);
                }

                // ORDER 3: Beef Chuck x5 for Customer 3 (5 kg)
                if (!Orders.Any(o => o.OrderNumber == "DRIVER-ORDER-003") && beefChuck != null)
                {
                    var items = new List<OrderItem>
                    {
                        new OrderItem { Id = driverNextItemId++, ProductId = beefChuck.Id, Product = beefChuck, Quantity = 5, UnitPrice = beefChuck.Price }
                    };
                    var order3 = new Order
                    {
                        Id = driverNextOrderId++,
                        OrderNumber = "DRIVER-ORDER-003",
                        CustomerId = driverCustomer3.Id,
                        Customer = driverCustomer3,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                        Items = items,
                        Total = beefChuck.Price * 5,
                        Status = OrderStatusEnum.Confirmed,
                        IsPaid = true,
                        IsPaymentConfirmed = true,
                        PaymentDate = DateTime.UtcNow.AddMinutes(-30),
                        PaymentMethodString = "Card",
                        IsInvoiced = true,
                        InvoiceNumber = "INV-DRIVER-003",
                        InvoiceDate = DateTime.UtcNow.AddMinutes(-30),
                        TotalWeightKg = 5
                    };
                    Orders.Add(order3);
                    driverOrderIds.Add(order3.Id);
                }

                // Get or create a vehicle for Test Driver
                var testVehicle = Vehicles.FirstOrDefault(v => v.Registration == "CHT001");
                if (testVehicle == null)
                {
                    testVehicle = new Vehicle
                    {
                        Id = Vehicles.Any() ? Vehicles.Max(v => v.Id) + 1 : 200,
                        Registration = "CHT001",
                        Model = "Chester Delivery Truck",
                        Capacity = 3500,
                        EmptyWeightKg = 2800,
                        MaxLoadCapacityKg = 3500,
                        CurrentLoadWeightKg = 0
                    };
                    Vehicles.Add(testVehicle);
                }

                // Create a route for Test Driver (if orders exist and route doesn't already exist)
                if (driverOrderIds.Any() && !Routes.Any(r => r.AssignedDriverId == testDriver.Id && r.Status == "SentToDriver"))
                {
                    var driverRoute = new Route
                    {
                        Id = Routes.Any() ? Routes.Max(r => r.Id) + 1 : 100,
                        Name = "Test Driver Route - " + DateTime.Now.ToString("yyyy-MM-dd"),
                        Destination = "Durban Area Deliveries",
                        Date = DateTime.Now.AddHours(1),
                        Status = "SentToDriver",
                        AssignedDriverId = testDriver.Id,
                        AssignedVehicleId = testVehicle.Id,
                        ItemsCount = driverOrderIds.Sum(id => Orders.FirstOrDefault(o => o.Id == id)?.Items?.Count ?? 0),
                        TotalMassKg = driverOrderIds.Sum(id => Orders.FirstOrDefault(o => o.Id == id)?.TotalWeightKg ?? 0)
                    };
                    driverRoute.OrderIds.AddRange(driverOrderIds);
                    Routes.Add(driverRoute);

                    // Update order statuses to OutForDelivery
                    foreach (var orderId in driverOrderIds)
                    {
                        var order = Orders.FirstOrDefault(o => o.Id == orderId);
                        if (order != null)
                        {
                            order.Status = OrderStatusEnum.OutForDelivery;
                        }
                    }

                    // Send notification to driver (if not already sent)
                    if (!Notifications.Any(n => n.UserId == testDriver.Id && n.ReferenceId == driverRoute.Id))
                    {
                        var notification = new Notification
                        {
                            Id = Notifications.Any() ? Notifications.Max(n => n.Id) + 1 : 1,
                            UserId = testDriver.Id,
                            Title = "New Route Assigned",
                            Message = $"You have been assigned a new route with {driverOrderIds.Count} stops. Scheduled for {DateTime.Now.AddHours(1):dd MMM yyyy HH:mm}.",
                            Type = "Route",
                            ReferenceId = driverRoute.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        Notifications.Add(notification);
                    }

                    System.Diagnostics.Debug.WriteLine($"=== SEEDED: Test Driver Route with {driverOrderIds.Count} orders, notification sent ===");
                }
            }

            // ========================================
            // SEED: RECEIVER TEST DATA
            // ========================================

            // Create a test customer for receiver orders
            var receiverTestCustomer = Customers.FirstOrDefault(c => c.Name == "Receiver Test Customer");
            if (receiverTestCustomer == null)
            {
                receiverTestCustomer = new Customer
                {
                    Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 600,
                    Name = "Receiver Test Customer",
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "0811111111",
                    Address = "123 Receiver Street, Durban",
                    OutstandingBalance = 0,
                    MonthlyCreditLimit = 50000,
                    CurrentMonthDebt = 0
                };
                Customers.Add(receiverTestCustomer);
            }

            // Get products for receiver test orders
            var receiverBeef = Products.FirstOrDefault(p => p.ProductCode == "BEEF-001");
            var receiverChicken = Products.FirstOrDefault(p => p.ProductCode == "CHICK-003");
            var receiverPork = Products.FirstOrDefault(p => p.ProductCode == "PORK-001");

            // Create test orders for receiver to process
            int receiverNextItemId = Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Any()
                ? Orders.SelectMany(o => o.Items ?? new List<OrderItem>()).Max(i => i.Id) + 1
                : 1;
            int receiverNextOrderId = Orders.Any() ? Orders.Max(o => o.Id) + 1 : 6000;

            // ORDER 1: Ready for Receiver (Confirmed, invoiced, not yet picked)
            if (!Orders.Any(o => o.OrderNumber == "RECEIVER-TEST-001") && receiverBeef != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = receiverNextItemId++, ProductId = receiverBeef.Id, Product = receiverBeef, Quantity = 10, UnitPrice = receiverBeef.Price }
                };
                Orders.Add(new Order
                {
                    Id = receiverNextOrderId++,
                    OrderNumber = "RECEIVER-TEST-001",
                    CustomerId = receiverTestCustomer.Id,
                    Customer = receiverTestCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    Items = items,
                    Total = receiverBeef.Price * 10,
                    Status = OrderStatusEnum.Confirmed,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-2),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-REC-001",
                    InvoiceDate = DateTime.UtcNow.AddHours(-2),
                    TotalWeightKg = 10
                });
            }

            // ORDER 2: OutForDelivery (Driver on the way - for Enterprise receipt testing)
            if (!Orders.Any(o => o.OrderNumber == "RECEIVER-TEST-002") && receiverChicken != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = receiverNextItemId++, ProductId = receiverChicken.Id, Product = receiverChicken, Quantity = 5, UnitPrice = receiverChicken.Price }
                };
                Orders.Add(new Order
                {
                    Id = receiverNextOrderId++,
                    OrderNumber = "RECEIVER-TEST-002",
                    CustomerId = receiverTestCustomer.Id,
                    Customer = receiverTestCustomer,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Items = items,
                    Total = receiverChicken.Price * 5,
                    Status = OrderStatusEnum.OutForDelivery,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddHours(-1),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-REC-002",
                    InvoiceDate = DateTime.UtcNow.AddHours(-1),
                    TotalWeightKg = 25
                });
            }


            // ========================================
            // SEED: TASKS FOR STAFF MANAGEMENT
            // ========================================
            if (!WorkTasks.Any())
            {
                int taskId = 1;
                var manager = Users.FirstOrDefault(u => u.Role == "WarehouseManager");
                var staff = Users.Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver").ToList();

                WorkTasks.Add(new WorkTask
                {
                    WorkTaskId = taskId++,
                    Title = "Pick Order #CHECKER-READY-001",
                    Description = "Pick items for order CHECKER-READY-001: Beef Mince x5",
                    Type = WorkTaskType.OrderPicking,
                    Priority = WorkTaskPriority.High,
                    Status = WorkTaskStatus.Pending,
                    CreatedDate = DateTime.Now.AddHours(-3),
                    CreatedBy = manager?.Username ?? "System",
                    ReferenceType = "Order",
                    ReferenceId = Orders.FirstOrDefault(o => o.OrderNumber == "CHECKER-READY-001")?.Id
                });

                WorkTasks.Add(new WorkTask
                {
                    WorkTaskId = taskId++,
                    Title = "Receive Stock - Beef Shipment",
                    Description = "Receive and process incoming beef shipment from supplier",
                    Type = WorkTaskType.StockReceiving,
                    Priority =  WorkTaskPriority.Medium,
                    Status = WorkTaskStatus.Pending,
                    CreatedDate = DateTime.Now.AddHours(-2),
                    CreatedBy = manager?.Username ?? "System",
                    ReferenceType = "Stock",
                    DueDate = DateTime.Now.AddDays(2)
                });

                WorkTasks.Add(new WorkTask
                {
                    WorkTaskId = taskId++,
                    Title = "Delivery Route - Test Driver Route",
                    Description = "Deliver orders on assigned route",
                    Type = WorkTaskType.Delivery,
                    Priority = WorkTaskPriority.High,
                    Status = WorkTaskStatus.Pending,
                    CreatedDate = DateTime.Now.AddHours(-1),
                    CreatedBy = manager?.Username ?? "System",
                    ReferenceType = "Route",
                    ReferenceId = Routes.FirstOrDefault()?.Id,
                    DueDate = DateTime.Now.AddHours(2)
                });

                // Pre-assign a task to a staff member
                var warehouseStaff = Users.FirstOrDefault(u => u.Role == "Warehouse");
                if (warehouseStaff != null && manager != null)
                {
                    var firstTask = WorkTasks.FirstOrDefault();
                    if (firstTask != null)
                    {
                        firstTask.AssignedToId = warehouseStaff.Id;
                        firstTask.Status = WorkTaskStatus.Assigned;

                        var assignment = new TaskAssignment
                        {
                            AssignmentId = 1,
                            WorkTaskId = firstTask.WorkTaskId,
                            StaffId = warehouseStaff.Id,
                            AssignedById = manager.Id,
                            AssignedDate = DateTime.Now.AddHours(-2),
                            Status = AssignmentStatus.Pending,
                            Notes = "Please complete this order before end of shift"
                        };
                        TaskAssignments.Add(assignment);
                    }
                }
            }



            // ========================================
            // SEED: UC2 - ADDITIONAL ASSIGNED TASKS (status variety)
            // ========================================
            if (TaskAssignments.Count == 1) // only the original single assignment exists so far
            {
                var taskManager = Users.FirstOrDefault(u => u.Role == "WarehouseManager");
                var taskChecker = Users.FirstOrDefault(u => u.Role == "Checker");
                var taskDriver = Users.FirstOrDefault(u => u.Role == "Driver");
                int nextWorkTaskId = WorkTasks.Any() ? WorkTasks.Max(t => t.WorkTaskId) + 1 : 4;
                int nextAssignId = TaskAssignments.Any() ? TaskAssignments.Max(a => a.AssignmentId) + 1 : 2;

                if (taskChecker != null && taskManager != null)
                {
                    var checkerTask = new WorkTask
                    {
                        WorkTaskId = nextWorkTaskId++,
                        Title = "Pick Order #ORD-20260811-002",
                        Description = "Pick and prepare order for dispatch",
                        Type = WorkTaskType.OrderPicking,
                        Priority = WorkTaskPriority.Medium,
                        Status = WorkTaskStatus.InProgress,
                        CreatedDate = DateTime.Now.AddHours(-2),
                        CreatedBy = taskManager.Username,
                        AssignedToId = taskChecker.Id
                    };
                    WorkTasks.Add(checkerTask);
                    TaskAssignments.Add(new TaskAssignment
                    {
                        AssignmentId = nextAssignId++,
                        WorkTaskId = checkerTask.WorkTaskId,
                        StaffId = taskChecker.Id,
                        AssignedById = taskManager.Id,
                        AssignedDate = DateTime.Now.AddHours(-2),
                        Status = AssignmentStatus.Accepted,
                        Notes = "Priority order - customer waiting"
                    });
                }

                if (taskDriver != null && taskManager != null)
                {
                    var driverTask = new WorkTask
                    {
                        WorkTaskId = nextWorkTaskId++,
                        Title = "Deliver Order #ORD-20260811-001",
                        Description = "Complete delivery route stop 1",
                        Type = WorkTaskType.Delivery,
                        Priority = WorkTaskPriority.High,
                        Status = WorkTaskStatus.Completed,
                        CreatedDate = DateTime.Now.AddDays(-1),
                        CompletedDate = DateTime.Now.AddHours(-5),
                        CreatedBy = taskManager.Username,
                        AssignedToId = taskDriver.Id
                    };
                    WorkTasks.Add(driverTask);
                    TaskAssignments.Add(new TaskAssignment
                    {
                        AssignmentId = nextAssignId++,
                        WorkTaskId = driverTask.WorkTaskId,
                        StaffId = taskDriver.Id,
                        AssignedById = taskManager.Id,
                        AssignedDate = DateTime.Now.AddDays(-1),
                        Status = AssignmentStatus.Completed,
                        Notes = "Standard delivery"
                    });
                }
            }

            // ========================================
            // SEED: UC5 - STAFF LEAVE REQUESTS (expanded)
            // ========================================
            if (!LeaveRequests.Any())
            {
                var warehouseStaffForLeave = Users.FirstOrDefault(u => u.Role == "Warehouse");
                var checkerStaffForLeave = Users.FirstOrDefault(u => u.Role == "Checker");
                var driverStaffForLeave = Users.FirstOrDefault(u => u.Role == "Driver");
                var leaveManager = Users.FirstOrDefault(u => u.Role == "WarehouseManager");

                if (warehouseStaffForLeave != null)
                {
                    // Pending - leave this one to approve LIVE during the demo
                    LeaveRequests.Add(new LeaveRequest
                    {
                        LeaveRequestId = 1,
                        StaffId = warehouseStaffForLeave.Id,
                        Staff = warehouseStaffForLeave,
                        Type = LeaveType.Annual,
                        StartDate = DateTime.Today.AddDays(5),
                        EndDate = DateTime.Today.AddDays(7),
                        TotalDays = 3,
                        Status = LeaveStatus.Pending,
                        RequestDate = DateTime.Now.AddDays(-1),
                        Reason = "Family event - need time off"
                    });
                }
                if (checkerStaffForLeave != null)
                {
                    // Already Approved - shows history/status variety
                    LeaveRequests.Add(new LeaveRequest
                    {
                        LeaveRequestId = 2,
                        StaffId = checkerStaffForLeave.Id,
                        Staff = checkerStaffForLeave,
                        Type = LeaveType.Sick,
                        StartDate = DateTime.Today.AddDays(-3),
                        EndDate = DateTime.Today.AddDays(-2),
                        TotalDays = 2,
                        Status = LeaveStatus.Approved,
                        RequestDate = DateTime.Now.AddDays(-4),
                        DecisionDate = DateTime.Now.AddDays(-3),
                        ReviewedBy = leaveManager?.FullName ?? "Warehouse Manager",
                        Reason = "Flu symptoms"
                    });
                }
                if (driverStaffForLeave != null)
                {
                    // Rejected - shows the full status range
                    LeaveRequests.Add(new LeaveRequest
                    {
                        LeaveRequestId = 3,
                        StaffId = driverStaffForLeave.Id,
                        Staff = driverStaffForLeave,
                        Type = LeaveType.Personal,
                        StartDate = DateTime.Today.AddDays(2),
                        EndDate = DateTime.Today.AddDays(2),
                        TotalDays = 1,
                        Status = LeaveStatus.Rejected,
                        RequestDate = DateTime.Now.AddDays(-2),
                        DecisionDate = DateTime.Now.AddDays(-1),
                        ReviewedBy = leaveManager?.FullName ?? "Warehouse Manager",
                        ManagerNotes = "Route already fully booked that day - please resubmit for another date",
                        Reason = "Personal matter"
                    });
                }
            }

            // ========================================
            // SEED: UC6-11 - STOCK TAKE SESSION (expanded)
            // ========================================
            if (!StockTakes.Any())
            {
                var manager = Users.FirstOrDefault(u => u.Role == "WarehouseManager");
                var warehouseStaffForCount = Users.FirstOrDefault(u => u.Role == "Warehouse");
                var checkerStaffForCount = Users.FirstOrDefault(u => u.Role == "Checker");

                var stockTake = new StockTake
                {
                    StockTakeId = 1,
                    Name = "August Stock Take 2026",
                    SessionNumber = "ST-20260811-001",
                    Location = "Warehouse Floor 175",
                    StartDate = DateTime.Today,
                    Status = StockTakeStatus.VariancesIdentified, // leave at this stage - approve live for UC10
                    CreatedBy = manager?.FullName ?? "Warehouse Manager",
                    CreatedDate = DateTime.Now.AddHours(-4),
                    TotalItemsCounted = 3,
                    TotalVariancesFound = 2
                };
                StockTakes.Add(stockTake);

                // UC7: TWO counters assigned - shows the assignment flow covers multiple staff
                if (warehouseStaffForCount != null && manager != null)
                {
                    StockTakeAssignments.Add(new StockTakeAssignment
                    {
                        StockTakeAssignmentId = 1,
                        StockTakeId = stockTake.StockTakeId,
                        StaffId = warehouseStaffForCount.Id,
                        Staff = warehouseStaffForCount,
                        AssignedById = manager.Id,
                        AssignedBy = manager,
                        AssignedDate = DateTime.Now.AddHours(-4),
                        Status = StockTakeAssignmentStatus.Completed,
                        AssignedLocation = "Warehouse Floor 175 - Beef Section",
                        ItemsCounted = 3,
                        CompletedDate = DateTime.Now.AddHours(-1)
                    });
                }
                if (checkerStaffForCount != null && manager != null)
                {
                    StockTakeAssignments.Add(new StockTakeAssignment
                    {
                        StockTakeAssignmentId = 2,
                        StockTakeId = stockTake.StockTakeId,
                        StaffId = checkerStaffForCount.Id,
                        Staff = checkerStaffForCount,
                        AssignedById = manager.Id,
                        AssignedBy = manager,
                        AssignedDate = DateTime.Now.AddHours(-3),
                        Status = StockTakeAssignmentStatus.InProgress, // still counting - matches the uncounted item below
                        AssignedLocation = "Freezer 180 - Chicken Section",
                        ItemsCounted = 0
                    });
                }

                // UC8/UC9: 4 count items - mix of exact match, two variances, and one not-yet-counted
                var stockTakeProduct1 = Products.FirstOrDefault(p => p.ProductCode == "BEEF-003"); // Beef Mince
                var stockTakeProduct2 = Products.FirstOrDefault(p => p.ProductCode == "BEEF-001"); // Fresh Beef Chuck
                var stockTakeProduct3 = Products.FirstOrDefault(p => p.ProductCode == "BEEF-002"); // Beef Stewing Meat
                var stockTakeProduct4 = Products.FirstOrDefault(p => p.ProductCode == "CHICK-001"); // Frozen Chicken Mixed Portions

                if (stockTakeProduct1 != null)
                {
                    StockCountItems.Add(new StockCountItem
                    {
                        StockCountItemId = 1,
                        StockTakeId = stockTake.StockTakeId,
                        ProductId = stockTakeProduct1.Id,
                        Product = stockTakeProduct1,
                        SystemQuantity = 100,
                        PhysicalCount = 95,
                        Variance = -5,
                        CountedByStaffId = warehouseStaffForCount?.Id,
                        CountedByStaff = warehouseStaffForCount,
                        Status = StockCountStatus.Variance,
                        CountedDate = DateTime.Now.AddHours(-2),
                        Notes = "Variance identified - 5 units short"
                    });
                }
                if (stockTakeProduct2 != null)
                {
                    StockCountItems.Add(new StockCountItem
                    {
                        StockCountItemId = 2,
                        StockTakeId = stockTake.StockTakeId,
                        ProductId = stockTakeProduct2.Id,
                        Product = stockTakeProduct2,
                        SystemQuantity = 100,
                        PhysicalCount = 100,
                        Variance = 0,
                        CountedByStaffId = warehouseStaffForCount?.Id,
                        CountedByStaff = warehouseStaffForCount,
                        Status = StockCountStatus.Verified,
                        CountedDate = DateTime.Now.AddHours(-1.5)
                    });
                }
                if (stockTakeProduct3 != null)
                {
                    StockCountItems.Add(new StockCountItem
                    {
                        StockCountItemId = 3,
                        StockTakeId = stockTake.StockTakeId,
                        ProductId = stockTakeProduct3.Id,
                        Product = stockTakeProduct3,
                        SystemQuantity = 100,
                        PhysicalCount = 88,
                        Variance = -12,
                        CountedByStaffId = warehouseStaffForCount?.Id,
                        CountedByStaff = warehouseStaffForCount,
                        Status = StockCountStatus.Variance,
                        CountedDate = DateTime.Now.AddHours(-1),
                        Notes = "Larger variance - flagged for investigation"
                    });
                }
                if (stockTakeProduct4 != null)
                {
                    // Deliberately left uncounted - count this one LIVE to demo the actual counting flow
                    StockCountItems.Add(new StockCountItem
                    {
                        StockCountItemId = 4,
                        StockTakeId = stockTake.StockTakeId,
                        ProductId = stockTakeProduct4.Id,
                        Product = stockTakeProduct4,
                        SystemQuantity = 60,
                        PhysicalCount = null,
                        Variance = 0,
                        Status = StockCountStatus.Pending
                    });
                }
            }



            // ========================================
            // SEED: UC1 - FULL WEEK SCHEDULE (for calendar demo)
            // ========================================
            if (!Shifts.Any())
            {
                var ucManager = Users.FirstOrDefault(u => u.Role == "WarehouseManager");
                var ucWarehouseStaff = Users.FirstOrDefault(u => u.Role == "Warehouse");
                var ucCheckerStaff = Users.FirstOrDefault(u => u.Role == "Checker");
                int shiftId = 1;
                int shiftAssignId = 1;

                // Yesterday - Completed shift, shows history
                var shiftYesterday = new Shift
                {
                    ShiftId = shiftId++,
                    Type = ShiftType.Morning,
                    ShiftDate = DateTime.Today.AddDays(-1),
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    Name = "Morning Team - Picking",
                    Status = ShiftStatus.Completed,
                    CreatedBy = ucManager?.FullName ?? "Warehouse Manager",
                    CreatedDate = DateTime.Now.AddDays(-3),
                    PublishedDate = DateTime.Now.AddDays(-2)
                };
                Shifts.Add(shiftYesterday);

                // Today - Published, currently active
                var shiftToday = new Shift
                {
                    ShiftId = shiftId++,
                    Type = ShiftType.Morning,
                    ShiftDate = DateTime.Today,
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    Name = "Morning Team - Picking",
                    Status = ShiftStatus.Published,
                    CreatedBy = ucManager?.FullName ?? "Warehouse Manager",
                    CreatedDate = DateTime.Now.AddDays(-2),
                    PublishedDate = DateTime.Now.AddDays(-1)
                };
                Shifts.Add(shiftToday);

                // Tomorrow - Draft (leave this one to publish LIVE during the demo)
                var shiftTomorrow = new Shift
                {
                    ShiftId = shiftId++,
                    Type = ShiftType.Morning,
                    ShiftDate = DateTime.Today.AddDays(1),
                    StartTime = new TimeSpan(6, 0, 0),
                    EndTime = new TimeSpan(14, 0, 0),
                    Name = "Morning Team - Picking",
                    Status = ShiftStatus.Draft,
                    CreatedBy = ucManager?.FullName ?? "Warehouse Manager",
                    CreatedDate = DateTime.Now.AddHours(-2)
                };
                Shifts.Add(shiftTomorrow);

                // Day after tomorrow - Afternoon shift, Draft too, shows shift-type variety
                var shiftDayAfter = new Shift
                {
                    ShiftId = shiftId++,
                    Type = ShiftType.Afternoon,
                    ShiftDate = DateTime.Today.AddDays(2),
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(22, 0, 0),
                    Name = "Afternoon Team - Receiving",
                    Status = ShiftStatus.Draft,
                    CreatedBy = ucManager?.FullName ?? "Warehouse Manager",
                    CreatedDate = DateTime.Now.AddHours(-1)
                };
                Shifts.Add(shiftDayAfter);

                if (ucWarehouseStaff != null && ucManager != null)
                {
                    ShiftAssignments.Add(new ShiftAssignment { ShiftAssignmentId = shiftAssignId++, ShiftId = shiftYesterday.ShiftId, StaffId = ucWarehouseStaff.Id, Staff = ucWarehouseStaff, AssignedById = ucManager.Id, AssignedBy = ucManager, AssignedDate = DateTime.Now.AddDays(-3), Status = ShiftAssignmentStatus.Completed, CheckInTime = DateTime.Today.AddDays(-1).AddHours(6), CheckOutTime = DateTime.Today.AddDays(-1).AddHours(14) });
                    ShiftAssignments.Add(new ShiftAssignment { ShiftAssignmentId = shiftAssignId++, ShiftId = shiftToday.ShiftId, StaffId = ucWarehouseStaff.Id, Staff = ucWarehouseStaff, AssignedById = ucManager.Id, AssignedBy = ucManager, AssignedDate = DateTime.Now.AddDays(-2), Status = ShiftAssignmentStatus.Confirmed, CheckInTime = DateTime.Today.AddHours(6) });
                }
                if (ucCheckerStaff != null && ucManager != null)
                {
                    ShiftAssignments.Add(new ShiftAssignment { ShiftAssignmentId = shiftAssignId++, ShiftId = shiftDayAfter.ShiftId, StaffId = ucCheckerStaff.Id, Staff = ucCheckerStaff, AssignedById = ucManager.Id, AssignedBy = ucManager, AssignedDate = DateTime.Now.AddHours(-1), Status = ShiftAssignmentStatus.Pending });
                }
                // shiftTomorrow deliberately left with NO staff assigned yet - assign it live during the demo
            }

            // ========================================
            // SEED: UC3 + UC4 - ATTENDANCE HISTORY & VARIED PERFORMANCE
            // (Feeds both Attendance tracking AND the Performance Dashboard's live calculations)
            // ========================================
            if (!Attendances.Any())
            {
                var perfStaff = Users.Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver").ToList();
                int attendanceId = 1;
                int extraTaskId = WorkTasks.Any() ? WorkTasks.Max(t => t.WorkTaskId) + 1 : 1;

                foreach (var s in perfStaff)
                {
                    var rnd = new Random(s.Id * 31); // fixed seed = same realistic numbers every run

                    for (int dayOffset = 7; dayOffset >= 1; dayOffset--)
                    {
                        var day = DateTime.Today.AddDays(-dayOffset);
                        if (day.DayOfWeek == DayOfWeek.Sunday) continue;

                        bool isLate = rnd.Next(100) < 20;
                        var clockIn = day.Date.AddHours(7).AddMinutes(isLate ? rnd.Next(15, 45) : rnd.Next(-5, 10));
                        var clockOut = clockIn.AddHours(8).AddMinutes(rnd.Next(-10, 30));
                        var hours = (decimal)(clockOut - clockIn).TotalHours;

                        Attendances.Add(new Attendance
                        {
                            AttendanceId = attendanceId++,
                            StaffId = s.Id,
                            Staff = s,
                            Date = day,
                            ClockInTime = clockIn,
                            ClockOutTime = clockOut,
                            Status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present,
                            TotalHours = Math.Round(hours, 2),
                            OvertimeHours = hours > 8 ? Math.Round(hours - 8, 2) : 0,
                            BreakMinutes = 30,
                            CreatedDate = day
                        });
                    }

                    // Today: clocked in, not yet out - except warehouse1, who stays free for the live face clock-in demo
                    if (s.Username != "warehouse1")
                    {
                        Attendances.Add(new Attendance
                        {
                            AttendanceId = attendanceId++,
                            StaffId = s.Id,
                            Staff = s,
                            Date = DateTime.Today,
                            ClockInTime = DateTime.Today.AddHours(7).AddMinutes(rnd.Next(-5, 10)),
                            Status = AttendanceStatus.Present,
                            TotalHours = 0,
                            CreatedDate = DateTime.Today
                        });
                    }

                    int completedCount = rnd.Next(4, 12);
                    for (int i = 0; i < completedCount; i++)
                    {
                        var createdDate = DateTime.Now.AddDays(-rnd.Next(1, 7));
                        WorkTasks.Add(new WorkTask
                        {
                            WorkTaskId = extraTaskId++,
                            Title = $"{s.Role} Task #{i + 1} - {s.FullName}",
                            Description = $"Completed {s.Role.ToLower()} task for performance history",
                            Type = s.Role == "Driver" ? WorkTaskType.Delivery : s.Role == "Checker" ? WorkTaskType.OrderPicking : WorkTaskType.StockReceiving,
                            Priority = WorkTaskPriority.Medium,
                            Status = WorkTaskStatus.Completed,
                            CreatedDate = createdDate,
                            CompletedDate = createdDate.AddHours(rnd.Next(1, 6)),
                            AssignedToId = s.Id,
                            CreatedBy = "System"
                        });
                    }
                }
            }

            // ========================================
            // SEED: RETURN ORDER FOR RECEIVER INSPECTION
            // ========================================

            // First create a delivered order that can be returned
            var returnedOrderCustomer = Customers.FirstOrDefault(c => c.Name == "Return Test Customer");
            if (returnedOrderCustomer == null)
            {
                returnedOrderCustomer = new Customer
                {
                    Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 601,
                    Name = "Return Test Customer",
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "0822222222",
                    Address = "456 Return Street, Durban",
                    OutstandingBalance = 0,
                    MonthlyCreditLimit = 50000,
                    CurrentMonthDebt = 0
                };
                Customers.Add(returnedOrderCustomer);
            }

            int returnOrderId = 0;
            if (!Orders.Any(o => o.OrderNumber == "RETURN-TEST-ORDER") && receiverPork != null)
            {
                var items = new List<OrderItem>
                {
                    new OrderItem { Id = receiverNextItemId++, ProductId = receiverPork.Id, Product = receiverPork, Quantity = 8, UnitPrice = receiverPork.Price }
                };
                var returnOrder = new Order
                {
                    Id = receiverNextOrderId++,
                    OrderNumber = "RETURN-TEST-ORDER",
                    CustomerId = returnedOrderCustomer.Id,
                    Customer = returnedOrderCustomer,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Items = items,
                    Total = receiverPork.Price * 8,
                    Status = OrderStatusEnum.Delivered,
                    IsPaid = true,
                    IsPaymentConfirmed = true,
                    PaymentDate = DateTime.UtcNow.AddDays(-2),
                    PaymentMethodString = "Card",
                    IsInvoiced = true,
                    InvoiceNumber = "INV-RETURN-001",
                    InvoiceDate = DateTime.UtcNow.AddDays(-2),
                    TotalWeightKg = 8
                };
                Orders.Add(returnOrder);
                returnOrderId = returnOrder.Id;
            }

            // Create a Return record for Receiver to inspect
            if (returnOrderId > 0 && !Returns.Any(r => r.OrderId == returnOrderId))
            {
                var driverUser = Users.FirstOrDefault(u => u.Role == "Driver");
                var newReturn = new Return
                {
                    ReturnId = Returns.Any() ? Returns.Max(r => r.ReturnId) + 1 : 1,
                    OrderId = returnOrderId,
                    ReturnDate = DateTime.Now.AddDays(-1),
                    ReturnReason = "Quality Issue - Meat spoiled upon opening",
                    CustomerReason = "The meat had an unusual smell and color when opened",
                    Status = "Pending",
                    RefundAmount = receiverPork != null ? receiverPork.Price * 8 : 719.92m,
                    DriverPhotoUrl = null
                };
                Returns.Add(newReturn);

                // Add Return Items
                var order = Orders.FirstOrDefault(o => o.Id == returnOrderId);
                if (order != null && order.Items != null)
                {
                    int nextReturnItemId = ReturnItems.Any() ? ReturnItems.Max(ri => ri.ReturnItemId) + 1 : 1;
                    foreach (var item in order.Items)
                    {
                        ReturnItems.Add(new ReturnItem
                        {
                            ReturnItemId = nextReturnItemId++,
                            ReturnId = newReturn.ReturnId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Condition = "PendingInspection",
                            StockAction = "Pending"
                        });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== SEEDED: Return #{newReturn.ReturnId} for order RETURN-TEST-ORDER ===");
            }

            // ========================================
            // SEED: PALLETS FOR RECEIVER TO VIEW
            // ========================================

            // Ensure we have at least 3 pallets in the system
            if (!Products.Any(p => p.IsPallet && p.PalletCode.StartsWith("TEST-PALLET")))
            {
                int nextPalletId = Products.Any() ? Products.Max(p => p.Id) + 1 : 7000;
                int palletCounter = 9000;

                var palletProducts = new[]
                {
                    new { Code = "BEEF-001", Name = "Fresh Beef Chuck", Qty = 15, Price = 89.99m, Location = "Warehouse Floor 175" },
                    new { Code = "CHICK-003", Name = "Chicken Leg Quarters", Qty = 20, Price = 79.99m, Location = "Freezer 180" },
                    new { Code = "PORK-001", Name = "Pork Chops", Qty = 12, Price = 89.99m, Location = "Warehouse Floor 191" }
                };

                foreach (var p in palletProducts)
                {
                    var palletCode = palletCounter.ToString();
                    palletCounter++;

                    // Create pallet header
                    var palletHeader = new Product
                    {
                        Id = nextPalletId++,
                        ProductCode = "PAL-" + palletCode,
                        Name = "Pallet - " + p.Name,
                        SerialNumber = "",
                        IsPallet = true,
                        PalletCode = palletCode,
                        Quantity = p.Qty,
                        Price = p.Price,
                        Location = p.Location,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        Category = "Pallet",
                        ImageUrl = "/Content/Images/placeholders/pallet.jpg"
                    };
                    Products.Add(palletHeader);

                    // Create individual items on pallet
                    long startSerial = 9990000000L + palletCounter;
                    var product = Products.FirstOrDefault(prod => prod.ProductCode == p.Code);
                    if (product == null)
                    {
                        product = new Product
                        {
                            Id = nextPalletId++,
                            ProductCode = p.Code,
                            Name = p.Name + " (1 kg)",
                            SerialNumber = (startSerial).ToString(),
                            IsPallet = false,
                            PalletCode = palletCode,
                            Quantity = 1,
                            Price = p.Price,
                            Location = p.Location,
                            CreatedAt = DateTime.UtcNow.AddDays(-10),
                            Category = "Meat",
                            ImageUrl = "/Content/Images/placeholders/meat-generic.jpg"
                        };
                        Products.Add(product);
                    }

                    for (int i = 1; i < p.Qty; i++)
                    {
                        var item = new Product
                        {
                            Id = nextPalletId++,
                            ProductCode = p.Code,
                            Name = p.Name + " (1 kg)",
                            SerialNumber = (startSerial + i).ToString(),
                            IsPallet = false,
                            PalletCode = palletCode,
                            Quantity = 1,
                            Price = p.Price,
                            Location = p.Location,
                            CreatedAt = DateTime.UtcNow.AddDays(-10),
                            Category = "Meat",
                            ImageUrl = "/Content/Images/placeholders/meat-generic.jpg"
                        };
                        Products.Add(item);
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== SEEDED: 3 test pallets for Receiver ===");
            }

            // ========================================
            // FIX LOGIN - Add existing customer accounts so they can login directly
            // ========================================

            // Add Standard Customer (already exists from earlier, but ensure user account exists)
            var standardCustomerUser = Users.FirstOrDefault(u => u.Username == "standard@demo.com");
            if (standardCustomerUser == null && Customers.Any(c => c.Name == "Standard Customer"))
            {
                Users.Add(new User
                {
                    Id = Users.Any() ? Users.Max(u => u.Id) + 1 : 1000,
                    Username = "standard@demo.com",
                    Password = "Demo123",
                    Role = "Customer",
                    FullName = "Standard Customer"
                });
            }

            // Add Instore Customer
            var instoreCustomerUser = Users.FirstOrDefault(u => u.Username == "instore@demo.com");
            if (instoreCustomerUser == null)
            {
                var instoreCustomer = Customers.FirstOrDefault(c => c.Name == "Instore Customer");
                if (instoreCustomer == null)
                {
                    instoreCustomer = new Customer
                    {
                        Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 700,
                        Name = "Instore Customer",
                        CustomerType = CustomerTypeEnum.Instore,
                        ContactPhone = "0315551234",
                        Address = "Chester Store - 45 Umbilo Road, Durban",
                        OutstandingBalance = 0,
                        MonthlyCreditLimit = 0,
                        CurrentMonthDebt = 0
                    };
                    Customers.Add(instoreCustomer);
                }

                Users.Add(new User
                {
                    Id = Users.Any() ? Users.Max(u => u.Id) + 1 : 1001,
                    Username = "instore@demo.com",
                    Password = "Demo123",
                    Role = "Customer",
                    FullName = "Instore Customer"
                });
            }

            // Add Enterprise Customer
            var enterpriseCustomerUser = Users.FirstOrDefault(u => u.Username == "enterprise@demo.com");
            if (enterpriseCustomerUser == null)
            {
                var enterpriseCustomer = Customers.FirstOrDefault(c => c.Name == "Enterprise Customer");
                if (enterpriseCustomer == null)
                {
                    enterpriseCustomer = new Customer
                    {
                        Id = Customers.Any() ? Customers.Max(c => c.Id) + 1 : 701,
                        Name = "Enterprise Customer",
                        CustomerType = CustomerTypeEnum.Enterprise,
                        ContactPhone = "0315555678",
                        Address = "Enterprise HQ, 1 Business Park, Durban",
                        OutstandingBalance = 0,
                        MonthlyCreditLimit = 100000,
                        CurrentMonthDebt = 0
                    };
                    Customers.Add(enterpriseCustomer);
                }

                Users.Add(new User
                {
                    Id = Users.Any() ? Users.Max(u => u.Id) + 1 : 1002,
                    Username = "enterprise@demo.com",
                    Password = "Demo123",
                    Role = "Customer",
                    FullName = "Enterprise Customer"
                });
            }




            System.Diagnostics.Debug.WriteLine("=== LOGIN FIX: Added customer accounts for testing ===");


            System.Diagnostics.Debug.WriteLine($"=== RECEIVER SEED COMPLETE: Orders ready, Returns ready, Pallets ready ===");

            System.Diagnostics.Debug.WriteLine($"=== SEEDED: {Orders.Count} total orders ===");
        }
    }
}