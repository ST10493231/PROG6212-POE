using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using RaceDay.Authentication;
using RaceDay.Common;
using RaceDay.Controllers;
using RaceDay.Database;
using RaceDay.DTOs;
using RaceDay.Extensions;
using RaceDay.Models;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

// Database (EF Core, Code-First against SQL Server)

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<RaceDayDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// ---------- Controllers ----------
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// ---------- Server-side session ----------
// Session data (UserId, Role, Email, FullName) is stored server-side (in-memory cache
// here; swap AddDistributedMemoryCache for AddStackExchangeRedisCache etc. in production
// / when running multiple API instances behind a load balancer). The client only ever
// holds an opaque, HttpOnly session-ID cookie -- it cannot read or forge the role itself.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ---------- Authentication ----------
// A custom scheme reads the authenticated identity out of session state on every
// request, so [Authorize] / [Authorize(Roles = "Organiser")] work exactly as they
// would with any other ASP.NET Core auth handler.
builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, options => { });

builder.Services.AddAuthorization();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management platform (Part 2). " +
                      "Authentication is server-side session based: call POST /api/auth/login " +
                      "from this Swagger page using 'Try it out' and the session cookie set in " +
                      "the response will automatically be sent on every subsequent request from this page."
    });

    // Surfaces every controller/action <summary> and each DTO property's doc comment
    // in the Swagger UI, so every endpoint describes its purpose, expected input, and response.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ---------- CORS ----------
// Session cookies require explicit origins + AllowCredentials (wildcard origins are
// rejected by browsers once credentials are involved). Add the Part 3 MVC app's
// origin(s) here once that project exists.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        policy
            .WithOrigins("https://localhost:7000", "http://localhost:5000") // TODO: replace with the real Part 3 MVC origin(s)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves wwwroot/uploads (profile pictures, event banners)

app.UseCors("AllowMvcClient");

// Session MUST be registered before Authentication/Authorization -- the session
// authentication handler reads Context.Session, which only exists once this runs.
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so RaceDay.Tests can spin up an in-memory instance of the app.
public partial class Program { }
