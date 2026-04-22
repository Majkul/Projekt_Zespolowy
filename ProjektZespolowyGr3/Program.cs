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

builder.Services.AddDbContext<MyDBContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPayuOrderSyncService, PayuOrderSyncService>();

builder.Services.AddTransient<HelperService>();

// files
builder.Services.AddScoped<IFileService, FileService>();

//Authentication
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
