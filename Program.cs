using Microsoft.EntityFrameworkCore;
using WasteManagement.Models;
using System.Security.Cryptography;
using System.Text;

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot",
});

// DB setup
builder.Services.AddDbContext<WasteManagementContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger/OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// ✅ CORS policy for GitHub Pages
var corsPolicy = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy =>
        {
            policy.WithOrigins("https://kksambo.github.io")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.WebHost.UseUrls($"http://*:{port}"); // Listen on all interfaces

var app = builder.Build();

// Apply DB migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WasteManagementContext>();
    dbContext.Database.Migrate();
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(corsPolicy); // ✅ Apply CORS here

// Routes
app.MapGet("/", () => "Hello ledwaba!");

// SmartBins
app.MapPost("/api/smartbins", async (WasteManagementContext db, SmartBin smartBin) =>
{
    db.SmartBins.Add(smartBin);
    await db.SaveChangesAsync();
    return Results.Created($"/api/smartbins/{smartBin.Id}", smartBin);
});

app.MapGet("/api/smartbins", async (WasteManagementContext db) => await db.SmartBins.ToListAsync());

app.MapPut("/api/smartbins/{id}", async (WasteManagementContext db, int id, SmartBin updatedBin) =>
{
    var bin = await db.SmartBins.FindAsync(id);
    if (bin == null) return Results.NotFound($"Bin with ID {id} not found.");

    bin.capacity = updatedBin.capacity;
    bin.currentWeight = updatedBin.currentWeight;
    await db.SaveChangesAsync();

    return Results.Ok(bin);
});

// AppUsers
app.MapPost("/api/AppUsers", async (WasteManagementContext db, AppUser appUser) =>
{
    var existingUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == appUser.Email);
    if (existingUser != null) return Results.BadRequest("Email already exists.");

    db.AppUsers.Add(appUser);
    await db.SaveChangesAsync();
    return Results.Created($"/api/AppUsers/{appUser.Id}", appUser);
});

app.MapGet("/api/AppUsers", async (WasteManagementContext db) => await db.AppUsers.ToListAsync());

app.MapDelete("/api/deleteUser/{id}", async (int id, WasteManagementContext db) =>
{
    var appUser = await db.AppUsers.FindAsync(id);
    if (appUser == null) return Results.NotFound();

    db.AppUsers.Remove(appUser);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/deleteBin/{id}", async (int id, WasteManagementContext db) =>
{
    var bin = await db.SmartBins.FindAsync(id);
    if (bin == null) return Results.NotFound();

    db.SmartBins.Remove(bin);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Rewards
app.MapPost("/api/Rewards", async (WasteManagementContext db, Reward reward) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == reward.UserEmail);
    if (user == null) return Results.NotFound("User not found");

    if (user.Points < reward.PointsRequired) return Results.BadRequest("Not enough points");

    user.Points -= reward.PointsRequired;
    user.amount += reward.Amount;

    db.Rewards.Add(reward);
    await db.SaveChangesAsync();

    return Results.Created($"/api/Rewards/{reward.Id}", reward);
});

app.MapGet("/api/Rewards", async (WasteManagementContext db) => await db.Rewards.ToListAsync());

app.MapPost("/api/getPoints", async (WasteManagementContext db, string email) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
    return user != null ? Results.Ok(user.Points) : Results.NotFound("User not found");
});

app.MapPost("/api/givePoints", async (WasteManagementContext db, Point point) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == point.UserEmail);
    if (user == null) return Results.NotFound("User not found");

    user.Points += point.Points;
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Points allocated successfully", UpdatedPoints = user.Points });
});

// Login
app.MapPost("/api/login", async (WasteManagementContext db, AppUser loginRequest) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
    if (user == null || user.Password != loginRequest.Password) return Results.Unauthorized();

    var token = GenerateToken(user.Email);
    return Results.Ok(new { Token = token, Message = "Login successful" });
});

// Deposits
app.MapPost("/api/deposit", async (WasteManagementContext db, DepositRequest request) =>
{
    var bin = await db.SmartBins.FindAsync(request.BinId);
    if (bin == null) return Results.NotFound($"Bin with ID {request.BinId} not found.");

    db.DepositRequests.Add(request);
    bin.currentWeight += request.Weight;

    await db.SaveChangesAsync();
    return Results.Ok(bin);
});

app.MapGet("/api/deposit", async (WasteManagementContext db) => await db.DepositRequests.ToListAsync());

// Token generator
string GenerateToken(string email)
{
    using var hmac = new HMACSHA256();
    var tokenBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(email + DateTime.UtcNow));
    return Convert.ToBase64String(tokenBytes);
}

app.Run();
