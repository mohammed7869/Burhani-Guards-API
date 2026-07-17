using Asp.Versioning;
using BurhaniGuards.Api;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Hubs;
using BurhaniGuards.Api.Models;
using BurhaniGuards.Api.Persistence.Mongo;
using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Persistence.SqlServer;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.Repositories.Interfaces;
using BurhaniGuards.Api.Repositories.Mongo;
using BurhaniGuards.Api.Repositories.MySql;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.Middleware;
using Microsoft.Extensions.FileProviders;
using MySqlMemberRepository = BurhaniGuards.Api.Repositories.MySql.MemberRepository;
using MySqlMemberSnapshotRepository = BurhaniGuards.Api.Repositories.MySql.MemberSnapshotRepository;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all interfaces (0.0.0.0) for network access
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // Listen on all network interfaces on port 5000
});

// Configure JSON options to use camelCase
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "BurhaniGuards API", 
        Version = "v1" 
    });
    
    // Add Bearer token authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure options
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection(MongoOptions.SectionName));
builder.Services.Configure<MySqlOptions>(builder.Configuration.GetSection(MySqlOptions.SectionName));
builder.Services.Configure<SqlServerOptions>(builder.Configuration.GetSection(SqlServerOptions.SectionName));

// Add contexts
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<MySqlContext>();
builder.Services.AddSingleton<MySqlBootstrapper>();
builder.Services.AddSingleton<SqlServerContext>();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddHttpContextAccessor();

// Register repositories (Dapper-based repositories for users table)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMiqaatRepository, MiqaatRepository>();
builder.Services.AddScoped<IMiqaatMemberRepository, MiqaatMemberRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IDapperMemberRepository, DapperMemberRepository>();
builder.Services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
builder.Services.AddScoped<IQardanHasanaRepository, QardanHasanaRepository>();
builder.Services.AddScoped<IQardanRepaymentRepository, QardanRepaymentRepository>();
builder.Services.AddScoped<ISurveyRepository, SurveyRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register old repositories (for backward compatibility if needed)
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.ICaptainRepository, BurhaniGuards.Api.Repositories.SqlServer.CaptainRepository>();
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.IMemberRepository, MySqlMemberRepository>();
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.IMemberSnapshotRepository, MySqlMemberSnapshotRepository>();

// Register services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMiqaatService, MiqaatService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IQardanHasanaService, QardanHasanaService>();
builder.Services.AddScoped<IQardanRepaymentService, QardanRepaymentService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IFcmPushService, FcmPushService>();

// Register old services (for backward compatibility if needed)
builder.Services.AddScoped<ICaptainAuthService, CaptainAuthService>();
builder.Services.AddScoped<IMemberAuthService, MemberAuthService>();
builder.Services.AddScoped<IUnifiedAuthService, UnifiedAuthService>();

// Add API Versioning
builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver"));
});

// Add Authentication with a default scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
    options.DefaultScheme = "Bearer";
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BurhaniGuards.Api.Middleware.TokenAuthenticationHandler>(
    "Bearer", options => { });

// Add CORS for Flutter app and SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow all origins (needed for mobile apps)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR WebSocket connections
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<MySqlBootstrapper>();
    await bootstrapper.InitializeAsync();
}

// Enable Swagger in all environments for API testing
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Add middleware to set user context after authentication
app.UseMiddleware<BurhaniGuards.Api.Middleware.UserContextMiddleware>();

// Serve static files from upload directory
var uploadPath = @"C:\var\www\bgp_uploads";
if (Directory.Exists(uploadPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadPath),
        RequestPath = "/bgp_uploads"
    });
}

// Map Controllers
app.MapControllers();

// Map SignalR notification hub
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();

