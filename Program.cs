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
    // Removed Urls property as it is not valid for WebApplicationOptions
});

builder.Services.AddDbContext<WasteManagementContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var corsPolicy = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.WebHost.UseUrls($"http://*:{port}"); // Configure the URLs

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WasteManagementContext>();
    dbContext.Database.Migrate(); // Apply pending migrations
}

// Enable Swagger and Swagger UI for all environments
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Enable CORS
app.UseCors(corsPolicy);

app.MapGet("/", () => "Hello ledwaba!");

app.MapPost("/api/smartbins", async (WasteManagementContext db, SmartBin smartBin) =>
{
    db.SmartBins.Add(smartBin);
    await db.SaveChangesAsync();
    return Results.Created($"/api/smartbins/{smartBin.Id}", smartBin);
});

app.MapGet("/api/smartbins", async (WasteManagementContext db) =>
{
    return await db.SmartBins.ToListAsync();
});

app.MapPut("/api/smartbins/{id}", async (WasteManagementContext db, int id, SmartBin updatedBin) =>
{
    var bin = await db.SmartBins.FindAsync(id);
    if (bin == null)
    {
        return Results.NotFound($"Bin with ID {id} not found.");
    }

    // Update the bin's capacity and current weight
    bin.capacity = updatedBin.capacity;
    bin.currentWeight = updatedBin.currentWeight;  // Update current weight

    await db.SaveChangesAsync();

    return Results.Ok(bin);  // Respond with the updated bin
});




app.MapPost("/api/AppUsers", async (WasteManagementContext db, AppUser appUser) =>
{
    // Check if email already exists
    var existingUser = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == appUser.Email);
    if (existingUser != null)
    {
        return Results.BadRequest("Email already exists.");
    }
    db.AppUsers.Add(appUser);
    await db.SaveChangesAsync();
    return Results.Created($"/api/AppUsers/{appUser.Id}", appUser);
});

app.MapGet("/api/AppUsers", async (WasteManagementContext db) =>
{
    return await db.AppUsers.ToListAsync();
});

app.MapDelete("/api/deleteUser/{id}", async (int id, WasteManagementContext db) =>
{
    // Find the user by ID
    var appUser = await db.AppUsers.FindAsync(id);
    
    // If the user is not found, return a 404 (Not Found) response
    if (appUser == null)
    {
        return Results.NotFound();
    }

    // Remove the user from the database
    db.AppUsers.Remove(appUser);
    await db.SaveChangesAsync();

    // Return a 204 (No Content) response as confirmation
    return Results.NoContent();
});


app.MapDelete("/api/deleteBin/{id}", async (int id, WasteManagementContext db) =>
{
    // Find the user by ID
    var bin = await db.SmartBins.FindAsync(id);
    
    // If the user is not found, return a 404 (Not Found) response
    if (bin == null)
    {
        return Results.NotFound();
    }

    // Remove the user from the database
    db.SmartBins.Remove(bin);
    await db.SaveChangesAsync();

    // Return a 204 (No Content) response as confirmation
    return Results.NoContent();
});



app.MapPost("/api/Rewards", async (WasteManagementContext db, Reward reward) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == reward.UserEmail);
    if (user != null)
    {
        user.Points -= reward.PointsRequired;
        user.amount += reward.Amount;
        if (user.Points < 0)
        {
            return Results.BadRequest("Not enough points");
        }
        await db.SaveChangesAsync();
    }
    else
    {
        return Results.NotFound("User not found");
    }
    db.Rewards.Add(reward);
    await db.SaveChangesAsync();
    return Results.Created($"/api/Rewards/{reward.Id}", reward);
});

app.MapGet("/api/Rewards", async (WasteManagementContext db) =>
{
    return await db.Rewards.ToListAsync();
});

app.MapPost("/api/getPoints", async (WasteManagementContext db, string email) =>
{
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
    if (user != null)
    {
        return Results.Ok(user.Points);
    }
    return Results.NotFound("User not found");
});

app.MapPost("/api/givePoints", async (WasteManagementContext db, Point point) =>
{
    // Find the user by email directly in the database
    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == point.UserEmail);

    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    
    user.Points += point.Points;

 
    await db.SaveChangesAsync();

    return Results.Ok(new { Message = "Points allocated successfully", UpdatedPoints = user.Points });
});




app.MapPost("/api/login", async (WasteManagementContext db, AppUser loginRequest) =>
{

    var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

    if (user == null || user.Password != loginRequest.Password)
    {
        return Results.Unauthorized();
    }

   
    var token = GenerateToken(user.Email);

    return Results.Ok(new { Token = token, Message = "Login successful" });
});

app.MapPost("/api/deposit", async (WasteManagementContext db, DepositRequest request) =>
{
    var bin = await db.SmartBins.FindAsync(request.BinId);
    if (bin == null)
    {
        return Results.NotFound($"Bin with ID {request.BinId} not found.");
    }
      db.DepositRequests.Add(request);
    await db.SaveChangesAsync();
    // Update the bin's current weight
    bin.currentWeight += request.Weight;

    // Save the changes to the database
    await db.SaveChangesAsync();

    return Results.Ok(bin);  // Respond with the updated bin

});

app.MapGet("/api/deposit", async (WasteManagementContext db) =>
{
    return await db.DepositRequests.ToListAsync();
});


string GenerateToken(string email)
{
    using var hmac = new HMACSHA256();
    var tokenBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(email + DateTime.UtcNow));
    return Convert.ToBase64String(tokenBytes);
}

app.Run();
