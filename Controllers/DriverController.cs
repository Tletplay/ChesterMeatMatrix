using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    public class DriverController : Controller
    {
        // Helper: Check if user is Driver or Admin
        private bool IsDriver()
        {
            var role = Session["Role"] as string;
            return role == "Driver" || role == "Admin";
        }

        // Helper: Get current driver's User record
        private User GetCurrentDriverUser()
        {
            var username = Session["Username"] as string;
            return InMemoryDatabase.Users.FirstOrDefault(u => u.Username == username && u.Role == "Driver");
        }

        // ========================================
        // DRIVER DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsDriver())
            {
                TempData["Error"] = "Access denied. Driver only.";
                return RedirectToAction("Login", "Account");
            }

            var driverUser = GetCurrentDriverUser();
            if (driverUser == null)
            {
                TempData["Error"] = "Driver record not found.";
                return RedirectToAction("Login", "Account");
            }

            var routes = InMemoryDatabase.Routes
                .Where(r => r.AssignedDriverId == driverUser.Id)
                .ToList();

            // Count routes by status (excluding cancelled orders)
            var pendingCount = routes.Count(r => r.Status == "Assigned" || r.Status == "SentToDriver");
            var inProgressCount = routes.Count(r => r.Status == "InProgress");
            var completedCount = routes.Count(r => r.Status == "Completed" || r.Status == "ReadyForSignature");

            ViewBag.PendingCount = pendingCount;
            ViewBag.InProgressCount = inProgressCount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.FullName = driverUser.FullName;

            return View();
        }

        // ========================================
        // MY ROUTES (List all assigned routes)
        // ========================================

        [HttpGet]
        public ActionResult MyRoutes()
        {
            if (!IsDriver())
            {
                return RedirectToAction("Login", "Account");
            }

            var driverUser = GetCurrentDriverUser();
            if (driverUser == null)
            {
                return RedirectToAction("Index");
            }

            var routes = InMemoryDatabase.Routes
                .Where(r => r.AssignedDriverId == driverUser.Id)
                .OrderByDescending(r => r.Date)
                .ToList();

            // Calculate progress for each route
            foreach (var route in routes)
            {
                if (route.OrderIds != null && route.OrderIds.Any())
                {
                    var orders = InMemoryDatabase.Orders.Where(o => route.OrderIds.Contains(o.Id) && o.Status != OrderStatusEnum.Cancelled).ToList();
                    route.OrderCount = orders.Count;
                    route.TotalAmount = orders.Sum(o => o.Total);

                    // Calculate delivery progress
                    var deliveredCount = orders.Count(o => o.Status == OrderStatusEnum.Delivered);
                    route.ItemsCount = deliveredCount;
                }
            }

            return View(routes);
        }

        // ========================================
        // START DELIVERY FOR A ROUTE
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StartDelivery(int routeId)
        {
            if (!IsDriver())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null)
            {
                return Json(new { success = false, message = "Route not found" });
            }

            route.Status = "InProgress";

            if (route.OrderIds != null)
            {
                foreach (var orderId in route.OrderIds)
                {
                    var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                    if (order != null && order.Status != OrderStatusEnum.Delivered && order.Status != OrderStatusEnum.Cancelled)
                    {
                        order.Status = OrderStatusEnum.OutForDelivery;
                    }
                }
            }

            return Json(new { success = true, message = "Delivery started. You can now mark stops as delivered." });
        }

        // ========================================
        // GET ROUTE STOPS (for stop-by-stop delivery)
        // ========================================

        [HttpGet]
        public ActionResult RouteStops(int? routeId)
        {
            if (routeId == null || routeId == 0)
            {
                TempData["Error"] = "Route ID missing";
                return RedirectToAction("MyRoutes");
            }
            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null) return HttpNotFound();

            var orders = InMemoryDatabase.Orders
                .Where(o => route.OrderIds != null && route.OrderIds.Contains(o.Id) && o.Status != OrderStatusEnum.Cancelled)
                .ToList();

            // Load customer details
            foreach (var order in orders)
            {
                if (order.Customer == null && order.CustomerId > 0)
                {
                    order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
                }
            }

            ViewBag.Route = route;
            ViewBag.RouteId = routeId;

            return View(orders);
        }

        // ========================================
        // MARK SINGLE STOP AS DELIVERED (WITH SIGNATURE & PHOTO)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult MarkStopDelivered(int routeId, int orderId, string signatureData, string deliveryPhotoBase64)
        {
            if (!IsDriver())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            // Verify order belongs to this route
            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null || !route.OrderIds.Contains(orderId))
            {
                return Json(new { success = false, message = "Order not part of this route" });
            }

            // Validate signature
            if (string.IsNullOrWhiteSpace(signatureData) || !signatureData.StartsWith("data:image/"))
            {
                return Json(new { success = false, message = "Please capture customer signature before marking as delivered" });
            }

            // Validate photo (OPTIONAL - no longer required)
            // Photo can be empty string - just log if missing
            if (string.IsNullOrWhiteSpace(deliveryPhotoBase64))
            {
                System.Diagnostics.Debug.WriteLine($"Delivery stop {orderId} completed without photo");
            }

            // Store signature and photo for this order (you may need to add these fields to Order model)
            // For now, store in route as additional data
            order.Status = OrderStatusEnum.Delivered;
            order.IsPaid = true;
            order.IsPaymentConfirmed = true;
            order.PaymentDate = DateTime.UtcNow;

            // Store delivery proof (optional: add to order model)
            // order.DeliverySignature = signatureData;
            // order.DeliveryPhotoUrl = deliveryPhotoBase64;

            // Check if all stops are delivered
            var allDelivered = true;
            if (route.OrderIds != null)
            {
                foreach (var oid in route.OrderIds)
                {
                    var o = InMemoryDatabase.Orders.FirstOrDefault(x => x.Id == oid);
                    if (o != null && o.Status != OrderStatusEnum.Delivered && o.Status != OrderStatusEnum.Cancelled)
                    {
                        allDelivered = false;
                        break;
                    }
                }
            }

            if (allDelivered)
            {
                route.Status = "Completed";
            }

            return Json(new { success = true, message = "Stop marked as delivered", allCompleted = allDelivered });
        }

        // ========================================
        // RETURN PAGE (Customer returned goods at delivery)
        // ========================================

        [HttpGet]
        public ActionResult ReturnOrder(int routeId, int orderId)
        {
            if (!IsDriver())
            {
                return RedirectToAction("Login", "Account");
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null) return HttpNotFound();

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return HttpNotFound();

            if (!route.OrderIds.Contains(orderId))
            {
                return HttpNotFound();
            }

            if (order.Customer == null && order.CustomerId > 0)
            {
                order.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
            }

            ViewBag.RouteId = routeId;
            ViewBag.OrderId = orderId;
            ViewBag.Order = order;

            return View();
        }

        // ========================================
        // SUBMIT RETURN (Immediate return at delivery)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitReturn(int routeId, int orderId, string returnReason, string customerReason, string driverPhotoBase64)
        {
            if (!IsDriver())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return HttpNotFound();

            var driverUser = GetCurrentDriverUser();
            var driverName = driverUser?.FullName ?? Session["FullName"] as string ?? "Driver";

            // Create Return record
            var nextReturnId = InMemoryDatabase.Returns.Any() ? InMemoryDatabase.Returns.Max(r => r.ReturnId) + 1 : 1;

            var returnRecord = new Return
            {
                ReturnId = nextReturnId,
                OrderId = orderId,
                ReturnDate = DateTime.Now,
                ReturnReason = returnReason,
                CustomerReason = customerReason,
                DriverPhotoUrl = driverPhotoBase64,
                Status = "Pending",
                RefundAmount = order.Total
            };
            InMemoryDatabase.Returns.Add(returnRecord);

            // Create ReturnItems from OrderItems
            if (order.Items != null)
            {
                var nextReturnItemId = InMemoryDatabase.ReturnItems.Any() ? InMemoryDatabase.ReturnItems.Max(ri => ri.ReturnItemId) + 1 : 1;

                foreach (var item in order.Items)
                {
                    InMemoryDatabase.ReturnItems.Add(new ReturnItem
                    {
                        ReturnItemId = nextReturnItemId++,
                        ReturnId = returnRecord.ReturnId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Condition = "PendingInspection",
                        StockAction = "Pending"
                    });
                }
            }

            // Update order status to Cancelled (returned)
            order.Status = OrderStatusEnum.Cancelled;

            TempData["Success"] = "Return recorded successfully. The return will be processed by the warehouse.";
            return RedirectToAction("RouteStops", new { routeId = routeId });
        }

        // ========================================
        // COMPLETED DELIVERIES (History)
        // ========================================

        [HttpGet]
        public ActionResult CompletedDeliveries(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsDriver())
            {
                return RedirectToAction("Login", "Account");
            }

            var driverUser = GetCurrentDriverUser();
            if (driverUser == null)
            {
                return RedirectToAction("Index");
            }

            var routes = InMemoryDatabase.Routes
                .Where(r => r.AssignedDriverId == driverUser.Id && (r.Status == "Completed" || r.Status == "ReadyForSignature"))
                .AsQueryable();

            if (fromDate.HasValue)
            {
                routes = routes.Where(r => r.SignatureDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                routes = routes.Where(r => r.SignatureDate <= toDate.Value.AddDays(1));
            }

            // Load order counts
            foreach (var route in routes)
            {
                if (route.OrderIds != null)
                {
                    route.OrderCount = route.OrderIds.Count(o => InMemoryDatabase.Orders.FirstOrDefault(x => x.Id == o)?.Status != OrderStatusEnum.Cancelled);
                    var orders = InMemoryDatabase.Orders.Where(o => route.OrderIds.Contains(o.Id) && o.Status != OrderStatusEnum.Cancelled).ToList();
                    route.TotalAmount = orders.Sum(o => o.Total);
                }
            }

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(routes.OrderByDescending(r => r.SignatureDate).ToList());
        }
    }
}