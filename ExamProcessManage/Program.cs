using ExamProcessManage.Data;
using ExamProcessManage.Dbconnection;
using ExamProcessManage.Interfaces;
using ExamProcessManage.Repository;
using ExamProcessManage.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.AddEnvironmentVariables();

// Đăng ký DatabaseConnection như một Singleton
builder.Services.AddSingleton<DatabaseConnection>();

// Đăng ký DbContext sử dụng DatabaseConnection Singleton
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var databaseConnection = serviceProvider.GetRequiredService<DatabaseConnection>();
    options.UseMySql(databaseConnection.GetConnectionString(), ServerVersion.AutoDetect(databaseConnection.GetConnectionString()));
});

//Add JWT Authentication Middleware - This code will intercept HTTP request and validate the JWT.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
            .GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    }
  );

// Add services to the container.
builder.Services.AddScoped<IUserService, AccountService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers(options =>
{
    // Cấu hình policy xác thực toàn cục
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    // Cấu hình các tùy chọn JSON
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Giữ nguyên tên thuộc tính (không chuyển sang camelCase)
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull; // Bỏ qua các giá trị null
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Bỏ qua các vòng lặp tham chiếu
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseCors(x => x.
    WithOrigins("*").AllowAnyMethod().AllowAnyHeader());

//https://referbruv.com/blog/building-custom-responses-for-unauthorized-requests-in-aspnet-core/
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == (int)System.Net.HttpStatusCode.Unauthorized)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Unauthenticated\"}");
        return;
        //  context.Response.Redirect("/api/v1/account/unAuthenticated");
    }

    if (context.Response.StatusCode == (int)System.Net.HttpStatusCode.Forbidden)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
        return;
        // context.Response.Redirect("/api/v1/account/unAuthenticated");
    }
});

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();