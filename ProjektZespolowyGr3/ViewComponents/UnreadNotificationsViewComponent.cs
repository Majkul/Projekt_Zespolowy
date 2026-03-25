using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;
using System.Security.Claims;

namespace ProjektZespolowyGr3.ViewComponents
{
    public class UnreadNotificationsViewComponent : ViewComponent
    {
        private readonly MyDBContext _context;
        private readonly IPayuOrderSyncService _payuSync;

        public UnreadNotificationsViewComponent(MyDBContext context, IPayuOrderSyncService payuSync)
        {
            _context = context;
            _payuSync = payuSync;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View(0);

            var idStr = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var userId))
                return View(0);

            await _payuSync.SyncPendingOrdersForSellerAsync(userId);
            var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return View(count);
        }
    }
}
