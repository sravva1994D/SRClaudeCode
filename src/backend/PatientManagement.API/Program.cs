using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var databaseOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
if (databaseOptions.ApplyMigrationsOnStartup)
{
    var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
    await app.Services.GetRequiredService<MigrationRunner>().ApplyAsync(scripts, CancellationToken.None);
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
