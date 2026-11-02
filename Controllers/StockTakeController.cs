using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    public class StockTakeController : Controller
    {
        private bool IsWarehouseManager()
        {
            var role = Session["Role"] as string;
            return role == "Admin" || role == "WarehouseManager";
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsWarehouseManager())
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Login", "Account");
            }

            var sessions = InMemoryDatabase.StockTakes
                .OrderByDescending(s => s.CreatedDate)
                .ToList();

            return View(sessions);
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Staff = InMemoryDatabase.Users
                .Where(u => u.Role == "Warehouse" || u.Role == "Checker")
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.Products = InMemoryDatabase.Products
                .Where(p => !p.IsPallet)
                .OrderBy(p => p.Name)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string name, string location, DateTime startDate, string notes, int[] selectedStaffIds, int[] selectedProductIds)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            var sessionNumber = $"ST-{DateTime.Now:yyyyMMdd}-{(InMemoryDatabase.StockTakes.Count(s => s.CreatedDate.Date == DateTime.Today) + 1):D3}";

            var nextId = InMemoryDatabase.StockTakes.Any()
                ? InMemoryDatabase.StockTakes.Max(s => s.StockTakeId) + 1
                : 1;

            var stockTake = new StockTake
            {
                StockTakeId = nextId,
                Name = name,
                SessionNumber = sessionNumber,
                Location = location,
                StartDate = startDate,
                Status = StockTakeStatus.Draft,
                CreatedBy = Session["FullName"] as string ?? Session["Username"] as string,
                CreatedDate = DateTime.Now,
                Notes = notes
            };

            InMemoryDatabase.StockTakes.Add(stockTake);

            // Assign Counters
            if (selectedStaffIds != null && selectedStaffIds.Any())
            {
                foreach (var staffId in selectedStaffIds)
                {
                    var assignId = InMemoryDatabase.StockTakeAssignments.Any()
                        ? InMemoryDatabase.StockTakeAssignments.Max(a => a.StockTakeAssignmentId) + 1
                        : 1;

                    var assignment = new StockTakeAssignment
                    {
                        StockTakeAssignmentId = assignId,
                        StockTakeId = stockTake.StockTakeId,
                        StaffId = staffId,
                        AssignedById = 1,
                        AssignedDate = DateTime.Now,
                        Status = StockTakeAssignmentStatus.Pending,
                        Notes = "Assigned to stock take"
                    };

                    InMemoryDatabase.StockTakeAssignments.Add(assignment);
                }
            }

            // Generate Count Sheet Items
            if (selectedProductIds != null && selectedProductIds.Any())
            {
                foreach (var productId in selectedProductIds)
                {
                    var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == productId);
                    if (product != null)
                    {
                        var systemStock = InMemoryDatabase.StockBatches
                            .Where(b => b.ProductId == productId)
                            .Sum(b => b.Quantity);

                        var itemId = InMemoryDatabase.StockCountItems.Any()
                            ? InMemoryDatabase.StockCountItems.Max(i => i.StockCountItemId) + 1
                            : 1;

                        var countItem = new StockCountItem
                        {
                            StockCountItemId = itemId,
                            StockTakeId = stockTake.StockTakeId,
                            ProductId = productId,
                            Product = product,
                            SystemQuantity = systemStock,
                            Status = StockCountStatus.Pending
                        };

                        InMemoryDatabase.StockCountItems.Add(countItem);
                    }
                }
            }
            else
            {
                var allProducts = InMemoryDatabase.Products.Where(p => !p.IsPallet).ToList();
                foreach (var product in allProducts)
                {
                    var systemStock = InMemoryDatabase.StockBatches
                        .Where(b => b.ProductId == product.Id)
                        .Sum(b => b.Quantity);

                    var itemId = InMemoryDatabase.StockCountItems.Any()
                        ? InMemoryDatabase.StockCountItems.Max(i => i.StockCountItemId) + 1
                        : 1;

                    var countItem = new StockCountItem
                    {
                        StockCountItemId = itemId,
                        StockTakeId = stockTake.StockTakeId,
                        ProductId = product.Id,
                        Product = product,
                        SystemQuantity = systemStock,
                        Status = StockCountStatus.Pending
                    };

                    InMemoryDatabase.StockCountItems.Add(countItem);
                }
            }

            TempData["Success"] = $"Stock take session '{sessionNumber}' created with {InMemoryDatabase.StockCountItems.Count(i => i.StockTakeId == stockTake.StockTakeId)} items to count.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null || id == 0)
            {
                TempData["Error"] = "No stock take session selected.";
                return RedirectToAction("Index");
            }

            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == id);
            if (stockTake == null)
            {
                TempData["Error"] = "Stock take session not found.";
                return RedirectToAction("Index");
            }

            stockTake.Assignments = InMemoryDatabase.StockTakeAssignments
                .Where(a => a.StockTakeId == id)
                .ToList();

            foreach (var assignment in stockTake.Assignments)
            {
                assignment.Staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == assignment.StaffId);
            }

            stockTake.CountItems = InMemoryDatabase.StockCountItems
                .Where(i => i.StockTakeId == id)
                .ToList();

            foreach (var item in stockTake.CountItems)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            stockTake.TotalItemsCounted = stockTake.CountItems.Count(i => i.PhysicalCount.HasValue);
            stockTake.TotalVariancesFound = stockTake.CountItems.Count(i => i.Variance != 0);

            return View(stockTake);
        }

        [HttpGet]
        public ActionResult CountSheet(int id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == id);
            if (stockTake == null)
            {
                return HttpNotFound();
            }

            var countItems = InMemoryDatabase.StockCountItems
                .Where(i => i.StockTakeId == id)
                .ToList();

            foreach (var item in countItems)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            ViewBag.StockTake = stockTake;
            return View(countItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartCounting(int id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == id);
            if (stockTake == null)
            {
                return HttpNotFound();
            }

            stockTake.Status = StockTakeStatus.Counting;

            TempData["Success"] = "Stock take session started. Counters can now enter counts.";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnterPhysicalCount(int itemId, int physicalCount, string notes)
        {
            if (!IsWarehouseManager())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var countItem = InMemoryDatabase.StockCountItems.FirstOrDefault(i => i.StockCountItemId == itemId);
            if (countItem == null)
            {
                TempData["Error"] = "Count item not found.";
                return RedirectToAction("Details", new { id = 0 });
            }

            countItem.PhysicalCount = physicalCount;
            countItem.Variance = physicalCount - countItem.SystemQuantity;
            countItem.Status = StockCountStatus.Counted;
            countItem.CountedDate = DateTime.Now;
            countItem.Notes = notes;

            // Get staff who entered the count
            var staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == Session["Username"] as string);
            if (staff != null)
            {
                countItem.CountedByStaffId = staff.Id;
            }

            // Update the StockTake's total items counted
            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == countItem.StockTakeId);
            if (stockTake != null)
            {
                stockTake.TotalItemsCounted = InMemoryDatabase.StockCountItems
                    .Count(i => i.StockTakeId == stockTake.StockTakeId && i.PhysicalCount.HasValue);
                stockTake.TotalVariancesFound = InMemoryDatabase.StockCountItems
                    .Count(i => i.StockTakeId == stockTake.StockTakeId && i.Variance != 0);
            }

            TempData["Success"] = $"Physical count {physicalCount} saved. Variance: {countItem.Variance}";
            return RedirectToAction("Details", new { id = countItem.StockTakeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveCorrection(int itemId, string investigationNotes)
        {
            if (!IsWarehouseManager())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var countItem = InMemoryDatabase.StockCountItems.FirstOrDefault(i => i.StockCountItemId == itemId);
            if (countItem == null)
            {
                TempData["Error"] = "Count item not found.";
                return RedirectToAction("Details", new { id = 0 });
            }

            // Update count item status
            countItem.Status = StockCountStatus.Verified;
            countItem.VerifiedDate = DateTime.Now;
            countItem.Notes = investigationNotes;

            // Get staff who approved
            var staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == Session["Username"] as string);
            if (staff != null)
            {
                countItem.VerifiedByStaffId = staff.Id;
            }

            // IMPORTANT: Update actual stock to match physical count
            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == countItem.StockTakeId);
            if (stockTake != null)
            {
                // Get the product
                var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == countItem.ProductId);
                if (product != null)
                {
                    // Find stock batches and adjust to match physical count
                    var batches = InMemoryDatabase.StockBatches
                        .Where(b => b.ProductId == countItem.ProductId)
                        .ToList();

                    if (batches.Any())
                    {
                        // Adjust first batch to match physical count
                        var totalAdjustment = countItem.PhysicalCount.Value - countItem.SystemQuantity;

                        // Record stock adjustment
                        var movement = new StockMovement
                        {
                            Id = InMemoryDatabase.StockMovements.Any() ? InMemoryDatabase.StockMovements.Max(m => m.Id) + 1 : 1,
                            ProductId = countItem.ProductId,
                            MovementType = "ADJUST",
                            Quantity = Math.Abs(totalAdjustment),
                            Reference = $"StockTake-{stockTake.SessionNumber}",
                            MovementDate = DateTime.Now,
                            PerformedBy = Session["FullName"] as string ?? Session["Username"] as string,
                            Notes = $"Stock take correction: {investigationNotes}"
                        };
                        InMemoryDatabase.StockMovements.Add(movement);

                        // Adjust the batch quantity
                        var firstBatch = batches.FirstOrDefault();
                        if (firstBatch != null)
                        {
                            // Update product quantity
                            product.Quantity = countItem.PhysicalCount.Value;

                            // Update batch to match
                            firstBatch.Quantity = countItem.PhysicalCount.Value;
                        }
                    }
                    else
                    {
                        // No batch exists, create one
                        product.Quantity = countItem.PhysicalCount.Value;
                        var newBatch = new StockBatch
                        {
                            Id = InMemoryDatabase.StockBatches.Any() ? InMemoryDatabase.StockBatches.Max(b => b.Id) + 1 : 1,
                            ProductId = countItem.ProductId,
                            Product = product,
                            BatchNumber = $"STOCKTAKE-{stockTake.SessionNumber}-{DateTime.Now:yyyyMMdd}",
                            Quantity = countItem.PhysicalCount.Value,
                            UnitWeightKg = 1.0m,
                            ReceivedDate = DateTime.Now,
                            Location = product.Location,
                            Supplier = "Stock Take Correction",
                            CostPerUnit = product.Price
                        };
                        InMemoryDatabase.StockBatches.Add(newBatch);
                    }

                    // Update StockTake status to VariancesIdentified
                    stockTake.Status = StockTakeStatus.VariancesIdentified;
                }
            }

            TempData["Success"] = $"Correction approved. Stock updated to match physical count.";
            return RedirectToAction("Details", new { id = countItem.StockTakeId });
        }

        [HttpGet]
        public ActionResult Report(int id)
        {
            if (!IsWarehouseManager())
            {
                return RedirectToAction("Login", "Account");
            }

            var stockTake = InMemoryDatabase.StockTakes.FirstOrDefault(s => s.StockTakeId == id);
            if (stockTake == null)
            {
                return HttpNotFound();
            }

            // Load count items
            stockTake.CountItems = InMemoryDatabase.StockCountItems
                .Where(i => i.StockTakeId == id)
                .ToList();

            foreach (var item in stockTake.CountItems)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            // Recalculate stats
            stockTake.TotalItemsCounted = stockTake.CountItems.Count(i => i.PhysicalCount.HasValue);
            stockTake.TotalVariancesFound = stockTake.CountItems.Count(i => i.Variance != 0);

            return View(stockTake);
        }
    }
}