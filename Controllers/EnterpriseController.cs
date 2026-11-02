using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using StripeNet = Stripe;
using Stripe.Checkout;
using System.Configuration;
using System.Web;

namespace ChesterMeatMatrix.Controllers
{
    
    public class EnterpriseController : Controller
    {
        // Helper: Check if logged in user is Enterprise Customer
        private bool IsEnterpriseCustomer()
        {
            var role = Session["Role"] as string;
            if (role != "Customer") return false;

            var fullName = Session["FullName"] as string;
            var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == fullName);

            return customer != null && customer.CustomerType == CustomerTypeEnum.Enterprise;
        }

        // Helper: Get current enterprise customer
        private Customer GetCurrentEnterpriseCustomer()
        {
            var fullName = Session["FullName"] as string;
            return InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == fullName && c.CustomerType == CustomerTypeEnum.Enterprise);
        }

        // ========================================
        // ENTERPRISE DASHBOARD
        // ========================================

        [HttpGet]
        public ActionResult Index()
        {
            if (!IsEnterpriseCustomer())
            {
                TempData["Error"] = "Access denied. Enterprise customers only.";
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();

            // Calculate total outstanding (unpaid orders that are enterprise method)
            var outstandingOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id &&
                           o.PaymentMethodString == "Enterprise" &&
                           !o.IsPaid)
                .Sum(o => o.Total);

            // Add any current month debt
            var totalDue = outstandingOrders + customer.CurrentMonthDebt;

            ViewBag.CustomerName = customer.Name;
            ViewBag.TotalDue = totalDue;
            ViewBag.CartCount = 0; // Cart is cookie-based

            return View();
        }

        // ========================================
        // ACCOUNT BALANCE
        // ========================================

        [HttpGet]
        public ActionResult AccountBalance()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();

            // Get all unpaid orders
            var unpaidOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id &&
                           o.PaymentMethodString == "Enterprise" &&
                           !o.IsPaid)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            ViewBag.TotalBalance = unpaidOrders.Sum(o => o.Total) + customer.CurrentMonthDebt;
            ViewBag.CartCount = 0;

            return View(unpaidOrders);
        }

        // ========================================
        // PAY ALL (Stripe Checkout for Enterprise)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayAll()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();

            // Get all unpaid orders
            var unpaidOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id &&
                           o.PaymentMethodString == "Enterprise" &&
                           !o.IsPaid)
                .ToList();

            if (!unpaidOrders.Any())
            {
                TempData["Error"] = "No unpaid orders to pay.";
                return RedirectToAction("AccountBalance");
            }

            var totalAmount = unpaidOrders.Sum(o => o.Total) + customer.CurrentMonthDebt;

            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YOUR_SECRET_KEY"))
            {
                // Fallback for testing - mark as paid
                MarkOrdersAsPaid(unpaidOrders, customer);
                TempData["Message"] = $"Payment processed! R {totalAmount:F2} paid. (Stripe simulation)";
                return RedirectToAction("AccountBalance");
            }

            StripeNet.StripeConfiguration.ApiKey = secretKey;
            var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
            var successUrl = domain + Url.Action("PayAllSuccess", "Enterprise");
            var cancelUrl = domain + Url.Action("AccountBalance", "Enterprise");

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmountDecimal = totalAmount * 100,
                            Currency = "zar",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Chester Meat - Bulk Payment",
                                Description = $"Payment for {unpaidOrders.Count} order(s)"
                            }
                        },
                        Quantity = 1
                    }
                },
                CustomerEmail = customer.ContactPhone ?? customer.Name,
                Metadata = new Dictionary<string, string>
                {
                    { "CustomerId", customer.Id.ToString() },
                    { "IsBulkPayment", "true" }
                }
            };

            try
            {
                var service = new SessionService();
                var session = service.Create(options);
                return Redirect(session.Url);
            }
            catch (StripeNet.StripeException ex)
            {
                TempData["Error"] = "Stripe error: " + ex.Message;
                return RedirectToAction("AccountBalance");
            }
        }

        [HttpGet]
        public ActionResult PayAllSuccess()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var unpaidOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id &&
                           o.PaymentMethodString == "Enterprise" &&
                           !o.IsPaid)
                .ToList();

            MarkOrdersAsPaid(unpaidOrders, customer);

            TempData["Message"] = $"Payment successful! {unpaidOrders.Count} order(s) paid.";
            return RedirectToAction("AccountBalance");
        }

        private void MarkOrdersAsPaid(List<Order> orders, Customer customer)
        {
            foreach (var order in orders)
            {
                order.IsPaid = true;
                order.PaymentDate = DateTime.UtcNow;
                order.Status = OrderStatusEnum.Confirmed;
                order.PaymentMethodString = "Enterprise(Paid)";

                // Generate invoice
                if (!order.IsInvoiced)
                {
                    order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                    order.InvoiceDate = DateTime.UtcNow;
                    order.IsInvoiced = true;
                }
            }

            // Reset current month debt
            customer.CurrentMonthDebt = 0m;

            // No SaveChanges needed for InMemoryDatabase - objects are already updated
        }

        // ========================================
        // TRACK ORDER
        // ========================================

        [HttpGet]
        public ActionResult TrackOrder(string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var orders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(search));
            }
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.CreatedAt >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                orders = orders.Where(o => o.CreatedAt < endDate);
            }

            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CartCount = 0;

            return View(orders.OrderByDescending(o => o.CreatedAt).ToList());
        }

        [HttpGet]
        public ActionResult GetOrderDetails(int orderId)
        {
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Content("<p>Order not found.</p>");
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<div class='order-details-modal'>");
            sb.Append("<h4>Order Items</h4>");
            sb.Append("<ul class='list-group'>");

            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    var productName = item.Product != null ? item.Product.Name : "Product not found";
                    sb.Append($"<li class='list-group-item'>{productName} - {item.Quantity} x R {item.UnitPrice:F2} = R {(item.Quantity * item.UnitPrice):F2}</li>");
                }
            }
            else
            {
                sb.Append("<li>No items found</li>");
            }

            sb.Append("</ul>");
            sb.Append($"<p><strong>Order Status:</strong> {order.Status}</p>");
            sb.Append($"<p><strong>Payment Method:</strong> {order.PaymentMethodString}</p>");
            sb.Append($"<p><strong>Paid:</strong> {(order.IsPaid ? "Yes" : "No")}</p>");
            sb.Append("</div>");

            return Content(sb.ToString());
        }

        // ========================================
        // REPORTS & ANALYTICS
        // ========================================

        [HttpGet]
        public ActionResult Reports()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var allOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id)
                .ToList();

            var paidOrders = allOrders.Where(o => o.IsPaid).ToList();
            var unpaidOrders = allOrders.Where(o => !o.IsPaid && o.PaymentMethodString == "Enterprise").ToList();

            ViewBag.TotalSpent = paidOrders.Sum(o => o.Total);
            ViewBag.TotalOutstanding = unpaidOrders.Sum(o => o.Total) + customer.CurrentMonthDebt;
            ViewBag.PaidCount = paidOrders.Count;
            ViewBag.UnpaidCount = unpaidOrders.Count;
            ViewBag.CartCount = 0;

            return View(allOrders.OrderByDescending(o => o.CreatedAt).ToList());
        }

        [HttpGet]
        public JsonResult GetReportData(int months = 6)
        {
            if (!IsEnterpriseCustomer())
            {
                return Json(new { error = "Unauthorized" }, JsonRequestBehavior.AllowGet);
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cutoff = DateTime.UtcNow.AddMonths(-months + 1);

            var orders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id && o.CreatedAt >= cutoff)
                .ToList();

            var labels = new List<string>();
            var paidData = new List<decimal>();
            var unpaidData = new List<decimal>();

            for (int i = months - 1; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var label = monthDate.ToString("yyyy-MM");
                labels.Add(label);

                var monthOrders = orders.Where(o => o.CreatedAt.Year == monthDate.Year && o.CreatedAt.Month == monthDate.Month);
                var paidSum = monthOrders.Where(o => o.IsPaid).Sum(o => (decimal?)o.Total) ?? 0m;
                var unpaidSum = monthOrders.Where(o => !o.IsPaid && o.PaymentMethodString == "Enterprise").Sum(o => (decimal?)o.Total) ?? 0m;

                paidData.Add(decimal.Round(paidSum, 2));
                unpaidData.Add(decimal.Round(unpaidSum, 2));
            }

            var totalPaid = paidData.Sum();
            var totalUnpaid = unpaidData.Sum();

            return Json(new
            {
                labels,
                paidData,
                unpaidData,
                pie = new { paid = totalPaid, unpaid = totalUnpaid }
            }, JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // EDIT PROFILE
        // ========================================

        [HttpGet]
        public ActionResult EditProfile()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();
            ViewBag.CartCount = 0;

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(Customer model)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();

            if (ModelState.IsValid)
            {
                customer.Name = model.Name;
                customer.ContactPhone = model.ContactPhone;
                customer.Address = model.Address;

                TempData["Message"] = "Profile updated successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.CartCount = 0;
            return View(customer);
        }

        // ========================================
        // PLACE ORDER (Redirect to Customer Products)
        // ========================================

        public ActionResult Products()
        {
            return RedirectToAction("Products", "Customer");
        }

        // ========================================
        // RECEIVE ORDER (Enterprise confirms delivery)
        // ========================================

        [HttpGet]
        public ActionResult PendingReceipt()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();

            // Get orders that are OutForDelivery (driver is on the way)
            var pendingOrders = InMemoryDatabase.Orders
                .Where(o => o.CustomerId == customer.Id && o.Status == OrderStatusEnum.OutForDelivery)
                .OrderBy(o => o.CreatedAt)
                .ToList();

            return View(pendingOrders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkOrderReceived(int orderId)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("PendingReceipt");
            }

            var customer = GetCurrentEnterpriseCustomer();
            if (order.CustomerId != customer.Id)
            {
                TempData["Error"] = "Unauthorized.";
                return RedirectToAction("PendingReceipt");
            }

            // Update order status
            order.Status = OrderStatusEnum.Delivered;
            order.IsPaid = true;
            order.IsPaymentConfirmed = true;
            order.PaymentDate = DateTime.UtcNow;

            TempData["Success"] = $"Order #{order.OrderNumber} marked as received. You can now file a claim if needed.";
            return RedirectToAction("PendingReceipt");
        }
    }
}