using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedStockAlert.Controllers
{
    [Authorize]   // ต้องล็อกอินถึงจะเข้าได้
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            // TODO: ดึงข้อมูลจริงจาก AlertEngine / Database
            ViewData["UserName"] = User.Identity?.Name ?? "เภสัชกร";
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return Content("เกิดข้อผิดพลาดในระบบ");
        }
    }
}
