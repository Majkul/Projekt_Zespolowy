using Microsoft.AspNetCore.Mvc;
using ProjektZespolowyGr3.Models;

public class OfferController : Controller
{
    private readonly IWebHostEnvironment _env;

    public OfferController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create(OfferViewModel model, List<IFormFile> photos)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var savedFiles = new List<string>();
        foreach (var photo in photos)
        {
            if (photo.Length > 0)
            {
                var fileName = Path.GetFileName(photo.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                savedFiles.Add("/uploads/" + fileName);
            }
        }

        // TODO: Save offer metadata (title, description, tags, etc.) in DB
        // and associate with savedFiles paths.

        return RedirectToAction("Index", "Home");
    }
}

