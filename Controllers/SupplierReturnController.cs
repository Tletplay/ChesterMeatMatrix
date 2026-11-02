using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    [Authorize]
    public class SupplierReturnController : Controller
    {
        private bool IsWarehouseOrAdmin()
        {
            var role = Session["Role"] as string;
            return role == "Warehouse" || role == "Admin" || role == "WarehouseManager";
        }

        // GET: SupplierReturn/Index
        [HttpGet]
        public ActionResult Index(string status = "Pending")
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var returns = InMemoryDatabase.SupplierReturns
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.ReturnDate)
                .ToList();

            // Load related data
            foreach (var item in returns)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
                item.StockBatch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == item.BatchId);
            }

            ViewBag.Status = status;
            ViewBag.StatusOptions = new[] { "Pending", "Approved", "Completed", "Rejected" };

            return View(returns);
        }

        // GET: SupplierReturn/Create
        [HttpGet]
        public ActionResult Create()
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Products = InMemoryDatabase.Products.Where(p => !p.IsPallet).OrderBy(p => p.Name).ToList();
            ViewBag.Batches = InMemoryDatabase.StockBatches.Where(b => b.Quantity > 0).OrderByDescending(b => b.ReceivedDate).ToList();

            return View();
        }

        // POST: SupplierReturn/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int productId, int? batchId, int quantity, string returnReason, string notes, string supplierName)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Create");
            }

            // Check if enough stock exists
            int availableStock = 0;
            if (batchId.HasValue)
            {
                var batch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == batchId.Value);
                if (batch != null)
                {
                    availableStock = batch.Quantity;
                }
            }
            else
            {
                availableStock = InMemoryDatabase.StockBatches.Where(b => b.ProductId == productId).Sum(b => b.Quantity);
            }

            if (quantity > availableStock)
            {
                TempData["Error"] = $"Insufficient stock. Available: {availableStock}";
                return RedirectToAction("Create");
            }

            // Generate return number
            var returnNumber = $"SR-{DateTime.Now:yyyyMMdd}-{(InMemoryDatabase.SupplierReturns.Count(r => r.ReturnDate.Date == DateTime.Today) + 1):D3}";

            var nextId = InMemoryDatabase.SupplierReturns.Any() ? InMemoryDatabase.SupplierReturns.Max(r => r.SupplierReturnId) + 1 : 1;

            var supplierReturn = new SupplierReturn
            {
                SupplierReturnId = nextId,
                ReturnNumber = returnNumber,
                ProductId = productId,
                BatchId = batchId,
                Quantity = quantity,
                ReturnReason = returnReason,
                Notes = notes,
                SupplierName = supplierName,
                Status = "Pending",
                ReturnDate = DateTime.Now
            };

            InMemoryDatabase.SupplierReturns.Add(supplierReturn);

            TempData["Success"] = $"Supplier return #{returnNumber} created. Stock will be deducted upon approval.";
            return RedirectToAction("Index");
        }

        // GET: SupplierReturn/Approve
        [HttpGet]
        public ActionResult Approve(int id)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var supplierReturn = InMemoryDatabase.SupplierReturns.FirstOrDefault(r => r.SupplierReturnId == id);
            if (supplierReturn == null)
            {
                return HttpNotFound();
            }

            supplierReturn.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == supplierReturn.ProductId);
            supplierReturn.StockBatch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == supplierReturn.BatchId);

            return View(supplierReturn);
        }

        // POST: SupplierReturn/ProcessApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessApproval(int id, string decision, string supplierReference)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var supplierReturn = InMemoryDatabase.SupplierReturns.FirstOrDefault(r => r.SupplierReturnId == id);
            if (supplierReturn == null)
            {
                return HttpNotFound();
            }

            var userName = Session["FullName"] as string ?? Session["Username"] as string;

            if (decision == "Approve")
            {
                supplierReturn.Status = "Approved";
                supplierReturn.ApprovedDate = DateTime.Now;
                supplierReturn.ApprovedBy = userName;
                supplierReturn.SupplierReference = supplierReference;

                // Deduct stock
                if (supplierReturn.BatchId.HasValue)
                {
                    var batch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == supplierReturn.BatchId.Value);
                    if (batch != null)
                    {
                        batch.Quantity -= supplierReturn.Quantity;

                        // Record stock movement
                        var nextMovementId = InMemoryDatabase.StockMovements.Any() ? InMemoryDatabase.StockMovements.Max(m => m.Id) + 1 : 1;
                        InMemoryDatabase.StockMovements.Add(new StockMovement
                        {
                            Id = nextMovementId,
                            ProductId = supplierReturn.ProductId,
                            MovementType = "OUT",
                            Quantity = supplierReturn.Quantity,
                            Reference = supplierReturn.ReturnNumber,
                            MovementDate = DateTime.Now,
                            PerformedBy = userName,
                            Notes = $"Returned to supplier: {supplierReturn.ReturnReason}"
                        });
                    }
                }
                else
                {
                    // Deduct from multiple batches (FIFO)
                    int remaining = supplierReturn.Quantity;
                    var batches = InMemoryDatabase.StockBatches
                        .Where(b => b.ProductId == supplierReturn.ProductId && b.Quantity > 0)
                        .OrderBy(b => b.ReceivedDate)
                        .ToList();

                    foreach (var batch in batches)
                    {
                        if (remaining <= 0) break;
                        int deduct = Math.Min(batch.Quantity, remaining);
                        batch.Quantity -= deduct;
                        remaining -= deduct;
                    }
                }

                TempData["Success"] = $"Return #{supplierReturn.ReturnNumber} approved. Stock deducted.";
            }
            else if (decision == "Reject")
            {
                supplierReturn.Status = "Rejected";
                TempData["Success"] = $"Return #{supplierReturn.ReturnNumber} rejected.";
            }

            return RedirectToAction("Index");
        }

        // GET: SupplierReturn/Details
        [HttpGet]
        public ActionResult Details(int id)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var supplierReturn = InMemoryDatabase.SupplierReturns.FirstOrDefault(r => r.SupplierReturnId == id);
            if (supplierReturn == null)
            {
                return HttpNotFound();
            }

            supplierReturn.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == supplierReturn.ProductId);
            supplierReturn.StockBatch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.Id == supplierReturn.BatchId);

            return View(supplierReturn);
        }
    }
}