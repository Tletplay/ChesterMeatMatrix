using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private bool IsCustomer()
        {
            var role = Session["Role"] as string;
            return role == "Customer";
        }

        private bool IsAdminOrSales()
        {
            var role = Session["Role"] as string;
            return role == "Admin" || role == "SalesRep";
        }

        private Customer GetCurrentCustomer()
        {
            var fullName = Session["FullName"] as string;
            return InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == fullName);
        }

        // ========================================
        // CUSTOMER: File a Claim
        // ========================================

        [HttpGet]
        public ActionResult FileClaim(int orderId)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId && o.CustomerId == customer.Id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Customer");
            }

            // Only allow claims on delivered orders
            if (order.Status != OrderStatusEnum.Delivered)
            {
                TempData["Error"] = "Claims can only be filed after order is delivered.";
                return RedirectToAction("OrderConfirmation", "Customer", new { id = orderId });
            }

            ViewBag.Order = order;
            ViewBag.Products = order.Items?.Select(i => i.Product).Where(p => p != null).ToList() ?? new System.Collections.Generic.List<Product>();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FileClaim(int orderId, int? productId, string claimType, string description, string photoBase64)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();
            var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId && o.CustomerId == customer.Id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Customer");
            }

            if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "Please provide claim type and description.";
                return RedirectToAction("FileClaim", new { orderId = orderId });
            }

            // Generate claim number
            var claimNumber = $"CLM-{DateTime.Now:yyyyMMdd}-{InMemoryDatabase.Claims.Count(c => c.ClaimDate.Date == DateTime.Today) + 1:D3}";

            var nextId = InMemoryDatabase.Claims.Any() ? InMemoryDatabase.Claims.Max(c => c.ClaimId) + 1 : 1;

            var claim = new Claim
            {
                ClaimId = nextId,
                ClaimNumber = claimNumber,
                OrderId = orderId,
                CustomerId = customer.Id,
                ProductId = productId,
                ClaimType = claimType,
                Description = description,
                PhotoUrl = photoBase64,
                Status = "Pending",
                ClaimDate = DateTime.Now,
                CreditAmount = 0
            };

            InMemoryDatabase.Claims.Add(claim);

            TempData["Success"] = $"Claim #{claimNumber} filed successfully. Our team will review it within 48 hours.";
            return RedirectToAction("Index", "Customer");
        }

        // ========================================
        // CUSTOMER: View My Claims
        // ========================================

        [HttpGet]
        public ActionResult MyClaims()
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();
            var claims = InMemoryDatabase.Claims
                .Where(c => c.CustomerId == customer.Id)
                .OrderByDescending(c => c.ClaimDate)
                .ToList();

            // Load related data
            foreach (var claim in claims)
            {
                claim.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == claim.OrderId);
                claim.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == claim.ProductId);
            }

            return View(claims);
        }

        // ========================================
        // CUSTOMER: View Claim Details
        // ========================================

        [HttpGet]
        public ActionResult ClaimDetails(int id)
        {
            if (!IsCustomer())
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = GetCurrentCustomer();
            var claim = InMemoryDatabase.Claims.FirstOrDefault(c => c.ClaimId == id && c.CustomerId == customer.Id);

            if (claim == null)
            {
                return HttpNotFound();
            }

            claim.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == claim.OrderId);
            claim.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == claim.ProductId);

            return View(claim);
        }

        // ========================================
        // ADMIN/SALES: Manage Claims
        // ========================================

        [HttpGet]
        public ActionResult ManageClaims(string status = "Pending")
        {
            if (!IsAdminOrSales())
            {
                return RedirectToAction("Login", "Account");
            }

            var claims = InMemoryDatabase.Claims
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.ClaimDate)
                .ToList();

            // Load related data
            foreach (var claim in claims)
            {
                claim.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == claim.OrderId);
                claim.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == claim.CustomerId);
                claim.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == claim.ProductId);
            }

            // Add counts to ViewBag
            ViewBag.Status = status;
            ViewBag.StatusOptions = new[] { "Pending", "Approved", "Rejected", "Credited" };
            ViewBag.PendingCount = InMemoryDatabase.Claims.Count(c => c.Status == "Pending");
            ViewBag.ApprovedCount = InMemoryDatabase.Claims.Count(c => c.Status == "Approved");
            ViewBag.RejectedCount = InMemoryDatabase.Claims.Count(c => c.Status == "Rejected");
            ViewBag.CreditedCount = InMemoryDatabase.Claims.Count(c => c.Status == "Credited");

            return View(claims);
        }

        [HttpGet]
        public ActionResult ReviewClaim(int id)
        {
            if (!IsAdminOrSales())
            {
                return RedirectToAction("Login", "Account");
            }

            var claim = InMemoryDatabase.Claims.FirstOrDefault(c => c.ClaimId == id);

            if (claim == null)
            {
                return HttpNotFound();
            }

            claim.Order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == claim.OrderId);
            claim.Customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == claim.CustomerId);
            claim.Product = InMemoryDatabase.Products.FirstOrDefault(p => p.Id == claim.ProductId);

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessClaim(int claimId, string decision, decimal creditAmount, string resolutionNotes)
        {
            if (!IsAdminOrSales())
            {
                return RedirectToAction("Login", "Account");
            }

            var claim = InMemoryDatabase.Claims.FirstOrDefault(c => c.ClaimId == claimId);

            if (claim == null)
            {
                return HttpNotFound();
            }

            claim.Status = decision;
            claim.ResolutionNotes = resolutionNotes;
            claim.ReviewedDate = DateTime.Now;
            claim.ReviewedBy = Session["FullName"] as string ?? Session["Username"] as string;

            if (decision == "Approved" || decision == "Credited")
            {
                claim.CreditAmount = creditAmount;

                // If credit approved, add to customer's account
                if (creditAmount > 0)
                {
                    var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Id == claim.CustomerId);
                    if (customer != null)
                    {
                        // Create a credit note
                        var nextCreditId = InMemoryDatabase.CreditNotes.Any() ? InMemoryDatabase.CreditNotes.Max(cn => cn.CreditNoteId) + 1 : 1;

                        var creditNote = new CreditNote
                        {
                            CreditNoteId = nextCreditId,
                            CreditNoteNumber = $"CN-{claim.ClaimNumber}",
                            ReturnId = 0, // Not linked to a return
                            Amount = creditAmount,
                            RemainingBalance = creditAmount,
                            AmountUsed = 0,
                            Status = "Active",
                            IssueDate = DateTime.Now,
                            IssuedBy = claim.ReviewedBy,
                            Notes = $"From claim #{claim.ClaimNumber}: {resolutionNotes}",
                            CustomerId = customer.Id
                        };
                        InMemoryDatabase.CreditNotes.Add(creditNote);
                    }
                }
            }

            TempData["Success"] = $"Claim #{claim.ClaimNumber} {decision.ToLower()}.";
            return RedirectToAction("ManageClaims");
        }
    }
}