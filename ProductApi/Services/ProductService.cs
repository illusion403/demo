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
            Category = request.Category
        };

        var created = await _repository.CreateAsync(product, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ValidateUpdateRequest(request);

        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return null;
        }

        var updatedProduct = new Product
        {
            Id = existing.Id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            IsActive = request.IsActive,
            CreatedAt = existing.CreatedAt
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
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product name is required", nameof(request));

        if (request.Price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(request));

        if (request.StockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(request));
    }

    private static void ValidateUpdateRequest(UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product name is required", nameof(request));

        if (request.Price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(request));

        if (request.StockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(request));
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
