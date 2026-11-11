using Microsoft.Extensions.Options;
using PatientManagement.API.Middleware;
using PatientManagement.Infrastructure;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Outermost first: every request gets a correlation id, and every failure is shaped by the error middleware.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Patient Management API v1"));
}

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
if (databaseOptions.ApplyMigrationsOnStartup)
{
    var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
    await app.Services.GetRequiredService<MigrationRunner>().ApplyAsync(scripts, CancellationToken.None);
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
