using DomPogrzebowyProjekt.Models.System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using ProjektZespolowyGr3.Models;
using ProjektZespolowyGr3.Models.System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyDBContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
