using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    
    public class SalesController : Controller
    {
        // Helper: Check if user is SalesRep or Admin
        private bool IsSalesRep()
        {
            var role = Session["Role"] as string;
            return role == "SalesRep" || role == "Admin";
        }

        // ========================================
        // SALES DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsSalesRep())
            {
                TempData["Error"] = "Access denied. Sales representatives only.";
                return RedirectToAction("Index", "Dashboard");
            }

            // Count orders pending confirmation (not confirmed, not delivered, not cancelled)
            var pendingCount = InMemoryDatabase.Orders
                .Count(o => o.Status != OrderStatusEnum.Confirmed &&
                            o.Status != OrderStatusEnum.Delivered &&
                            o.Status != OrderStatusEnum.Cancelled);

            ViewBag.PendingCount = pendingCount;
            ViewBag.FullName = Session["FullName"] as string;

            return View();
        }



        // ========================================
        // PENDING ORDERS (Orders awaiting confirmation)
        // ========================================

        [HttpGet]
        public ActionResult PendingOrders(string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!IsSalesRep())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var orders = InMemoryDatabase.Orders
    .Where(o => o.IsPaid == true           // Payment completed
             && o.IsInvoiced == false)      // No invoice yet
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

            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(orders.OrderByDescending(o => o.CreatedAt).ToList());
        }

        // ========================================
        // CONFIRM ORDER (Generate Invoice)
        // ========================================

        [HttpGet]
        public ActionResult ConfirmOrder(int id)
        {
            if (!IsSalesRep())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmOrder(int orderId, string confirmedBy)
        {
            if (!IsSalesRep())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("PendingOrders");
            }

            // Only update status if not already Confirmed
            if (order.Status != OrderStatusEnum.Confirmed)
            {
                order.Status = OrderStatusEnum.Confirmed;
            }
            order.ConfirmedBy = confirmedBy ?? Session["FullName"] as string;

            // Generate invoice if not exists (ALWAYS do this)
            if (!order.IsInvoiced)
            {
                order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                order.InvoiceDate = DateTime.UtcNow;
                order.IsInvoiced = true;
            }

            // Generate invoice if not exists
            if (!order.IsInvoiced)
            {
                order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                order.InvoiceDate = DateTime.UtcNow;
                order.IsInvoiced = true;
            }

            TempData["Success"] = $"Order #{order.OrderNumber} confirmed. Invoice #{order.InvoiceNumber} generated.";
            return RedirectToAction("PendingOrders");
        }

        // ========================================
        // INVOICES (View all invoices)
        // ========================================

        [HttpGet]
        public ActionResult Invoices(string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!IsSalesRep())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var orders = InMemoryDatabase.Orders
                .Where(o => o.IsInvoiced)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(search) ||
                                           o.InvoiceNumber.Contains(search) ||
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

            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(orders.OrderByDescending(o => o.CreatedAt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult BulkConfirmOrders(int[] orderIds)
        {
            if (!IsSalesRep())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (orderIds == null || orderIds.Length == 0)
            {
                return Json(new { success = false, message = "No orders selected" });
            }

            var confirmedCount = 0;
            var errors = new List<string>();

            foreach (var orderId in orderIds)
            {
                var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    errors.Add($"Order #{orderId} not found");
                    continue;
                }

                // Check if already invoiced
                if (order.IsInvoiced)
                {
                    errors.Add($"Order #{order.OrderNumber} already has an invoice");
                    continue;
                }

                // Update status if not already Confirmed
                if (order.Status != OrderStatusEnum.Confirmed)
                {
                    order.Status = OrderStatusEnum.Confirmed;
                }

                order.ConfirmedBy = Session["FullName"] as string ?? Session["Username"] as string;

                // Generate invoice
                order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                order.InvoiceDate = DateTime.UtcNow;
                order.IsInvoiced = true;

                confirmedCount++;
            }

            var message = $"{confirmedCount} order(s) confirmed with invoices generated.";
            if (errors.Any())
            {
                message += " " + string.Join(" ", errors);
            }

            return Json(new { success = true, message = message });
        }
    }
}