using CashFlow.ConsolidationService.Endpoints;
using CashFlow.ConsolidationService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();
builder.Services.AddTelemetry();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddHandlers();
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDefaultRateLimiter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtBearer("CashFlow.ConsolidationService.xml");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapDailySummaryEndpoints();

await app.MigrateDatabaseAsync();

app.Run();
