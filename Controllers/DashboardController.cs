using System.Linq;
using System;
using System.Web.Mvc;
using ChesterMeatMatrix.Data;

namespace ChesterMeatMatrix.Controllers
{
    public class DashboardController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Allow access if user is authenticated via FormsAuth or session exists
            var isAuthenticated = (Session != null && Session["Username"] != null) ||
                                  (User != null && User.Identity != null && User.Identity.IsAuthenticated);

            if (!isAuthenticated)
            {
                // redirect to login with return url
                filterContext.Result = RedirectToAction("Login", "Account", new { returnUrl = filterContext.HttpContext.Request.RawUrl });
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            var role = Session["Role"] as string;
            ViewBag.Role = role;
            ViewBag.FullName = Session["FullName"] as string;
            return View();
        }

        [HttpGet]
        public ActionResult PickOrder()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PickOrder(string customerOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(customerOrderNumber))
            {
                ModelState.AddModelError("", "Order number required.");
                return View();
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.OrderNumber == customerOrderNumber);

            if (order == null)
            {
                ModelState.AddModelError("", "Order not found.");
                return View();
            }

            if (order.IsInvoiced)
            {
                ModelState.AddModelError("", "This order has already been processed and cannot be picked again.");
                return View();
            }

            return RedirectToAction("ValidatePick", new { id = order.Id });
        }

        [HttpGet]
        public ActionResult ValidatePick(int id)
        {
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [HttpPost]
        public ActionResult ValidateScannedItem(int orderId, string scannedCode)
        {
            if (string.IsNullOrEmpty(scannedCode))
            {
                return Json(new { ok = false, message = "No code provided." });
            }

            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { ok = false, message = "Order not found." });
            }

            // find product or pallet
            var product = InMemoryDatabase.Products.FirstOrDefault(p => p.SerialNumber == scannedCode || p.PalletCode == scannedCode);

            if (product == null)
            {
                return Json(new { ok = false, message = "Product not found in stock." });
            }

            var pickedKey = "picked_" + orderId;
            var picked = Session[pickedKey] as System.Collections.Generic.List<string>;

            if (picked == null)
            {
                picked = new System.Collections.Generic.List<string>();
                Session[pickedKey] = picked;
            }

            var added = new System.Collections.Generic.List<string>();
            var skippedOverpick = new System.Collections.Generic.List<string>();

            if (product.IsPallet)
            {
                // add all items under this pallet that match any ordered product codes
                var members = InMemoryDatabase.Products.Where(p => !p.IsPallet && p.PalletCode == product.PalletCode).ToList();

                foreach (var m in members)
                {
                    var orderItem = order.Items.FirstOrDefault(it => it.Product != null && it.Product.ProductCode == m.ProductCode);

                    if (orderItem != null)
                    {
                        // count already picked for this product
                        var alreadyPicked = picked.Count(sn => InMemoryDatabase.Products.Any(pp => pp.SerialNumber == sn && pp.ProductCode == m.ProductCode));

                        if (alreadyPicked >= orderItem.Quantity)
                        {
                            skippedOverpick.Add(m.SerialNumber);
                            continue;
                        }

                        if (!picked.Contains(m.SerialNumber))
                        {
                            picked.Add(m.SerialNumber);
                            added.Add(m.SerialNumber);
                        }
                    }
                }
            }
            else
            {
                // single item
                var orderItem = order.Items.FirstOrDefault(it => it.Product != null && it.Product.ProductCode == product.ProductCode);

                if (orderItem == null)
                {
                    return Json(new { ok = false, message = "Scanned product is not part of this order." });
                }

                var alreadyPicked = picked.Count(sn => InMemoryDatabase.Products.Any(pp => pp.SerialNumber == sn && pp.ProductCode == product.ProductCode));

                if (alreadyPicked >= orderItem.Quantity)
                {
                    return Json(new { ok = false, message = "Already picked required quantity for this product (over-pick prevented)." });
                }

                if (!picked.Contains(product.SerialNumber))
                {
                    picked.Add(product.SerialNumber);
                    added.Add(product.SerialNumber);
                }
            }

            return Json(new { ok = true, added = added, skipped = skippedOverpick, totalPicked = picked.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetReadyToInvoice(int orderId)
        {
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            var pickedKey = "picked_" + orderId;
            var picked = Session[pickedKey] as System.Collections.Generic.List<string>;

            if (picked == null || !picked.Any())
            {
                TempData["Error"] = "No items have been picked for this order.";
                return RedirectToAction("ValidatePick", new { id = orderId });
            }

            // Remove picked products from stock
            foreach (var sn in picked.ToList())
            {
                var p = InMemoryDatabase.Products.FirstOrDefault(x => x.SerialNumber == sn && !x.IsPallet);

                if (p != null)
                {
                    // adjust pallet record if present
                    if (!string.IsNullOrEmpty(p.PalletCode))
                    {
                        var pallet = InMemoryDatabase.Products.FirstOrDefault(x => x.IsPallet && x.PalletCode == p.PalletCode);

                        if (pallet != null)
                        {
                            pallet.Quantity = Math.Max(0, pallet.Quantity - 1);

                            // optionally remove pallet if empty
                            if (pallet.Quantity == 0)
                            {
                                InMemoryDatabase.Products.Remove(pallet);
                            }
                        }
                    }

                    InMemoryDatabase.Products.Remove(p);
                }
            }

            // mark order ready for invoice
            order.IsInvoiced = true;
            order.InvoiceNumber = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(6);
            order.InvoiceDate = DateTime.UtcNow;

            // clear session picked
            Session.Remove(pickedKey);

            TempData["Message"] = "Order set ready for invoice. Picked items removed from stock.";

            // After setting ready, redirect to PickOrder list and disallow reuse of this order number in future
            return RedirectToAction("PickOrder");
        }

        [HttpGet]
        public ActionResult Warehouse(string q)
        {
            // Build pallet summaries: group individual items by pallet code (or product when pallet missing)
            var items = InMemoryDatabase.Products.Where(p => !p.IsPallet).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var query = q.Trim();
                items = items.Where(p => (p.ProductCode ?? "").IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0
                                      || (p.Name ?? "").IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0
                                      || (p.PalletCode ?? "").IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0
                                      || (p.Location ?? "").IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }

            // Group by product code and pallet code so each pallet for a product is a separate row
            var groups = items.GroupBy(p => new { ProductCode = p.ProductCode ?? string.Empty, PalletCode = p.PalletCode ?? string.Empty }).ToList();
            var summaries = new System.Collections.Generic.List<ChesterMeatMatrix.Models.PalletSummary>();

            foreach (var g in groups)
            {
                var key = g.Key;

                // try to find a pallet record matching this pallet code
                var pallet = !string.IsNullOrEmpty(key.PalletCode)
                    ? InMemoryDatabase.Products.FirstOrDefault(x => x.IsPallet && x.PalletCode == key.PalletCode)
                    : null;

                var first = g.FirstOrDefault();

                var summary = new ChesterMeatMatrix.Models.PalletSummary
                {
                    ProductCode = key.ProductCode,
                    PalletCode = key.PalletCode,
                    Name = pallet?.Name ?? first?.Name ?? string.Empty,
                    Location = pallet?.Location ?? first?.Location ?? string.Empty,
                    // quantity is the count of serial-numbered items in this group
                    Quantity = g.Count()
                };

                summaries.Add(summary);
            }

            ViewBag.Query = q;
            return View("WarehouseSummary", summaries.OrderBy(s => s.ProductCode).ThenBy(s => s.PalletCode).ToList());
        }

        [HttpGet]
        public ActionResult Receiver()
        {
            // Show recent pallets for receiver dashboard
            var pallets = InMemoryDatabase.Products.Where(p => p.IsPallet).OrderByDescending(p => p.CreatedAt).Take(20).ToList();
            return View(pallets);
        }
    }
}
