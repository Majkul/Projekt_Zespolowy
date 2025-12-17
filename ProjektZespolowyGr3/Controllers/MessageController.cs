using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProjektZespolowyGr3.Controllers
{
    public class MessageController : Controller
    {
        private static List<ThreadViewModel> threads = new List<ThreadViewModel>();
        private static List<MessageViewModel> messages = new List<MessageViewModel>();

        // Show all threads for logged-in user
        public IActionResult Threads(string userId)
        {
            var userThreads = threads.Where(t => t.User1Id == userId || t.User2Id == userId).ToList();
            return View(userThreads);
        }

        // Show messages in a thread
        public IActionResult Thread(int threadId)
        {
            var thread = threads.FirstOrDefault(t => t.Id == threadId);
            if (thread == null) return NotFound();

            var threadMessages = messages.Where(m => m.ThreadId == threadId).OrderBy(m => m.SentAt).ToList();
            return View(threadMessages);
        }

        // Send message
        [HttpPost]
        public IActionResult Send(MessageViewModel model)
        {
            var thread = threads.FirstOrDefault(t => t.Id == model.ThreadId);
            if (thread == null) return BadRequest("Nie istnieje taki w¹tek.");
            if (thread.IsBlocked) return BadRequest("W¹tek zosta³ zablokowany przez autora oferty.");

            if (ModelState.IsValid)
            {
                model.Id = messages.Count + 1;
                messages.Add(model);
                // Simulate near real-time: receiver gets badge/counter update
                TempData["MessageSent"] = "Wiadomoœæ wys³ana.";
                return RedirectToAction("Thread", new { threadId = model.ThreadId });
            }

            return View("Thread", messages.Where(m => m.ThreadId == model.ThreadId).ToList());
        }

        // Block conversation (only offer author can do this)
        public IActionResult BlockThread(int threadId)
        {
            var thread = threads.FirstOrDefault(t => t.Id == threadId);
            if (thread != null)
            {
                thread.IsBlocked = true;
            }
            return RedirectToAction("Threads", new { userId = thread?.User1Id });
        }
    }
}

