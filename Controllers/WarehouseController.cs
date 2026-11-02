using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
   
    public class WarehouseController : Controller
    {
        // Helper: Check if user is Warehouse or Admin
        private bool IsWarehouse()
        {
            var role = Session["Role"] as string;
            return role == "Warehouse" || role == "Admin";
        }

        // ========================================
        // WAREHOUSE DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsWarehouse())
            {
                TempData["Error"] = "Access denied. Warehouse only.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Statistics
            var totalProducts = InMemoryDatabase.Products.Count(p => !p.IsPallet);
            var totalStock = InMemoryDatabase.StockBatches.Sum(b => b.Quantity);
            var lowStockCount = InMemoryDatabase.StockBatches.Count(b => b.Quantity < 10 && b.Quantity > 0);
            var outOfStockCount = InMemoryDatabase.Products.Count(p => p.Quantity == 0 && !p.IsPallet);
            var totalBatches = InMemoryDatabase.StockBatches.Count;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalStock = totalStock;
            ViewBag.LowStockCount = lowStockCount;
            ViewBag.OutOfStockCount = outOfStockCount;
            ViewBag.TotalBatches = totalBatches;
            ViewBag.FullName = Session["FullName"] as string;

            return View();
        }

        // ========================================
        // STOCK LEVELS (View all stock)
        // ========================================

        // ========================================
        // STOCK LEVELS (View all stock)
        // ========================================
        [HttpGet]
        public ActionResult StockLevels(string search, string status = "all")
        {
            if (!IsWarehouse())
            {
                return RedirectToAction("Login", "Account");
            }

            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p => p.Name.Contains(search) || p.ProductCode.Contains(search));
            }

            if (status == "low")
            {
                products = products.Where(p => p.Quantity > 0 && p.Quantity < 10);
            }
            else if (status == "out")
            {
                products = products.Where(p => p.Quantity == 0);
            }
            else if (status == "instock")
            {
                products = products.Where(p => p.Quantity > 0);
            }

            var productList = products.OrderBy(p => p.Name).ToList();

            // Create a list of anonymous objects for view data
            var productStockInfo = new List<object>();

            foreach (var product in productList)
            {
                var batchCount = InMemoryDatabase.StockBatches.Count(b => b.ProductId == product.Id);
                var totalStock = InMemoryDatabase.StockBatches.Where(b => b.ProductId == product.Id).Sum(b => b.Quantity);
                var oldestBatch = InMemoryDatabase.StockBatches
                    .Where(b => b.ProductId == product.Id && b.Quantity > 0)
                    .OrderBy(b => b.ReceivedDate)
                    .FirstOrDefault();

                productStockInfo.Add(new
                {
                    Product = product,
                    BatchCount = batchCount,
                    TotalStock = totalStock,
                    OldestBatch = oldestBatch
                });
            }

            ViewBag.Search = search;
            ViewBag.CurrentStatus = status;
            ViewBag.ProductStockInfo = productStockInfo;

            return View(productList);
        }

        // ========================================
        // BATCH DETAILS
        // ========================================

        [HttpGet]
        public ActionResult BatchDetails(int id)
        {
            if (!IsWarehouse())
            {
                return RedirectToAction("Login", "Account");
            }

            var batch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == id);
            if (batch == null)
            {
                return HttpNotFound();
            }

            var movements = InMemoryDatabase.StockMovements
                .Where(m => m.BatchId == id || m.Reference == batch.BatchNumber)
                .OrderByDescending(m => m.MovementDate)
                .ToList();

            ViewBag.Movements = movements;
            return View(batch);
        }

        // ========================================
        // LOW STOCK ALERTS
        // ========================================

        [HttpGet]
        public ActionResult LowStock()
        {
            if (!IsWarehouse())
            {
                return RedirectToAction("Login", "Account");
            }

            var lowStockBatches = InMemoryDatabase.StockBatches
                .Where(b => b.Quantity > 0 && b.Quantity < 10)
                .OrderBy(b => b.Quantity)
                .ToList();

            var outOfStock = InMemoryDatabase.Products
                .Where(p => p.Quantity == 0 && !p.IsPallet)
                .ToList();

            ViewBag.LowStockBatches = lowStockBatches;
            ViewBag.OutOfStock = outOfStock;

            return View();
        }

        // ========================================
        // STOCK MOVEMENTS HISTORY
        // ========================================

        [HttpGet]
        public ActionResult Movements(DateTime? fromDate, DateTime? toDate, string movementType = "all")
        {
            if (!IsWarehouse())
            {
                return RedirectToAction("Login", "Account");
            }

            var movements = InMemoryDatabase.StockMovements.AsQueryable();

            if (fromDate.HasValue)
            {
                movements = movements.Where(m => m.MovementDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                movements = movements.Where(m => m.MovementDate <= toDate.Value.AddDays(1));
            }
            if (movementType != "all")
            {
                movements = movements.Where(m => m.MovementType == movementType);
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.MovementType = movementType;

            return View(movements.OrderByDescending(m => m.MovementDate).ToList());
        }

        // ========================================
        // PRODUCT DETAILS
        // ========================================

        [HttpGet]
        public ActionResult ProductDetails(int id)
        {
            if (!IsWarehouse())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var batches = InMemoryDatabase.StockBatches
                .Where(b => b.ProductId == id)
                .OrderBy(b => b.ReceivedDate)
                .ToList();

            var movements = InMemoryDatabase.StockMovements
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.MovementDate)
                .Take(50)
                .ToList();

            ViewBag.Batches = batches;
            ViewBag.Movements = movements;

            return View(product);
        }
    }
}