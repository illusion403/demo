using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProductApi.Controllers;
using ProductApi.Models;
using ProductApi.Services;
using Xunit;

namespace ProductApi.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockService;
    private readonly Mock<ILogger<ProductsController>> _mockLogger;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockService = new Mock<IProductService>();
        _mockLogger = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkResultWithProducts()
    {
        // Arrange
        var products = new List<ProductResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1" },
            new() { Id = Guid.NewGuid(), Name = "Product 2" }
        };

        _mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ProductResponse>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Products retrieved successfully", apiResponse.Message);
        Assert.Equal(2, ((IEnumerable<ProductResponse>)apiResponse.Data!).Count());
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkResult()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductResponse>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ProductResponse>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Empty(apiResponse.Data!);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOkResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new ProductResponse
        {
            Id = productId,
            Name = "Test Product",
            Price = 99.99m
        };

        _mockService.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.GetById(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ProductResponse>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(productId, apiResponse.Data!.Id);
    }

    [Fact]
    public async Task GetById_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockService.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        // Act
        var result = await _controller.GetById(productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains(productId.ToString(), apiResponse.Message);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "Description",
            Price = 49.99m,
            StockQuantity = 10,
            Category = "Category"
        };

        var createdProduct = new ProductResponse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Price = request.Price
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ProductResponse>>(createdResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(request.Name, apiResponse.Data!.Name);
        Assert.Equal("Product created successfully", apiResponse.Message);
    }

    [Fact]
    public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Invalid",
            Price = -1m,
            StockQuantity = 5,
            Category = "Category"
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Price cannot be negative"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _controller.Create(request));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOkResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 79.99m,
            StockQuantity = 20,
            Category = "Updated Category",
            IsActive = true
        };

        var updatedProduct = new ProductResponse
        {
            Id = productId,
            Name = request.Name,
            Price = request.Price
        };

        _mockService.Setup(s => s.UpdateAsync(productId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProduct);

        // Act
        var result = await _controller.Update(productId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ProductResponse>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(request.Name, apiResponse.Data!.Name);
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Price = 79.99m,
            StockQuantity = 20,
            Category = "Updated Category"
        };

        _mockService.Setup(s => s.UpdateAsync(productId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        // Act
        var result = await _controller.Update(productId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(productId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region GetPaged Tests

    [Fact]
    public async Task GetPaged_ValidParameters_ReturnsOkResult()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;
        var pagedResponse = new PagedResponse<ProductResponse>
        {
            Items = new List<ProductResponse> { new() { Id = Guid.NewGuid(), Name = "Product 1" } },
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = 1,
            TotalPages = 1
        };

        _mockService.Setup(s => s.GetPagedAsync(pageNumber, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetPaged(pageNumber, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductResponse>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Data!.Items);
    }

    #endregion

    #region GetByCategory Tests

    [Fact]
    public async Task GetByCategory_ValidCategory_ReturnsOkResult()
    {
        // Arrange
        var category = "Electronics";
        var products = new List<ProductResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Laptop", Category = category },
            new() { Id = Guid.NewGuid(), Name = "Mouse", Category = category }
        };

        _mockService.Setup(s => s.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetByCategory(category);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<IEnumerable<ProductResponse>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(2, ((IEnumerable<ProductResponse>)apiResponse.Data!).Count());
    }

    [Fact]
    public async Task GetByCategory_EmptyCategory_ReturnsBadRequest()
    {
        // Arrange
        var category = "";

        // Act
        var result = await _controller.GetByCategory(category);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("empty", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
