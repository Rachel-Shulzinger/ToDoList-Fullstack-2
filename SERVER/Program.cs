using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoDB"))));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.WithOrigins(
            "https://todolist-fullstack-2-02.onrender.com",
            "http://localhost:3000",
            "http://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// CORS must be before Authentication
app.UseCors();

// No authentication middleware - API is public (todos available without login)

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

// Authentication endpoints removed - API provides public todos endpoints only

app.MapGet("/items", async (ToDoDbContext context) =>
{
    return Results.Ok(await context.Items.ToListAsync());
})
.WithName("GetAllItems");

app.MapPost("/items", async (Item item, ToDoDbContext context) =>
{
    context.Items.Add(item);
    await context.SaveChangesAsync();
    return Results.Created($"/items/{item.Id}", item);
})
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
.WithName("DeleteItem");

app.Run();
