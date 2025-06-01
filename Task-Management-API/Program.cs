using Task_Management_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Task_Management_API.Repository;
using Task_Management_API.Interfaces;
using Task_Management_API.Services;
using Task_Management_API.DTO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure AdminSettings
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));

// Add Identity with Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => { })
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add Service for Congiuration With Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IRoleSeederService, RoleSeederService>();

// Add Services For Inject UserRepository
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add Services For Inject TaskRepository
builder.Services.AddScoped<ITaskRepository, TaskRepository>();

// Add Service To Make Authentication Read From JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:IssuerIP"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:AudienceIP"],
        ValidateLifetime = true, 
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecritKey"]))
    };
    // Start Blacklist Validation
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var cacheService = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();

            string token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token) && await cacheService.GetAsync<string>(token) != null)
            {
                context.Fail("This token has been blacklisted.");
                return; 
            }

        },
        OnAuthenticationFailed = context =>
        {
            return Task.CompletedTask;
        }
    };
    // End BlackList Validate
});

// Redis Configuration
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// Register Redis Cache Service
builder.Services.AddScoped<ICacheService, RedisCacheService>();


var app = builder.Build();

// Seed roles and admin users
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IRoleSeederService>();
    await seeder.SeedRolesAndAdminAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();