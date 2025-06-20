# Task Management API

A robust, scalable Task Management API built with ASP.NET Core, featuring comprehensive task management capabilities, secure authentication, role-based authorization, and Redis caching for optimal performance.

## ✨ Features

- **Secure Authentication & Authorization**: JWT-based authentication with role management
- **Complete Task Management**: Full CRUD operations with intelligent caching
- **User Management**: Profile management with role-based access control
- **High Performance**: Redis caching for enhanced response times
- **Admin Panel**: Configurable admin account management
- **Task Tracking**: Multi-status task progression with due date management
- **Scalable Architecture**: Pagination support for large datasets
- **RESTful Design**: Clean, intuitive API endpoints
- **Interactive Documentation**: Comprehensive Swagger/OpenAPI integration
- **Modern ORM**: Entity Framework Core with SQL Server and SQLite support

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Database**: Entity Framework Core with SQL Server/SQLite
- **Authentication**: ASP.NET Core Identity + JWT
- **Caching**: Redis (StackExchange.Redis)
- **Documentation**: Swagger/OpenAPI
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

## 📋 Prerequisites

Before getting started, ensure you have:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server or SQL Server Express
- Docker (recommended for Redis)
- Visual Studio 2022 or VS Code

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/TaskManagementAPI.git
cd TaskManagementAPI
```

### 2. Setup Redis

Using Docker (Recommended):
```bash
docker run -d --name redis-cache -p 6379:6379 redis:latest
```

Verify Redis is running:
```bash
docker exec -it redis-cache redis-cli ping
# Expected output: PONG
```

### 3. Configure Application Settings

Create your `appsettings.json` configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Your_Database_Connection_String_Here",
    "RedisConnection": "localhost:6379"
  },
  "Redis": {
    "InstanceName": "TaskManagementAPI",
    "DefaultCacheExpiration": "00:15:00"
  },
  "AdminSettings": {
    "AdminEmails": [
      "admin@yourcompany.com"
    ],
    "DefaultAdminPassword": "SecurePassword@123"
  },
  "JWT": {
    "SecretKey": "Your_JWT_Secret_Key_Min_32_Characters",
    "AudienceIP": "TaskManagementAPI",
    "IssuerIP": "https://localhost:5001"
  }
}
```

> ⚠️ **Security Note**: Replace all placeholder values with secure, environment-specific configurations before deployment.

### 4. Database Setup

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### 5. Run the Application

```bash
dotnet run
```

Navigate to the Swagger documentation: `https://localhost:5001/swagger`

## 📚 API Documentation

### Authentication Endpoints
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout

### Task Management Endpoints
- `GET /api/tasks` - Retrieve tasks (cached)
- `GET /api/tasks/{id}` - Get specific task
- `POST /api/tasks` - Create new task
- `PUT /api/tasks/{id}` - Update existing task
- `DELETE /api/tasks/{id}` - Delete task

### User Management Endpoints
- `GET /api/users/profile` - Get user profile
- `PUT /api/users/profile` - Update user profile
- `DELETE /api/users/account` - Delete user account

> **Performance**: All GET requests utilize Redis caching, while write operations automatically invalidate relevant cache entries.

## 🔄 Caching Strategy

The application implements intelligent Redis caching:

- **Read Operations**: Automatically cached with user-specific and endpoint-specific keys
- **Write Operations**: Smart cache invalidation to maintain data consistency
- **Configurable Expiration**: Adjustable cache duration via configuration
- **Performance Benefits**: Reduced database load and improved response times

## 📄 Pagination Support

All list endpoints support pagination:

```http
GET /api/tasks?pageNumber=1&pageSize=20&sortBy=createdAt&sortOrder=desc
```

**Response Format:**
```json
{
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 5,
    "totalCount": 100,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

## 🏗️ Data Models

### Task Model
```json
{
  "id": 1,
  "title": "Project Planning",
  "description": "Plan the next sprint activities",
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2024-12-31T23:59:59Z",
  "categoryId": 1,
  "userId": "user-guid",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-02T12:00:00Z"
}
```

### User Profile Model
```json
{
  "id": "user-guid",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "userName": "johndoe",
  "roles": ["User"],
  "createdAt": "2024-01-01T00:00:00Z"
}
```

## 🔐 Authentication

The API uses JWT Bearer token authentication. Include the token in request headers:

```http
Authorization: Bearer your-jwt-token-here
```

### Token Management
- Tokens include user claims and roles
- Logout functionality blacklists tokens
- Configurable token expiration
- Automatic cache cleanup on logout

## 👑 Administrative Features

- **Auto-Bootstrap**: Admin accounts created automatically on first run
- **Role Management**: Configurable role-based permissions
- **User Management**: Administrative oversight capabilities
- **System Configuration**: Runtime configuration management

> **Production Note**: Always change default admin credentials before deploying to production environments.

## 🐳 Docker Deployment

### Using Docker Compose (Recommended)

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - ConnectionStrings__RedisConnection=redis:6379
    depends_on:
      - database
      - redis

  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    ports:
      - "1433:1433"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```

### Environment Variables

Create a `.env` file for sensitive configurations:

```env
SA_PASSWORD=Your_password123!
SQL_DB=TaskDb
```

**Deploy the stack:**
```bash
docker-compose up -d
```

## 🔧 Configuration Guide

### Environment-Specific Settings

- **Development**: Use SQLite for rapid development
- **Staging**: Mirror production configuration with test data
- **Production**: Use SQL Server with Redis clustering for high availability

### Security Configuration

1. **JWT Secrets**: Use cryptographically secure random keys (32+ characters)
2. **Database Credentials**: Use strong passwords and consider Azure Key Vault
3. **Redis Security**: Configure AUTH and SSL for production
4. **CORS Policy**: Restrict to known origins in production

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Monitoring and Logging

- **Structured Logging**: Configured for different log levels
- **Health Checks**: Built-in endpoint monitoring
- **Performance Metrics**: Redis and database performance tracking
- **Error Handling**: Comprehensive exception management

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# coding standards
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting

## 🐛 Troubleshooting

### Common Issues

**Redis Connection Failed**
```bash
# Check Redis status
docker ps | grep redis
docker logs redis-container-name
```

**Database Migration Issues**
```bash
# Reset migrations
dotnet ef database drop
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**JWT Authentication Issues**
- Verify JWT secret key length (minimum 32 characters)
- Check token expiration settings
- Validate audience and issuer configuration

## 👨‍💻 Author

***Mohamed Atef***
- GitHub: [@MohamedAtef72](https://github.com/MohamedAtef72)
- LinkedIn: [Mohamed Atef](https://www.linkedin.com/in/mohamed-atef-088a55272/)
- Email: ateefmohamed832@gmail.com

**Built with ❤️ using ASP.NET Core**
