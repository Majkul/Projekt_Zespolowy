using DomPogrzebowyProjekt.Models.System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Build connection string from environment variables if DATABASE_URL is set
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
    // Convert postgres:// URL to Npgsql format
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

// TODO ZMIENIC potem wywalic
builder.Services.AddTransient<HelperService>();

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
builder.Services.AddTransient<EmailService>();
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

<<<<<<< HEAD
// Bind settings
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
var mongoSettings = builder.Configuration.GetSection("MongoSettings").Get<MongoSettings>();

// Register MongoClient
builder.Services.AddSingleton<IMongoClient>(s => new MongoClient(mongoSettings.ConnectionString));

// Register database
builder.Services.AddScoped(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoSettings.DatabaseName);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";   // your login page
    options.AccessDeniedPath = "/Auth/Denied";
});
=======
// Forward headers for Replit proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Listen on all interfaces on port 5000
builder.WebHost.UseUrls("http://0.0.0.0:5000");
>>>>>>> b261900 (Configure application to run on port 5000 and connect to PostgreSQL)

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();
<<<<<<< HEAD
=======

>>>>>>> b261900 (Configure application to run on port 5000 and connect to PostgreSQL)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
