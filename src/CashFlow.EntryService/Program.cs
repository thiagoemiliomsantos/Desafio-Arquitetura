using System.Security.Claims;
using System.Text.Json.Serialization;
using CashFlow.EntryService.Endpoints;
using CashFlow.EntryService.Extensions;
using Serilog;

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
builder.Services.AddServiceHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwtBearer("CashFlow.EntryService.xml");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapTokenEndpoints();
}

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0}ms) usuário={UserId}";
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        diagCtx.Set("UserId", httpCtx.User.FindFirstValue(ClaimTypes.Name) ?? "anonymous");
});
app.UseRateLimiter();
app.UseRequestTimeouts();
app.UseAuthentication();
app.UseAuthorization();
app.MapEntryEndpoints();
app.MapHealthEndpoints();

await app.MigrateDatabaseAsync();

app.Run();

#pragma warning disable CS1591
public partial class Program { }
#pragma warning restore CS1591
