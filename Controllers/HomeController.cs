using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/Index - This is your new landing page
        public ActionResult Index()
        {
            // Check if user is already logged in (has a session)
            bool isLoggedIn = Session != null && Session["Username"] != null;
            ViewBag.IsLoggedIn = isLoggedIn;

            // If logged in, we can show a different button, but we don't auto-redirect anymore.
            // The user can still navigate to the dashboard via a link.
            return View();
        }

        // These can stay for now, or be removed later.
        public ActionResult About()
        {
            ViewBag.Message = "About Chester Meat Matrix";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Chester Meat Matrix";
            return View();
        }
    }
}