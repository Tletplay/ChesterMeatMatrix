using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    //[Authorize]
    public class WarehouseManagerController : Controller
    {
        // Helper: Check if user is WarehouseManager, Warehouse, or Admin
        private bool IsWarehouseManager()
        {
            var role = Session["Role"] as string;
            return role == "WarehouseManager" || role == "Warehouse" || role == "Admin";
        }

        // ========================================
        // DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Dashboard()
        {
            if (!IsWarehouseManager())
            {
                TempData["Error"] = "Access denied. Warehouse Manager only.";
                return RedirectToAction("Index", "Dashboard");
            }

            // DEBUG: Add these lines
            System.Diagnostics.Debug.WriteLine("=== WAREHOUSE MANAGER DASHBOARD DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"Session Role: {Session["Role"]}");
            System.Diagnostics.Debug.WriteLine($"LowStockAlerts count: {InMemoryDatabase.LowStockAlerts.Count}");
            System.Diagnostics.Debug.WriteLine($"ExpectedStocks count: {InMemoryDatabase.ExpectedStocks.Count}");

            // Count pending alerts
            var pendingAlerts = InMemoryDatabase.LowStockAlerts.Count(a => a.AlertStatus == "Pending");
            ViewBag.PendingAlerts = pendingAlerts;
            System.Diagnostics.Debug.WriteLine($"Pending Alerts: {pendingAlerts}");

            // Count expected deliveries
            var today = DateTime.Today;
            var arrivingToday = InMemoryDatabase.ExpectedStocks
                .Count(e => e.Status != "Received" && e.Status != "Cancelled"
                    && e.ExpectedDeliveryDate.Date == today);
            ViewBag.ArrivingToday = arrivingToday;
            System.Diagnostics.Debug.WriteLine($"Arriving Today: {arrivingToday}");

            var weekEnd = today.AddDays(7);
            var arrivingThisWeek = InMemoryDatabase.ExpectedStocks
                .Count(e => e.Status != "Received" && e.Status != "Cancelled"
                    && e.ExpectedDeliveryDate >= today && e.ExpectedDeliveryDate <= weekEnd);
            ViewBag.ArrivingThisWeek = arrivingThisWeek;
            System.Diagnostics.Debug.WriteLine($"Arriving This Week: {arrivingThisWeek}");

            // Count low stock products
            var lowStockCount = GetLowStockProducts().Count;
            ViewBag.LowStockCount = lowStockCount;
            System.Diagnostics.Debug.WriteLine($"Low Stock Count: {lowStockCount}");

            // Recent expected deliveries
            var recentExpected = InMemoryDatabase.ExpectedStocks
                .Where(e => e.Status != "Received")
                .OrderBy(e => e.ExpectedDeliveryDate)
                .Take(5)
                .ToList();
            ViewBag.RecentExpected = recentExpected;
            System.Diagnostics.Debug.WriteLine($"Recent Expected: {recentExpected.Count}");

            ViewBag.FullName = Session["FullName"] as string;
            return View();
        }

        // ========================================
        // LOW STOCK ALERTS
        // ========================================

        [HttpGet]
        public ActionResult LowStockAlerts(string status = "Pending")
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var alerts = InMemoryDatabase.LowStockAlerts
                .Where(a => a.AlertStatus == status)
                .OrderByDescending(a => a.AlertDate)
                .ToList();

            ViewBag.Status = status;
            ViewBag.StatusOptions = new[] { "Pending", "Approved", "Rejected", "Ordered", "Received" };

            return View(alerts);
        }

        [HttpGet]
        public ActionResult CheckLowStock()
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var lowStockProducts = GetLowStockProducts();
            var existingAlerts = InMemoryDatabase.LowStockAlerts
                .Where(a => a.AlertStatus == "Pending")
                .Select(a => a.ProductId)
                .ToList();

            var newAlerts = new List<LowStockAlert>();
            var userName = Session["Username"] as string ?? "System";

            foreach (var item in lowStockProducts)
            {
                if (existingAlerts.Contains(item.ProductId)) continue;

                var threshold = GetOrCreateThreshold(item.ProductId);
                var suggestedQty = threshold.ReorderQuantity;

                var nextId = InMemoryDatabase.LowStockAlerts.Any()
                    ? InMemoryDatabase.LowStockAlerts.Max(a => a.LowStockAlertId) + 1
                    : 1;

                var alert = new LowStockAlert
                {
                    LowStockAlertId = nextId,
                    ProductId = item.ProductId,
                    CurrentStockQuantity = item.AvailableStock,
                    ThresholdLevel = item.ThresholdLevel,
                    SuggestedOrderQuantity = suggestedQty,
                    AlertStatus = "Pending",
                    AlertDate = DateTime.Now
                };

                InMemoryDatabase.LowStockAlerts.Add(alert);
                newAlerts.Add(alert);
            }

            if (newAlerts.Any())
            {
                TempData["Success"] = $"{newAlerts.Count} new low stock alert(s) created.";
            }
            else
            {
                TempData["Info"] = "No new low stock alerts.";
            }

            return RedirectToAction("LowStockAlerts");
        }

        [HttpGet]
        public ActionResult ReviewAlert(int id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var alert = InMemoryDatabase.LowStockAlerts.FirstOrDefault(a => a.LowStockAlertId == id);
            if (alert == null) return HttpNotFound();

            if (alert.AlertStatus != "Pending")
            {
                TempData["Error"] = "This alert has already been reviewed.";
                return RedirectToAction("LowStockAlerts");
            }

            var recentMovements = InMemoryDatabase.StockMovements
                .Where(m => m.ProductId == alert.ProductId)
                .OrderByDescending(m => m.MovementDate)
                .Take(10)
                .ToList();
            ViewBag.RecentMovements = recentMovements;

            var threshold = InMemoryDatabase.ProductStockThresholds
                .FirstOrDefault(t => t.ProductId == alert.ProductId);
            ViewBag.Threshold = threshold;

            return View(alert);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessAlertDecision(int alertId, string decision, string reviewerNotes,
            int? orderQuantity, DateTime? expectedDeliveryDate, string supplierReference,
            decimal? expectedUnitWeightKg, int? batchCount, int? batchSize)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var alert = InMemoryDatabase.LowStockAlerts.FirstOrDefault(a => a.LowStockAlertId == alertId);
            if (alert == null) return HttpNotFound();

            if (alert.AlertStatus != "Pending")
            {
                TempData["Error"] = "This alert has already been processed.";
                return RedirectToAction("LowStockAlerts");
            }

            var userName = Session["Username"] as string ?? "System";
            alert.ReviewedDate = DateTime.Now;
            alert.ReviewedBy = userName;
            alert.ReviewerNotes = reviewerNotes;

            if (decision == "Approve")
            {
                alert.AlertStatus = "Approved";

                int batches = batchCount ?? 1;
                int size = batchSize ?? 25;
                int totalQty = batches * size;

                var nextExpectedId = InMemoryDatabase.ExpectedStocks.Any()
                    ? InMemoryDatabase.ExpectedStocks.Max(e => e.ExpectedStockId) + 1
                    : 1;

                var expectedStock = new ExpectedStock
                {
                    ExpectedStockId = nextExpectedId,
                    ProductId = alert.ProductId,
                    ExpectedQuantity = totalQty,
                    ExpectedUnitWeightKg = expectedUnitWeightKg ?? 1.0m,
                    ExpectedDeliveryDate = expectedDeliveryDate ?? DateTime.Now.AddDays(7),
                    Status = "Pending",
                    SupplierReference = supplierReference ?? $"PO-{DateTime.Now:yyMMdd}-{alert.ProductId}",
                    PurchaseOrderNumber = GeneratePONumber(),
                    Notes = $"From Alert #{alertId}. {batches} batches x {size} units @ {expectedUnitWeightKg ?? 1.0m}kg. {reviewerNotes ?? ""}",
                    CreatedDate = DateTime.Now,
                    CreatedBy = userName,
                    LowStockAlertId = alert.LowStockAlertId,
                    BatchSize = size,
                    BatchCount = batches
                };

                InMemoryDatabase.ExpectedStocks.Add(expectedStock);

                alert.SupplierReference = expectedStock.SupplierReference;
                alert.ExpectedDeliveryDate = expectedStock.ExpectedDeliveryDate;
                alert.ExpectedUnitWeightKg = expectedStock.ExpectedUnitWeightKg;

                TempData["Success"] = $"Alert approved. PO {expectedStock.PurchaseOrderNumber} created: {batches} batches x {size} units = {totalQty} total.";
            }
            else if (decision == "Reject")
            {
                alert.AlertStatus = "Rejected";
                TempData["Success"] = "Alert rejected.";
            }
            else
            {
                TempData["Error"] = "Invalid decision.";
                return RedirectToAction("ReviewAlert", new { id = alertId });
            }

            return RedirectToAction("LowStockAlerts");
        }

        // ========================================
        // EXPECTED STOCK MANAGEMENT
        // ========================================

        [HttpGet]
        public ActionResult ExpectedStock(string status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var expectedStocks = InMemoryDatabase.ExpectedStocks.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                expectedStocks = expectedStocks.Where(e => e.Status == status);
            }
            if (fromDate.HasValue)
            {
                expectedStocks = expectedStocks.Where(e => e.ExpectedDeliveryDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                expectedStocks = expectedStocks.Where(e => e.ExpectedDeliveryDate <= toDate.Value);
            }

            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.StatusOptions = new[] { "Pending", "InTransit", "PartiallyReceived", "Received", "Cancelled" };
            ViewBag.Products = InMemoryDatabase.Products.OrderBy(p => p.Name).ToList();

            return View(expectedStocks.OrderBy(e => e.ExpectedDeliveryDate).ToList());
        }

        [HttpGet]
        public ActionResult ExpectedStockDetails(int id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var expectedStock = InMemoryDatabase.ExpectedStocks.FirstOrDefault(e => e.ExpectedStockId == id);
            if (expectedStock == null) return HttpNotFound();

            var receivingHistory = InMemoryDatabase.StockBatches
                .Where(sb => sb.BatchNumber.Contains(expectedStock.PurchaseOrderNumber)
                    || sb.BatchNumber.Contains(expectedStock.SupplierReference ?? ""))
                .OrderByDescending(sb => sb.ReceivedDate)
                .ToList();

            ViewBag.ReceivingHistory = receivingHistory;
            return View(expectedStock);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateExpectedStockStatus(int expectedStockId, string newStatus, string notes)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var expectedStock = InMemoryDatabase.ExpectedStocks.FirstOrDefault(e => e.ExpectedStockId == expectedStockId);
            if (expectedStock == null) return HttpNotFound();

            expectedStock.Status = newStatus;
            expectedStock.LastUpdated = DateTime.Now;

            if (!string.IsNullOrEmpty(notes))
            {
                expectedStock.Notes = (expectedStock.Notes ?? "") + "\n" + DateTime.Now.ToString("g") + ": " + notes;
            }

            TempData["Success"] = $"Expected stock status updated to {newStatus}.";
            return RedirectToAction("ExpectedStockDetails", new { id = expectedStockId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateManualExpectedStock(int productId, int expectedQuantity,
            decimal expectedUnitWeightKg, DateTime expectedDeliveryDate, string supplierReference,
            string notes, int batchSize, int batchCount)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return HttpNotFound();

            var userName = Session["Username"] as string ?? "System";
            int totalQty = batchCount > 0 && batchSize > 0 ? batchCount * batchSize : expectedQuantity;

            var nextId = InMemoryDatabase.ExpectedStocks.Any()
                ? InMemoryDatabase.ExpectedStocks.Max(e => e.ExpectedStockId) + 1
                : 1;

            var expectedStock = new ExpectedStock
            {
                ExpectedStockId = nextId,
                ProductId = productId,
                ExpectedQuantity = totalQty,
                ExpectedUnitWeightKg = expectedUnitWeightKg,
                ExpectedDeliveryDate = expectedDeliveryDate,
                Status = "Pending",
                SupplierReference = supplierReference,
                PurchaseOrderNumber = GeneratePONumber(),
                Notes = $"{batchCount} batches x {batchSize} units @ {expectedUnitWeightKg}kg. {notes ?? ""}",
                CreatedDate = DateTime.Now,
                CreatedBy = userName,
                BatchSize = batchSize > 0 ? batchSize : totalQty,
                BatchCount = batchCount > 0 ? batchCount : 1
            };

            InMemoryDatabase.ExpectedStocks.Add(expectedStock);

            TempData["Success"] = $"PO created: {expectedStock.PurchaseOrderNumber}";
            return RedirectToAction("ExpectedStock");
        }

        // ========================================
        // STOCK REPORT
        // ========================================

        [HttpGet]
        public ActionResult StockReport()
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).OrderBy(p => p.Name).ToList();

            // Calculate available stock per product
            var stockBatches = InMemoryDatabase.StockBatches
                .Where(sb => sb.Quantity > 0)
                .GroupBy(sb => sb.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var thresholds = InMemoryDatabase.ProductStockThresholds.ToDictionary(t => t.ProductId);
            var report = new List<StockReportItem>();

            foreach (var product in products)
            {
                int currentStock = stockBatches.ContainsKey(product.Id) ? stockBatches[product.Id] : 0;
                ProductStockThreshold threshold = thresholds.ContainsKey(product.Id) ? thresholds[product.Id] : null;

                var expectedIncoming = InMemoryDatabase.ExpectedStocks
                    .Where(e => e.ProductId == product.Id && e.Status != "Received" && e.Status != "Cancelled")
                    .Sum(e => e.ExpectedQuantity - e.QuantityReceived);

                var totalWeight = InMemoryDatabase.StockBatches
                    .Where(sb => sb.ProductId == product.Id && sb.Quantity > 0)
                    .Sum(sb => (decimal?)sb.Quantity * sb.UnitWeightKg) ?? 0;

                int minThreshold = threshold != null ? threshold.MinimumStockLevel : 50;
                int reorderQty = threshold != null ? threshold.ReorderQuantity : 100;
                int defaultBatchSize = threshold != null && threshold.DefaultBatchSize > 0 ? threshold.DefaultBatchSize : 25;
                decimal defaultUnitWeight = threshold != null && threshold.DefaultUnitWeightKg > 0 ? threshold.DefaultUnitWeightKg : 1.0m;
                int batchCount = (int)Math.Ceiling((double)reorderQty / defaultBatchSize);

                report.Add(new StockReportItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentStock = currentStock,
                    MinimumThreshold = minThreshold,
                    ReorderQuantity = reorderQty,
                    ExpectedIncoming = expectedIncoming,
                    Status = GetStockStatus(currentStock, minThreshold),
                    TotalWeightKg = totalWeight,
                    DefaultUnitWeightKg = defaultUnitWeight,
                    DefaultBatchSize = defaultBatchSize,
                    BatchCount = batchCount
                });
            }

            return View(report.OrderBy(r => r.Status).ThenBy(r => r.ProductName).ToList());
        }

        // ========================================
        // MANAGE THRESHOLDS
        // ========================================

        [HttpGet]
        public ActionResult ManageThresholds()
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).OrderBy(p => p.Name).ToList();
            var thresholds = InMemoryDatabase.ProductStockThresholds.ToDictionary(t => t.ProductId);

            var model = new List<ProductThresholdViewModel>();

            foreach (var product in products)
            {
                var availableStock = InMemoryDatabase.StockBatches
                    .Where(sb => sb.ProductId == product.Id && sb.Quantity > 0)
                    .Sum(sb => sb.Quantity);

                ProductStockThreshold threshold = thresholds.ContainsKey(product.Id) ? thresholds[product.Id] : null;

                model.Add(new ProductThresholdViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentStock = availableStock,
                    MinimumStockLevel = threshold != null ? threshold.MinimumStockLevel : 50,
                    ReorderQuantity = threshold != null ? threshold.ReorderQuantity : 100,
                    AutoCreateAlert = threshold != null ? threshold.AutoCreateAlert : true,
                    HasThreshold = threshold != null
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveThreshold(int productId, int minimumStockLevel, int reorderQuantity,
            bool autoCreateAlert, int defaultBatchSize, decimal defaultUnitWeightKg)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var threshold = InMemoryDatabase.ProductStockThresholds.FirstOrDefault(t => t.ProductId == productId);
            var userName = Session["Username"] as string ?? "System";

            if (threshold == null)
            {
                var nextId = InMemoryDatabase.ProductStockThresholds.Any()
                    ? InMemoryDatabase.ProductStockThresholds.Max(t => t.ProductStockThresholdId) + 1
                    : 1;

                threshold = new ProductStockThreshold
                {
                    ProductStockThresholdId = nextId,
                    ProductId = productId,
                    MinimumStockLevel = minimumStockLevel,
                    ReorderQuantity = reorderQuantity,
                    AutoCreateAlert = autoCreateAlert,
                    DefaultBatchSize = defaultBatchSize,
                    DefaultUnitWeightKg = defaultUnitWeightKg,
                    LastUpdated = DateTime.Now,
                    UpdatedBy = userName
                };
                InMemoryDatabase.ProductStockThresholds.Add(threshold);
            }
            else
            {
                threshold.MinimumStockLevel = minimumStockLevel;
                threshold.ReorderQuantity = reorderQuantity;
                threshold.AutoCreateAlert = autoCreateAlert;
                threshold.DefaultBatchSize = defaultBatchSize;
                threshold.DefaultUnitWeightKg = defaultUnitWeightKg;
                threshold.LastUpdated = DateTime.Now;
                threshold.UpdatedBy = userName;
            }

            TempData["Success"] = "Stock threshold saved.";
            return RedirectToAction("ManageThresholds");
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        private List<LowStockProductInfo> GetLowStockProducts()
        {
            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).ToList();
            var thresholds = InMemoryDatabase.ProductStockThresholds.ToDictionary(t => t.ProductId);
            var lowStockProducts = new List<LowStockProductInfo>();

            foreach (var product in products)
            {
                var availableStock = InMemoryDatabase.StockBatches
                    .Where(sb => sb.ProductId == product.Id && sb.Quantity > 0)
                    .Sum(sb => sb.Quantity);

                int thresholdLevel = thresholds.ContainsKey(product.Id) ? thresholds[product.Id].MinimumStockLevel : 50;

                if (availableStock <= thresholdLevel)
                {
                    lowStockProducts.Add(new LowStockProductInfo
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        AvailableStock = availableStock,
                        ThresholdLevel = thresholdLevel,
                        Shortfall = thresholdLevel - availableStock
                    });
                }
            }

            return lowStockProducts;
        }

        private ProductStockThreshold GetOrCreateThreshold(int productId)
        {
            var threshold = InMemoryDatabase.ProductStockThresholds.FirstOrDefault(t => t.ProductId == productId);

            if (threshold == null)
            {
                var nextId = InMemoryDatabase.ProductStockThresholds.Any()
                    ? InMemoryDatabase.ProductStockThresholds.Max(t => t.ProductStockThresholdId) + 1
                    : 1;

                threshold = new ProductStockThreshold
                {
                    ProductStockThresholdId = nextId,
                    ProductId = productId,
                    MinimumStockLevel = 50,
                    ReorderQuantity = 100,
                    AutoCreateAlert = true,
                    DefaultBatchSize = 25,
                    DefaultUnitWeightKg = 1.0m,
                    LastUpdated = DateTime.Now,
                    UpdatedBy = "System"
                };
                InMemoryDatabase.ProductStockThresholds.Add(threshold);
            }

            return threshold;
        }

        private string GeneratePONumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int count = InMemoryDatabase.ExpectedStocks.Count(e => e.PurchaseOrderNumber != null && e.PurchaseOrderNumber.Contains(datePart)) + 1;
            return $"PO-{datePart}-{count:D3}";
        }

        private string GetStockStatus(int currentStock, int threshold)
        {
            if (currentStock <= 0) return "Out of Stock";
            if (currentStock <= threshold / 2) return "Critical";
            if (currentStock <= threshold) return "Low";
            return "Healthy";
        }
    }

    // ========================================
    // SUPPORTING VIEW MODELS
    // ========================================

    public class StockReportItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumThreshold { get; set; }
        public int ReorderQuantity { get; set; }
        public int ExpectedIncoming { get; set; }
        public string Status { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal DefaultUnitWeightKg { get; set; }
        public int DefaultBatchSize { get; set; }
        public int BatchCount { get; set; }
    }

    public class ProductThresholdViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStockLevel { get; set; }
        public int ReorderQuantity { get; set; }
        public bool AutoCreateAlert { get; set; }
        public bool HasThreshold { get; set; }
    }

    public class LowStockProductInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int AvailableStock { get; set; }
        public int ThresholdLevel { get; set; }
        public int Shortfall { get; set; }
    }
}