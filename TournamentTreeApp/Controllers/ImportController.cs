using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TournamentsTreeApp.Controllers
{
    public class ImportController : Controller
    {
        // GET: Import
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }
    }
}