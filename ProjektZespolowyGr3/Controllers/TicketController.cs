using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;
using System.Collections.Generic;

namespace ProjektZespolowyGr3.Controllers
{
    public class TicketController : Controller
    {
        private static List<TicketViewModel> tickets = new List<TicketViewModel>();

        // Show all tickets (for Admin)
        public IActionResult Index()
        {
            return View(tickets);
        }

        // Show form to report suspicious offer
        public IActionResult Create(int offerId)
        {
            return View(new TicketViewModel { OfferId = offerId });
        }

        [HttpPost]
        public IActionResult Create(TicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Id = tickets.Count + 1;
                tickets.Add(model);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // Admin action: update ticket
        public IActionResult Resolve(int id, string action)
        {
            var ticket = tickets.Find(t => t.Id == id);
            if (ticket != null)
            {
                ticket.AdminAction = action;
                ticket.Status = action == "Oczekuj¹ce" ? TicketStatus.Pending : TicketStatus.Closed;
            }
            return RedirectToAction("Index");
        }
    }
}

