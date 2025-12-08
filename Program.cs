using Asp.Versioning;
using BurhaniGuards.Api;
using BurhaniGuards.Api.Contracts.Requests;
using BurhaniGuards.Api.Models;
using BurhaniGuards.Api.Persistence.Mongo;
using BurhaniGuards.Api.Persistence.MySql;
using BurhaniGuards.Api.Persistence.SqlServer;
using BurhaniGuards.Api.Repositories;
using BurhaniGuards.Api.Repositories.Interfaces;
using BurhaniGuards.Api.Repositories.Mongo;
using BurhaniGuards.Api.Repositories.MySql;
using BurhaniGuards.Api.Services;
using MySqlMemberRepository = BurhaniGuards.Api.Repositories.MySql.MemberRepository;
using MySqlMemberSnapshotRepository = BurhaniGuards.Api.Repositories.MySql.MemberSnapshotRepository;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Register old repositories (for backward compatibility if needed)
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.ICaptainRepository, BurhaniGuards.Api.Repositories.SqlServer.CaptainRepository>();
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.IMemberRepository, MySqlMemberRepository>();
builder.Services.AddScoped<BurhaniGuards.Api.Repositories.Interfaces.IMemberSnapshotRepository, MySqlMemberSnapshotRepository>();

// Register services
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();

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

// Add CORS for Flutter app
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

// Map Controllers
app.MapControllers();

app.Run();

