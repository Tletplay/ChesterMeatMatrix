using System;
using System.Linq;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System.Collections.Generic;
using QRCoder;

namespace ChesterMeatMatrix.Controllers
{
    public class ReceiverController : Controller
    {
        private bool IsReceiver()
        {
            var role = Session["Role"] as string;
            return role == "Receiver" || role == "Admin";
        }

        // ==================== DASHBOARD ====================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsReceiver())
            {
                TempData["Error"] = "Access denied. Receiver only.";
                return RedirectToAction("Login", "Account");
            }

            // Get real stats for dashboard
            var pallets = InMemoryDatabase.Products.Where(p => p.IsPallet).ToList();
            var totalPallets = pallets.Count;
            var totalItems = InMemoryDatabase.Products.Count(p => !p.IsPallet);
            var uniqueLocations = InMemoryDatabase.Products.Select(p => p.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct().Count();
            var pendingReturns = InMemoryDatabase.Returns.Count(r => r.Status == "Pending");

            ViewBag.TotalPallets = totalPallets;
            ViewBag.TotalItems = totalItems;
            ViewBag.UniqueLocations = uniqueLocations;
            ViewBag.PendingReturns = pendingReturns;
            ViewBag.FullName = Session["FullName"] as string ?? "Receiver";

            return View();
        }

        // ==================== PALLETS (View all pallets) ====================

        // ==================== PALLETS (View all pallets) ====================

        [HttpGet]
        public ActionResult Pallets(string search)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            var pallets = InMemoryDatabase.Products
                .Where(p => p.IsPallet)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                pallets = pallets.Where(p => p.PalletCode.Contains(search) || p.Name.Contains(search));
            }

            ViewBag.Search = search;
            return View(pallets.OrderByDescending(p => p.CreatedAt).ToList());
        }

        // ==================== CREATE (Receive New Stock) ====================

        [HttpGet]
        public ActionResult Create()
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            // Get available locations for dropdown
            ViewBag.Locations = new List<string>
            {
                "Warehouse Floor 175",
                "Warehouse Floor 191",
                "Picking Freezer 180",
                "Mobile Freezer 170",
                "Congeller Freezer 200"
            };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string productCode,
            string name,
            string mass,
            decimal? price,
            int quantity = 0,
            string location = "Warehouse Floor 175")
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            // Validation
            if (string.IsNullOrEmpty(productCode) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(mass) || quantity <= 0 || !price.HasValue || price.Value <= 0m)
            {
                ModelState.AddModelError("", "Please provide product code, name, mass, a positive unit price and a positive quantity.");
                ViewBag.Locations = new List<string> { "Warehouse Floor 175", "Warehouse Floor 191", "Picking Freezer 180", "Mobile Freezer 170", "Congeller Freezer 200" };
                return View();
            }

            // Generate pallet code
            var palletCode = "8" + DateTime.UtcNow.Ticks.ToString().Substring(6);

            int nextId = InMemoryDatabase.Products.Any() ? InMemoryDatabase.Products.Max(p => p.Id) + 1 : 1;

            // Generate serial numbers
            long defaultBase = 9978326999L;
            var numericSerials = InMemoryDatabase.Products
                .Select(p => p.SerialNumber)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => long.TryParse(s, out long val) ? (long?)val : null)
                .Where(v => v.HasValue)
                .Select(v => v.Value);

            long maxExisting = numericSerials.Any() ? numericSerials.Max() : defaultBase;
            long startSerial = maxExisting + 1;
            var createdSerialNumbers = new List<string>();

            // Create individual products
            for (int i = 0; i < quantity; i++)
            {
                var serial = (startSerial + i).ToString();
                createdSerialNumbers.Add(serial);

                var prod = new Product
                {
                    Id = nextId++,
                    ProductCode = productCode,
                    Name = name + (string.IsNullOrEmpty(mass) ? "" : " (" + mass + ")"),
                    SerialNumber = serial,
                    IsPallet = false,
                    PalletCode = palletCode,
                    Quantity = 1,
                    Price = price.Value,
                    Location = location,
                    CreatedAt = DateTime.UtcNow,
                    Category = "General",
                    ImageUrl = "/Content/Images/placeholders/meat-generic.jpg"
                };
                InMemoryDatabase.Products.Add(prod);
            }

            // Create pallet record
            var pallet = new Product
            {
                Id = nextId++,
                ProductCode = "PAL-" + palletCode,
                Name = "Pallet - " + name,
                SerialNumber = "",
                IsPallet = true,
                PalletCode = palletCode,
                Quantity = quantity,
                Location = location,
                Price = price.Value,
                CreatedAt = DateTime.UtcNow,
                Category = "Pallet",
                ImageUrl = "/Content/Images/placeholders/pallet.jpg"
            };
            InMemoryDatabase.Products.Add(pallet);

            // Create stock batch for FIFO tracking
            var batch = new StockBatch
            {
                Id = InMemoryDatabase.StockBatches.Any() ? InMemoryDatabase.StockBatches.Max(b => b.Id) + 1 : 1,
                ProductId = pallet.Id,
                BatchNumber = $"BATCH-{productCode}-{DateTime.Now:yyyyMMdd}",
                Quantity = quantity,
                UnitWeightKg = decimal.TryParse(mass?.Replace("kg", "").Trim(), out decimal wt) ? wt : 1.0m,
                ReceivedDate = DateTime.UtcNow,
                Location = location,
                Supplier = "Supplier",
                CostPerUnit = price.Value * 0.65m
            };
            InMemoryDatabase.StockBatches.Add(batch);

            TempData["Success"] = $"Received {quantity} x {name} (Total weight: {quantity * (decimal.TryParse(mass?.Replace("kg", "").Trim(), out decimal w) ? w : 1.0m)} kg). Pallet created: {palletCode}.";
            return RedirectToAction("Pallets");
        }

        // ==================== PALLET DETAILS ====================

        [HttpGet]
        public ActionResult PalletDetails(string palletCode, string q)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(palletCode))
                return RedirectToAction("Pallets");

            var pallet = InMemoryDatabase.Products.FirstOrDefault(p => p.IsPallet && p.PalletCode == palletCode);
            if (pallet == null)
                return HttpNotFound();

            var items = InMemoryDatabase.Products.Where(p => !p.IsPallet && p.PalletCode == palletCode).AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                var query = q.Trim();
                items = items.Where(p => p.ProductCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            ViewBag.Pallet = pallet;
            ViewBag.Query = q;
            return View(items.OrderBy(p => p.ProductCode).ToList());
        }

        // ==================== WAREHOUSE STOCK ====================

        [HttpGet]
        public ActionResult Stock(string q, string location)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            var items = InMemoryDatabase.Products.Where(p => !p.IsPallet).AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                var query = q.Trim();
                items = items.Where(p => p.ProductCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         p.SerialNumber.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrEmpty(location))
            {
                items = items.Where(p => p.Location != null && p.Location.IndexOf(location, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            ViewBag.Query = q;
            ViewBag.Location = location;
            ViewBag.Locations = InMemoryDatabase.Products.Select(p => p.Location).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            return View(items.OrderBy(p => p.ProductCode).ToList());
        }

        // ==================== SCAN ITEM (QR Code) ====================

        [HttpGet]
        public ActionResult ScanItem()
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public JsonResult ValidateScannedItem(string scannedCode)
        {
            if (string.IsNullOrEmpty(scannedCode))
                return Json(new { success = false, message = "No code provided." });

            string itemCode = scannedCode;
            string batchNumber = "";
            string productName = "";
            decimal unitWeight = 0;

            if (scannedCode.Contains("|"))
            {
                var parts = scannedCode.Split('|');
                foreach (var part in parts)
                {
                    if (part.StartsWith("ITEM:")) itemCode = part.Replace("ITEM:", "");
                    if (part.StartsWith("BATCH:")) batchNumber = part.Replace("BATCH:", "");
                    if (part.StartsWith("PRODUCT:")) productName = part.Replace("PRODUCT:", "");
                    if (part.StartsWith("WEIGHT:")) decimal.TryParse(part.Replace("WEIGHT:", "").Replace("kg", ""), out unitWeight);
                }
            }

            var qrItem = InMemoryDatabase.BatchItemQrCodes.FirstOrDefault(q => q.ItemCode == itemCode);
            if (qrItem == null)
                return Json(new { success = false, message = "Item not found in inventory. Invalid QR code.", itemCode = itemCode });

            var isAssigned = InMemoryDatabase.Orders.SelectMany(o => o.Items ?? new List<OrderItem>())
                .Any(oi => oi.AssignedItemCodes != null && oi.AssignedItemCodes.Contains(itemCode));

            if (isAssigned)
                return Json(new { success = false, message = "Item already scanned and assigned to an order.", itemCode = itemCode });

            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == qrItem.ProductId);

            return Json(new
            {
                success = true,
                itemCode = qrItem.ItemCode,
                batchNumber = batchNumber,
                productName = product?.Name ?? productName,
                unitWeight = qrItem.UnitWeightKg,
                productId = qrItem.ProductId,
                message = $"Item verified: {product?.Name ?? productName} - {qrItem.UnitWeightKg}kg"
            });
        }

        // ==================== RETURNS MANAGEMENT ====================

        [HttpGet]
        public ActionResult Returns(string status = "Pending")
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            var returns = InMemoryDatabase.Returns.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                returns = returns.Where(r => r.Status == status);

            var result = returns.OrderByDescending(r => r.ReturnDate).ToList();

            foreach (var r in result)
            {
                r.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == r.OrderId);
                if (r.Order != null)
                    r.Order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == r.Order.CustomerId);
            }

            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = new[] { "Pending", "Inspected", "Completed", "All" };
            return View(result);
        }

        [HttpGet]
        public ActionResult InspectReturn(int id)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            var returnRecord = InMemoryDatabase.Returns.FirstOrDefault(r => r.ReturnId == id);
            if (returnRecord == null)
                return HttpNotFound();

            returnRecord.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == returnRecord.OrderId);
            if (returnRecord.Order != null)
                returnRecord.Order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == returnRecord.Order.CustomerId);

            returnRecord.ReturnItems = InMemoryDatabase.ReturnItems.Where(ri => ri.ReturnId == id).ToList();
            foreach (var ri in returnRecord.ReturnItems)
                ri.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == ri.ProductId);

            return View(returnRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmInspection(int returnId, string[] itemConditions, string[] stockActions, string receiverNotes)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            var returnRecord = InMemoryDatabase.Returns.FirstOrDefault(r => r.ReturnId == returnId);
            if (returnRecord == null)
                return HttpNotFound();

            var items = InMemoryDatabase.ReturnItems.Where(ri => ri.ReturnId == returnId).ToList();

            if (items.Count != itemConditions.Length || items.Count != stockActions.Length)
            {
                TempData["Error"] = "Item data mismatch. Please try again.";
                return RedirectToAction("InspectReturn", new { id = returnId });
            }

            int restockedCount = 0;
            int disposedCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                items[i].Condition = itemConditions[i];
                items[i].StockAction = stockActions[i];

                if (stockActions[i] == "Increase")
                {
                    // Restock the returned items
                    var existingBatch = InMemoryDatabase.StockBatches.FirstOrDefault(b => b.ProductId == items[i].ProductId && b.Quantity > 0);
                    if (existingBatch != null)
                    {
                        existingBatch.Quantity += items[i].Quantity;
                    }
                    else
                    {
                        var newBatch = new StockBatch
                        {
                            Id = InMemoryDatabase.StockBatches.Any() ? InMemoryDatabase.StockBatches.Max(b => b.Id) + 1 : 1,
                            ProductId = items[i].ProductId,
                            Quantity = items[i].Quantity,
                            ReceivedDate = DateTime.Now,
                            Location = "Return Restock"
                        };
                        InMemoryDatabase.StockBatches.Add(newBatch);
                    }
                    restockedCount++;
                }
                else
                {
                    disposedCount++;
                }
            }

            returnRecord.Status = "Inspected";
            returnRecord.ReceiverNotes = receiverNotes;
            returnRecord.InspectionDate = DateTime.Now;

            TempData["Success"] = $"Return #{returnId} inspection completed. {restockedCount} item(s) restocked, {disposedCount} item(s) disposed.";
            return RedirectToAction("Returns");
        }

        [HttpGet]
        public ActionResult PalletDetails(string palletCode)
        {
            if (!IsReceiver())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(palletCode))
                return RedirectToAction("Pallets");

            var pallet = InMemoryDatabase.Products.FirstOrDefault(p => p.IsPallet && p.PalletCode == palletCode);
            if (pallet == null)
                return HttpNotFound();

            var items = InMemoryDatabase.Products.Where(p => !p.IsPallet && p.PalletCode == palletCode).ToList();

            ViewBag.Pallet = pallet;
            return View(items);
        }


    }
}