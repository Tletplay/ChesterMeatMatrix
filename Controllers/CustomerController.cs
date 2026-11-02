using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using Newtonsoft.Json;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using StripeNet = Stripe;

namespace ChesterMeatMatrix.Controllers
{
    public class CustomerController : Controller
    {
        // ========================================
        // COOKIE-BASED CART CONSTANTS
        // ========================================
        private const string CART_COOKIE_KEY = "ChesterCart";
        private const int CART_COOKIE_DAYS = 30;

        private bool IsCustomer()
        {
            return Session != null && (Session["Role"] as string) == "Customer";
        }

        // ========================================
        // COOKIE-BASED CART METHODS
        // ========================================
        private List<CartItem> GetCart()
        {
            var cart = new List<CartItem>();

            // Try to get from cookie first
            if (Request.Cookies[CART_COOKIE_KEY] != null)
            {
                var cartJson = Request.Cookies[CART_COOKIE_KEY].Value;
                if (!string.IsNullOrEmpty(cartJson))
                {
                    try
                    {
                        cart = JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
                    }
                    catch { cart = new List<CartItem>(); }
                }
            }

            // If no cookie, check session (for backward compatibility)
            if (cart == null || cart.Count == 0)
            {
                cart = Session["GuestCart"] as List<CartItem> ?? new List<CartItem>();
                if (cart.Any())
                {
                    SaveCart(cart); // Migrate to cookie
                }
            }

            return cart ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            var cookie = new HttpCookie(CART_COOKIE_KEY, cartJson)
            {
                Expires = DateTime.Now.AddDays(CART_COOKIE_DAYS),
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Set(cookie);

            // Also keep in session for immediate access
            Session["GuestCart"] = cart;
        }

        private void ClearCart()
        {
            if (Request.Cookies[CART_COOKIE_KEY] != null)
            {
                var cookie = new HttpCookie(CART_COOKIE_KEY)
                {
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Set(cookie);
            }
            Session["GuestCart"] = null;
        }

        // ========================================
        // ORDER NUMBER GENERATION
        // ========================================
        private string GenerateOrderNumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int counter = OrderNumberCounter.GetNextCounter();
            return $"ORD-{datePart}-{counter:D3}";
        }

        // ========================================
        // FIFO STOCK DEDUCTION METHOD
        // ========================================
        private bool DeductStockFIFO(int productId, int quantityRequested, string orderNumber)
        {
            // Get all batches for this product with available stock, ordered by ReceivedDate (oldest first = FIFO)
            var batches = InMemoryDatabase.StockBatches
                .Where(b => b.ProductId == productId && b.Quantity > 0)
                .OrderBy(b => b.ReceivedDate)  // FIFO: oldest batches first
                .ToList();

            if (!batches.Any())
            {
                return false; // No stock available
            }

            int remainingToDeduct = quantityRequested;
            var deductedBatches = new List<(StockBatch batch, int deductedQty)>();

            foreach (var batch in batches)
            {
                if (remainingToDeduct <= 0) break;

                int deductFromThisBatch = Math.Min(batch.Quantity, remainingToDeduct);
                batch.Quantity -= deductFromThisBatch;
                remainingToDeduct -= deductFromThisBatch;

                deductedBatches.Add((batch, deductFromThisBatch));

                // Record stock movement for this deduction
                int nextMovementId = InMemoryDatabase.StockMovements.Any()
                    ? InMemoryDatabase.StockMovements.Max(m => m.Id) + 1
                    : 1;

                var movement = new StockMovement
                {
                    Id = nextMovementId,
                    ProductId = productId,
                    MovementType = "OUT",
                    Quantity = deductFromThisBatch,
                    Reference = orderNumber,
                    MovementDate = DateTime.UtcNow,
                    PerformedBy = Session["Username"] as string ?? "Customer",
                    Notes = $"Order {orderNumber} - FIFO deduction from batch {batch.BatchNumber}",
                    BatchId = batch.Id
                };
                InMemoryDatabase.StockMovements.Add(movement);
            }

            if (remainingToDeduct > 0)
            {
                // Not enough stock - rollback is not needed since we check before calling this method
                return false;
            }

            return true;
        }

        // ========================================
        // EMAIL METHODS
        // ========================================
        private void SendOrderConfirmationEmail(string toEmail, string customerName, Order order, string paymentMethod)
        {
            System.Diagnostics.Debug.WriteLine($"=== EMAIL: Order Confirmation ===");
            System.Diagnostics.Debug.WriteLine($"To: {toEmail}");
            System.Diagnostics.Debug.WriteLine($"Customer: {customerName}");
            System.Diagnostics.Debug.WriteLine($"Order #{order.OrderNumber} - Total: R {order.Total:F2} - Method: {paymentMethod}");
            System.Diagnostics.Debug.WriteLine($"=================================");
        }

        private void SendEftPendingEmail(string toEmail, string customerName, Order order)
        {
            System.Diagnostics.Debug.WriteLine($"=== EMAIL: EFT Payment Pending ===");
            System.Diagnostics.Debug.WriteLine($"To: {toEmail}");
            System.Diagnostics.Debug.WriteLine($"Customer: {customerName}");
            System.Diagnostics.Debug.WriteLine($"Order #{order.OrderNumber} - Total: R {order.Total:F2}");
            System.Diagnostics.Debug.WriteLine($"Bank Details: Chester Meat Bank, Acc: 1234567890, Ref: {order.OrderNumber}");
            System.Diagnostics.Debug.WriteLine($"=================================");
        }

        // ========================================
        // GET: /Customer/Index - Dashboard
        // ========================================
        public ActionResult Index()
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = Session["FullName"] as string;

            var cust = InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == ViewBag.FullName);
            var orders = cust != null
                ? InMemoryDatabase.Orders.Where(o => o.CustomerId == cust.Id).OrderByDescending(o => o.CreatedAt).ToList()
                : new List<Order>();

            return View(orders);
        }

        // ========================================
        // POST: /Customer/UpdateProfile
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(string fullName, string email, string phone, string address)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUsername = Session["Username"] as string;
            var currentFullName = Session["FullName"] as string;

            // Find the customer
            var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == currentFullName);

            if (customer != null)
            {
                // Update customer info
                customer.Name = fullName;
                customer.ContactPhone = phone;
                customer.Address = address;

                // Update session full name
                Session["FullName"] = fullName;
            }

            // Update user email/username if changed
            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == currentUsername);
            if (user != null && email != currentUsername)
            {
                // Check if new email is already taken
                if (!InMemoryDatabase.Users.Any(u => u.Username == email && u.Id != user.Id))
                {
                    user.Username = email;
                    Session["Username"] = email;

                    // Update auth cookie
                    FormsAuthentication.SetAuthCookie(email, false);
                }
            }

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Index", new { tab = "profile" });
        }

        // ========================================
        // GET: /Customer/Products - Product Catalog
        // ========================================
        [HttpGet]
        public ActionResult Products(string category, string sort = "name_asc", int page = 1, int pageSize = 12)
        {
            var validCategories = new[] { "Beef", "Chicken", "Wors & Sausages" };
            var allProducts = InMemoryDatabase.Products
                .Where(p => !p.IsPallet && validCategories.Contains(p.Category))
                .ToList();

            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                allProducts = allProducts.Where(p => p.Category == category).ToList();
            }

            switch (sort)
            {
                case "price_asc":
                    allProducts = allProducts.OrderBy(p => p.Price).ToList();
                    break;
                case "price_desc":
                    allProducts = allProducts.OrderByDescending(p => p.Price).ToList();
                    break;
                case "name_desc":
                    allProducts = allProducts.OrderByDescending(p => p.Name).ToList();
                    break;
                case "name_asc":
                default:
                    allProducts = allProducts.OrderBy(p => p.Name).ToList();
                    break;
            }

            var totalProducts = allProducts.Count();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            var products = allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var categoryCounts = InMemoryDatabase.Products
     .Where(p => !p.IsPallet && validCategories.Contains(p.Category))
     .GroupBy(p => p.Category)
     .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.CurrentCategory = category ?? "all";
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryCounts = categoryCounts;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }

        // ========================================
        // GET: /Customer/ProductsByCategory
        // ========================================
        public ActionResult ProductsByCategory(string category)
        {
            var allProducts = InMemoryDatabase.Products.Where(p => !p.IsPallet).ToList();
            var filteredProducts = new List<Product>();

            switch (category)
            {
                case "BeefChuck": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-001").ToList(); break;
                case "BeefStewing": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-002").ToList(); break;
                case "BeefMince": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-003").ToList(); break;
                case "BeefBurgers": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-004").ToList(); break;
                case "BeefSausage": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-005").ToList(); break;
                case "BeefKidneys": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-006").ToList(); break;
                case "BeefTripe": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-007").ToList(); break;
                case "BeefMeatyBones": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-008").ToList(); break;
                case "OxTongues": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-009").ToList(); break;
                case "Biltong": filteredProducts = allProducts.Where(p => p.ProductCode == "BEEF-010").ToList(); break;
                case "ChickenMixed": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-001" || p.ProductCode == "CHICK-002").ToList(); break;
                case "ChickenLegQuarters": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-003" || p.ProductCode == "CHICK-004").ToList(); break;
                case "ChickenWings": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-005" || p.ProductCode == "CHICK-006").ToList(); break;
                case "ChickenNuggets": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-007" || p.ProductCode == "CHICK-008" || p.ProductCode == "CHICK-009" || p.ProductCode == "CHICK-010").ToList(); break;
                case "ChickenBurgers": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-011").ToList(); break;
                case "ChickenLivers": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-012" || p.ProductCode == "CHICK-013").ToList(); break;
                case "ChickenNecks": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-014" || p.ProductCode == "CHICK-015").ToList(); break;
                case "ChickenViennas": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-016").ToList(); break;
                case "FreshWholeChicken": filteredProducts = allProducts.Where(p => p.ProductCode == "CHICK-017").ToList(); break;
                case "PorkChops": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-001").ToList(); break;
                case "PorkRashers": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-002").ToList(); break;
                case "PorkSausages": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-003").ToList(); break;
                case "PorkLiver": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-004").ToList(); break;
                case "PorkTrotters": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-005").ToList(); break;
                case "LambBones": filteredProducts = allProducts.Where(p => p.ProductCode == "LAMB-001").ToList(); break;
                case "Russians": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-006" || p.ProductCode == "PORK-007").ToList(); break;
                case "Polony": filteredProducts = allProducts.Where(p => p.ProductCode == "PORK-008").ToList(); break;
                case "FrozenChips": filteredProducts = allProducts.Where(p => p.ProductCode == "ADD-001").ToList(); break;
                case "MixedVeg": filteredProducts = allProducts.Where(p => p.ProductCode == "ADD-002").ToList(); break;
                case "Eggs": filteredProducts = allProducts.Where(p => p.ProductCode == "ADD-003").ToList(); break;
                case "Sauces": filteredProducts = allProducts.Where(p => p.ProductCode == "ADD-004" || p.ProductCode == "ADD-005").ToList(); break;
                case "Spices": filteredProducts = allProducts.Where(p => p.ProductCode == "ADD-006" || p.ProductCode == "ADD-007").ToList(); break;
                default: filteredProducts = allProducts; break;
            }

            ViewBag.CategoryName = category;
            return View("StartOrder", filteredProducts);
        }

        // ========================================
        // GET: /Customer/StartOrder
        // ========================================
        [HttpGet]
        public ActionResult StartOrder()
        {
            var products = InMemoryDatabase.Products.Where(p => !p.IsPallet).ToList();
            return View(products);
        }

        // ========================================
        // AJAX: Add to Cart
        // ========================================
        [HttpPost]
        public JsonResult AddToCart(int productId, int quantity)
        {
            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        // ========================================
        // AJAX: Get Cart Count
        // ========================================
        [HttpGet]
        public JsonResult GetCartCount()
        {
            var cart = GetCart();
            return Json(new { count = cart.Sum(c => c.Quantity) }, JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // AJAX: Remove From Cart
        // ========================================
        [HttpPost]
        public JsonResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.ProductId == productId);
            SaveCart(cart);

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity)
            });
        }

        // ========================================
        // GET: View Cart Page
        // ========================================
        [HttpGet]
        public ActionResult ViewCart()
        {
            var cart = GetCart();
            return View(cart);
        }

        // ========================================
        // GET: Cart Slide Panel
        // ========================================
        [HttpGet]
        public ActionResult GetCartPanel()
        {
            var cart = GetCart();
            return PartialView("_CartPanel", cart);
        }

        // ========================================
        // POST: Update Cart Quantity
        // ========================================
        [HttpPost]
        public JsonResult UpdateCartQty(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (quantity < 1) quantity = 1;
                item.Quantity = quantity;
                SaveCart(cart);
            }
            return Json(new { success = true, cartCount = cart.Sum(c => c.Quantity) });
        }

        // ========================================
        // POST: Place Order
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Products");
            }

            if (!IsCustomer())
            {
                TempData["ReturnUrl"] = Url.Action("CompleteOrder", "Customer");
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("CompleteOrder", "Customer") });
            }

            return CompleteOrder();
        }

        // ========================================
        // GET: Complete Order (After Login)
        // ========================================
        [HttpGet]
        public ActionResult CompleteOrder()
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Products");
            }

            var fullName = Session["FullName"] as string;
            var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == fullName);

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = InMemoryDatabase.Customers.Any() ? InMemoryDatabase.Customers.Max(c => c.Id) + 1 : 1,
                    Name = fullName ?? "Guest Customer",
                    IsEnterprise = false,
                    CustomerType = CustomerTypeEnum.Standard,
                    ContactPhone = "",
                    Address = "",
                    OutstandingBalance = 0m,
                    MonthlyCreditLimit = 0m,
                    CurrentMonthDebt = 0m
                };
                InMemoryDatabase.Customers.Add(customer);
            }

            var items = new List<OrderItem>();
            decimal total = 0m;
            decimal totalWeight = 0m;

            foreach (var cartItem in cart)
            {
                var prod = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (prod == null) continue;

                int nextItemId = 1;
                if (InMemoryDatabase.Orders.Any())
                {
                    var allItems = InMemoryDatabase.Orders.SelectMany(o => o.Items ?? new List<OrderItem>());
                    if (allItems.Any())
                    {
                        nextItemId = allItems.Max(it => it.Id) + 1;
                    }
                }

                items.Add(new OrderItem
                {
                    Id = nextItemId,
                    ProductId = prod.Id,
                    Product = prod,
                    Quantity = cartItem.Quantity,
                    UnitPrice = prod.Price
                });

                total += prod.Price * cartItem.Quantity;

                // Calculate weight from product name (e.g., "Fresh Beef Chuck (1 kg)" -> 1)
                decimal weightPerUnit = 1.0m;
                var name = prod.Name;
                var match = System.Text.RegularExpressions.Regex.Match(name, @"\((\d+(?:\.\d+)?)\s*kg\)");
                if (match.Success)
                {
                    decimal.TryParse(match.Groups[1].Value, out weightPerUnit);
                }
                totalWeight += weightPerUnit * cartItem.Quantity;
            }

            if (!items.Any())
            {
                TempData["Error"] = "No valid items in cart.";
                return RedirectToAction("Products");
            }

            // ========================================
            // STEP 1: CHECK STOCK AVAILABILITY BEFORE CREATING ORDER
            // ========================================
            bool stockAvailable = true;
            string outOfStockProduct = "";

            foreach (var cartItem in cart)
            {
                var prod = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (prod == null) continue;

                // Get total available stock from batches
                var totalAvailable = InMemoryDatabase.StockBatches
                    .Where(b => b.ProductId == prod.Id)
                    .Sum(b => b.Quantity);

                if (totalAvailable < cartItem.Quantity)
                {
                    stockAvailable = false;
                    outOfStockProduct = prod.Name;
                    TempData["Error"] = $"Insufficient stock for {outOfStockProduct}. Available: {totalAvailable}, Requested: {cartItem.Quantity}";
                    break;
                }
            }

            if (!stockAvailable)
            {
                return RedirectToAction("ViewCart");
            }

            // ========================================
            // STEP 2: CREATE THE ORDER (WITH WEIGHT)
            // ========================================
            var newOrder = new Order
            {
                Id = InMemoryDatabase.Orders.Any() ? InMemoryDatabase.Orders.Max(o => o.Id) + 1 : 1,
                OrderNumber = GenerateOrderNumber(),
                CustomerId = customer.Id,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                IsInstore = (customer.CustomerType == CustomerTypeEnum.Instore),
                Items = items,
                Total = total,
                TotalWeightKg = totalWeight,  // ADD THIS LINE
                IsInvoiced = false,
                Status = OrderStatusEnum.Pending,
                IsPaid = false,
                IsPaymentConfirmed = false,
                PaymentMethodString = null
            };

            InMemoryDatabase.Orders.Add(newOrder);

            // ========================================
            // STEP 3: DEDUCT STOCK USING FIFO
            // ========================================
            foreach (var cartItem in cart)
            {
                var prod = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (prod != null)
                {
                    bool deducted = DeductStockFIFO(prod.Id, cartItem.Quantity, newOrder.OrderNumber);
                    if (!deducted)
                    {
                        TempData["Error"] = $"Failed to deduct stock for {prod.Name}. Please contact support.";
                        InMemoryDatabase.Orders.Remove(newOrder);
                        return RedirectToAction("ViewCart");
                    }

                    prod.Quantity -= cartItem.Quantity;
                }
            }

            ClearCart();

            if (customer.CustomerType == CustomerTypeEnum.Enterprise)
            {
                return RedirectToAction("OrderConfirmation", new { id = newOrder.Id });
            }
            else
            {
                return RedirectToAction("Payment", new { id = newOrder.Id });
            }
        }

        // ========================================
        // GET: Payment Page
        // ========================================
        [HttpGet]
        public ActionResult Payment(int id)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            if (order.IsPaid) return RedirectToAction("OrderConfirmation", new { id = id });

            return View(order);
        }

        // ========================================
        // POST: Create Stripe Checkout Session
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCheckoutSession(int id)
        {
            if (!IsCustomer()) return Json(new { error = "not_authenticated" });

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return Json(new { error = "order_not_found" });

            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YOUR_SECRET_KEY"))
            {
                return Json(new { error = "stripe_not_configured" });
            }

            try
            {
                StripeNet.StripeConfiguration.ApiKey = secretKey;
                var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
                var successUrl = domain + Url.Action("StripeWebhookSuccess", "Customer", new { orderId = id });
                var cancelUrl = domain + Url.Action("Payment", "Customer", new { id = id });

                // ? FIX: Round to 2 decimal places
                var totalWithVat = Math.Round(order.Total * 1.15m, 2);
                var amountInCents = (int)(totalWithVat * 100);

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
                        UnitAmount = amountInCents,  // ? Changed from UnitAmountDecimal to UnitAmount
                        Currency = "zar",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Order " + order.OrderNumber
                        }
                    },
                    Quantity = 1
                }
            },
                    Metadata = new Dictionary<string, string>
            {
                { "orderId", order.Id.ToString() },
                { "customerId", order.CustomerId.ToString() }
            }
                };

                var service = new SessionService();
                var session = service.Create(options);
                if (session == null) return Json(new { error = "no_session" });

                order.StripeSessionId = session.Id;

                return Json(new { url = session.Url, sessionId = session.Id });
            }
            catch (StripeNet.StripeException ex)
            {
                return Json(new { error = "stripe_error", message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { error = "server_error", message = ex.Message });
            }
        }

        // ========================================
        // POST: Payment Processing
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Payment(int id, string method)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Account");

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);

            // CARD PAYMENT
            // CARD PAYMENT
            if (method == "Card")
            {
                order.PaymentMethodString = "Card";

                var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
                if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YOUR_SECRET_KEY"))
                {
                    order.Status = OrderStatusEnum.Confirmed;
                    order.IsPaid = true;
                    order.IsPaymentConfirmed = true;
                    order.PaymentDate = DateTime.UtcNow;
                    TempData["Message"] = "Card payment simulated. (Stripe keys not configured)";
                    SendOrderConfirmationEmail(customer?.ContactPhone ?? customer?.Name, customer?.Name, order, "Card");
                    return RedirectToAction("OrderConfirmation", new { id = id });
                }

                StripeNet.StripeConfiguration.ApiKey = secretKey;
                var domain = Request.Url.Scheme + "://" + Request.Url.Authority;
                var successUrl = domain + Url.Action("StripeWebhookSuccess", "Customer", new { orderId = id });
                var cancelUrl = domain + Url.Action("Payment", "Customer", new { id = id });

                // ? FIX: Round to 2 decimal places
                var totalWithVat = Math.Round(order.Total * 1.15m, 2);
                var amountInCents = (int)(totalWithVat * 100);

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
                    UnitAmount = amountInCents,  // ? Changed from UnitAmountDecimal to UnitAmount
                    Currency = "zar",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Order " + order.OrderNumber
                    }
                },
                Quantity = 1
            }
        },
                    Metadata = new Dictionary<string, string>
        {
            { "orderId", order.Id.ToString() },
            { "customerId", customer.Id.ToString() }
        }
                };

                try
                {
                    var service = new SessionService();
                    var session = service.Create(options);
                    if (session == null || string.IsNullOrEmpty(session.Url))
                    {
                        TempData["Message"] = "Unable to start Stripe Checkout session.";
                        return RedirectToAction("Payment", new { id = id });
                    }
                    order.StripeSessionId = session.Id;
                    return Redirect(session.Url);
                }
                catch (StripeNet.StripeException ex)
                {
                    TempData["Message"] = "Stripe error: " + ex.Message;
                    return RedirectToAction("Payment", new { id = id });
                }
            }

            // EFT PAYMENT
            else if (method == "EFT")
            {
                order.PaymentMethodString = "EFT";
                order.Status = OrderStatusEnum.Pending;
                order.IsPaid = true;
                order.IsPaymentConfirmed = false;

                SendEftPendingEmail(customer?.ContactPhone ?? customer?.Name, customer?.Name, order);

                TempData["Message"] = "Order placed! Please complete the EFT payment using the bank details. We'll confirm your order once payment is received.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            // CASH PAYMENT (Instore Only)
            else if (method == "Cash")
            {
                if (customer.CustomerType != CustomerTypeEnum.Instore)
                {
                    TempData["Message"] = "Cash on Delivery is only available for Instore customers.";
                    return RedirectToAction("Payment", new { id = id });
                }

                order.PaymentMethodString = "Cash";
                order.Status = OrderStatusEnum.Confirmed;
                order.IsPaid = true;
                order.IsPaymentConfirmed = true;
                order.PaymentDate = DateTime.UtcNow;

                SendOrderConfirmationEmail(customer?.ContactPhone ?? customer?.Name, customer?.Name, order, "Cash on Delivery");
                TempData["Message"] = "Order confirmed! Pay in cash when your order arrives.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            // ENTERPRISE PAYMENT
            else if (method == "Enterprise")
            {
                if (customer.CustomerType != CustomerTypeEnum.Enterprise)
                {
                    TempData["Message"] = "Enterprise payment method not available for your account type.";
                    return RedirectToAction("Payment", new { id = id });
                }

                order.PaymentMethodString = "Enterprise";
                order.Status = OrderStatusEnum.Confirmed;
                order.IsPaid = false;
                order.IsPaymentConfirmed = true;

                customer.CurrentMonthDebt += order.Total;

                SendOrderConfirmationEmail(customer?.ContactPhone ?? customer?.Name, customer?.Name, order, "Monthly Account");
                TempData["Message"] = $"Order confirmed! Amount R {order.Total:F2} added to your monthly account.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            return RedirectToAction("OrderConfirmation", new { id = id });
        }

        // ========================================
        // GET: Stripe Webhook Success
        // ========================================
        [HttpGet]
        public ActionResult StripeWebhookSuccess(int orderId, string session_id)
        {
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return HttpNotFound();

            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            if (!string.IsNullOrEmpty(secretKey) && !secretKey.Contains("YOUR_SECRET_KEY"))
            {
                try
                {
                    StripeNet.StripeConfiguration.ApiKey = secretKey;
                    var service = new SessionService();
                    var session = service.Get(session_id);

                    if (session != null && session.PaymentStatus == "paid" && !order.IsPaymentConfirmed)
                    {
                        order.Status = OrderStatusEnum.Confirmed;
                        order.IsPaid = true;
                        order.PaymentDate = DateTime.UtcNow;
                        order.IsPaymentConfirmed = true;

                        var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == order.CustomerId);
                        SendOrderConfirmationEmail(customer?.ContactPhone ?? customer?.Name, customer?.Name, order, "Card");
                        TempData["Message"] = "Payment successful! Your order is confirmed.";
                    }
                }
                catch { }
            }

            return RedirectToAction("OrderConfirmation", new { id = orderId });
        }

        // ========================================
        // GET: Order Confirmation
        // ========================================
        [HttpGet]
        public ActionResult OrderConfirmation(int id)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Account");

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            var sessionId = Request.QueryString["session_id"];
            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];

            if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(secretKey) && !secretKey.Contains("YOUR_SECRET_KEY"))
            {
                try
                {
                    StripeNet.StripeConfiguration.ApiKey = secretKey;
                    var service = new SessionService();
                    var session = service.Get(sessionId);
                    if (session != null && session.PaymentStatus == "paid" && !order.IsPaid)
                    {
                        order.IsPaid = true;
                        order.PaymentDate = DateTime.UtcNow;
                        order.PaymentMethodString = "Card(Stripe)";
                        order.Status = OrderStatusEnum.Confirmed;
                        TempData["Message"] = "Payment completed via Stripe.";
                    }
                }
                catch { }
            }

            return View(order);
        }

        // ========================================
        // GET: Download Order PDF
        // ========================================
        [HttpGet]
        public ActionResult DownloadOrderPdf(int id)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Account");

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            if (!order.IsPaid)
            {
                TempData["Message"] = "Order PDF is available after payment.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            var pdf = BuildSimplePdfForOrder(order);
            return File(pdf, "application/pdf", order.OrderNumber + ".pdf");
        }

        // ========================================
        // GET: Download Invoice PDF
        // ========================================
        [HttpGet]
        public ActionResult DownloadInvoicePdf(int id)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Account");

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            if (!order.IsPaid)
            {
                TempData["Message"] = "Invoice is available after payment.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            if (!order.IsInvoiced)
            {
                order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                order.InvoiceDate = DateTime.UtcNow;
                order.IsInvoiced = true;
            }

            var pdf = BuildInvoicePdfForOrder(order);
            return File(pdf, "application/pdf", order.InvoiceNumber + ".pdf");
        }

        // ========================================
        // POST: Generate Invoice
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateInvoice(int id)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Account");

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return HttpNotFound();

            if (!order.IsPaid)
            {
                TempData["Message"] = "Invoice can only be generated after payment.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            if (order.IsInvoiced)
            {
                TempData["Message"] = "Invoice already generated.";
                return RedirectToAction("OrderConfirmation", new { id = id });
            }

            order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
            order.InvoiceDate = DateTime.UtcNow;
            order.IsInvoiced = true;
            TempData["Message"] = "Invoice generated.";
            return RedirectToAction("OrderConfirmation", new { id = id });
        }

        // ========================================
        // CREATE PAYMENT INTENT (for embedded card form)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePaymentIntent(int id)
        {
            if (!IsCustomer())
            {
                return Json(new { error = "not_authenticated" });
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return Json(new { error = "order_not_found" });
            }

            // Calculate total with VAT (15%)
            var totalWithVat = order.Total * 1.15m;
            var amountInCents = (long)(totalWithVat * 100);

            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YOUR_SECRET_KEY"))
            {
                return Json(new { error = "stripe_not_configured" });
            }

            try
            {
                StripeNet.StripeConfiguration.ApiKey = secretKey;

                var options = new StripeNet.PaymentIntentCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "zar",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
            {
                { "orderId", order.Id.ToString() },
                { "orderNumber", order.OrderNumber },
                { "customerId", order.CustomerId.ToString() }
            }
                };

                var service = new StripeNet.PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return Json(new
                {
                    success = true,
                    clientSecret = paymentIntent.ClientSecret,
                    orderId = order.Id
                });
            }
            catch (StripeNet.StripeException ex)
            {
                return Json(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ========================================
        // CONFIRM PAYMENT (after successful card charge)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(int orderId, string paymentIntentId)
        {
            if (!IsCustomer())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            var secretKey = ConfigurationManager.AppSettings["StripeSecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YOUR_SECRET_KEY"))
            {
                // Fallback for testing
                order.IsPaid = true;
                order.PaymentDate = DateTime.UtcNow;
                order.Status = OrderStatusEnum.Confirmed;
                order.PaymentMethodString = "Card(Stripe)";
                order.IsPaymentConfirmed = true;

                return Json(new { success = true, redirectUrl = Url.Action("OrderConfirmation", "Customer", new { id = order.Id }) });
            }

            try
            {
                StripeNet.StripeConfiguration.ApiKey = secretKey;
                var service = new StripeNet.PaymentIntentService();
                var paymentIntent = service.Get(paymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    order.IsPaid = true;
                    order.PaymentDate = DateTime.UtcNow;
                    order.Status = OrderStatusEnum.Confirmed;
                    order.PaymentMethodString = "Card(Stripe)";
                    order.IsPaymentConfirmed = true;

                    // Generate invoice
                    if (!order.IsInvoiced)
                    {
                        order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10);
                        order.InvoiceDate = DateTime.UtcNow;
                        order.IsInvoiced = true;
                    }

                    return Json(new { success = true, redirectUrl = Url.Action("OrderConfirmation", "Customer", new { id = order.Id }) });
                }
                else
                {
                    return Json(new { success = false, message = "Payment not completed. Status: " + paymentIntent.Status });
                }
            }
            catch (StripeNet.StripeException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region Private Helper Methods (PDF generation)

        private byte[] BuildSimplePdfForOrder(Order order)
        {
            var objects = new List<string>();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
            objects.Add(null);
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var sb = new StringBuilder();
            void AddText(float x, float y, int fontSize, string text)
            {
                sb.AppendLine("BT");
                sb.AppendFormat("/F1 {0} Tf\n", fontSize);
                sb.AppendFormat("1 0 0 1 {0} {1} Tm\n", x, y);
                sb.AppendFormat("({0}) Tj\n", EscapePdfString(text));
                sb.AppendLine("ET");
            }

            AddText(36, 760, 22, "CHESTER");
            AddText(36, 742, 12, "WHOLESALE MEAT");
            AddText(380, 780, 9, "(031) 274 5501");
            AddText(380, 766, 9, "(031) 279 5573");
            AddText(260, 720, 14, "SALES ORDER");
            AddText(36, 690, 10, "Order #: " + order.OrderNumber);
            AddText(36, 674, 10, "Date: " + order.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            AddText(36, 658, 10, "Customer: " + (order.Customer?.Name ?? ""));

            float tableY = 620;
            AddText(36, tableY, 10, "Product Code");
            AddText(110, tableY, 10, "Description");
            AddText(320, tableY, 10, "Class");
            AddText(360, tableY, 10, "Unit");
            AddText(410, tableY, 10, "Qty");
            AddText(450, tableY, 10, "Unit Price");
            AddText(520, tableY, 10, "Value");

            float rowY = tableY - 18;
            foreach (var it in order.Items)
            {
                AddText(36, rowY, 9, it.Product?.ProductCode ?? "");
                AddText(110, rowY, 9, it.Product?.Name ?? "");
                AddText(320, rowY, 9, "-");
                AddText(360, rowY, 9, "EA");
                AddText(410, rowY, 9, it.Quantity.ToString());
                AddText(450, rowY, 9, it.UnitPrice.ToString("C"));
                AddText(520, rowY, 9, (it.UnitPrice * it.Quantity).ToString("C"));
                rowY -= 16;
            }

            AddText(420, 220, 10, "Total Value:");
            AddText(520, 220, 10, order.Total.ToString("C"));
            var vat = order.Total * 0.15m;
            AddText(420, 200, 10, "Vat (15%):");
            AddText(520, 200, 10, vat.ToString("C"));
            AddText(420, 180, 12, "Total Inclusive:");
            AddText(520, 180, 12, (order.Total + vat).ToString("C"));
            AddText(36, 140, 9, "Notes:");
            AddText(36, 126, 9, "Thank you for ordering from Chester Wholesale Meat.");

            var contentBytes = Encoding.ASCII.GetBytes(sb.ToString());

            using (var ms = new MemoryStream())
            using (var w = new StreamWriter(ms, Encoding.ASCII))
            {
                w.Write("%PDF-1.1\n");
                w.Flush();
                var offsets = new List<long> { 0 };
                for (int i = 0; i < objects.Count; i++)
                {
                    offsets.Add(ms.Position);
                    w.Write((i + 1) + " 0 obj\n");
                    if (i == 3)
                    {
                        w.Write("<< /Length " + contentBytes.Length + " >>\nstream\n");
                        w.Flush();
                        ms.Write(contentBytes, 0, contentBytes.Length);
                        w.Write("\nendstream\n");
                    }
                    else
                    {
                        w.Write(objects[i] + "\n");
                    }
                    w.Write("endobj\n");
                    w.Flush();
                }
                var xrefStart = ms.Position;
                w.Write("xref\n0 " + (objects.Count + 1) + "\n");
                w.Write("0000000000 65535 f \n");
                foreach (var off in offsets.Skip(1))
                {
                    w.Write(off.ToString("D10") + " 00000 n \n");
                }
                w.Write("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n");
                w.Write(xrefStart + "\n%%EOF");
                w.Flush();
                return ms.ToArray();
            }
        }

        private byte[] BuildInvoicePdfForOrder(Order order)
        {
            var objects = new List<string>();
            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
            objects.Add(null);
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var sb = new StringBuilder();
            void AddText(float x, float y, int fontSize, string text)
            {
                sb.AppendLine("BT");
                sb.AppendFormat("/F1 {0} Tf\n", fontSize);
                sb.AppendFormat("1 0 0 1 {0} {1} Tm\n", x, y);
                sb.AppendFormat("({0}) Tj\n", EscapePdfString(text));
                sb.AppendLine("ET");
            }

            AddText(36, 760, 22, "CHESTER");
            AddText(36, 742, 12, "WHOLESALE MEAT");
            AddText(260, 720, 16, "TAX INVOICE");
            AddText(36, 700, 10, "Invoice #: " + (order.InvoiceNumber ?? ""));
            AddText(36, 684, 10, "Invoice Date: " + (order.InvoiceDate?.ToString("yyyy-MM-dd HH:mm") ?? ""));
            AddText(36, 668, 10, "Order #: " + order.OrderNumber);
            AddText(36, 652, 10, "Customer: " + (order.Customer?.Name ?? ""));

            float tableY = 620;
            AddText(36, tableY, 10, "Product Code");
            AddText(110, tableY, 10, "Description");
            AddText(360, tableY, 10, "Qty");
            AddText(450, tableY, 10, "Unit Price");
            AddText(520, tableY, 10, "Value");

            float rowY = tableY - 18;
            foreach (var it in order.Items)
            {
                AddText(36, rowY, 9, it.Product?.ProductCode ?? "");
                AddText(110, rowY, 9, it.Product?.Name ?? "");
                AddText(360, rowY, 9, it.Quantity.ToString());
                AddText(450, rowY, 9, it.UnitPrice.ToString("C"));
                AddText(520, rowY, 9, (it.UnitPrice * it.Quantity).ToString("C"));
                rowY -= 16;
            }

            AddText(420, 220, 10, "Total Value:");
            AddText(520, 220, 10, order.Total.ToString("C"));
            var vat = order.Total * 0.15m;
            AddText(420, 200, 10, "Vat (15%):");
            AddText(520, 200, 10, vat.ToString("C"));
            AddText(420, 180, 12, "Total Inclusive:");
            AddText(520, 180, 12, (order.Total + vat).ToString("C"));

            var contentBytes = Encoding.ASCII.GetBytes(sb.ToString());

            using (var ms = new MemoryStream())
            using (var w = new StreamWriter(ms, Encoding.ASCII))
            {
                w.Write("%PDF-1.1\n");
                w.Flush();
                var offsets = new List<long> { 0 };
                for (int i = 0; i < objects.Count; i++)
                {
                    offsets.Add(ms.Position);
                    w.Write((i + 1) + " 0 obj\n");
                    if (i == 3)
                    {
                        w.Write("<< /Length " + contentBytes.Length + " >>\nstream\n");
                        w.Flush();
                        ms.Write(contentBytes, 0, contentBytes.Length);
                        w.Write("\nendstream\n");
                    }
                    else
                    {
                        w.Write(objects[i] + "\n");
                    }
                    w.Write("endobj\n");
                    w.Flush();
                }
                var xrefStart = ms.Position;
                w.Write("xref\n0 " + (objects.Count + 1) + "\n");
                w.Write("0000000000 65535 f \n");
                foreach (var off in offsets.Skip(1))
                {
                    w.Write(off.ToString("D10") + " 00000 n \n");
                }
                w.Write("trailer\n<< /Size " + (objects.Count + 1) + " /Root 1 0 R >>\nstartxref\n");
                w.Write(xrefStart + "\n%%EOF");
                w.Flush();
                return ms.ToArray();
            }
        }

        private string EscapePdfString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
        #endregion
    }

    // CartItem model for guest cart
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}