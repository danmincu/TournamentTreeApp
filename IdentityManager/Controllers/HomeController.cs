using System.Web.Mvc;

namespace IdentitySample.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated && User.IsInRole("Administrator"))
              return RedirectToAction("Index", "UsersAdmin");
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Manage");
            return RedirectToAction("Register", "Account");
            //return View();
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
