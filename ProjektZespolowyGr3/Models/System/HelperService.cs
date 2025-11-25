using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using ProjektZespolowyGr3.Models.DbModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjektZespolowyGr3.Models.ViewModels;

namespace ProjektZespolowyGr3.Models.System
{
    public class HelperService
    {
        private readonly MyDBContext _context;

        public HelperService(MyDBContext context)
        {
            _context = context;
        }

        public int MakeSomeTags()
        {
            if (!_context.Tags.Any())
            {
                var tags = new List<Tag>
                {
                    new Tag { Name = "Electronics" },
                    new Tag { Name = "Furniture" },
                    new Tag { Name = "Books" },
                    new Tag { Name = "Clothing" },
                    new Tag { Name = "Toys" }
                };
                _context.Tags.AddRange(tags);
                _context.SaveChanges();
            }
            return _context.Tags.Count();
        }

        public void PopulateAvailableTags(CreateListingViewModel model)
        {
            model.AvailableTags = _context.Tags
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Text = t.Name,
                    Value = t.Id.ToString()
                })
                .ToList();
        }

        // ZMIENIC TODO
        public int GetCurrentUserId()
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == 1);
            if (user == null)
            {
                var newUser = new Models.DbModels.User
                {
                    Id = 1,
                    Username = "testuser",
                    Email = "test@test.com",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(newUser);
                _context.SaveChanges();
                return newUser.Id;
            }
            else
            {
                return user.Id;
            }
        }

    }
}