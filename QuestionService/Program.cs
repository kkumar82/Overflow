using System.Net.Sockets;
using Common;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using QuestionService.Data;
using QuestionService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceDefaults();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();

// builder.Services.AddAuthentication().AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>
// {
//     options.RequireHttpsMetadata = false;
//     options.Audience = "overflow";
// });
builder.Services.AddKeycloalAuthentication();

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDb");

// builder.Services.AddOpenTelemetry().WithTracing(traceProviderBuilder =>
// {
//     traceProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
//             .AddService(builder.Environment.ApplicationName))
//         .AddSource("Wolverine");
// });

// var retryPolicy = Policy
//     .Handle<BrokerUnreachableException>()
//     .Or<SocketException>()
//     .WaitAndRetryAsync(
//         retryCount: 5,
//         retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
//         (exception, timeSpan, retryCount) =>
//         {
//             Console.WriteLine($" Retry attempt {retryCount} failed. Retrying in ${timeSpan.Seconds} seconds...");
//         });
//
// await retryPolicy.ExecuteAsync(async () =>
// {
//     var endPoint = builder.Configuration.GetConnectionString("messaging")
//         ?? throw new InvalidOperationException("messaging  connection string not found");
//     var factory = new ConnectionFactory
//     {
//         Uri = new Uri(endPoint)
//     };
//     await using var connection = await factory.CreateConnectionAsync();
// });

// builder.Host.UseWolverine(opts =>
// {
//     opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();
//     opts.PublishAllMessages().ToRabbitExchange("questions");
// });
await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    opts.PublishAllMessages().ToRabbitExchange("questions");
    opts.ApplicationAssembly = typeof(Program).Assembly; // where to look for wolverine handlers
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred while migrating or seeding the database.");
}

app.Run();