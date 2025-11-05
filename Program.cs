using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

// הוספת DbContext ל-Services Container
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))));

// הוספת CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// הוספת JWT Service
builder.Services.AddScoped<JwtService>();

// הוספת Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// הוספת Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// הפעלת CORS
app.UseCors();

// הפעלת Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// הפעלת Swagger (רק בסביבת פיתוח)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");

// Register endpoint - הרשמה
app.MapPost("/auth/register", async (RegisterRequest request, ToDoDbContext context, JwtService jwtService) =>
{
    // בדיקה אם המשתמש כבר קיים
    if (await context.Users.AnyAsync(u => u.Username == request.Username))
    {
        return Results.BadRequest("Username already exists");
    }

    // יצירת משתמש חדש עם הצפנת סיסמה (פשוטה לדוגמה)
    var user = new User
    {
        Username = request.Username,
        Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        CreatedAt = DateTime.Now
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    var token = jwtService.GenerateToken(user);
    return Results.Ok(new { Token = token, User = new { user.Id, user.Username } });
})
.WithName("Register")
.WithSummary("Register a new user");

// Login endpoint - התחברות
app.MapPost("/auth/login", async (LoginRequest request, ToDoDbContext context, JwtService jwtService) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
    
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
    {
        return Results.Unauthorized();
    }

    var token = jwtService.GenerateToken(user);
    return Results.Ok(new { Token = token, User = new { user.Id, user.Username } });
})
.WithName("Login")
.WithSummary("Login user");

// שליפת כל המשימות - GET /items
app.MapGet("/items", async (ToDoDbContext context) =>
{
    return await context.Items.ToListAsync();
})
.RequireAuthorization()
.WithName("GetAllItems")
.WithSummary("Get all todo items")
.WithDescription("Retrieves all todo items from the database");

// הוספת משימה חדשה - POST /items
app.MapPost("/items", async (Item item, ToDoDbContext context) =>
{
    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
})
.RequireAuthorization()
.WithName("CreateItem")
.WithSummary("Create a new todo item")
.WithDescription("Creates a new todo item in the database");

// עדכון משימה - PUT /items/{id}
app.MapPut("/items/{id}", async (int id, Item updatedItem, ToDoDbContext context) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null)
    {
        return Results.NotFound();
    }
    
    item.Name = updatedItem.Name;
    item.IsComplete = updatedItem.IsComplete;
    
    await context.SaveChangesAsync();
    return Results.Ok(item);
})
.RequireAuthorization()
.WithName("UpdateItem")
.WithSummary("Update an existing todo item")
.WithDescription("Updates an existing todo item by ID");

// מחיקת משימה - DELETE /items/{id}
app.MapDelete("/items/{id}", async (int id, ToDoDbContext context) =>
{
    var item = await context.Items.FindAsync(id);
    if (item == null)
    {
        return Results.NotFound();
    }
    
    context.Items.Remove(item);
    await context.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("DeleteItem")
.WithSummary("Delete a todo item")
.WithDescription("Deletes a todo item by ID");

app.Run();
