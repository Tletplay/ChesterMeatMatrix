using System;
using System.Linq;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;

namespace ChesterMeatMatrix.Controllers
{
    public class AdminController : Controller
    {
        // Basic admin check using session - prototype only
        private bool IsAdmin()
        {
            return Session != null && (Session["Role"] as string) == "Admin";
        }

        // ========== EXISTING STAFF MANAGEMENT (KEPT AS IS) ==========

        public ActionResult Index()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var users = InMemoryDatabase.Users
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Username)
                .ToList();

            return View(users);
        }

        [HttpGet]
        public ActionResult EditUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(int id, string username, string password, string role, string fullname)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError("", "Username and role are required.");
                return View(user);
            }

            user.Username = username;

            if (!string.IsNullOrEmpty(password))
            {
                user.Password = password;
            }

            user.Role = role;
            user.FullName = fullname;

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == id);

            if (user != null)
            {
                InMemoryDatabase.Users.Remove(user);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult CreateUser()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Update the CreateUser POST method to include Warehouse role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(string username, string password, string role, string fullname)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError("", "Username, password and role are required.");
                return View();
            }

            if (InMemoryDatabase.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View();
            }

            var newUser = new User
            {
                Id = InMemoryDatabase.Users.Any() ? InMemoryDatabase.Users.Max(u => u.Id) + 1 : 1,
                Username = username,
                Password = password,
                Role = role,
                FullName = fullname
            };

            InMemoryDatabase.Users.Add(newUser);

            return RedirectToAction("Index");
        }

        // ========== EFT PENDING ORDERS QUEUE (NEW) ==========

        // GET: Admin/EftPendingOrders
        public ActionResult EftPendingOrders()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var pendingEftOrders = InMemoryDatabase.Orders
                .Where(o => o.PaymentMethodString == "EFT" && !o.IsPaymentConfirmed)
                .OrderBy(o => o.CreatedAt)
                .ToList();

            return View(pendingEftOrders);
        }

        // POST: Admin/ConfirmEftPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmEftPayment(int orderId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return HttpNotFound();

            // Update order status
            order.Status = OrderStatusEnum.Confirmed;
            order.IsPaid = true;
            order.PaymentDate = DateTime.UtcNow;
            order.IsPaymentConfirmed = true;
            order.ConfirmedBy = Session["Username"] as string;

            // Get customer for reference
            var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);

            // Debug output for confirmation
            System.Diagnostics.Debug.WriteLine($"=== ORDER CONFIRMED BY ADMIN ===");
            System.Diagnostics.Debug.WriteLine($"Order #{order.OrderNumber} - Customer: {customer?.Name}");
            System.Diagnostics.Debug.WriteLine($"Total: R {order.Total:F2}");
            System.Diagnostics.Debug.WriteLine($"Confirmed by: {order.ConfirmedBy}");
            System.Diagnostics.Debug.WriteLine($"=================================");

            TempData["Message"] = $"Order #{order.OrderNumber} confirmed. Customer notified.";
            return RedirectToAction("EftPendingOrders");
        }

        // ========== ORDERS MANAGEMENT ==========

        [HttpGet]
        public ActionResult Orders(string status = null)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Orders Management";
            ViewBag.CurrentStatus = status;

            var orders = InMemoryDatabase.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "pending")
                {
                    orders = orders.Where(o => !o.IsInvoiced);
                }
                else if (status == "completed")
                {
                    orders = orders.Where(o => o.IsInvoiced);
                }
            }

            var orderList = orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    CustomerName = o.Customer != null ? o.Customer.Name : "Unknown",
                    o.IsInvoiced,
                    o.InvoiceNumber,
                    o.InvoiceDate,
                    o.CreatedAt,
                    ItemCount = (o.Items == null ? 0 : o.Items.Count)
                }).ToList();

            return View(orderList);
        }

        [HttpGet]
        public ActionResult OrderDetails(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = $"Order Details - {order.OrderNumber}";

            return View(order);
        }

        // ========== WAREHOUSE MANAGEMENT ==========

        [HttpGet]
        public ActionResult Warehouse(string search = null)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Warehouse Management";
            ViewBag.SearchTerm = search;

            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                products = products.Where(p =>
                    (p.ProductCode ?? "").ToLower().Contains(s) ||
                    (p.Name ?? "").ToLower().Contains(s) ||
                    (p.PalletCode ?? "").ToLower().Contains(s) ||
                    (p.Location ?? "").ToLower().Contains(s)
                );
            }

            // Group by PalletCode for summary view
            var grouped = products
                .GroupBy(p => new { p.PalletCode, p.ProductCode })
                .Select(g => new PalletSummary
                {
                    ProductCode = g.Key.ProductCode,
                    PalletCode = string.IsNullOrEmpty(g.Key.PalletCode) ? "No Pallet" : g.Key.PalletCode,
                    Name = g.First().Name,
                    Location = g.First().Location ?? "Unknown",
                    Quantity = g.Count()
                })
                .OrderBy(g => g.ProductCode)
                .ToList();

            return View(grouped);
        }

        // ========== ROUTES & DELIVERY ==========

        [HttpGet]
        public ActionResult Routes()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Routes & Delivery Management";

            var routes = InMemoryDatabase.Routes
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Destination,
                    OrderCount = (r.OrderIds == null ? 0 : r.OrderIds.Count),
                    AssignedDriver = r.AssignedDriverId.HasValue
                        ? InMemoryDatabase.Users.FirstOrDefault(u => u.Id == r.AssignedDriverId.Value)?.FullName
                        : "Not Assigned",
                    AssignedVehicle = r.AssignedVehicleId.HasValue
                        ? InMemoryDatabase.Vehicles.FirstOrDefault(v => v.Id == r.AssignedVehicleId.Value)?.Registration
                        : "Not Assigned"
                }).ToList();

            return View(routes);
        }

        // ========== RECEIVER DASHBOARD ==========

        [HttpGet]
        public ActionResult Receiver()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Receiver Dashboard";

            var pallets = InMemoryDatabase.Products
                .Where(p => p.IsPallet)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToList();

            var totalPallets = pallets.Count;
            var totalItems = InMemoryDatabase.Products.Count(p => !p.IsPallet);

            ViewBag.TotalPallets = totalPallets;
            ViewBag.TotalItems = totalItems;

            return View(pallets);
        }

        // ========== ADMIN DASHBOARD ==========

        [HttpGet]
        public ActionResult Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Title = "Dashboard";

            // REAL DATA FROM YOUR EXISTING DATABASE
            var totalOrders = InMemoryDatabase.Orders.Count;
            var pendingOrders = InMemoryDatabase.Orders.Count(o => !o.IsInvoiced);
            var completedOrders = InMemoryDatabase.Orders.Count(o => o.IsInvoiced);

            var totalProducts = InMemoryDatabase.Products.Count(p => !p.IsPallet);
            var totalPallets = InMemoryDatabase.Products.Count(p => p.IsPallet);
            var totalStockQuantity = InMemoryDatabase.Products.Where(p => !p.IsPallet).Sum(p => (int?)p.Quantity) ?? 0;

            var totalStaff = InMemoryDatabase.Users.Count;
            var totalCustomers = InMemoryDatabase.Customers.Count;

            // Staff by role
            var staffByRole = InMemoryDatabase.Users
                .GroupBy(u => u.Role)
                .ToDictionary(g => g.Key, g => g.Count());

            // Recent orders (last 5) - Create a list of strongly typed objects using a simple class
            var recentOrdersList = new System.Collections.Generic.List<RecentOrderItem>();
            var recentDbOrders = InMemoryDatabase.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var order in recentDbOrders)
            {
                recentOrdersList.Add(new RecentOrderItem
                {
                    OrderNumber = order.OrderNumber ?? "N/A",
                    CustomerName = order.Customer?.Name ?? "Unknown",
                    IsInvoiced = order.IsInvoiced,
                    CreatedAt = order.CreatedAt
                });
            }

            //// Orders ready for picking (not invoiced, has items)
            //var readyToPickList = new System.Collections.Generic.List<ReadyToPickItem>();
            //var readyDbOrders = InMemoryDatabase.Orders
            //    .Where(o => !o.IsInvoiced && o.Items != null && o.Items.Count > 0)
            //    .OrderBy(o => o.CreatedAt)
            //    .Take(10)
            //    .ToList();

            //foreach (var order in readyDbOrders)
            //{
            //    readyToPickList.Add(new ReadyToPickItem
            //    {
            //        OrderNumber = order.OrderNumber ?? "N/A",
            //        CustomerName = order.Customer?.Name ?? "Unknown"
            //    });
            //}

            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalPallets = totalPallets;
            ViewBag.TotalStockQuantity = totalStockQuantity;
            ViewBag.TotalStaff = totalStaff;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.StaffByRole = staffByRole;
            ViewBag.RecentOrders = recentOrdersList;
            //ViewBag.ReadyToPick = readyToPickList;

            return View();
        }
    }

    // This model is needed for the Warehouse view
    public class PalletSummary
    {
        public string ProductCode { get; set; }
        public string PalletCode { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public int Quantity { get; set; }
    }

    // Models for Admin Dashboard
    public class RecentOrderItem
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public bool IsInvoiced { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReadyToPickItem
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
    }
}