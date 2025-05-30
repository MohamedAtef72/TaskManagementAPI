Task Management API
A fully-featured Task Management API System built with ASP.NET Core, providing comprehensive task management capabilities with user authentication and authorization.
🚀 Features

User Authentication & Authorization: Secure user registration, login, and JWT-based authentication
Task Management: Complete CRUD operations for tasks
User Management: User profile management and role-based access control
Status Tracking: Track task progress with different status levels
Priority Levels: Set and manage task priorities
Due Date Management: Set deadlines and manage task scheduling
Pagination: Efficient data retrieval with customizable page sizes and navigation
RESTful API: Clean and intuitive API endpoints
Swagger Documentation: Interactive API documentation
Entity Framework Core: Robust data persistence layer
Database Integration: Support for SQL Server and SQLite

🛠 Technologies Used

ASP.NET Core 8.0
Entity Framework Core
ASP.NET Core Identity
JWT Authentication
SQL Server / SQLite
Swagger/OpenAPI
AutoMapper
FluentValidation

📋 Prerequisites
Before running this application, make sure you have the following installed:

.NET 8.0 SDK
SQL Server (or SQL Server Express)
Visual Studio 2022 or Visual Studio Code

⚡ Quick Start
1. Clone the Repository
bashgit clone https://github.com/MohamedAtef72/TaskManagementAPI.git
cd TaskManagementAPI
2. Configure Database Connection
Update the connection string in appsettings.json:
json{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagementDB;Trusted_Connection=true;"
  }
}
3. Configure JWT Settings
Update JWT settings in appsettings.json:
json{
  "JWT": {
    "Key": "YourSecretKeyHere",
    "Issuer": "TaskManagementAPI",
    "Audience": "TaskManagementClient",
    "DurationInMinutes": 60
  }
}
4. Run Database Migrations
bashdotnet ef migrations add InitialCreate
dotnet ef database update
5. Run the Application
bashdotnet run
The API will be available at:

HTTPS: https://localhost:7001
HTTP: http://localhost:5000
Swagger UI: https://localhost:7001/swagger

📚 API Endpoints
Authentication
httpPOST /api/auth/register        # User registration
POST /api/auth/login          # User login
POST /api/auth/logout         # User logout
Tasks
httpGET    /api/tasks                    # Get all tasks for authenticated user (with pagination)
GET    /api/tasks/{id}               # Get specific task by ID
POST   /api/tasks                    # Create new task
PUT    /api/tasks/{id}               # Update existing task
DELETE /api/tasks/{id}               # Delete task
GET    /api/tasks/by-category/{categoryId}  # Get tasks by category (with pagination)
GET    /api/tasks/by-status/{status}        # Get tasks by status (with pagination)
Users
httpGET    /api/users/profile     # Get user profile
PUT    /api/users/profile     # Update user profile
DELETE /api/users/profile     # Delete user account
📄 Pagination
The API implements efficient pagination for endpoints that return multiple records. This helps improve performance and user experience when dealing with large datasets.
Pagination Parameters
All paginated endpoints support the following query parameters:
ParameterTypeDefaultDescriptionpageNumberint1The page number to retrieve (1-based)pageSizeint10Number of items per page (max: 100)
Pagination Response Format
Paginated responses follow this structure:
json{
  "data": [
    {
      // ... actual data items
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalPages": 5,
    "totalCount": 50
  }
}
Example Requests
Get Tasks with Pagination
httpGET /api/tasks?pageNumber=1&pageSize=20
Pagination Implementation Details
Default Page Size: 10 items per page
Maximum Page Size: 100 items per page
Minimum Page Number: 1
Performance: Uses efficient database queries with OFFSET and FETCH
Validation: Invalid pagination parameters return appropriate error responses
📊 Data Models
Task Model
json{
  "id": 1,
  "title": "Complete API Documentation",
  "description": "Write comprehensive API documentation",
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2024-12-31T23:59:59",
  "categoryId": 1,
  "userId": "user-guid",
  "createdAt": "2024-01-01T00:00:00",
  "updatedAt": "2024-01-01T00:00:00"
}
User Model
json{
  "id": "user-guid",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "userName": "johndoe",
  "createdAt": "2024-01-01T00:00:00"
}
🔐 Authentication
This API uses JWT (JSON Web Tokens) for authentication. To access protected endpoints:

Register a new user or login with existing credentials
Include the JWT token in the Authorization header:

Authorization: Bearer <your-jwt-token>
🗄 Database Schema
The application uses Entity Framework Core with the following main entities:

Users: Store user information and authentication data
Tasks: Store task details and relationships
Categories: Store task categories
UserRoles: Manage user roles and permissions

🚀 Deployment
Docker Deployment
dockerfile# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "TaskManagementAPI.dll"]
Environment Variables
Set the following environment variables for production:

ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection
JWT__Key
JWT__Issuer
JWT__Audience

🧪 Testing
Run the test suite:
bashdotnet test
📄 API Documentation
Interactive API documentation is available via Swagger UI at /swagger when running the application.
🤝 Contributing

Fork the repository
Create a feature branch (git checkout -b feature/AmazingFeature)
Commit your changes (git commit -m 'Add some AmazingFeature')
Push to the branch (git push origin feature/AmazingFeature)
Open a Pull Request

👨‍💻 Author
Mohamed Atef

GitHub: @MohamedAtef72
LinkedIn: https://www.linkedin.com/in/mohamed-atef-088a55272/
Email: ateefmohamed832@gmail.com

🙏 Acknowledgments

ASP.NET Core team for the excellent framework
Entity Framework Core for robust data access
Swagger for API documentation
JWT for secure authentication