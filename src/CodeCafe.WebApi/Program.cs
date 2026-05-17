using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.WebApi.Configuration;
using CodeCafe.WebApi.Health;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddSingleton<ReadinessState>();
builder.Services.AddHostedService<ReadinessShutdownService>();
builder.Services.AddProblemDetails();
builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .PostConfigure(options =>
    {
        if (builder.Environment.IsDevelopment() && options.AllowedOrigins.Length == 0)
        {
            options.AllowedOrigins = CorsOptions.DevelopmentAllowedOrigins;
        }
    })
    .Validate(options => builder.Environment.IsDevelopment() || options.AllowedOrigins.Length > 0,
        "Cors:AllowedOrigins must be set in non-development environments.")
    .Validate(options => options.AllowedOrigins.All(origin =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)),
        "Cors:AllowedOrigins values must be absolute HTTP or HTTPS origins.")
    .ValidateOnStart();
builder.Services.AddCors();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseExceptionHandler();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }));
app.MapGet("/health/ready", (ReadinessState readinessState) =>
{
    return readinessState.IsReady
        ? Results.Ok(new { status = "Ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
app.UseCors(policy =>
{
    policy.WithOrigins(corsOptions.AllowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
});

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
