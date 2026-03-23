using Moq;
using ProductApi.Models;
using ProductApi.Repositories;
using ProductApi.Services;
using Xunit;

namespace ProductApi.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _service = new ProductService(_mockRepository.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsProductResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            StockQuantity = 10,
            Category = "Test Category"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal(product.Name, result.Name);
        Assert.Equal(product.Price, result.Price);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingProduct_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.GetByIdAsync(productId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Product 2", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Product 3", IsActive = false }
        };

        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.Where(p => p.IsActive));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ValidCategory_ReturnsProducts()
    {
        // Arrange
        var category = "Electronics";
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Laptop", Category = category },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Category = category }
        };

        _mockRepository.Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        var result = await _service.GetByCategoryAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, p => Assert.Equal(category, p.Category));
    }

    [Fact]
    public async Task GetByCategoryAsync_EmptyCategory_ThrowsArgumentException()
    {
        // Arrange
        var category = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetByCategoryAsync(category));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "New Description",
            Price = 49.99m,
            StockQuantity = 20,
            Category = "New Category"
        };

        Product? capturedProduct = null;
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Price, result.Price);
        Assert.NotEqual(Guid.Empty, result.Id);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Name is required")]
    [InlineData(null, "Name is required")]
    [InlineData("   ", "Name is required")]
    public async Task CreateAsync_InvalidName_ThrowsArgumentException(string? name, string expectedMessage)
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = name!,
            Description = "Description",
            Price = 10m,
            StockQuantity = 5,
            Category = "Category"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
        Assert.Contains("name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_NegativePrice_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Product",
            Price = -1m,
            StockQuantity = 5,
            Category = "Category"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
        Assert.Contains("price", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_NegativeStock_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Product",
            Price = 10m,
            StockQuantity = -1,
            Category = "Category"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
        Assert.Contains("stock", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Name",
            Description = "Old Description",
            Price = 10m,
            StockQuantity = 5,
            Category = "Old Category",
            IsActive = true
        };

        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 20m,
            StockQuantity = 10,
            Category = "Updated Category",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        // Act
        var result = await _service.UpdateAsync(productId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Price, result.Price);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingProduct_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Price = 20m,
            StockQuantity = 10,
            Category = "Updated Category"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _service.UpdateAsync(productId, request);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingProduct_DeletesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(productId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProduct_ReturnsFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(productId);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPagedAsync Tests

    [Theory]
    [InlineData(1, 10, 2)]
    [InlineData(2, 5, 2)]
    [InlineData(1, 100, 2)]
    public async Task GetPagedAsync_ValidParameters_ReturnsPagedResults(int pageNumber, int pageSize, int totalItems)
    {
        // Arrange
        var products = Enumerable.Range(1, totalItems)
            .Select(i => new Product { Id = Guid.NewGuid(), Name = $"Product {i}" })
            .ToList();

        _mockRepository.Setup(r => r.GetPagedAsync(pageNumber, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, totalItems));

        // Act
        var result = await _service.GetPagedAsync(pageNumber, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(pageNumber, result.PageNumber);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(totalItems, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 10, 1, 10)]  // Invalid page number, should default to 1
    [InlineData(1, 0, 1, 10)]   // Invalid page size, should default to 10
    [InlineData(1, 200, 1, 100)] // Page size too large, should cap at 100
    public async Task GetPagedAsync_InvalidParameters_CorrectsValues(int inputPage, int inputSize, int expectedPage, int expectedSize)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPagedAsync(expectedPage, expectedSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        // Act
        var result = await _service.GetPagedAsync(inputPage, inputSize);

        // Assert
        Assert.Equal(expectedPage, result.PageNumber);
        Assert.Equal(expectedSize, result.PageSize);
    }

    #endregion
}
