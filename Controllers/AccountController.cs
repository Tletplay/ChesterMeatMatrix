using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace ChesterMeatMatrix.Controllers
{
    public class AccountController : Controller
    {
        // Helper method to restore pending cart after login
        private void RestorePendingCart()
        {
            if (TempData["PendingCart"] != null)
            {
                var pendingCart = TempData["PendingCart"] as List<CartItem>;
                if (pendingCart != null && pendingCart.Count > 0)
                {
                    Session["GuestCart"] = pendingCart;
                }
            }
        }

        // GET: /Account/Login
        public ActionResult Login(string returnUrl)
        {
            // If already logged in, go to dashboard
            if (Session["Username"] != null)
            {
                var role = Session["Role"] as string;
                if (role == "Customer") return RedirectToAction("Index", "Customer");
                if (role == "Admin") return RedirectToAction("Dashboard", "Admin");
                if (role == "Checker") return RedirectToAction("Index", "Checker");
                if (role == "Router") return RedirectToAction("Dashboard", "Router");
                if (role == "Driver") return RedirectToAction("Index", "Driver");
                if (role == "Receiver") return RedirectToAction("Index", "Receiver");
                if (role == "WarehouseManager") return RedirectToAction("Dashboard", "WarehouseManager");
                if (role == "Warehouse") return RedirectToAction("Index", "Warehouse");
                if (role == "SalesRep") return RedirectToAction("Index", "Sales");
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string userType, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username and password required.");
                return View();
            }

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user == null)
            {
                // If customer selected and account not found, redirect to SignUp
                if (!string.IsNullOrWhiteSpace(userType) && userType.ToLower() == "customer")
                {
                    TempData["SignUpUsername"] = username;
                    return RedirectToAction("SignUp");
                }

                ModelState.AddModelError("", "Invalid login.");
                return View();
            }

            // set auth cookie and session
            FormsAuthentication.SetAuthCookie(user.Username, false);
            Session["Username"] = user.Username;
            Session["Role"] = user.Role;
            Session["FullName"] = user.FullName;

            // Restore pending cart if exists (for guest checkout)
            RestorePendingCart();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Customer")
            {
                // Check if this customer is an Enterprise customer
                var customer = InMemoryDatabase.Customers.FirstOrDefault(c => c.Name == user.FullName);
                if (customer != null && customer.CustomerType == CustomerTypeEnum.Enterprise)
                {
                    return RedirectToAction("Index", "Enterprise");
                }
                return RedirectToAction("Index", "Customer");
            }

            if (user.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (string.Equals(user.Role, "Checker", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Checker");
            }

            if (string.Equals(user.Role, "Router", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Router");
            }

            if (string.Equals(user.Role, "Driver", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Driver");
            }

            if (string.Equals(user.Role, "Receiver", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Receiver");
            }

            if (string.Equals(user.Role, "Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Warehouse");
            }

            if (string.Equals(user.Role, "SalesRep", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Sales");
            }
            if (string.Equals(user.Role, "WarehouseManager", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "WarehouseManager");
            }

            return RedirectToAction("Index", "Dashboard");


        }

        // GET: /Account/SignUp
        [HttpGet]
        public ActionResult SignUp()
        {
            ViewBag.Username = TempData["SignUpUsername"] as string;
            return View();
        }

        // POST: /Account/SignUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Add "customerType" parameter to the method signature
        public ActionResult SignUp(
    string username,
    string password,
    string confirmPassword,
    string name,
    string phone,
    string address,
    string isEnterprise,
    string customerType,  // <-- ADD THIS NEW PARAMETER
    string companyName,
    string street,
    string suburb,
    string city,
    string province,
    string postalCode)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Username, password and confirm password are required.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Password and confirm password do not match.");
                return View();
            }

            // prevent duplicate username
            if (InMemoryDatabase.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username already exists.");
                return View();
            }

            // determine enterprise flag
            CustomerTypeEnum selectedType;
            switch (customerType)
            {
                case "instore":
                    selectedType = CustomerTypeEnum.Instore;
                    break;
                case "enterprise":
                    selectedType = CustomerTypeEnum.Enterprise;
                    break;
                default:
                    selectedType = CustomerTypeEnum.Standard;
                    break;
            }

            var enterprise = (selectedType == CustomerTypeEnum.Enterprise);
            // finalAddress remains the provided address for instore customers
            string finalAddress = address;

            // if enterprise, validate detailed address
            if (enterprise)
            {
                if (string.IsNullOrWhiteSpace(companyName) ||
                    string.IsNullOrWhiteSpace(street) ||
                    string.IsNullOrWhiteSpace(suburb) ||
                    string.IsNullOrWhiteSpace(city) ||
                    string.IsNullOrWhiteSpace(province) ||
                    string.IsNullOrWhiteSpace(postalCode))
                {
                    ModelState.AddModelError("", "Enterprise customers must provide full company address (company name, street, area, city, province and postal code).");
                    return View();
                }

                finalAddress = companyName + "\n" + street + "\n" + suburb + "\n" + city + "\n" + province + "\n" + postalCode;
            }

            // For enterprise customers use companyName as customer name
            var customerName = enterprise ? companyName : name;

            // If instore and no name provided, fall back to username local-part so signup doesn't silently fail
            if (!enterprise && string.IsNullOrWhiteSpace(customerName))
            {
                // use part before @ if it's an email, otherwise use the username
                try
                {
                    var at = username?.IndexOf('@') ?? -1;
                    customerName = (at > 0) ? username.Substring(0, at) : username;
                }
                catch
                {
                    customerName = username ?? "Customer";
                }
            }

            var newCustomer = new Customer
            {
                Id = InMemoryDatabase.Customers.Any() ? InMemoryDatabase.Customers.Max(c => c.Id) + 1 : 1,
                Name = customerName,
                ContactPhone = phone,
                Address = finalAddress,
                IsEnterprise = enterprise,
                CustomerType = selectedType,  // <-- ADD THIS
                OutstandingBalance = 0m,
                MonthlyCreditLimit = 0m,
                CurrentMonthDebt = 0m
            };
            InMemoryDatabase.Customers.Add(newCustomer);

            var newUser = new User
            {
                Id = InMemoryDatabase.Users.Any() ? InMemoryDatabase.Users.Max(u => u.Id) + 1 : 1,
                Username = username,
                Password = password,
                Role = "Customer",
                FullName = customerName
            };
            InMemoryDatabase.Users.Add(newUser);

            // If enterprise customer, do not auto-login - direct them to login page
            if (enterprise)
            {
                TempData["Message"] = "Enterprise account created. Please sign in to continue.";
                return RedirectToAction("Login");
            }

            // login the new instore customer
            FormsAuthentication.SetAuthCookie(newUser.Username, false);
            Session["Username"] = newUser.Username;
            Session["Role"] = newUser.Role;
            Session["FullName"] = newUser.FullName;

            // Restore pending cart if exists (for guest checkout)
            RestorePendingCart();

            return RedirectToAction("Index", "Customer");
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}