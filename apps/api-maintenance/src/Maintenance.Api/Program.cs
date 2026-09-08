using Maintenance.Api.Auth;
using Maintenance.Api.Errors;
using Maintenance.Api.Hosting;
using Maintenance.Api.Observability;
using Maintenance.Api.RateLimiting;
using Maintenance.Application;
using Maintenance.Application.Auth;
using Maintenance.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Structured JSON logs with scopes (the request logging middleware adds userId when known).
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "O";
});

builder.Services.Configure<TokenOptions>(builder.Configuration.GetSection(TokenOptions.Section));
builder.Services.Configure<ClientOptions>(builder.Configuration.GetSection(ClientOptions.Section));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));

builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.Section));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

// Options are resolved through DI so configuration added late (for example by the test host) is honoured.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Value.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Value.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwt.Value.SecurityKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(_ => { });
builder.Services.AddOptions<RateLimiterOptions>()
    .Configure<IOptions<RateLimitOptions>>((options, limits) => AuthRateLimitPolicy.Configure(options, limits.Value));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new DevelopmentOnlyConvention(builder.Environment.IsDevelopment()));
}).ConfigureApiBehaviorOptions(options =>
{
    // Model binding failures are reported by the application's validators instead.
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Services.GetRequiredService<IOptions<JwtOptions>>().Value.Validate();
app.Services.MigrateDatabase();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
