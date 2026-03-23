using System.Collections.Concurrent;
using ProductApi.Models;

namespace ProductApi.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product?.IsActive == true ? product : null);
    }

    public Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.Values.Where(p => p.IsActive).ToList().AsEnumerable());
    }

    public Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Task.FromResult(Enumerable.Empty<Product>());

        var sanitizedCategory = category.Trim();
        var products = _products.Values
            .Where(p => p.IsActive && p.Category.Equals(sanitizedCategory, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(products.AsEnumerable());
    }

    public Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (product.Id == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty", nameof(product));

        if (!_products.TryAdd(product.Id, product))
            throw new InvalidOperationException($"Product with ID '{product.Id}' already exists");

        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        // Check if product exists first
        if (!_products.TryGetValue(product.Id, out var existing))
        {
            return Task.FromResult<Product?>(null);
        }

        product.UpdatedAt = DateTime.UtcNow;

        // Update existing properties atomically to avoid race conditions
        var updated = _products.AddOrUpdate(product.Id, product, (key, existingProduct) =>
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;
            existingProduct.IsActive = product.IsActive;
            existingProduct.UpdatedAt = product.UpdatedAt;
            return existingProduct;
        });
        return Task.FromResult<Product?>(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_products.TryGetValue(id, out var product))
        {
            return Task.FromResult(false);
        }

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        _products[id] = product;
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.TryGetValue(id, out var product) && product.IsActive);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.Values.Count(p => p.IsActive));
    }

    public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        if (pageSize < 1)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageSize > 100)
            throw new ArgumentException("Page size cannot exceed 100", nameof(pageSize));

        var activeProducts = _products.Values.Where(p => p.IsActive).ToList();
        var totalCount = activeProducts.Count;

        // Calculate skip with overflow protection
        long skipLong = (long)(pageNumber - 1) * pageSize;
        int skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;

        var items = activeProducts
            .Skip(skip)
            .Take(pageSize);

        return Task.FromResult<(IEnumerable<Product> Items, int TotalCount)>((items, totalCount));
    }
}
