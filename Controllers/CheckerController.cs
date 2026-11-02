using System;
using System.Linq;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using QRCoder;
using System.Drawing;
using System.IO;

namespace ChesterMeatMatrix.Controllers
{
    public class CheckerController : Controller
    {
        // Helper: Check if user is Checker or Admin
        private bool IsChecker()
        {
            var role = Session["Role"] as string;
            return role == "Checker" || role == "Admin";
        }

        // ========================================
        // CHECKER DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsChecker())
            {
                TempData["Error"] = "Access denied. Checker only.";
                return RedirectToAction("Index", "Dashboard");
            }

            var pendingCount = InMemoryDatabase.Orders
                .Count(o => o.Status == OrderStatusEnum.Confirmed);

            ViewBag.PendingCount = pendingCount;
            ViewBag.FullName = Session["FullName"] as string;

            return View();
        }

        // ========================================
        // PENDING ORDERS (Confirmed, ready to pick)
        // ========================================

        [HttpGet]
        public ActionResult PendingOrders(string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!IsChecker())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var orders = InMemoryDatabase.Orders
                .Where(o => o.Status == OrderStatusEnum.Confirmed)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(search) ||
                                           (o.Customer != null && o.Customer.Name.Contains(search)));
            }
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));
            }

            // Load customer data for each order
            var orderList = orders.OrderByDescending(o => o.CreatedAt).ToList();
            foreach (var order in orderList)
            {
                if (order.Customer == null && order.CustomerId > 0)
                {
                    order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
                }
            }

            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(orderList);
        }

        // ========================================
        // MARK AS READY FOR ROUTER
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult MarkAsReady(int orderId)
        {
            try
            {
                var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Check if order is already locked/marked ready
                if (order.Status == OrderStatusEnum.Processing)
                {
                    return Json(new { success = false, message = "Order already marked as ready for Router" });
                }

                // Validate weight was recorded
                if (order.TotalWeightKg <= 0)
                {
                    return Json(new { success = false, message = "Please record the order weight before marking as ready" });
                }

                // Calculate expected weight for validation
                decimal expectedWeight = CalculateExpectedWeight(order);
                decimal percentageDifference = expectedWeight > 0
                    ? Math.Abs((order.TotalWeightKg - expectedWeight) / expectedWeight) * 100
                    : 0;

                // Check if within tolerance (10%)
                if (percentageDifference > 10m)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Weight difference of {Math.Round(percentageDifference, 2)}% exceeds 10% tolerance. Please verify and adjust weight, or contact manager for override.",
                        requiresOverride = true,
                        expectedWeight = expectedWeight,
                        actualWeight = order.TotalWeightKg,
                        differencePercentage = Math.Round(percentageDifference, 2)
                    });
                }

                // Mark as ready for Router
                order.Status = OrderStatusEnum.Processing;

                // Lock the order (prevent further edits)
                // We'll use a custom property or just rely on status check

                System.Diagnostics.Debug.WriteLine($"Order {order.OrderNumber} marked as ready for Router. Weight: {order.TotalWeightKg} kg, Expected: {expectedWeight} kg");

                return Json(new { success = true, message = "Order marked as ready for Router" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in MarkAsReady: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========================================
        // GET ORDER DETAILS (AJAX)
        // ========================================

        [HttpGet]
        public JsonResult GetOrderDetails(int orderId)
        {
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { error = "Order not found" }, JsonRequestBehavior.AllowGet);
            }

            // Load customer if needed
            if (order.Customer == null && order.CustomerId > 0)
            {
                order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
            }

            // Calculate expected weight
            decimal expectedWeight = CalculateExpectedWeight(order);
            decimal weightDifference = order.TotalWeightKg > 0 ? order.TotalWeightKg - expectedWeight : 0;
            decimal percentageDifference = expectedWeight > 0 && order.TotalWeightKg > 0
                ? Math.Abs((order.TotalWeightKg - expectedWeight) / expectedWeight) * 100
                : 0;
            bool isLocked = order.Status == OrderStatusEnum.Processing || order.Status == OrderStatusEnum.OutForDelivery;

            var items = order.Items.Select(i => new
            {
                productName = i.Product?.Name ?? "Unknown",
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                subtotal = i.Quantity * i.UnitPrice,
                unitWeight = ExtractWeightFromProductName(i.Product?.Name ?? "")
            });

            return Json(new
            {
                orderNumber = order.OrderNumber,
                orderDate = order.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                customerName = order.Customer?.Name ?? "N/A",
                deliveryAddress = order.Customer?.Address ?? "N/A",
                phone = order.Customer?.ContactPhone ?? "N/A",
                totalAmount = order.Total,
                currentWeight = order.TotalWeightKg,
                expectedWeight = expectedWeight,
                weightDifference = weightDifference,
                percentageDifference = Math.Round(percentageDifference, 2),
                isWeightRecorded = order.TotalWeightKg > 0,
                isWithinTolerance = percentageDifference <= 10m,
                isLocked = isLocked,
                status = order.Status.ToString(),
                items = items
            }, JsonRequestBehavior.AllowGet);
        }

        // Helper method to extract weight from product name
        private decimal ExtractWeightFromProductName(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return 1.0m;

            var match = System.Text.RegularExpressions.Regex.Match(productName, @"\((\d+(?:\.\d+)?)\s*kg\)");
            if (match.Success)
            {
                decimal weight;
                if (decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out weight))
                {
                    return weight;
                }
            }
            return 1.0m; // Default to 1kg
        }

        // ========================================
        // QR CODE GENERATION (Download PNG)
        // ========================================

        [HttpGet]
        public ActionResult GenerateQrCode(int orderId)
        {
            if (!IsChecker())
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Dashboard");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return HttpNotFound();
            }

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // Create QR code with order number and ID
                string qrData = $"{order.OrderNumber}|{order.Id}";

                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] imageBytes = ms.ToArray();
                            return File(imageBytes, "image/png", $"QR_{order.OrderNumber}.png");
                        }
                    }
                }
            }
        }

        // ========================================
        // DISPLAY QR CODE IN BROWSER
        // ========================================

        [HttpGet]
        public ActionResult ShowQrCode(int orderId)
        {
            if (!IsChecker())
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Dashboard");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return HttpNotFound();
            }

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                string qrData = $"{order.OrderNumber}|{order.Id}";
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            byte[] imageBytes = ms.ToArray();
                            return File(imageBytes, "image/png");
                        }
                    }
                }
            }
        }

        // ========================================
        // SCAN QR CODE PAGE
        // ========================================

        [HttpGet]
        public ActionResult ScanOrder()
        {
            if (!IsChecker())
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // ========================================
        // PROCESS SCANNED QR CODE (AJAX)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ProcessScannedCode(string scannedCode)
        {
            if (!IsChecker())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (string.IsNullOrEmpty(scannedCode))
            {
                return Json(new { success = false, message = "No code provided" });
            }

            // Parse the QR data (format: ORDER-NUMBER|ORDER-ID)
            var parts = scannedCode.Split('|');
            if (parts.Length < 2)
            {
                // Try treating as just order number
                var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.OrderNumber == scannedCode);
                if (order != null)
                {
                    return GetOrderData(order);
                }
                return Json(new { success = false, message = "Invalid QR code format" });
            }

            string orderNumber = parts[0];
            if (!int.TryParse(parts[1], out int orderId))
            {
                return Json(new { success = false, message = "Invalid order ID" });
            }

            var foundOrder = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId && o.OrderNumber == orderNumber);
            if (foundOrder == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            return GetOrderData(foundOrder);
        }

        private JsonResult GetOrderData(Order order)
        {
            // Load customer if needed
            if (order.Customer == null && order.CustomerId > 0)
            {
                order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
            }

            var orderItems = order.Items.Select(i => new
            {
                productName = i.Product?.Name ?? "Unknown",
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                subtotal = i.Quantity * i.UnitPrice
            });

            return Json(new
            {
                success = true,
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                customerName = order.Customer?.Name ?? "N/A",
                total = order.Total,
                totalWeight = order.TotalWeightKg,
                items = orderItems,
                status = order.Status.ToString()
            });
        }

        // ========================================
        // UPDATE ORDER WEIGHT (AJAX)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateOrderWeight(int orderId, decimal weight)
        {
            if (!IsChecker())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            if (weight <= 0)
            {
                return Json(new { success = false, message = "Weight must be greater than 0" });
            }

            // Calculate expected weight
            decimal expectedWeight = CalculateExpectedWeight(order);

            // Calculate difference percentage
            decimal difference = Math.Abs(weight - expectedWeight);
            decimal percentageDifference = expectedWeight > 0 ? (difference / expectedWeight) * 100 : 0;

            // Check if within tolerance (10%)
            bool isWithinTolerance = percentageDifference <= 10m;

            // Update order weight
            order.TotalWeightKg = weight;

            return Json(new
            {
                success = true,
                message = $"Weight updated to {weight} kg",
                expectedWeight = expectedWeight,
                actualWeight = weight,
                percentageDifference = Math.Round(percentageDifference, 2),
                isWithinTolerance = isWithinTolerance,
                difference = Math.Round(difference, 2)
            });
        }

        // Helper: Calculate expected weight from order items
        // Helper: Calculate expected weight from order items
        private decimal CalculateExpectedWeight(Order order)
        {
            if (order.Items == null || !order.Items.Any())
                return 0m;

            decimal totalWeight = 0m;

            foreach (var item in order.Items)
            {
                if (item.Product == null)
                    continue;

                var name = item.Product.Name ?? "";
                var match = System.Text.RegularExpressions.Regex.Match(name, @"\((\d+(?:\.\d+)?)\s*kg\)");

                decimal unitWeight = 1.0m;
                if (match.Success)
                {
                    decimal.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out unitWeight);
                }

                totalWeight += unitWeight * item.Quantity;
            }

            return Math.Round(totalWeight, 2);
        }

    }
}