# 🎯 Burhani Guards API - Complete Interview Preparation Guide

> **Your Experience Level**: 1.5 - 2 Years  
> **Technology Stack**: .NET 8, ASP.NET Core Web API, MySQL, Dapper, MongoDB

---

## 📋 Table of Contents

1. [Project Overview](#1-project-overview)
2. [How APIs Work - Fundamentals](#2-how-apis-work---fundamentals)
3. [Frontend-API-Database Communication](#3-frontend-api-database-communication)
4. [Project Architecture](#4-project-architecture)
5. [Key Components Deep Dive](#5-key-components-deep-dive)
6. [Authentication & Authorization](#6-authentication--authorization)
7. [Database Layer](#7-database-layer)
8. [Important Code Patterns](#8-important-code-patterns)
9. [Common Interview Questions](#9-common-interview-questions)
10. [Technical Terminology](#10-technical-terminology)

---

## 1. Project Overview

### What is Burhani Guards API?

This is a **RESTful Web API** built with **.NET 8** that serves as the backend for a **mobile application** (Flutter). It manages:

- **User Authentication** (Login, Password Management)
- **Member Management** (CRUD operations for members)
- **Miqaat Management** (Events/gatherings management)
- **Attendance Tracking** (Points system for volunteers)
- **Role-Based Access Control** (Captain, Member, Admin)

### Business Context

The application manages volunteer activities for a community organization. It tracks:
- **Members**: Volunteers who participate in events
- **Captains**: Leaders who create and manage events
- **Resource Admins**: Administrators who approve events
- **Miqaats**: Religious/community events requiring volunteers

### Tech Stack Summary

| Technology | Purpose |
|------------|---------|
| .NET 8 | Framework |
| ASP.NET Core | Web API Framework |
| MySQL | Primary Database |
| Dapper | Micro ORM for data access |
| MongoDB | Secondary storage (member profiles) |
| BCrypt | Password hashing |
| Swagger | API Documentation |

---

## 2. How APIs Work - Fundamentals

### What is an API?

**API (Application Programming Interface)** is a set of rules and protocols that allows different software applications to communicate with each other.

```
┌─────────────────┐        HTTP Request         ┌─────────────────┐
│                 │  ───────────────────────►   │                 │
│  Frontend App   │                             │   Backend API   │
│  (Flutter/Web)  │  ◄───────────────────────   │   (.NET Core)   │
│                 │        HTTP Response        │                 │
└─────────────────┘                             └─────────────────┘
```

### REST API Principles

**REST (Representational State Transfer)** is an architectural style. Key principles:

1. **Stateless**: Each request contains all information needed
2. **Resource-Based**: URLs represent resources (e.g., `/api/users`, `/api/miqaat`)
3. **HTTP Methods**: 
   - `GET` - Retrieve data
   - `POST` - Create new data
   - `PUT` - Update entire resource
   - `PATCH` - Partial update
   - `DELETE` - Remove resource

### HTTP Request Anatomy

```http
POST /api/1.0/login HTTP/1.1
Host: localhost:5000
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

{
    "itsNumber": "12345678",
    "password": "securePassword123"
}
```

**Components:**
- **HTTP Method**: `POST`
- **Endpoint/URL**: `/api/1.0/login`
- **Headers**: Metadata (Content-Type, Authorization)
- **Body**: JSON payload with data

### HTTP Response Anatomy

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "id": 1,
    "fullName": "John Doe",
    "email": "john@example.com",
    "token": "abc123...",
    "role": "captain"
}
```

**Status Codes:**
- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Client error
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - No permission
- `404 Not Found` - Resource doesn't exist
- `500 Internal Server Error` - Server error

---

## 3. Frontend-API-Database Communication

### Complete Request-Response Flow

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           COMPLETE API FLOW                                   │
└──────────────────────────────────────────────────────────────────────────────┘

Step 1: User Action
┌─────────────┐
│   Flutter   │ User clicks "Login" button
│    App      │ 
└──────┬──────┘
       │
       ▼
Step 2: HTTP Request
┌─────────────────────────────────────────┐
│ POST /api/1.0/login                     │
│ Headers: Content-Type: application/json │
│ Body: { "itsNumber": "123", "pass": ""} │
└─────────────────────┬───────────────────┘
                      │
                      ▼
Step 3: API Controller Receives Request
┌─────────────────────────────────────────┐
│         AuthController.Login()          │
│   - Validates input                     │
│   - Calls UserService.Login()           │
└─────────────────────┬───────────────────┘
                      │
                      ▼
Step 4: Service Layer (Business Logic)
┌─────────────────────────────────────────┐
│         UserService.Login()             │
│   - Calls UserRepository.GetByItsId()   │
│   - Verifies password with BCrypt       │
│   - Generates token                     │
└─────────────────────┬───────────────────┘
                      │
                      ▼
Step 5: Repository Layer (Data Access)
┌─────────────────────────────────────────┐
│     UserRepository.GetByItsId()         │
│   - Opens MySQL connection              │
│   - Executes SQL: SELECT * FROM members │
│   - Returns UserModel object            │
└─────────────────────┬───────────────────┘
                      │
                      ▼
Step 6: MySQL Database
┌─────────────────────────────────────────┐
│           MySQL Database                │
│   - Executes query                      │
│   - Returns row data                    │
└─────────────────────┬───────────────────┘
                      │
                      ▼ (Response flows back up)

Step 7: JSON Response to Frontend
┌─────────────────────────────────────────┐
│ HTTP 200 OK                             │
│ {                                       │
│   "id": 1,                              │
│   "fullName": "Ahmed Ali",              │
│   "token": "xyz123...",                 │
│   "role": "captain"                     │
│ }                                       │
└─────────────────────────────────────────┘
```

### Example: Login Flow in Code

**1. Flutter Frontend Request:**
```dart
final response = await http.post(
  Uri.parse('http://api.example.com/api/1.0/login'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'itsNumber': '12345678',
    'password': 'myPassword'
  }),
);
```

**2. API Controller (AuthController.cs):**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Password))
    {
        return BadRequest(new { message = "Password is required" });
    }

    // Call Service Layer
    var user = await _userService.Login(request.ItsNumber, request.Password);
    
    if (user == null)
    {
        return BadRequest(new { message = "Invalid credentials" });
    }

    // Generate token and return response
    var token = _tokenService.GenerateToken(user.itsId, GetRoleFromRank(user.rank));
    return Ok(new AuthResponse(user.id, user.fullName, ..., token));
}
```

**3. Service Layer (UserService.cs):**
```csharp
public async Task<UserViewModel?> Login(string itsId, string password)
{
    // Get user from repository
    var user = await _userRepository.GetByItsId(itsId);
    
    if (user == null) return null;

    // Verify password using BCrypt
    bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    
    if (!passwordValid) return null;

    return MapToViewModel(user);
}
```

**4. Repository Layer (UserRepository.cs):**
```csharp
public async Task<UserModel?> GetByItsId(string itsId)
{
    using (var connection = _context.CreateConnection())
    {
        var sql = @"SELECT * FROM `members` WHERE `its_id` = @ItsId AND `is_active` = 1";
        var user = await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { ItsId = itsId });
        return user;
    }
}
```

---

## 4. Project Architecture

### Folder Structure

```
BurhaniGuards.Api/
├── Controllers/           # API Endpoints (Entry points)
│   ├── AuthController.cs      # Login, Password management
│   ├── MiqaatController.cs    # Miqaat CRUD operations
│   ├── UserController.cs      # User management
│   └── BaseController.cs      # Common controller functionality
│
├── Services/              # Business Logic Layer
│   ├── UserService.cs         # User-related business logic
│   ├── MiqaatService.cs       # Miqaat business logic
│   ├── TokenService.cs        # JWT token generation
│   └── EmailService.cs        # Email notifications
│
├── Repositories/          # Data Access Layer
│   ├── UserRepository.cs      # User database operations
│   ├── MiqaatRepository.cs    # Miqaat database operations
│   └── Interfaces/            # Repository contracts
│
├── BusinessModel/         # Entity Models (Database tables)
│   ├── UserModel.cs
│   ├── MiqaatModel.cs
│   └── MemberModel.cs
│
├── Contracts/             # DTOs (Data Transfer Objects)
│   ├── Requests/              # Input DTOs
│   │   ├── LoginRequest.cs
│   │   └── CreateMiqaatRequest.cs
│   └── Responses/             # Output DTOs
│       ├── AuthResponse.cs
│       └── MiqaatResponse.cs
│
├── ViewModel/             # View Models for data transformation
│
├── Middleware/            # Custom middleware
│   ├── TokenAuthenticationHandler.cs
│   └── UserContextMiddleware.cs
│
├── Constants/             # Static constants
│   └── MemberRank.cs
│
├── Persistence/           # Database contexts
│   ├── MySql/
│   └── Mongo/
│
├── Program.cs             # Application entry point & DI configuration
├── appsettings.json       # Configuration file
└── DapperContext.cs       # Database connection factory
```

### Layered Architecture Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER                       │
│                       (Controllers)                          │
│    Handles HTTP requests, validation, response formatting    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     BUSINESS LAYER                           │
│                       (Services)                             │
│    Contains business logic, rules, and orchestration         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     DATA ACCESS LAYER                        │
│                      (Repositories)                          │
│    Handles database operations, queries, and mapping         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        DATABASE                              │
│                   (MySQL / MongoDB)                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 5. Key Components Deep Dive

### 5.1 Controllers

Controllers are the **entry point** for HTTP requests. They:
- Receive HTTP requests
- Validate input
- Call services
- Return HTTP responses

**Example - AuthController.cs:**

```csharp
[Route("api/{v:apiVersion}")]  // Route prefix
[ApiController]                 // Enables automatic model validation
[ApiVersion("1.0")]            // API versioning
public class AuthController : BaseController
{
    private readonly IUserService _userService;  // Dependency Injection

    public AuthController(IUserService userService)
    {
        _userService = userService;  // Constructor injection
    }

    [AllowAnonymous]           // No authentication required
    [HttpPost("login")]        // POST /api/1.0/login
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // [FromBody] tells ASP.NET to deserialize JSON body to LoginRequest
        
        // Validation
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        // Call service
        var user = await _userService.Login(request.ItsNumber, request.Password);
        
        if (user == null)
        {
            return BadRequest(new { message = "Invalid credentials" });
        }

        // Return success response
        return Ok(authResponse);
    }
}
```

**Key Attributes:**
- `[Route]` - Defines URL pattern
- `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` - HTTP methods
- `[Authorize]` - Requires authentication
- `[AllowAnonymous]` - Bypasses authentication
- `[FromBody]` - Binds request body to parameter
- `[FromQuery]` - Binds query string parameters

### 5.2 Services (Business Logic)

Services contain the **core business logic**. They:
- Implement business rules
- Coordinate between repositories
- Transform data
- Handle complex operations

**Example - UserService.cs:**

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<UserViewModel?> Login(string itsId, string password)
    {
        // 1. Get user from database
        var user = await _userRepository.GetByItsId(itsId);
        if (user == null) return null;

        // 2. Business logic: Password verification
        bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!passwordValid) return null;

        // 3. Transform and return
        return MapToViewModel(user);
    }

    public async Task<bool> ChangePassword(ChangePasswordRequest request)
    {
        // Business rule: Passwords must match
        if (request.NewPassword != request.ConfirmPassword)
            return false;

        var user = await _userRepository.GetByItsId(request.ItsNumber);
        if (user == null) return false;

        // Hash new password
        user.NewPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        
        await _userRepository.UpdatePassword(user);

        // Send confirmation email
        await _emailService.SendEmailAsync(user.Email, "Password Changed", "...");

        return true;
    }
}
```

### 5.3 Repositories (Data Access)

Repositories handle **all database operations**. They:
- Execute SQL queries
- Map database results to objects
- Handle connections
- Abstract database technology

**Example - UserRepository.cs:**

```csharp
public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<UserModel?> GetByItsId(string itsId)
    {
        // Create database connection
        using (var connection = _context.CreateConnection())
        {
            // SQL query with parameterized input (prevents SQL injection)
            var sql = @"
                SELECT 
                    `id` AS Id,
                    `its_id` AS ItsId,
                    `full_name` AS FullName,
                    `email` AS Email,
                    `password_hash` AS PasswordHash
                FROM `members` 
                WHERE `its_id` = @ItsId AND `is_active` = 1
            ";

            // Execute query and map to object
            var user = await connection.QueryFirstOrDefaultAsync<UserModel>(
                sql, 
                new { ItsId = itsId }
            );
            return user;
        }
    }

    public async Task<int> Add(UserModel model)
    {
        using (var connection = _context.CreateConnection())
        {
            var sql = @"
                INSERT INTO `members` (`its_id`, `full_name`, `email`, `password_hash`)
                VALUES (@ItsId, @FullName, @Email, @PasswordHash);
                SELECT LAST_INSERT_ID();
            ";

            var id = await connection.QuerySingleAsync<int>(sql, model);
            return id;
        }
    }
}
```

---

## 6. Authentication & Authorization

### How Authentication Works in This API

```
┌─────────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION FLOW                           │
└─────────────────────────────────────────────────────────────────┘

1. User Logs In
   └── POST /api/1.0/login { itsNumber, password }

2. Server Validates Credentials
   └── Checks ITS Number exists in database
   └── Verifies password using BCrypt

3. Server Generates Token
   └── Creates SHA256 hash token
   └── Stores token → user mapping in TokenStore

4. Server Returns Token
   └── { "token": "abc123...", "fullName": "...", "role": "captain" }

5. Client Stores Token
   └── Saved in Flutter app (SharedPreferences)

6. Client Sends Token with Every Request
   └── Authorization: Bearer abc123...

7. Server Validates Token on Each Request
   └── TokenAuthenticationHandler extracts token
   └── UserContextMiddleware loads user from TokenStore
```

### Token Authentication Handler

```csharp
public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        
        // Extract token from "Bearer <token>"
        if (!authHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Create claims identity for authenticated user
        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, token),
            new Claim(ClaimTypes.AuthenticationMethod, "Bearer")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Role-Based Authorization

```csharp
// In MiqaatController.cs
[Authorize]  // Requires any authenticated user
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateMiqaatRequest request)
{
    // Check specific role
    if (CurrentUser.roles != 2)  // 2 = Captain
    {
        return Forbid("Only Captains can create miqaats");
    }
    // ... rest of logic
}
```

**Role Constants (MemberRank.cs):**
```csharp
public static class MemberRank
{
    public const int Member = 1;
    public const int Captain = 2;
    public const int ViceCaptain = 3;
    public const int GroupLeader = 5;
    public const int ResourceAdmin = 7;  // Admin
}
```

---

## 7. Database Layer

### Dapper - Micro ORM

**Dapper** is a lightweight ORM (Object-Relational Mapper) that provides:
- High performance (near raw ADO.NET speed)
- Simple API
- Direct SQL control
- Object mapping

**DapperContext.cs:**
```csharp
public class DapperContext
{
    private readonly string? _connectionString;

    public DapperContext(IOptions<MySqlOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    // Factory method to create database connections
    public IDbConnection CreateConnection() => new MySqlConnection(_connectionString);
}
```

### Common Dapper Operations

```csharp
// Query single object
var user = await connection.QueryFirstOrDefaultAsync<UserModel>(
    "SELECT * FROM members WHERE id = @Id", 
    new { Id = 1 }
);

// Query list
var users = await connection.QueryAsync<UserModel>(
    "SELECT * FROM members WHERE is_active = 1"
);

// Insert and get ID
var id = await connection.QuerySingleAsync<int>(
    "INSERT INTO members (name) VALUES (@Name); SELECT LAST_INSERT_ID();",
    new { Name = "John" }
);

// Update
var rowsAffected = await connection.ExecuteAsync(
    "UPDATE members SET name = @Name WHERE id = @Id",
    new { Name = "Jane", Id = 1 }
);

// Delete
await connection.ExecuteAsync(
    "DELETE FROM members WHERE id = @Id",
    new { Id = 1 }
);
```

### Database Schema (Key Tables)

**members table:**
```sql
CREATE TABLE `members` (
    `id` INT PRIMARY KEY AUTO_INCREMENT,
    `its_id` VARCHAR(20),
    `full_name` VARCHAR(100),
    `email` VARCHAR(100),
    `rank` VARCHAR(50),
    `roles` INT,
    `jamiyat` VARCHAR(100),
    `jamaat` VARCHAR(100),
    `gender` VARCHAR(10),
    `age` INT,
    `contact` VARCHAR(20),
    `password_hash` VARCHAR(255),
    `new_password_hash` VARCHAR(255),
    `is_active` BOOLEAN DEFAULT TRUE,
    `is_approved` BOOLEAN DEFAULT TRUE,
    `created_at` DATETIME,
    `updated_at` DATETIME
);
```

**local_miqaat table:**
```sql
CREATE TABLE `local_miqaat` (
    `id` BIGINT PRIMARY KEY AUTO_INCREMENT,
    `miqaat_name` VARCHAR(200),
    `jamaat` VARCHAR(100),
    `jamiyat` VARCHAR(100),
    `from_date` DATE,
    `till_date` DATE,
    `miqaat_days` INT,
    `volunteer_limit` INT,
    `about_miqaat` TEXT,
    `admin_approval` INT,  -- 0=Pending, 1=Approved, 2=Rejected
    `captain_name` VARCHAR(100),
    `miqaat_image1` VARCHAR(255),
    `miqaat_image2` VARCHAR(255),
    `notes` TEXT,
    `created_at` DATETIME,
    `updated_at` DATETIME
);
```

---

## 8. Important Code Patterns

### 8.1 Dependency Injection (DI)

DI is a technique where dependencies are "injected" rather than created inside a class.

**Registration in Program.cs:**
```csharp
// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// Lifetime options:
// AddSingleton - One instance for entire application
// AddScoped - One instance per HTTP request
// AddTransient - New instance every time requested
```

**Using DI in Constructor:**
```csharp
public class AuthController : BaseController
{
    private readonly IUserService _userService;  // Interface, not concrete class

    public AuthController(IUserService userService)  // DI injects implementation
    {
        _userService = userService;
    }
}
```

### 8.2 Option Pattern (Configuration)

**appsettings.json:**
```json
{
  "MySql": {
    "ConnectionString": "Server=localhost;Database=bgpdb;..."
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "noreply@example.com"
  }
}
```

**Options Class:**
```csharp
public class MySqlOptions
{
    public const string SectionName = "MySql";
    public string ConnectionString { get; set; } = string.Empty;
}
```

**Registration:**
```csharp
builder.Services.Configure<MySqlOptions>(
    builder.Configuration.GetSection(MySqlOptions.SectionName)
);
```

**Usage:**
```csharp
public class DapperContext
{
    public DapperContext(IOptions<MySqlOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }
}
```

### 8.3 Records for DTOs (C# 9+)

**Request DTOs:**
```csharp
// Immutable, concise syntax
public record LoginRequest(string? ItsNumber, string? Email, string Password);

// Equivalent to:
public class LoginRequest
{
    public string? ItsNumber { get; init; }
    public string? Email { get; init; }
    public string Password { get; init; }
}
```

**Response DTOs:**
```csharp
public sealed record AuthResponse(
    int Id,
    string? Profile,
    string FullName,
    string Email,
    string Role,
    string Token,
    bool RequiresPasswordChange = false
);
```

### 8.4 Async/Await Pattern

```csharp
// Async method signature
public async Task<UserViewModel?> Login(string itsId, string password)
{
    // await suspends execution until task completes
    var user = await _userRepository.GetByItsId(itsId);
    
    // Multiple awaits can happen sequentially
    var permissions = await _permissionRepository.GetForUser(user.Id);
    
    return MapToViewModel(user);
}
```

### 8.5 Middleware Pipeline

```csharp
// Program.cs - Order matters!
app.UseCors();              // 1. CORS handling
app.UseRouting();           // 2. Route matching
app.UseAuthentication();    // 3. Identify user
app.UseAuthorization();     // 4. Check permissions
app.UseMiddleware<UserContextMiddleware>();  // 5. Custom middleware
app.MapControllers();       // 6. Execute controller
```

---

## 9. Common Interview Questions

### Basic Questions

**Q1: What is REST API?**
> REST (Representational State Transfer) is an architectural style for building web services. It uses standard HTTP methods (GET, POST, PUT, DELETE), is stateless (each request is independent), and resources are identified by URLs.

**Q2: What is the difference between PUT and PATCH?**
> - `PUT` replaces the entire resource
> - `PATCH` updates only specific fields

**Q3: What is Dependency Injection?**
> DI is a design pattern where dependencies are provided to a class rather than created inside it. This makes code more testable, maintainable, and follows the Dependency Inversion Principle.

**Q4: What is the difference between AddSingleton, AddScoped, and AddTransient?**
> - `Singleton`: One instance for entire application lifetime
> - `Scoped`: One instance per HTTP request
> - `Transient`: New instance every time it's requested

### Intermediate Questions

**Q5: Why use Repository Pattern?**
> - Abstracts data access logic from business logic
> - Makes switching databases easier
> - Enables unit testing with mock repositories
> - Single Responsibility Principle

**Q6: What is Dapper and why use it over Entity Framework?**
> Dapper is a micro-ORM that:
> - Is much faster than EF (minimal overhead)
> - Gives full control over SQL queries
> - Is simpler to learn
> - Better for existing databases
> Trade-off: Manual SQL writing, no migrations

**Q7: How do you handle authentication in this API?**
> - User logs in with credentials
> - Server validates and generates a token (SHA256 hash)
> - Token is stored in TokenStore with user mapping
> - Client sends token in Authorization header for subsequent requests
> - TokenAuthenticationHandler validates token on each request
> - UserContextMiddleware loads user context from token

**Q8: What is CORS and why is it needed?**
> CORS (Cross-Origin Resource Sharing) is a security feature that restricts web pages from making requests to a different domain. Since your Flutter app might run on a different origin than the API, CORS headers tell the browser it's okay to make these requests.

### Advanced Questions

**Q9: Explain the request pipeline in ASP.NET Core.**
> 1. Request enters Kestrel web server
> 2. Goes through middleware pipeline (CORS → Routing → Auth → Custom)
> 3. Reaches controller
> 4. Controller calls services
> 5. Services call repositories
> 6. Repository queries database
> 7. Response returns through same pipeline in reverse

**Q10: How do you prevent SQL injection?**
> Using parameterized queries:
> ```csharp
> // SAFE: Parameters
> connection.Query("SELECT * FROM users WHERE id = @Id", new { Id = userId });
> 
> // UNSAFE: String concatenation
> connection.Query($"SELECT * FROM users WHERE id = {userId}");
> ```

**Q11: How would you scale this API?**
> - Add caching (Redis)
> - Database read replicas
> - Horizontal scaling with load balancer
> - Async processing for emails (background jobs)
> - Connection pooling
> - CDN for static files

### Code-Specific Questions

**Q12: Explain how login works in your API.**
> 1. `AuthController.Login` receives request
> 2. Validates input (ITS number and password required)
> 3. Calls `UserService.Login()` 
> 4. Repository queries database for user by ITS ID
> 5. BCrypt verifies password against stored hash
> 6. TokenService generates new token
> 7. Token stored in TokenStore with user info
> 8. Returns AuthResponse with token and user details

**Q13: How do you handle different user roles?**
> Roles defined in MemberRank constants (1=Member, 2=Captain, 7=Admin). Controllers check `CurrentUser.roles` to authorize actions. For example, only Captains (role=2) can create miqaats.

---

## 10. Technical Terminology

| Term | Definition | Example in Project |
|------|------------|-------------------|
| **API** | Application Programming Interface | BurhaniGuards.Api |
| **REST** | Representational State Transfer | HTTP methods on resources |
| **Endpoint** | URL that accepts requests | `/api/1.0/login` |
| **Controller** | Handles HTTP requests | `AuthController` |
| **Service** | Contains business logic | `UserService` |
| **Repository** | Handles data access | `UserRepository` |
| **DTO** | Data Transfer Object | `LoginRequest`, `AuthResponse` |
| **ORM** | Object-Relational Mapper | Dapper |
| **Middleware** | Code that runs between request/response | `TokenAuthenticationHandler` |
| **DI** | Dependency Injection | Services registered in `Program.cs` |
| **JWT** | JSON Web Token | Token-based auth |
| **CORS** | Cross-Origin Resource Sharing | `AddCors()` in Program.cs |
| **BCrypt** | Password hashing algorithm | `BCrypt.Net.BCrypt.HashPassword()` |
| **Async/Await** | Asynchronous programming | `Task<IActionResult>` |
| **Scoped** | DI lifetime per request | Services registered as Scoped |
| **Model** | Entity representing database table | `UserModel` |
| **ViewModel** | Data shaped for presentation | `CurrentUserViewModel` |

---

## 📝 Quick Revision Checklist

### Before Your Interview:

- [ ] Understand the layered architecture (Controller → Service → Repository)
- [ ] Know HTTP methods and status codes
- [ ] Explain how authentication works
- [ ] Understand Dependency Injection
- [ ] Know difference between Dapper and Entity Framework
- [ ] Explain async/await pattern
- [ ] Understand middleware pipeline
- [ ] Know your role-based access control
- [ ] Be ready to explain any endpoint end-to-end

### Key Points to Emphasize:

✅ "I followed clean architecture with separation of concerns"  
✅ "Used Repository pattern for testability and abstraction"  
✅ "Implemented secure authentication with token-based auth"  
✅ "Used Dapper for performance-critical database operations"  
✅ "Applied BCrypt for secure password hashing"  
✅ "Implemented role-based authorization for different user types"  
✅ "Used async/await for non-blocking I/O operations"

---

**Good luck with your interview! 🚀**

*Remember: It's okay to say "I don't know" for topics outside this project. Focus on demonstrating deep understanding of what you've built.*
