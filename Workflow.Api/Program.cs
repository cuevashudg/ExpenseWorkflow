using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Workflow.Api.Authorization;
using Workflow.Api.Data;
using Workflow.Application.Services;
using Workflow.Domain.Entities;
using Workflow.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// TODO: REMOVE BEFORE PROD — Dev auth bypass flag
var bypassAuth = builder.Environment.IsDevelopment() &&
                 builder.Configuration.GetValue<bool>("DevSettings:BypassAuth");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure Database: Use In-Memory DB if connection string not provided (development mode)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<WorkflowDbContext>(options =>
        options.UseInMemoryDatabase("WorkflowDb"));
}
else
{
    builder.Services.AddDbContext<WorkflowDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("Workflow.Infrastructure")));
}

// Register Application Services

builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<ExpenseBulkAuthorizationService>();

// Register user lookup service for authorization
builder.Services.AddScoped<Workflow.Api.Authorization.IUserLookupService, Workflow.Api.Authorization.UserLookupService>();

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<WorkflowDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
// TODO: REMOVE BEFORE PROD — DevBypass scheme selection
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = bypassAuth ? "DevBypass" : JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

// TODO: REMOVE BEFORE PROD — Register dev bypass auth handler
if (bypassAuth)
{
    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("DevBypass", null);
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExpenseAccess", policy =>
        policy.Requirements.Add(new Workflow.Api.Authorization.ExpenseAccessRequirement()));
});

// Register the resource-based authorization handler
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Workflow.Api.Authorization.ExpenseAccessHandler>();

var app = builder.Build();

// Seed roles and users (development only)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedAsync(services);
    
    if (app.Environment.IsDevelopment())
    {
        await UserSeeder.SeedAsync(services);

        // TODO: REMOVE BEFORE PROD — Seed dev data when bypass is enabled
        /*if (app.Configuration.GetValue<bool>("DevSettings:BypassAuth"))
        {
            await DevDataSeeder.SeedAsync(services);
        }*/
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Restrict static file access to assets directory only (secure)
// Do NOT serve files from /uploads/ via static middleware
var webRootPath = app.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath
    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(webRootPath, "assets")),
    RequestPath = "/assets"
};
app.UseStaticFiles(staticFileOptions);

// Enable CORS
app.UseCors("AllowFrontend");

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


