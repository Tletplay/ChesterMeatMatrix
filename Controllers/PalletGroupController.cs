using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    [Authorize]
    public class PalletGroupController : Controller
    {
        private bool IsWarehouseOrAdmin()
        {
            var role = Session["Role"] as string;
            return role == "Warehouse" || role == "Admin" || role == "WarehouseManager";
        }

        // GET: PalletGroup/Index
        [HttpGet]
        public ActionResult Index()
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var groups = InMemoryDatabase.PalletGroups
                .OrderByDescending(g => g.CreatedDate)
                .ToList();

            // Load pallets for each group
            foreach (var group in groups)
            {
                group.Pallets = InMemoryDatabase.Pallets.Where(p => p.PalletGroupId == group.PalletGroupId).ToList();

                // Calculate total quantity from pallets if not set
                if (group.TotalQuantity == 0 && group.Pallets != null)
                {
                    group.TotalQuantity = group.Pallets.Sum(p => p.Quantity);
                }
            }

            return View(groups);
        }

        // GET: PalletGroup/Create
        [HttpGet]
        public ActionResult Create()
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.AvailablePallets = InMemoryDatabase.Pallets
                .Where(p => p.PalletGroupId == null)
                .OrderBy(p => p.PalletNumber)
                .ToList();

            return View();
        }

        // POST: PalletGroup/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string groupName, string productType, string location, int[] selectedPalletIds)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var userName = Session["FullName"] as string ?? Session["Username"] as string;

            // Generate group number
            var groupNumber = $"PG-{DateTime.Now:yyyyMMdd}-{(InMemoryDatabase.PalletGroups.Count(g => g.CreatedDate.Date == DateTime.Today) + 1):D3}";

            var nextId = InMemoryDatabase.PalletGroups.Any() ? InMemoryDatabase.PalletGroups.Max(g => g.PalletGroupId) + 1 : 1;

            var group = new PalletGroup
            {
                PalletGroupId = nextId,
                GroupNumber = groupNumber,
                GroupName = groupName,
                ProductType = productType,
                Location = location,
                CreatedBy = userName,
                CreatedDate = DateTime.Now,
                Status = "Active",
                TotalQuantity = 0
            };

            InMemoryDatabase.PalletGroups.Add(group);

            // Assign selected pallets to this group
            if (selectedPalletIds != null && selectedPalletIds.Any())
            {
                int totalQuantity = 0;
                foreach (var palletId in selectedPalletIds)
                {
                    var pallet = InMemoryDatabase.Pallets.FirstOrDefault(p => p.PalletId == palletId);
                    if (pallet != null)
                    {
                        pallet.PalletGroupId = group.PalletGroupId;
                        totalQuantity += pallet.Quantity;
                    }
                }
                group.TotalQuantity = totalQuantity;
            }

            TempData["Success"] = $"Pallet group #{groupNumber} created with {selectedPalletIds?.Length ?? 0} pallets.";
            return RedirectToAction("Index");
        }

        // GET: PalletGroup/Details
        [HttpGet]
        public ActionResult Details(int id)
        {
            if (!IsWarehouseOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var group = InMemoryDatabase.PalletGroups.FirstOrDefault(g => g.PalletGroupId == id);
            if (group == null)
            {
                return HttpNotFound();
            }

            group.Pallets = InMemoryDatabase.Pallets
                .Where(p => p.PalletGroupId == id)
                .ToList();

            // Calculate total quantity from pallets
            group.TotalQuantity = group.Pallets.Sum(p => p.Quantity);

            return View(group);
        }

        // POST: PalletGroup/RemovePallet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemovePallet(int groupId, int palletId)
        {
            if (!IsWarehouseOrAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var group = InMemoryDatabase.PalletGroups.FirstOrDefault(g => g.PalletGroupId == groupId);
            if (group == null)
            {
                return Json(new { success = false, message = "Group not found" });
            }

            var pallet = InMemoryDatabase.Pallets.FirstOrDefault(p => p.PalletId == palletId);
            if (pallet == null)
            {
                return Json(new { success = false, message = "Pallet not found" });
            }

            pallet.PalletGroupId = null;
            group.TotalQuantity -= pallet.Quantity;

            return Json(new { success = true, message = "Pallet removed from group" });
        }
    }
}