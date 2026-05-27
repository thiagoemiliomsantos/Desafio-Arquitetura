using System.Text.Json.Serialization;
using CashFlow.EntryService.Endpoints;
using CashFlow.EntryService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.AddSerilog();
builder.Services.AddTelemetry();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddHandlers();
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDefaultRateLimiter();
builder.Services.AddRequestTimeouts();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtBearer("CashFlow.EntryService.xml");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapTokenEndpoints();
}

app.UseRateLimiter();
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();
app.MapEntryEndpoints();

await app.MigrateDatabaseAsync();

app.Run();
