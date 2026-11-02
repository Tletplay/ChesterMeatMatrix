using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ChesterMeatMatrix.Controllers
{
    public class TestController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Hello from TestController";
            return View();
        }
    }
}