using System;
using System.Linq;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;

namespace ChesterMeatMatrix.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        // Helper: Check if user is Admin
        private bool IsAdmin()
        {
            var role = Session["Role"] as string;
            return role == "Admin";
        }

        // ========================================
        // REPORTS DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Access denied. Admin only.";
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // ========================================
        // GET DASHBOARD STATS & CHART DATA (AJAX)
        // ========================================

        [HttpGet]
        public JsonResult GetDashboardData()
        {
            if (!IsAdmin())
            {
                return Json(new { error = "Unauthorized" }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Basic Stats
                var totalOrders = InMemoryDatabase.Orders.Count;
                var totalRevenue = InMemoryDatabase.Orders.Where(o => o.IsPaid).Sum(o => o.Total);
                var pendingOrders = InMemoryDatabase.Orders.Count(o => o.Status == OrderStatusEnum.Pending);
                var deliveredOrders = InMemoryDatabase.Orders.Count(o => o.Status == OrderStatusEnum.Delivered);
                var processingOrders = InMemoryDatabase.Orders.Count(o => o.Status == OrderStatusEnum.Processing);
                var outForDeliveryOrders = InMemoryDatabase.Orders.Count(o => o.Status == OrderStatusEnum.OutForDelivery);
                var confirmedOrders = InMemoryDatabase.Orders.Count(o => o.Status == OrderStatusEnum.Confirmed);

                // Customer Stats
                var totalCustomers = InMemoryDatabase.Customers.Count;
                var enterpriseCustomers = InMemoryDatabase.Customers.Count(c => c.CustomerType == CustomerTypeEnum.Enterprise);
                var instoreCustomers = InMemoryDatabase.Customers.Count(c => c.CustomerType == CustomerTypeEnum.Instore);
                var standardCustomers = InMemoryDatabase.Customers.Count(c => c.CustomerType == CustomerTypeEnum.Standard);

                // Orders by Month (Last 6 months)
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);
                var ordersByMonth = InMemoryDatabase.Orders
                    .Where(o => o.CreatedAt >= sixMonthsAgo)
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                // Sales by Month (Last 6 months)
                var salesByMonth = InMemoryDatabase.Orders
                    .Where(o => o.CreatedAt >= sixMonthsAgo && o.IsPaid)
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Total = g.Sum(o => o.Total)
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                // Recent Orders (Last 10)
                var recentOrders = InMemoryDatabase.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                        o.CreatedAt,
                        o.Total,
                        Status = o.Status.ToString(),
                        IsPaid = o.IsPaid
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    totalOrders,
                    totalRevenue,
                    pendingOrders,
                    deliveredOrders,
                    processingOrders,
                    outForDeliveryOrders,
                    confirmedOrders,
                    totalCustomers,
                    enterpriseCustomers,
                    instoreCustomers,
                    standardCustomers,
                    ordersByMonth,
                    salesByMonth,
                    recentOrders
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ========================================
        // GET INVOICES DATA (AJAX)
        // ========================================

        [HttpGet]
        public JsonResult GetInvoices(string status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!IsAdmin())
            {
                return Json(new { error = "Unauthorized" }, JsonRequestBehavior.AllowGet);
            }

            var orders = InMemoryDatabase.Orders
                .Where(o => o.IsInvoiced)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var isPaid = status == "Paid";
                orders = orders.Where(o => o.IsPaid == isPaid);
            }

            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt <= endDate.Value.AddDays(1));
            }

            var invoices = orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.InvoiceNumber,
                    o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    o.CreatedAt,
                    o.Total,
                    Status = o.IsPaid ? "Paid" : "Unpaid",
                    o.PaymentMethodString
                })
                .ToList();

            return Json(invoices, JsonRequestBehavior.AllowGet);
        }
    }
}