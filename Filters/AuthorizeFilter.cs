using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace MedStockAlert.Filters
{
    /// <summary>
    /// Global filter ที่บังคับให้ผู้ใช้ต้องล็อกอินก่อนเข้าถึงทุก action
    /// ยกเว้น action/controller ที่มี [AllowAnonymous]
    /// </summary>
    public class AuthorizeFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // ข้ามถ้า action หรือ controller มี [AllowAnonymous]
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(m => m is AllowAnonymousAttribute);

            if (hasAllowAnonymous)
                return;

            // ตรวจสอบว่าผู้ใช้ล็อกอินแล้วหรือยัง
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
