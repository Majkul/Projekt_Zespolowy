#!/usr/bin/env python3
"""
Run once in the shell:  python3 fix_merge.py
Merges origin/trade-offer-and-notifications, resolves the 5 known conflicts,
then commits and pushes – all in one atomic execution so Replit checkpoints
cannot abort the merge mid-way.
"""
import subprocess, sys, textwrap, os

def run(cmd, check=True):
    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    if result.stdout.strip():
        print(result.stdout.strip())
    if result.stderr.strip():
        print(result.stderr.strip(), file=sys.stderr)
    if check and result.returncode not in (0, 1):
        sys.exit(result.returncode)
    return result

print("=== Step 1: merge (conflicts expected) ===")
run("git merge origin/trade-offer-and-notifications", check=False)

# ── 1. Listing.cs ────────────────────────────────────────────────────────────
print("=== Step 2: fix Listing.cs ===")
path = "ProjektZespolowyGr3/Models/DbModels/Listing.cs"
src = open(path).read()
src = src.replace(
    "        public bool IsArchived { get; set; } = false;\n"
    "<<<<<<< HEAD\n"
    "        public DateTime? ArchivedAt { get; set; }\n"
    "=======\n"
    ">>>>>>> origin/trade-offer-and-notifications",
    "        public bool IsArchived { get; set; } = false;\n"
    "        public DateTime? ArchivedAt { get; set; }"
)
open(path, "w").write(src)

# ── 2. User.cs ───────────────────────────────────────────────────────────────
print("=== Step 3: fix User.cs ===")
open("ProjektZespolowyGr3/Models/DbModels/User.cs", "w").write(textwrap.dedent("""\
    using System.ComponentModel.DataAnnotations;

    namespace ProjektZespolowyGr3.Models.DbModels
    {
        public class User
        {
            [Required, Key]
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? Address { get; set; }
            public bool IsBanned { get; set; } = false;
            public bool IsAdmin { get; set; } = false;
            public bool IsDeleted { get; set; } = false;
            public string? PhoneNumber { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<Listing> Listings { get; set; } = new List<Listing>();
            public ICollection<Review> Reviews { get; set; } = new List<Review>();

            public ICollection<Message> SentMessages { get; set; } = new List<Message>();
            public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

            public ICollection<TradeProposal> TradeProposalsAsInitiator { get; set; } = new List<TradeProposal>();
            public ICollection<TradeProposal> TradeProposalsAsReceiver { get; set; } = new List<TradeProposal>();
            public ICollection<TradeProposalHistoryEntry> TradeProposalHistoryEntries { get; set; } = new List<TradeProposalHistoryEntry>();

            public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        }
    }
"""))

# ── 3. LoginViewModel.cs ──────────────────────────────────────────────────────
print("=== Step 4: fix LoginViewModel.cs ===")
open("ProjektZespolowyGr3/Models/ViewModels/LoginViewModel.cs", "w").write(textwrap.dedent("""\
    using System.ComponentModel.DataAnnotations;

    namespace ProjektZespolowyGr3.Models.ViewModels
    {

        public class LoginViewModel
        {
            [Required(ErrorMessage = "Login jest wymagany.")]
            public string Login { get; set; }

            [Required(ErrorMessage = "Hasło jest wymagane.")]
            public string Password { get; set; }
        }
    }
"""))

# ── 4. Program.cs ────────────────────────────────────────────────────────────
print("=== Step 5: fix Program.cs ===")
open("ProjektZespolowyGr3/Program.cs", "w").write(textwrap.dedent("""\
    using DomPogrzebowyProjekt.Models.System;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.EntityFrameworkCore;
    using ProjektZespolowyGr3.Models;
    using ProjektZespolowyGr3.Models.System;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrEmpty(connectionString))
    {
        var host = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("PGDATABASE") ?? "postgres";
        var user = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
        connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Disable";
    }
    else
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var pgUser = userInfo[0];
        var pgPassword = userInfo.Length > 1 ? userInfo[1] : "";
        var pgHost = uri.Host;
        var pgPort = uri.Port > 0 ? uri.Port : 5432;
        var pgDatabase = uri.AbsolutePath.TrimStart('/');
        connectionString = $"Host={pgHost};Port={pgPort};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Disable";
    }

    builder.Services.AddDbContext<MyDBContext>(options => options.UseNpgsql(connectionString));

    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IPayuOrderSyncService, PayuOrderSyncService>();

    builder.Services.AddTransient<HelperService>();

    builder.Services.AddAuthorization();
    builder.Services.AddTransient<AuthService>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddHttpClient();

    builder.Services.AddHttpClient("PayU")
        .ConfigurePrimaryHttpMessageHandler(() =>
            new HttpClientHandler
            {
                AllowAutoRedirect = false
            });

    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddScoped<IEmailService, EmailService>();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.WebHost.UseUrls("http://0.0.0.0:5000");

    var app = builder.Build();

    app.UseForwardedHeaders();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MyDBContext>();
        db.Database.Migrate();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
"""))

# ── 5. Details.cshtml ─────────────────────────────────────────────────────────
print("=== Step 6: fix Details.cshtml ===")
path = "ProjektZespolowyGr3/Views/Listings/Details.cshtml"
src = open(path).read()

# Conflict 1 – archive banner vs TradeError alert
src = src.replace(
    "<div class=\"container mt-4\">\n"
    "<<<<<<< HEAD\n"
    "    @if (Model.IsArchived)\n"
    "    {\n"
    "        <div class=\"alert alert-secondary d-flex align-items-center mb-3\" role=\"alert\">\n"
    "            <strong>Ogłoszenie zarchiwizowane</strong>\n"
    "            @if (Model.ArchivedAt.HasValue)\n"
    "            {\n"
    "                <span class=\"ms-2 text-muted small\">(@Model.ArchivedAt.Value.ToString(\"dd.MM.yyyy HH:mm\"))</span>\n"
    "            }\n"
    "        </div>\n"
    "=======\n"
    ">>>>>>> origin/trade-offer-and-notifications\n"
    "    @if (TempData[\"TradeError\"] != null)\n"
    "    {\n"
    "        <div class=\"alert alert-warning\">@TempData[\"TradeError\"]</div>\n"
    "    }",
    "<div class=\"container mt-4\">\n"
    "    @if (TempData[\"TradeError\"] != null)\n"
    "    {\n"
    "        <div class=\"alert alert-warning\">@TempData[\"TradeError\"]</div>\n"
    "    }\n"
    "    @if (Model.IsArchived)\n"
    "    {\n"
    "        <div class=\"alert alert-secondary d-flex align-items-center mb-3\" role=\"alert\">\n"
    "            <strong>Ogłoszenie zarchiwizowane</strong>\n"
    "            @if (Model.ArchivedAt.HasValue)\n"
    "            {\n"
    "                <span class=\"ms-2 text-muted small\">(@Model.ArchivedAt.Value.ToString(\"dd.MM.yyyy HH:mm\"))</span>\n"
    "            }\n"
    "        </div>\n"
    "    }"
)

# Conflict 2 – duplicate buy form block
src = src.replace(
    "<<<<<<< HEAD\n"
    "=======\n"
    "                    else if (!Model.IsSold && Model.StockQuantity > 0 && Model.Type == ListingType.Sale)\n"
    "                    {\n"
    "                        <form asp-controller=\"Payment\" asp-action=\"Buy\" method=\"post\" class=\"mb-2\">\n"
    "                            <input type=\"hidden\" name=\"listingId\" value=\"@Model.Id\" />\n"
    "                            <div class=\"mb-2\">\n"
    "                                <label for=\"buy-quantity\" class=\"form-label small mb-0\">Ilość</label>\n"
    "                                <input id=\"buy-quantity\" name=\"quantity\" type=\"number\" class=\"form-control\" value=\"1\" min=\"1\" max=\"@Model.StockQuantity\" required />\n"
    "                            </div>\n"
    "                            <button type=\"submit\" class=\"btn btn-primary w-100 btn-lg\">\n"
    "                                Kup teraz\n"
    "                            </button>\n"
    "                        </form>\n"
    "                    }\n"
    ">>>>>>> origin/trade-offer-and-notifications",
    ""
)

open(path, "w").write(src)

# Verify no conflict markers remain
remaining = [f for f in [
    "ProjektZespolowyGr3/Models/DbModels/Listing.cs",
    "ProjektZespolowyGr3/Models/DbModels/User.cs",
    "ProjektZespolowyGr3/Models/ViewModels/LoginViewModel.cs",
    "ProjektZespolowyGr3/Program.cs",
    "ProjektZespolowyGr3/Views/Listings/Details.cshtml",
] if "<<<<<<" in open(f).read()]
if remaining:
    print("ERROR: conflict markers still present in:", remaining)
    sys.exit(1)

print("=== Step 7: stage resolved files ===")
files = " ".join([
    "ProjektZespolowyGr3/Models/DbModels/Listing.cs",
    "ProjektZespolowyGr3/Models/DbModels/User.cs",
    "ProjektZespolowyGr3/Models/ViewModels/LoginViewModel.cs",
    "ProjektZespolowyGr3/Program.cs",
    "ProjektZespolowyGr3/Views/Listings/Details.cshtml",
])
run(f"git add {files}")

print("=== Step 8: commit ===")
run('git commit -m "Merge origin/trade-offer-and-notifications: resolve conflicts"')

print("=== Step 9: push ===")
run("git push")

print("=== Done! Merge committed and pushed successfully. ===")
