using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.DbModels;
using ProjektZespolowyGr3.Models.System;
using System.Security.Claims;

namespace ProjektZespolowyGr3.Controllers.User
{
    [Authorize]
    public class SellerCardController : Controller
    {
        private readonly MyDBContext _context;
        private readonly ICardFeeService _cardFeeService;

        public SellerCardController(MyDBContext context, ICardFeeService cardFeeService)
        {
            _context = context;
            _cardFeeService = cardFeeService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var card = await _context.SellerCards
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            var payouts = await _context.SellerPayouts
                .Where(p => p.SellerId == userId)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.Card = card;
            ViewBag.Payouts = payouts;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTokenization()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.SellerCards
                .AnyAsync(c => c.UserId == userId && c.IsActive);
            if (existing)
            {
                TempData["CardError"] = "Masz już aktywną kartę. Usuń ją najpierw, aby dodać nową.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customerIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var continueUrl = Url.Action("TokenizationSuccess", "SellerCard", null, Request.Scheme)!;

                var redirectUrl = await _cardFeeService.CreateTokenizationOrderAsync(userId, customerIp, continueUrl);
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                TempData["CardError"] = $"Nie udało się rozpocząć dodawania karty: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> TokenizationSuccess()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var tokenOrder = await _context.CardTokenizationOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            bool cardSaved = await _context.SellerCards
                .AnyAsync(c => c.UserId == userId && c.IsActive);

            ViewBag.CardSaved = cardSaved;
            ViewBag.TokenOrderCompleted = tokenOrder?.Completed ?? false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var cards = await _context.SellerCards
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            foreach (var c in cards)
                c.IsActive = false;

            await _context.SaveChangesAsync();
            TempData["CardSuccess"] = "Karta została usunięta.";
            return RedirectToAction(nameof(Index));
        }
    }
}
