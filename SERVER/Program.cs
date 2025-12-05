using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddScoped<JwtService>();

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root endpoint - health check
app.MapGet("/", () => Results.Ok(new { 
    status = "Server is running", 
    message = "TodoApi Server is healthy and ready to accept requests",
    timestamp = DateTime.UtcNow 
}))
.WithName("HealthCheck")
.WithSummary("Check if server is running");

app.MapPost("/auth/register", async (RegisterRequest request, ToDoDbContext context, JwtService jwtService) =>
{
    if (await context.Users.AnyAsync(u => u.Username == request.Username))
    {
        return Results.BadRequest("Username already exists");
    }

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

app.MapGet("/items", async (ToDoDbContext context) =>
{
    return Results.Ok(await context.Items.ToListAsync());
})
.RequireAuthorization()
.WithName("GetAllItems");

app.MapPost("/items", async (Item item, ToDoDbContext context) =>
{
    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
})
.RequireAuthorization()
.WithName("CreateItem");

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
.WithName("UpdateItem");

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
.WithName("DeleteItem");

app.Run();
