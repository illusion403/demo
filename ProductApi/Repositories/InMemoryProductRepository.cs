using System.Collections.Concurrent;
using ProductApi.Models;

namespace ProductApi.Repositories;

/// <summary>
/// Thread-safe in-memory implementation of the product repository using ConcurrentDictionary.
/// This implementation uses immutable update patterns to ensure thread safety.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();
    private const int MaxPageSize = 100;
    private const int MinPageSize = 1;
    private const int MinPageNumber = 1;

    /// <summary>
    /// Retrieves an active product by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The product if found and active; otherwise, null.</returns>
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Task.FromResult<Product?>(null);

        _products.TryGetValue(id, out var product);
        return Task.FromResult(product?.IsActive == true ? product : null);
    }

    /// <summary>
    /// Retrieves all active products.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of all active products.</returns>
    public Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var activeProducts = _products.Values
            .Where(p => p.IsActive)
            .AsEnumerable();
        
        return Task.FromResult(activeProducts);
    }

    /// <summary>
    /// Retrieves all active products in a specific category.
    /// </summary>
    /// <param name="category">The category to filter by (case-insensitive).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of active products in the specified category.</returns>
    /// <exception cref="ArgumentNullException">Thrown when category is null.</exception>
    public Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        if (category == null)
            throw new ArgumentNullException(nameof(category));

        if (string.IsNullOrWhiteSpace(category))
            return Task.FromResult(Enumerable.Empty<Product>());

        var sanitizedCategory = category.Trim();
        
        if (sanitizedCategory.Length == 0)
            return Task.FromResult(Enumerable.Empty<Product>());

        var products = _products.Values
            .Where(p => p.IsActive && 
                   !string.IsNullOrEmpty(p.Category) &&
                   p.Category.Equals(sanitizedCategory, StringComparison.OrdinalIgnoreCase))
            .AsEnumerable();
        
        return Task.FromResult(products);
    }

    /// <summary>
    /// Creates a new product in the repository.
    /// </summary>
    /// <param name="product">The product to create.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created product.</returns>
    /// <exception cref="ArgumentNullException">Thrown when product is null.</exception>
    /// <exception cref="ArgumentException">Thrown when product ID is empty or product data is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a product with the same ID already exists.</exception>
    public Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (product.Id == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(product));

        ValidateProductData(product);

        if (!_products.TryAdd(product.Id, product))
            throw new InvalidOperationException($"Product with ID '{product.Id}' already exists.");

        return Task.FromResult(product);
    }

    /// <summary>
    /// Updates an existing active product using an immutable update pattern for thread safety.
    /// </summary>
    /// <param name="product">The product with updated information.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated product if successful; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when product is null.</exception>
    /// <exception cref="ArgumentException">Thrown when product data is invalid.</exception>
    public Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (product.Id == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(product));

        ValidateProductData(product);

        // Check if product exists and is active
        if (!_products.TryGetValue(product.Id, out var existing) || !existing.IsActive)
        {
            return Task.FromResult<Product?>(null);
        }

        // Create a new Product instance to maintain thread safety (immutable update pattern)
        var updatedProduct = new Product
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            Category = product.Category,
            IsActive = product.IsActive,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Atomically replace the product
        var updated = _products.AddOrUpdate(product.Id, updatedProduct, (key, oldProduct) => updatedProduct);
        return Task.FromResult<Product?>(updated);
    }

    /// <summary>
    /// Soft deletes a product by marking it as inactive using an immutable update pattern.
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the product was successfully deleted; otherwise, false.</returns>
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Task.FromResult(false);

        if (!_products.TryGetValue(id, out var existing))
        {
            return Task.FromResult(false);
        }

        if (!existing.IsActive)
        {
            return Task.FromResult(false);
        }

        // Create a new Product instance with IsActive set to false to maintain thread safety
        var deletedProduct = new Product
        {
            Id = existing.Id,
            Name = existing.Name,
            Description = existing.Description,
            Price = existing.Price,
            StockQuantity = existing.StockQuantity,
            Category = existing.Category,
            IsActive = false,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Atomically replace the product
        return Task.FromResult(_products.TryUpdate(id, deletedProduct, existing));
    }

    /// <summary>
    /// Checks if an active product exists with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if an active product exists; otherwise, false.</returns>
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            return Task.FromResult(false);

        return Task.FromResult(_products.TryGetValue(id, out var product) && product.IsActive);
    }

    /// <summary>
    /// Gets the total count of active products.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of active products.</returns>
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_products.Values.Count(p => p.IsActive));
    }

    /// <summary>
    /// Retrieves a paginated list of active products.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing the page items and total count of active products.</returns>
    /// <exception cref="ArgumentException">Thrown when page parameters are invalid.</exception>
    public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < MinPageNumber)
            throw new ArgumentException($"Page number must be greater than or equal to {MinPageNumber}.", nameof(pageNumber));

        if (pageSize < MinPageSize)
            throw new ArgumentException($"Page size must be greater than or equal to {MinPageSize}.", nameof(pageSize));

        if (pageSize > MaxPageSize)
            throw new ArgumentException($"Page size cannot exceed {MaxPageSize}.", nameof(pageSize));

        var activeProducts = _products.Values.Where(p => p.IsActive).ToList();
        var totalCount = activeProducts.Count;

        // Calculate skip with overflow protection
        long skipLong = (long)(pageNumber - 1) * pageSize;
        int skip = skipLong > int.MaxValue ? int.MaxValue : (int)skipLong;

        var items = activeProducts
            .Skip(skip)
            .Take(pageSize)
            .AsEnumerable();

        return Task.FromResult<(IEnumerable<Product> Items, int TotalCount)>((items, totalCount));
    }

    /// <summary>
    /// Validates product data to ensure it meets business requirements.
    /// </summary>
    /// <param name="product">The product to validate.</param>
    /// <exception cref="ArgumentException">Thrown when product data is invalid.</exception>
    private static void ValidateProductData(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(product));

        if (product.Name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters.", nameof(product));

        if (product.Price < 0)
            throw new ArgumentException("Product price cannot be negative.", nameof(product));

        if (product.StockQuantity < 0)
            throw new ArgumentException("Product stock quantity cannot be negative.", nameof(product));

        if (string.IsNullOrWhiteSpace(product.Category))
            throw new ArgumentException("Product category cannot be null or empty.", nameof(product));

        if (product.Category.Length > 100)
            throw new ArgumentException("Product category cannot exceed 100 characters.", nameof(product));
    }
}
