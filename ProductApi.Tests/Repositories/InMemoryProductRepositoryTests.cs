using ProductApi.Models;
using ProductApi.Repositories;
using Xunit;

namespace ProductApi.Tests.Repositories;

public class InMemoryProductRepositoryTests
{
    private readonly InMemoryProductRepository _repository;

    public InMemoryProductRepositoryTests()
    {
        _repository = new InMemoryProductRepository();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingActiveProduct_ReturnsProduct()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10,
            Category = "Test Category"
        };
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveProduct_ReturnsNull()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Price = 99.99m,
            StockQuantity = 10,
            Category = "Test Category",
            IsActive = false
        };
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.GetByIdAsync(product.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetByIdAsync(Guid.Empty));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveProducts()
    {
        // Arrange
        var activeProduct = new Product { Name = "Active", Price = 10m, StockQuantity = 1, Category = "Cat" };
        var inactiveProduct = new Product { Name = "Inactive", Price = 10m, StockQuantity = 1, Category = "Cat", IsActive = false };
        await _repository.CreateAsync(activeProduct);
        await _repository.CreateAsync(inactiveProduct);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(activeProduct.Id, result.First().Id);
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ExistingCategory_ReturnsMatchingProducts()
    {
        // Arrange
        var product1 = new Product { Name = "Product 1", Price = 10m, StockQuantity = 1, Category = "Electronics" };
        var product2 = new Product { Name = "Product 2", Price = 20m, StockQuantity = 1, Category = "Electronics" };
        var product3 = new Product { Name = "Product 3", Price = 30m, StockQuantity = 1, Category = "Books" };
        await _repository.CreateAsync(product1);
        await _repository.CreateAsync(product2);
        await _repository.CreateAsync(product3);

        // Act
        var result = await _repository.GetByCategoryAsync("Electronics");

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal("Electronics", p.Category));
    }

    [Fact]
    public async Task GetByCategoryAsync_CaseInsensitive_ReturnsMatchingProducts()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Electronics" };
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.GetByCategoryAsync("electronics");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetByCategoryAsync_NullCategory_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetByCategoryAsync(null!));
    }

    [Fact]
    public async Task GetByCategoryAsync_WhiteSpaceCategory_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetByCategoryAsync("   ");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidProduct_CreatesAndReturnsProduct()
    {
        // Arrange
        var product = new Product
        {
            Name = "New Product",
            Description = "Description",
            Price = 99.99m,
            StockQuantity = 10,
            Category = "Category"
        };

        // Act
        var result = await _repository.CreateAsync(product);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.NotEqual(default, result.Id);
        var retrieved = await _repository.GetByIdAsync(product.Id);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task CreateAsync_NullProduct_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.Empty,
            Name = "Product",
            Price = 10m,
            StockQuantity = 1,
            Category = "Cat"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(product));
    }

    [Fact]
    public async Task CreateAsync_DuplicateId_ThrowsInvalidOperationException()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);
        var duplicate = new Product { Id = product.Id, Name = "Duplicate", Price = 20m, StockQuantity = 1, Category = "Cat" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.CreateAsync(duplicate));
    }

    [Fact]
    public async Task CreateAsync_InvalidName_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product { Name = "", Price = 10m, StockQuantity = 1, Category = "Cat" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(product));
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = -1m, StockQuantity = 1, Category = "Cat" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.CreateAsync(product));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingActiveProduct_UpdatesAndReturnsProduct()
    {
        // Arrange
        var product = new Product { Name = "Original", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);
        var updated = new Product
        {
            Id = product.Id,
            Name = "Updated",
            Price = 20m,
            StockQuantity = 5,
            Category = "NewCat",
            CreatedAt = product.CreatedAt,
            IsActive = true
        };

        // Act
        var result = await _repository.UpdateAsync(updated);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(20m, result.Price);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingProduct_ReturnsNull()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product",
            Price = 10m,
            StockQuantity = 1,
            Category = "Cat"
        };

        // Act
        var result = await _repository.UpdateAsync(product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_InactiveProduct_ReturnsNull()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat", IsActive = true };
        await _repository.CreateAsync(product);
        await _repository.DeleteAsync(product.Id);

        var updated = new Product
        {
            Id = product.Id,
            Name = "Updated",
            Price = 20m,
            StockQuantity = 1,
            Category = "Cat"
        };

        // Act
        var result = await _repository.UpdateAsync(updated);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_NullProduct_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_EmptyId_ThrowsArgumentException()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.Empty,
            Name = "Product",
            Price = 10m,
            StockQuantity = 1,
            Category = "Cat"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.UpdateAsync(product));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingActiveProduct_ReturnsTrueAndMarksInactive()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.DeleteAsync(product.Id);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByIdAsync(product.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyInactiveProduct_ReturnsFalse()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);
        await _repository.DeleteAsync(product.Id);

        // Act
        var result = await _repository.DeleteAsync(product.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_EmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeleteAsync(Guid.Empty));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ExistingActiveProduct_ReturnsTrue()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);

        // Act
        var result = await _repository.ExistsAsync(product.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_InactiveProduct_ReturnsFalse()
    {
        // Arrange
        var product = new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(product);
        await _repository.DeleteAsync(product.Id);

        // Act
        var result = await _repository.ExistsAsync(product.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingProduct_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ReturnsActiveProductCount()
    {
        // Arrange
        await _repository.CreateAsync(new Product { Name = "P1", Price = 10m, StockQuantity = 1, Category = "Cat" });
        await _repository.CreateAsync(new Product { Name = "P2", Price = 10m, StockQuantity = 1, Category = "Cat" });
        var p3 = new Product { Name = "P3", Price = 10m, StockQuantity = 1, Category = "Cat" };
        await _repository.CreateAsync(p3);
        await _repository.DeleteAsync(p3.Id);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(2, result);
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ValidPage_ReturnsCorrectItems()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            await _repository.CreateAsync(new Product { Name = $"Product {i}", Price = 10m, StockQuantity = 1, Category = "Cat" });
        }

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(2, 10);

        // Assert
        Assert.Equal(25, totalCount);
        Assert.Equal(10, items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_LargePageSize_HandlesOverflow()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            await _repository.CreateAsync(new Product { Name = $"Product {i}", Price = 10m, StockQuantity = 1, Category = "Cat" });
        }

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(1, 100);

        // Assert
        Assert.Equal(5, items.Count());
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public async Task GetPagedAsync_ExceedsTotal_ReturnsEmpty()
    {
        // Arrange
        await _repository.CreateAsync(new Product { Name = "Product", Price = 10m, StockQuantity = 1, Category = "Cat" });

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(10, 10);

        // Assert
        Assert.Empty(items);
        Assert.Equal(1, totalCount);
    }

    #endregion
}
