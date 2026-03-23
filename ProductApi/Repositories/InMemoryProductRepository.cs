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
        return Task.FromResult(_products.Values.Where(p => p.IsActive).AsEnumerable());
    }

    public Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var products = _products.Values
            .Where(p => p.IsActive && p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(products.AsEnumerable());
    }

    public Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (!_products.ContainsKey(product.Id))
        {
            return Task.FromResult<Product?>(null);
        }

        product.UpdatedAt = DateTime.UtcNow;
        _products[product.Id] = product;
        return Task.FromResult<Product?>(product);
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
        var allProducts = _products.Values.Where(p => p.IsActive).ToList();
        var totalCount = allProducts.Count;
        
        var items = allProducts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<(IEnumerable<Product> Items, int TotalCount)>((items, totalCount));
    }
}
