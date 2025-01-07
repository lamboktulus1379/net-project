using System.Data.Odbc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var server = Environment.GetEnvironmentVariable("SRV");
var database = Environment.GetEnvironmentVariable("DB");
var user = Environment.GetEnvironmentVariable("USR");
var password = Environment.GetEnvironmentVariable("PWD");

// Register OLEDB service with a scoped lifetime
builder.Services.AddTransient<OdbcConnection>(sp =>
{
    var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
    var connectionString = connectionStringTemplate.Replace("{SRV}", server)
        .Replace("{DB}", database)
        .Replace("{USR}", user).Replace("{PWD}", password);
    return new OdbcConnection(connectionString);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();