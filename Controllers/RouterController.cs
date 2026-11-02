using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Helpers;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;



namespace ChesterMeatMatrix.Controllers
{
    public class RouterController : Controller
    {
        private bool IsRouterOrAdmin()
        {
            var role = Session != null ? Session["Role"] as string : null;
            return string.Equals(role, "Router", System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase);
        }

        public ActionResult Index()
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var routes = InMemoryDatabase.Routes.OrderBy(r => r.Date).ToList();
            return View(routes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRouteFromOrders(int[] selectedOrderIds, string destination)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedOrderIds == null || selectedOrderIds.Length == 0)
            {
                TempData["Message"] = "No orders selected.";
                return RedirectToAction("Index");
            }

            var orders = InMemoryDatabase.Orders.Where(o => selectedOrderIds.Contains(o.Id)).ToList();

            if (!orders.Any())
            {
                TempData["Message"] = "Selected orders not found.";
                return RedirectToAction("Index");
            }

            // route date will be entered later on Routed Deliveries (Edit). Leave as unspecified marker (MinValue).
            System.DateTime dt = System.DateTime.MinValue;

            // determine destination: if a single enterprise order or all enterprise orders with same company, set to that company
            string dest = destination ?? string.Empty;
            var enterpriseOrders = orders.Where(o => o.Customer != null && o.Customer.IsEnterprise).ToList();

            if (enterpriseOrders.Count > 0)
            {
                var distinctNames = enterpriseOrders.Select(o => o.Customer.Name).Distinct().ToList();

                if (distinctNames.Count == 1)
                {
                    dest = distinctNames.First();
                }
            }

            // create route
            int newId = InMemoryDatabase.Routes.Any() ? InMemoryDatabase.Routes.Max(r => r.Id) + 1 : 1;

            var route = new Route
            {
                Id = newId,
                Name = "Route " + newId,
                Destination = dest,
                Date = dt,
                Status = "Pending"
            };

            // attach orders and compute items count + total mass (best-effort)
            // attach orders and compute items count + total mass
            int items = 0;
            decimal totalMass = 0m;

            foreach (var o in orders)
            {
                route.OrderIds.Add(o.Id);

                if (o.Items != null)
                {
                    items += o.Items.Sum(i => i.Quantity);
                }

                // USE THE WEIGHT RECORDED BY CHECKER
                totalMass += o.TotalWeightKg;
            }

            route.ItemsCount = items;
            route.TotalMassKg = totalMass;

            route.ItemsCount = items;
            route.TotalMassKg = totalMass;

            InMemoryDatabase.Routes.Add(route);

            TempData["Message"] = "Route created with selected orders.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkAssign(int[] selectedRouteIds, int? driverId, int? vehicleId)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedRouteIds == null || selectedRouteIds.Length == 0)
            {
                TempData["Message"] = "No routes selected.";
                return RedirectToAction("Index");
            }

            foreach (var id in selectedRouteIds)
            {
                var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == id);

                if (route == null) continue;

                if (driverId.HasValue)
                {
                    route.AssignedDriverId = driverId.Value;
                }

                if (vehicleId.HasValue)
                {
                    route.AssignedVehicleId = vehicleId.Value;
                }

                // If a date was provided on the index form for this route, parse and set it
                var dateKey = "routeDate_" + id;
                var dateVal = Request.Form[dateKey];

                if (!string.IsNullOrWhiteSpace(dateVal))
                {
                    System.DateTime parsed;

                    if (System.DateTime.TryParse(dateVal, out parsed))
                    {
                        route.Date = parsed;
                    }
                }

                route.Status = "Assigned";

                // ========================================
                // UPDATE ORDER STATUS FOR ALL ORDERS IN THIS ROUTE
                // ========================================
                if (route.OrderIds != null && route.OrderIds.Any())
                {
                    foreach (var orderId in route.OrderIds)
                    {
                        var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                        if (order != null && order.Status != OrderStatusEnum.OutForDelivery)
                        {
                            order.Status = OrderStatusEnum.OutForDelivery;
                        }
                    }
                }
            }

            TempData["Message"] = "Routes assigned and order status updated.";
            return RedirectToAction("RoutedDeliveries");
        }

        public ActionResult RoutedDeliveries()
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var routes = InMemoryDatabase.Routes
                .Where(r => string.Equals(r.Status, "Assigned", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.Status, "InProgress", System.StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(r.Status, "Done", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Date)
                .ToList();

            return View(routes);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == id);

            if (route == null)
            {
                return HttpNotFound();
            }

            ViewBag.Drivers = InMemoryDatabase.Users.Where(u => u.Role == "Driver").ToList();
            ViewBag.Vehicles = InMemoryDatabase.Vehicles.ToList();
            ViewBag.Statuses = new[] { "Pending", "Assigned", "InProgress", "Done" };

            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, string destination, string date, int? driverId, int? vehicleId, string status)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == id);

            if (route == null)
            {
                return HttpNotFound();
            }

            if (!string.IsNullOrWhiteSpace(destination))
            {
                route.Destination = destination;
            }

            if (!string.IsNullOrWhiteSpace(date))
            {
                System.DateTime parsed;

                if (System.DateTime.TryParse(date, out parsed))
                {
                    route.Date = parsed;
                }
            }

            if (driverId.HasValue)
            {
                route.AssignedDriverId = driverId.Value;
            }

            if (vehicleId.HasValue)
            {
                route.AssignedVehicleId = vehicleId.Value;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                route.Status = status;
            }

            // If status is being set to "Assigned" or "InProgress", update order status
            if (status == "Assigned" || status == "InProgress")
            {
                if (route.OrderIds != null && route.OrderIds.Any())
                {
                    foreach (var orderId in route.OrderIds)
                    {
                        var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                        if (order != null && order.Status != OrderStatusEnum.OutForDelivery)
                        {
                            order.Status = OrderStatusEnum.OutForDelivery;
                        }
                    }
                }
            }

            TempData["Message"] = "Route updated.";
            return RedirectToAction("RoutedDeliveries");
        }


        // ========================================
        // ENHANCED ROUTER METHODS (Add at bottom of RouterController)
        // ========================================

        [HttpGet]
        public ActionResult Dashboard()
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var pendingOrders = InMemoryDatabase.Orders
                .Where(o => o.Status == OrderStatusEnum.Confirmed || o.Status == OrderStatusEnum.Processing)
                .ToList();

            var activeRoutes = InMemoryDatabase.Routes
                .Where(r => r.Status == "Assigned" || r.Status == "InProgress")
                .ToList();

            // Calculate Durban vs Outside counts
            int durbanCount = 0;
            int outsideCount = 0;

            foreach (var order in pendingOrders)
            {
                string address = order.Customer?.Address ?? "";
                bool isOutside = address.IndexOf("Johannesburg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 address.IndexOf("Pretoria", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 address.IndexOf("Cape Town", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 address.IndexOf("Bloemfontein", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 address.IndexOf("Pietermaritzburg", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isOutside)
                    outsideCount++;
                else
                    durbanCount++;
            }

            var model = new RouterDashboardViewModel
            {
                PendingCount = pendingOrders.Count,
                ActiveCount = activeRoutes.Count,
                DurbanPendingCount = durbanCount,
                OutsidePendingCount = outsideCount,
                PendingWeightKg = pendingOrders.Sum(o => o.Total),
                AvailableDrivers = InMemoryDatabase.Users.Count(u => u.Role == "Driver"),
                AvailableTrucks = InMemoryDatabase.Vehicles.Count
            };

            return View(model);
        }

        // GET: Router/PendingOrders
        [HttpGet]
        public ActionResult PendingOrders(string search, DateTime? fromDate, DateTime? toDate)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = InMemoryDatabase.Orders
                .Where(o => o.Status == OrderStatusEnum.Confirmed || o.Status == OrderStatusEnum.Processing)
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

            var orderList = orders.OrderByDescending(o => o.CreatedAt).ToList();

            // Build area lookup dictionary
            var areaLookup = new Dictionary<int, string>();
            foreach (var order in orderList)
            {
                areaLookup[order.Id] = RoutingZoneHelper.GetAreaLabel(order.Customer?.Address ?? "");
            }

            ViewBag.AreaLookup = areaLookup;
            ViewBag.DurbanCount = orderList.Count(o => RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));
            ViewBag.OutsideCount = orderList.Count(o => !RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));
            ViewBag.PendingWeightKg = orderList.Sum(o => o.TotalWeightKg);
            ViewBag.PendingPackages = orderList.Sum(o => o.Items?.Count ?? 0);

            return View(orderList);
        }

        // GET: Router/AssignRoute
        [HttpGet]
        public async Task<ActionResult> AssignRoute(int[] selectedIds)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedIds == null || selectedIds.Length < 1)
            {
                TempData["Error"] = "Select at least 2 orders to create a route.";
                return RedirectToAction("PendingOrders");
            }

            var orders = InMemoryDatabase.Orders
                .Where(o => selectedIds.Contains(o.Id))
                .ToList();

            // Check for mixed Durban/Outside grouping
            var hasDurban = orders.Any(o => RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));
            var hasOutside = orders.Any(o => !RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));

            if (hasDurban && hasOutside)
            {
                TempData["Error"] = "You cannot route Durban-area orders together with orders outside the Durban delivery area. Please create separate routes for each area.";
                return RedirectToAction("PendingOrders");
            }

            // Build stops list
            var stops = orders.Select(o => new RouteAssignmentStopViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer?.Name ?? "N/A",
                DeliveryAddress = o.Customer?.Address ?? "N/A",
                AreaLabel = RoutingZoneHelper.GetAreaLabel(o.Customer?.Address ?? ""),
                IsDurbanArea = RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? "")
            }).ToList();

            // Try to geocode addresses
            await PopulateCoordinatesAsync(stops);

            // Calculate estimated distance
            var geocodedStops = stops.Where(s => s.Latitude.HasValue && s.Longitude.HasValue).ToList();
            var estimatedDistance = CalculateEstimatedDistanceKm(geocodedStops);

            var model = new RouteAssignmentViewModel
            {
                OrderIds = string.Join(",", stops.Select(s => s.OrderId)),
                SuggestedAreaLabel = stops.FirstOrDefault()?.AreaLabel ?? "Mixed Area",
                RestrictionMessage = "Rule: Durban-area stops cannot be routed together with stops outside the Durban delivery area.",
                Stops = stops,
                SelectedStopCount = stops.Count,
                GeocodedStopCount = geocodedStops.Count,
                TotalWeightKg = orders.Sum(o => o.TotalWeightKg),
                TotalPackages = orders.Sum(o => o.Items?.Count ?? 0),
                EstimatedDistanceKm = estimatedDistance,
                DistinctAreaCount = stops.Select(s => s.AreaLabel).Distinct().Count()
            };

            ViewBag.Drivers = new SelectList(InMemoryDatabase.Users.Where(u => u.Role == "Driver").ToList(), "Id", "FullName");
            ViewBag.Trucks = new SelectList(InMemoryDatabase.Vehicles.ToList(), "Id", "Registration");

            return View(model);
        }

        // POST: Router/CreateRoute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoute(string orderIds, int driverId, int truckId, DateTime scheduledDateTime)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(orderIds))
            {
                TempData["Error"] = "Please select at least 2 stops for the route.";
                return RedirectToAction("PendingOrders");
            }

            var ids = orderIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .Distinct()
                .ToList();

            if (ids.Count < 1)
            {
                TempData["Error"] = "At least 1 order required for a route.";
                return RedirectToAction("PendingOrders");
            }

            var orders = InMemoryDatabase.Orders.Where(o => ids.Contains(o.Id)).ToList();

            // Verify no mixed grouping
            var hasDurban = orders.Any(o => RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));
            var hasOutside = orders.Any(o => !RoutingZoneHelper.IsDurbanArea(o.Customer?.Address ?? ""));

            if (hasDurban && hasOutside)
            {
                TempData["Error"] = "Durban-area orders must be routed separately from orders outside the Durban delivery area.";
                return RedirectToAction("PendingOrders");
            }

            // Create new route
            var nextId = InMemoryDatabase.Routes.Any() ? InMemoryDatabase.Routes.Max(r => r.Id) + 1 : 1;
            var driver = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == driverId);
            var vehicle = InMemoryDatabase.Vehicles.FirstOrDefault(v => v.Id == truckId);

            var route = new Route
            {
                Id = nextId,
                Name = $"Route {nextId} - {scheduledDateTime:yyyy-MM-dd}",
                Destination = orders.FirstOrDefault()?.Customer?.Address ?? "Multiple Stops",
                Date = scheduledDateTime,
                Status = "Assigned",
                AssignedDriverId = driverId,
                AssignedVehicleId = truckId,
                ItemsCount = orders.Sum(o => o.Items?.Count ?? 0),
                TotalMassKg = orders.Sum(o => o.TotalWeightKg)
            };

            // Add order IDs to route
            foreach (var orderId in ids)
            {
                route.OrderIds.Add(orderId);
            }

            InMemoryDatabase.Routes.Add(route);

            // Update order status
            foreach (var order in orders)
            {
                order.Status = OrderStatusEnum.OutForDelivery;
            }

            TempData["Success"] = $"Route created with {ids.Count} orders and assigned to {driver?.FullName}.";
            return RedirectToAction("ActiveDeliveries");
        }

        // GET: Router/ActiveDeliveries
        [HttpGet]
        public ActionResult ActiveDeliveries()
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var routes = InMemoryDatabase.Routes
                .Where(r => r.Status == "Assigned" || r.Status == "InProgress")
                .OrderByDescending(r => r.Date)
                .ToList();

            // Load driver and vehicle names
            foreach (var route in routes)
            {
                var driver = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == route.AssignedDriverId);
                var vehicle = InMemoryDatabase.Vehicles.FirstOrDefault(v => v.Id == route.AssignedVehicleId);
                route.OrderCount = route.OrderIds?.Count ?? 0;
                route.TotalAmount = route.TotalMassKg;
            }

            return View(routes);
        }

        // POST: Router/SendToDriver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendToDriver(int routeId)
        {
            if (!IsRouterOrAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null)
            {
                return Json(new { success = false, message = "Route not found" });
            }

            // Update route status
            route.Status = "SentToDriver";

            // Update all orders in this route
            if (route.OrderIds != null)
            {
                foreach (var orderId in route.OrderIds)
                {
                    var order = InMemoryDatabase.Orders.FirstOrDefault(o => o.Id == orderId);
                    if (order != null)
                    {
                        order.Status = OrderStatusEnum.OutForDelivery;
                    }
                }
            }

            // SEND NOTIFICATION TO DRIVER
            if (route.AssignedDriverId.HasValue)
            {
                var driver = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == route.AssignedDriverId.Value);
                var orderCount = route.OrderIds?.Count ?? 0;

                SendNotification(
                    route.AssignedDriverId.Value,
                    "New Route Assigned",
                    $"You have been assigned a new route with {orderCount} stops. Scheduled for {route.Date:dd MMM yyyy}. Click to view details.",
                    "Route",
                    route.Id
                );
            }

            return Json(new { success = true, message = "Route sent to driver successfully. Notification sent." });
        }

        // ========================================
        // PRIVATE HELPER METHODS
        // ========================================

        private async Task PopulateCoordinatesAsync(List<RouteAssignmentStopViewModel> stops)
        {
            if (!OpenRouteServiceHelper.IsConfigured || stops == null || !stops.Any())
            {
                return; // Add this - returns Task
            }

            foreach (var stop in stops)
            {
                try
                {
                    var result = await OpenRouteServiceHelper.GeocodeAddressAsync(stop.DeliveryAddress);
                    if (result != null)
                    {
                        stop.Latitude = result.Latitude;
                        stop.Longitude = result.Longitude;
                    }
                }
                catch
                {
                    stop.Latitude = null;
                    stop.Longitude = null;
                }
            }
        }

        private double CalculateEstimatedDistanceKm(List<RouteAssignmentStopViewModel> stops)
        {
            if (stops == null || stops.Count < 2)
            {
                return 0;
            }

            double total = 0;
            for (int i = 1; i < stops.Count; i++)
            {
                total += CalculateDistanceKm(
                    stops[i - 1].Latitude.GetValueOrDefault(),
                    stops[i - 1].Longitude.GetValueOrDefault(),
                    stops[i].Latitude.GetValueOrDefault(),
                    stops[i].Longitude.GetValueOrDefault());
            }

            return Math.Round(total, 1);
        }

        private double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        // ========================================
        // SEND NOTIFICATION TO DRIVER
        // ========================================
        private void SendNotification(int driverId, string title, string message, string type, int? referenceId = null)
        {
            var notification = new Notification
            {
                Id = InMemoryDatabase.Notifications.Any() ? InMemoryDatabase.Notifications.Max(n => n.Id) + 1 : 1,
                UserId = driverId,
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            InMemoryDatabase.Notifications.Add(notification);
        }

        // ========================================
        // GET NOTIFICATIONS FOR DRIVER (AJAX)
        // ========================================
        [HttpGet]
        public JsonResult GetNotifications(int driverId)
        {
            var notifications = InMemoryDatabase.Notifications
                .Where(n => n.UserId == driverId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.ReferenceId,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("dd MMM yyyy HH:mm")
                })
                .Take(20)
                .ToList();

            var unreadCount = InMemoryDatabase.Notifications.Count(n => n.UserId == driverId && !n.IsRead);

            return Json(new { notifications = notifications, unreadCount = unreadCount }, JsonRequestBehavior.AllowGet);
        }

        // ========================================
        // MARK NOTIFICATION AS READ
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult MarkNotificationRead(int notificationId)
        {
            var notification = InMemoryDatabase.Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
            }
            return Json(new { success = true });
        }

        // ========================================
        // WEIGH TRUCK PAGE (GET)
        // ========================================
        // ========================================
        // WEIGH TRUCK PAGE (GET)
        // ========================================
        [HttpGet]
        public ActionResult WeighTruck(int routeId)
        {
            if (!IsRouterOrAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null)
            {
                return HttpNotFound();
            }

            // Get all orders in this route
            var orders = InMemoryDatabase.Orders.Where(o => route.OrderIds.Contains(o.Id)).ToList();
            var totalOrderWeight = orders.Sum(o => o.TotalWeightKg);

            var vehicle = route.AssignedVehicleId.HasValue
                ? InMemoryDatabase.Vehicles.FirstOrDefault(v => v.Id == route.AssignedVehicleId.Value)
                : null;

            ViewBag.TotalOrderWeight = totalOrderWeight;
            ViewBag.Vehicle = vehicle;
            ViewBag.Route = route;

            // CRITICAL FIX: Pass the route as the model to the view
            return View(route);  // ? This was missing the model parameter
        }

        // ========================================
        // SUBMIT TRUCK WEIGHT (POST)
        // ========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitTruckWeight(int routeId, decimal actualLoadWeightKg)
        {
            if (!IsRouterOrAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var route = InMemoryDatabase.Routes.FirstOrDefault(r => r.Id == routeId);
            if (route == null)
            {
                return Json(new { success = false, message = "Route not found" });
            }

            // Get all orders in this route
            var orders = InMemoryDatabase.Orders.Where(o => route.OrderIds.Contains(o.Id)).ToList();
            var expectedWeight = orders.Sum(o => o.TotalWeightKg);

            // Calculate difference
            var difference = Math.Abs(actualLoadWeightKg - expectedWeight);
            var percentageDifference = expectedWeight > 0 ? (difference / expectedWeight) * 100 : 0;

            // Check if within tolerance (10%)
            bool isWithinTolerance = percentageDifference <= 10m;

            // Update vehicle load weight if assigned
            if (route.AssignedVehicleId.HasValue)
            {
                var vehicle = InMemoryDatabase.Vehicles.FirstOrDefault(v => v.Id == route.AssignedVehicleId.Value);
                if (vehicle != null)
                {
                    vehicle.CurrentLoadWeightKg = actualLoadWeightKg;
                }
            }

            // Update route status or record
            route.Status = isWithinTolerance ? "ReadyForDispatch" : "WeightMismatch";

            return Json(new
            {
                success = true,
                isWithinTolerance = isWithinTolerance,
                expectedWeight = expectedWeight,
                actualWeight = actualLoadWeightKg,
                difference = difference,
                percentageDifference = Math.Round(percentageDifference, 2),
                message = isWithinTolerance
                    ? "Truck weight verified. Ready for dispatch."
                    : $"Weight mismatch of {Math.Round(percentageDifference, 2)}%. Please adjust load."
            });
        }
    }
}