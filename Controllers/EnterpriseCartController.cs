using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using ChesterMeatMatrix.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    [Authorize]
    public class EnterpriseCartController : Controller
    {
        // Helper: Check if user is Enterprise Customer
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

        // Helper: Get or create cart for customer
        private EnterpriseCart GetOrCreateCart(int customerId)
        {
            var cart = InMemoryDatabase.EnterpriseCarts.FirstOrDefault(c => c.CustomerId == customerId);
            if (cart == null)
            {
                var nextId = InMemoryDatabase.EnterpriseCarts.Any() ? InMemoryDatabase.EnterpriseCarts.Max(c => c.CartId) + 1 : 1;
                cart = new EnterpriseCart
                {
                    CartId = nextId,
                    CustomerId = customerId,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    Items = new List<EnterpriseCartItem>()
                };
                InMemoryDatabase.EnterpriseCarts.Add(cart);
            }
            return cart;
        }

        // ========================================
        // CART ACTIONS
        // ========================================

        // GET: EnterpriseCart/Index
        [HttpGet]
        public ActionResult Index()
        {
            if (!IsEnterpriseCustomer())
            {
                TempData["Error"] = "Access denied. Enterprise customers only.";
                return RedirectToAction("Index", "Customer");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            // Load product details for each cart item
            foreach (var item in cart.Items)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            // Get available stock for each product
            var availableStock = new Dictionary<int, int>();
            foreach (var item in cart.Items)
            {
                var stock = InMemoryDatabase.StockBatches
                    .Where(b => b.ProductId == item.ProductId && b.Quantity > 0)
                    .Sum(b => b.Quantity);
                availableStock[item.ProductId] = stock;
            }
            ViewBag.AvailableStock = availableStock;

            return View(cart);
        }

        // POST: EnterpriseCart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int quantity = 1)
        {
            if (!IsEnterpriseCustomer())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            if (quantity <= 0) quantity = 1;

            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            // Check available stock
            var available = InMemoryDatabase.StockBatches
                .Where(b => b.ProductId == productId && b.Quantity > 0)
                .Sum(b => b.Quantity);

            if (available <= 0)
            {
                return Json(new { success = false, message = "Product out of stock." });
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + quantity;
                if (newQty > available) newQty = available;
                existingItem.Quantity = newQty;
            }
            else
            {
                var nextItemId = InMemoryDatabase.EnterpriseCartItems.Any()
                    ? InMemoryDatabase.EnterpriseCartItems.Max(i => i.CartItemId) + 1
                    : 1;

                cart.Items.Add(new EnterpriseCartItem
                {
                    CartItemId = nextItemId,
                    CartId = cart.CartId,
                    ProductId = productId,
                    ProductName = product.Name,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    WeightPerUnitKg = 1.0m
                });
            }

            cart.UpdatedDate = DateTime.UtcNow;

            return Json(new { success = true, message = "Added to cart.", cartCount = cart.Items.Sum(i => i.Quantity) });
        }

        // POST: EnterpriseCart/UpdateCartItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCartItem(int productId, int quantity)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                return RedirectToAction("Index");
            }

            if (quantity <= 0)
            {
                // Remove item
                InMemoryDatabase.EnterpriseCartItems.Remove(item);
                cart.Items.Remove(item);
            }
            else
            {
                // Check available stock
                var available = InMemoryDatabase.StockBatches
                    .Where(b => b.ProductId == productId && b.Quantity > 0)
                    .Sum(b => b.Quantity);

                if (quantity > available) quantity = available;
                item.Quantity = quantity;
            }

            cart.UpdatedDate = DateTime.UtcNow;
            TempData["Success"] = "Cart updated.";
            return RedirectToAction("Index");
        }

        // POST: EnterpriseCart/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int productId)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                InMemoryDatabase.EnterpriseCartItems.Remove(item);
                cart.Items.Remove(item);
                cart.UpdatedDate = DateTime.UtcNow;
            }

            TempData["Success"] = "Item removed from cart.";
            return RedirectToAction("Index");
        }

        // ========================================
        // CHECKOUT
        // ========================================

        // GET: EnterpriseCart/Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            if (!cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Load product details
            foreach (var item in cart.Items)
            {
                item.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == item.ProductId);
            }

            ViewBag.Total = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            ViewBag.Customer = customer;

            return View(cart);
        }

        // POST: EnterpriseCart/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(string deliveryAddress, string phoneNumber, string specialInstructions)
        {
            if (!IsEnterpriseCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentEnterpriseCustomer();
            var cart = GetOrCreateCart(customer.Id);

            if (!cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["Error"] = "Delivery address is required.";
                return RedirectToAction("Checkout");
            }

            // Verify stock availability
            foreach (var item in cart.Items)
            {
                var available = InMemoryDatabase.StockBatches
                    .Where(b => b.ProductId == item.ProductId && b.Quantity > 0)
                    .Sum(b => b.Quantity);

                if (item.Quantity > available)
                {
                    TempData["Error"] = $"Insufficient stock for {item.ProductName}. Available: {available}";
                    return RedirectToAction("Index");
                }
            }

            // Create order
            var nextOrderId = InMemoryDatabase.Orders.Any() ? InMemoryDatabase.Orders.Max(o => o.Id) + 1 : 1;
            var orderNumber = GenerateOrderNumber();

            var orderItems = new List<OrderItem>();
            var nextItemId = 1;
            if (InMemoryDatabase.Orders.Any())
            {
                var allItems = InMemoryDatabase.Orders.SelectMany(o => o.Items ?? new List<OrderItem>());
                if (allItems.Any())
                {
                    nextItemId = allItems.Max(it => it.Id) + 1;
                }
            }

            decimal total = 0;

            foreach (var cartItem in cart.Items)
            {
                var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product == null) continue;

                orderItems.Add(new OrderItem
                {
                    Id = nextItemId++,
                    ProductId = product.Id,
                    Product = product,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price
                });

                total += product.Price * cartItem.Quantity;
            }

            var order = new Order
            {
                Id = nextOrderId,
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                Customer = customer,
                CreatedAt = DateTime.UtcNow,
                IsInstore = false,
                Items = orderItems,
                Total = total,
                IsInvoiced = false,
                Status = OrderStatusEnum.Pending,
                IsPaid = false,
                IsPaymentConfirmed = false,
                PaymentMethodString = "Enterprise"
            };

            InMemoryDatabase.Orders.Add(order);

            // Deduct stock using FIFO
            foreach (var cartItem in cart.Items)
            {
                var product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product != null)
                {
                    DeductStockFIFO(product.Id, cartItem.Quantity, order.OrderNumber);
                    product.Quantity -= cartItem.Quantity;
                }
            }

            // Add to customer's current month debt
            customer.CurrentMonthDebt += total;

            // Clear cart
            foreach (var item in cart.Items.ToList())
            {
                InMemoryDatabase.EnterpriseCartItems.Remove(item);
            }
            cart.Items.Clear();
            cart.UpdatedDate = DateTime.UtcNow;

            TempData["Success"] = $"Order #{orderNumber} placed successfully! Amount R {total:N2} added to your monthly account.";
            return RedirectToAction("OrderConfirmation", "Customer", new { id = order.Id });
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        private string GenerateOrderNumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int counter = OrderNumberCounter.GetNextCounter();
            return $"ENT-{datePart}-{counter:D3}";
        }

        private bool DeductStockFIFO(int productId, int quantityRequested, string orderNumber)
        {
            var batches = InMemoryDatabase.StockBatches
                .Where(b => b.ProductId == productId && b.Quantity > 0)
                .OrderBy(b => b.ReceivedDate)
                .ToList();

            if (!batches.Any()) return false;

            int remainingToDeduct = quantityRequested;

            foreach (var batch in batches)
            {
                if (remainingToDeduct <= 0) break;

                int deductFromThisBatch = Math.Min(batch.Quantity, remainingToDeduct);
                batch.Quantity -= deductFromThisBatch;
                remainingToDeduct -= deductFromThisBatch;

                // Record stock movement
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
                    PerformedBy = Session["Username"] as string ?? "Enterprise",
                    Notes = $"Enterprise order {orderNumber} - FIFO deduction",
                    BatchId = batch.Id
                };
                InMemoryDatabase.StockMovements.Add(movement);
            }

            return remainingToDeduct <= 0;
        }
    }
}