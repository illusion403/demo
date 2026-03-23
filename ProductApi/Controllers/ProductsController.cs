using Microsoft.AspNetCore.Mvc;
using ProductApi.Models;
using ProductApi.Services;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <returns>List of all active products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductResponse>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var products = await _productService.GetAllAsync(cancellationToken);

        return Ok(new ApiResponse<IEnumerable<ProductResponse>>
        {
            Success = true,
            Data = products,
            Message = "Products retrieved successfully"
        });
    }

    /// <summary>
    /// Get paginated products
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paged list of products</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProductResponse>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var pagedResult = await _productService.GetPagedAsync(pageNumber, pageSize, cancellationToken);

        return Ok(new ApiResponse<PagedResponse<ProductResponse>>
        {
            Success = true,
            Data = pagedResult,
            Message = "Products retrieved successfully"
        });
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Product with ID '{id}' not found",
                Data = null
            });
        }

        return Ok(new ApiResponse<ProductResponse>
        {
            Success = true,
            Data = product,
            Message = "Product retrieved successfully"
        });
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>List of products in the category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductResponse>>>> GetByCategory(
        [FromRoute] string category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Category cannot be empty",
                Data = null
            });
        }

        var products = await _productService.GetByCategoryAsync(category, cancellationToken);

        return Ok(new ApiResponse<IEnumerable<ProductResponse>>
        {
            Success = true,
            Data = products,
            Message = $"Products in category '{category}' retrieved successfully"
        });
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList(),
                Data = null
            });
        }

        var created = await _productService.CreateAsync(request, cancellationToken);

        _logger.LogInformation("Product created with ID {ProductId}", created.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new ApiResponse<ProductResponse>
            {
                Success = true,
                Data = created,
                Message = "Product created successfully"
            });
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid product ID",
                Data = null
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList(),
                Data = null
            });
        }

        var updated = await _productService.UpdateAsync(id, request, cancellationToken);

        if (updated == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for update", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Product with ID '{id}' not found",
                Data = null
            });
        }

        _logger.LogInformation("Product with ID {ProductId} updated", id);

        return Ok(new ApiResponse<ProductResponse>
        {
            Success = true,
            Data = updated,
            Message = "Product updated successfully"
        });
    }

    /// <summary>
    /// Delete a product (soft delete)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Invalid product ID provided for deletion");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid product ID",
                Data = null
            });
        }

        var deleted = await _productService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Product with ID '{id}' not found",
                Data = null
            });
        }

        _logger.LogInformation("Product with ID {ProductId} deleted", id);

        return NoContent();
    }
}
