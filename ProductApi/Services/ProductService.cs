using ProductApi.Models;
using ProductApi.Repositories;

namespace ProductApi.Services;

public interface IProductService
{
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductResponse>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResponse<ProductResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return products.Select(MapToResponse);
    }

    public async Task<IEnumerable<ProductResponse>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category cannot be empty", nameof(category));
        }

        var products = await _repository.GetByCategoryAsync(category, cancellationToken);
        return products.Select(MapToResponse);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ValidateCreateRequest(request);

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = SanitizeCategory(request.Category),
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(product, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ValidateUpdateRequest(request);

        // Create a new product instance with updated values
        var updatedProduct = new Product
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = SanitizeCategory(request.Category),
            UpdatedAt = DateTime.UtcNow,
            IsActive = request.IsActive
        };

        var result = await _repository.UpdateAsync(updatedProduct, cancellationToken);
        return result == null ? null : MapToResponse(result);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task<PagedResponse<ProductResponse>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<ProductResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static void ValidateCreateRequest(CreateProductRequest request)
    {
        ValidateProductRequest(request.Name, request.Price, request.StockQuantity, request.Category);
    }

    private static void ValidateUpdateRequest(UpdateProductRequest request)
    {
        ValidateProductRequest(request.Name, request.Price, request.StockQuantity, request.Category);
    }

    private static void ValidateProductRequest(string name, decimal price, int stockQuantity, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required", nameof(category));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));
    }

    private static string SanitizeCategory(string category)
    {
        return category?.Trim() ?? string.Empty;
    }

    private static ProductResponse MapToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        Category = product.Category,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        IsActive = product.IsActive
    };
}
