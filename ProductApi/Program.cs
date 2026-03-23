using ProductApi.Middleware;
using ProductApi.Repositories;
using ProductApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Product API",
        Version = "v1",
        Description = "A RESTful API for managing products",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
});

// Register application services
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product API v1");
        options.RoutePrefix = "swagger";
    });
}

// Add global exception handling middleware
app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

// Seed some sample data
SeedData(app);

app.Run();

static void SeedData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

    var sampleProducts = new[]
    {
        new ProductApi.Models.Product
        {
            Name = "Laptop",
            Description = "High-performance laptop for developers",
            Price = 1299.99m,
            StockQuantity = 50,
            Category = "Electronics"
        },
        new ProductApi.Models.Product
        {
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with long battery life",
            Price = 29.99m,
            StockQuantity = 200,
            Category = "Electronics"
        },
        new ProductApi.Models.Product
        {
            Name = "Coffee Maker",
            Description = "Automatic drip coffee maker with thermal carafe",
            Price = 79.99m,
            StockQuantity = 30,
            Category = "Home Appliances"
        },
        new ProductApi.Models.Product
        {
            Name = "Running Shoes",
            Description = "Lightweight running shoes with cushioned sole",
            Price = 89.99m,
            StockQuantity = 100,
            Category = "Sports"
        },
        new ProductApi.Models.Product
        {
            Name = "Yoga Mat",
            Description = "Non-slip exercise yoga mat",
            Price = 24.99m,
            StockQuantity = 150,
            Category = "Sports"
        }
    };

    foreach (var product in sampleProducts)
    {
        repository.CreateAsync(product).Wait();
    }
}
