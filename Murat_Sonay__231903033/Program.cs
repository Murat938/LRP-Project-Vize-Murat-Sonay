using LRP.Data;
using LRP.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lrp.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/login.html";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.Role, "Admin"));
    options.AddPolicy("Student", policy => policy.RequireClaim(ClaimTypes.Role, "Student"));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("login.html");
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/login.html"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var hasher = new PasswordHasher<string>();
        var admin = new UserAccount
        {
            Username = "admin",
            FullName = "Laboratuvar Admin",
            Role = "Admin",
            PasswordHash = hasher.HashPassword(null, "Admin@123")
        };
        db.Users.Add(admin);
        db.SaveChanges();
    }

    if (!db.Users.Any(u => u.Role == "Student"))
    {
        var hasher = new PasswordHasher<string>();
        var sampleStudent = new UserAccount
        {
            Username = "231903006",
            StudentNumber = "231903006",
            FullName = "Örnek Öğrenci",
            Role = "Student",
            PasswordHash = hasher.HashPassword(null, "231903006@2026")
        };
        db.Users.Add(sampleStudent);
        db.SaveChanges();
    }
}

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext db, HttpContext http) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    if (user is null)
        return Results.Unauthorized();

    var hasher = new PasswordHasher<string>();
    var verify = hasher.VerifyHashedPassword(null, user.PasswordHash, request.Password);
    if (verify == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, user.Role),
        new("FullName", user.FullName ?? string.Empty)
    };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

    return Results.Ok(new { user.Username, user.Role, user.FullName });
});

app.MapPost("/api/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});

app.MapGet("/api/auth/user", (ClaimsPrincipal user) =>
{
    if (!user.Identity?.IsAuthenticated ?? false)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        Username = user.Identity.Name,
        Role = user.FindFirstValue(ClaimTypes.Role),
        FullName = user.FindFirstValue("FullName")
    });
}).RequireAuthorization();

app.MapGet("/api/admin/labs", async (AppDbContext db) => await db.Labs.OrderBy(l => l.Name).ToListAsync())
    .RequireAuthorization("Admin");

app.MapPost("/api/admin/labs", async (Lab lab, AppDbContext db) =>
{
    db.Labs.Add(lab);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/labs/{lab.Id}", lab);
}).RequireAuthorization("Admin");

app.MapPut("/api/admin/labs/{id}", async (int id, Lab updatedLab, AppDbContext db) =>
{
    var lab = await db.Labs.FindAsync(id);
    if (lab is null) return Results.NotFound();
    lab.Name = updatedLab.Name;
    lab.Location = updatedLab.Location;
    lab.Description = updatedLab.Description;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

app.MapDelete("/api/admin/labs/{id}", async (int id, AppDbContext db) =>
{
    var lab = await db.Labs.Include(l => l.Computers).FirstOrDefaultAsync(l => l.Id == id);
    if (lab is null) return Results.NotFound();
    if (lab.Computers.Any()) return Results.BadRequest(new { message = "Laboratuvarda bağlı bilgisayarlar olduğu için silinemez." });
    db.Labs.Remove(lab);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

app.MapGet("/api/admin/computers", async (AppDbContext db) =>
    await db.Computers.Include(c => c.Lab).Include(c => c.AssignedUser).OrderBy(c => c.AssetCode)
        .Select(c => new
        {
            c.Id,
            c.AssetCode,
            c.Brand,
            c.Processor,
            c.Ram,
            c.HasHdmi,
            c.HasVeyon,
            Lab = c.Lab == null ? null : new { c.Lab.Id, c.Lab.Name },
            AssignedUser = c.AssignedUser == null ? null : new { c.AssignedUser.Id, c.AssignedUser.FullName, c.AssignedUser.StudentNumber }
        })
        .ToListAsync())
    .RequireAuthorization("Admin");

app.MapPost("/api/admin/computers", async (ComputerRequest request, AppDbContext db) =>
{
    var lab = await db.Labs.FindAsync(request.LabId);
    if (lab is null) return Results.BadRequest(new { message = "Lab bulunamadı." });

    var computer = new Computer
    {
        Brand = request.Brand,
        Processor = request.Processor,
        Ram = request.Ram,
        HasHdmi = request.HasHdmi,
        HasVeyon = request.HasVeyon,
        LabId = request.LabId,
        AssetCode = await GenerateAssetCode(db, request.LabId)
    };

    db.Computers.Add(computer);
    await db.SaveChangesAsync();
    return Results.Created($"/api/admin/computers/{computer.Id}", new
    {
        computer.Id,
        computer.AssetCode,
        computer.Brand,
        computer.Processor,
        computer.Ram,
        computer.HasHdmi,
        computer.HasVeyon,
        Lab = new { lab.Id, lab.Name }
    });
}).RequireAuthorization("Admin");

app.MapPut("/api/admin/computers/{id}", async (int id, ComputerRequest request, AppDbContext db) =>
{
    var computer = await db.Computers.FindAsync(id);
    if (computer is null) return Results.NotFound();
    computer.Brand = request.Brand;
    computer.Processor = request.Processor;
    computer.Ram = request.Ram;
    computer.HasHdmi = request.HasHdmi;
    computer.HasVeyon = request.HasVeyon;
    computer.LabId = request.LabId;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

app.MapDelete("/api/admin/computers/{id}", async (int id, AppDbContext db) =>
{
    var computer = await db.Computers.FindAsync(id);
    if (computer is null) return Results.NotFound();
    db.Computers.Remove(computer);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

app.MapPost("/api/admin/computers/{id}/unassign", async (int id, AppDbContext db) =>
{
    var computer = await db.Computers.Include(c => c.AssignedUser).FirstOrDefaultAsync(c => c.Id == id);
    if (computer is null) return Results.NotFound();
    if (computer.AssignedUserId is null) return Results.BadRequest(new { message = "Bu bilgisayar zaten atanmamış." });

    computer.AssignedUserId = null;
    computer.AssignedUser = null;
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization("Admin");

app.MapPost("/api/admin/assign", async (AssignmentRequest request, AppDbContext db) =>
{
    var computer = await db.Computers.Include(c => c.AssignedUser).FirstOrDefaultAsync(c => c.Id == request.ComputerId);
    if (computer is null) return Results.BadRequest(new { message = "Bilgisayar bulunamadı." });
    if (computer.AssignedUser is not null) return Results.BadRequest(new { message = "Bu bilgisayar zaten atanmış." });

    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.StudentNumber == request.StudentNumber);
    if (existingUser is not null)
    {
        return Results.BadRequest(new { message = "Bu öğrenci numarası ile zaten bir kullanıcı mevcut." });
    }

    var password = request.StudentNumber + "@2026";
    var hasher = new PasswordHasher<string>();
    var student = new UserAccount
    {
        Username = request.StudentNumber,
        StudentNumber = request.StudentNumber,
        FullName = request.FullName,
        Role = "Student",
        PasswordHash = hasher.HashPassword(null, password)
    };

    computer.AssignedUser = student;
    db.Users.Add(student);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        student.Username,
        student.FullName,
        student.StudentNumber,
        Password = password,
        computer.AssetCode
    });
}).RequireAuthorization("Admin");

app.MapGet("/api/student/computer", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userIdText = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdText, out var userId)) return Results.Unauthorized();
    var student = await db.Users.Include(u => u.AssignedComputer).ThenInclude(c => c.Lab).FirstOrDefaultAsync(u => u.Id == userId);
    if (student is null || student.AssignedComputer is null) return Results.NotFound(new { message = "Zimmetli bilgisayar bulunamadı." });

    return Results.Ok(new
    {
        student.FullName,
        student.StudentNumber,
        Computer = new
        {
            student.AssignedComputer.AssetCode,
            student.AssignedComputer.Brand,
            student.AssignedComputer.Processor,
            student.AssignedComputer.Ram,
            student.AssignedComputer.HasHdmi,
            student.AssignedComputer.HasVeyon,
            Lab = student.AssignedComputer.Lab?.Name
        }
    });
}).RequireAuthorization("Student");

app.Run();

static async Task<string> GenerateAssetCode(AppDbContext db, int labId)
{
    var lab = await db.Labs.FindAsync(labId);
    var labCode = lab?.Name?.ToUpper().Replace(" ", "") ?? "LAB";
    var count = await db.Computers.CountAsync(c => c.LabId == labId) + 1;
    return $"{labCode}-PC-{count:00}";
}

record LoginRequest(string Username, string Password);
record ComputerRequest(string Brand, string Processor, string Ram, bool HasHdmi, bool HasVeyon, int LabId);
record AssignmentRequest(string StudentNumber, string FullName, int ComputerId);
