using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PatientManagement.API;
using PatientManagement.API.Auth;
using PatientManagement.API.Middleware;
using PatientManagement.Application;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Interfaces;
using PatientManagement.Infrastructure;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the standard "sub" claim name so the user id is read the same way it was written.
        options.MapInboundClaims = false;
        var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        options.TokenValidationParameters = AccessTokenValidation.Create(authOptions);
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRateLimiter(RateLimitPolicies.AddPolicies);

if (builder.Environment.IsDevelopment())
{
    // The Angular dev server runs on another origin and needs credentials for the refresh cookie.
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("Development", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
}

var app = builder.Build();

// Outermost first: every request gets a correlation id, and every failure is shaped by the error middleware.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Patient Management API v1"));
    app.UseCors("Development");
}

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
if (databaseOptions.ApplyMigrationsOnStartup)
{
    var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
    await app.Services.GetRequiredService<MigrationRunner>().ApplyAsync(scripts, CancellationToken.None);
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
