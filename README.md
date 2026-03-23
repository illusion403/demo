# Product API - ASP.NET Core Web API Demo

A comprehensive RESTful API built with **.NET 10**, **ASP.NET Core Web API**, demonstrating best practices for API design, unit testing with **xUnit** and **Moq**, containerization with **Docker**, and CI/CD with **GitHub Actions**.

## Features

- **RESTful API Design** - Follows REST principles with proper HTTP methods and status codes
- **CRUD Operations** - Full Create, Read, Update, Delete functionality for products
- **Pagination Support** - Efficient data retrieval with configurable page size
- **Global Exception Handling** - Centralized error handling with consistent API responses
- **Comprehensive Logging** - Structured logging throughout the application
- **Unit Testing** - 100% service and controller coverage with xUnit and Moq
- **Swagger/OpenAPI Documentation** - Interactive API documentation
- **Docker Support** - Containerization for consistent deployment
- **CI/CD Pipeline** - Automated build, test, and deployment with GitHub Actions

## Tech Stack

- **.NET 10** - Latest .NET framework
- **ASP.NET Core Web API** - High-performance web framework
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework for unit tests
- **Docker** - Containerization
- **GitHub Actions** - CI/CD automation
- **Swagger/OpenAPI** - API documentation

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- [Docker](https://docs.docker.com/get-docker/) (optional, for containerization)

### Running Locally

1. **Clone and navigate to the project:**
   ```bash
   cd ProductApi
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the project:**
   ```bash
   dotnet build
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

5. **Access the API:**
   - API Base URL: `https://localhost:7001` or `http://localhost:5000`
   - Swagger UI: `https://localhost:7001/swagger` or `http://localhost:5000/swagger`

### Running with Docker

1. **Build and run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

2. **Access the API:**
   - API: `http://localhost:5000`
   - Swagger: `http://localhost:5000/swagger`

3. **Stop the containers:**
   ```bash
   docker-compose down
   ```

## API Endpoints

### Products

| Method | Endpoint | Description | Status Codes |
|--------|----------|-------------|--------------|
| GET | `/api/products` | Get all active products | 200 OK |
| GET | `/api/products/paged` | Get paginated products | 200 OK |
| GET | `/api/products/{id}` | Get product by ID | 200 OK, 404 Not Found |
| GET | `/api/products/category/{category}` | Get products by category | 200 OK, 400 Bad Request |
| POST | `/api/products` | Create new product | 201 Created, 400 Bad Request |
| PUT | `/api/products/{id}` | Update product | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | `/api/products/{id}` | Soft delete product | 204 No Content, 404 Not Found |

### Query Parameters

**GET /api/products/paged**
- `pageNumber` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 10, max: 100)

### Request/Response Examples

#### Create Product

**Request:**
```http
POST /api/products
Content-Type: application/json

{
  "name": "New Product",
  "description": "Product description",
  "price": 99.99,
  "stockQuantity": 50,
  "category": "Electronics"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Product created successfully",
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "New Product",
    "description": "Product description",
    "price": 99.99,
    "stockQuantity": 50,
    "category": "Electronics",
    "createdAt": "2024-01-15T10:30:00Z",
    "isActive": true
  }
}
```

#### Get Paginated Products

**Request:**
```http
GET /api/products/paged?pageNumber=1&pageSize=10
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Products retrieved successfully",
  "data": {
    "items": [...],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 50,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

## HTTP Status Codes

| Status Code | Usage |
|-------------|-------|
| 200 OK | Successful GET requests |
| 201 Created | Successful POST requests |
| 204 No Content | Successful DELETE requests |
| 400 Bad Request | Validation errors or invalid input |
| 404 Not Found | Resource not found |
| 500 Internal Server Error | Unhandled exceptions |

## Testing

### Running Unit Tests

```bash
dotnet test
```

### Running Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

```
ProductApi.Tests/
├── Services/
│   └── ProductServiceTests.cs    # Service layer tests
├── Controllers/
│   └── ProductsControllerTests.cs # Controller tests
```

## Project Structure

```
demox/
├── ProductApi/
│   ├── Controllers/
│   │   └── ProductsController.cs    # API controllers
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingMiddleware.cs
│   ├── Models/
│   │   ├── Product.cs              # Domain models
│   │   └── ProductDtos.cs          # DTOs
│   ├── Repositories/
│   │   ├── IProductRepository.cs   # Repository interface
│   │   └── InMemoryProductRepository.cs
│   ├── Services/
│   │   ├── IProductService.cs      # Service interface
│   │   └── ProductService.cs       # Business logic
│   ├── Program.cs                  # Application entry point
│   └── ProductApi.csproj
├── ProductApi.Tests/
│   ├── Controllers/
│   │   └── ProductsControllerTests.cs
│   ├── Services/
│   │   └── ProductServiceTests.cs
│   └── ProductApi.Tests.csproj
├── .github/
│   └── workflows/
│       └── dotnet.yml              # CI/CD pipeline
├── Dockerfile                      # Docker configuration
├── docker-compose.yml              # Docker Compose configuration
└── README.md
```

## CI/CD Pipeline

The GitHub Actions workflow (`/.github/workflows/dotnet.yml`) includes:

1. **Build & Restore** - Compiles the solution
2. **Unit Tests** - Runs xUnit tests with code coverage
3. **Docker Build** - Builds and tests Docker image
4. **Code Quality** - Runs code formatting and analyzers

### Pipeline Triggers

- Push to `main` or `develop` branches
- Pull requests to `main` branch

## Architecture Patterns

### Repository Pattern
- **IProductRepository** - Abstracts data access
- **InMemoryProductRepository** - In-memory implementation (easily replaceable with database)

### Service Layer Pattern
- **IProductService** - Defines business operations
- **ProductService** - Implements business logic and validation

### DTO Pattern
- **CreateProductRequest** - Input for creating products
- **UpdateProductRequest** - Input for updating products
- **ProductResponse** - Output representation
- **ApiResponse&lt;T&gt;** - Consistent API response wrapper

## Best Practices Demonstrated

- **Dependency Injection** - Services registered in DI container
- **Async/Await** - All operations are asynchronous
- **Cancellation Tokens** - Proper cancellation support
- **Input Validation** - Request validation with meaningful errors
- **Consistent API Responses** - Standardized response format
- **SOLID Principles** - Single responsibility, dependency inversion
- **Repository Pattern** - Abstraction of data access
- **Global Error Handling** - Centralized exception management
- **Comprehensive Logging** - Structured logging with context

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

**Built with .NET 10** 🚀
