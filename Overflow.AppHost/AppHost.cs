#pragma warning restore ASPIRECERTIFICATES001

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("production")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080));

#pragma warning disable ASPIRECERTIFICATES001
var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    //.WithoutHttpsCertificate()
#pragma warning restore ASPIRECERTIFICATES001
    .WithRealmImport("../infra/realms")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    //.WithEndpoint(6001, 8080, "keycloak", isExternal: true)
    .WithEnvironment("VIRTUAL_HOST", "id.overflow.local")
    .WithEnvironment("VIRTUAL_PORT", "8080");

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("p-data")
    .WithPgAdmin();


/* external projects above this line, individual projects below */

var typesenseApiKey = builder.AddParameter("typesense-api-key", secret: true);
// var typesenseApiKey = builder.Environment.IsDevelopment()
//     ? builder.Configuration["Parameters:typesense-api-key"] ??
//       throw new InvalidOperationException("Could not get typesense api key")
//     : "${TYPESENSE_API_KEY}";
//     
var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    //.WithArgs("--data-dir", "/data", "--api-key", typesenseApiKey, "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithEnvironment("TYPESENSE_API_KEY", typesenseApiKey) //new line added for chapter 50
    .WithEnvironment("TYPESENSE_DATA_DIR", "/data")
    .WithEnvironment("TYPESENSE_ENABLE_CORS", "true")
    .WithHttpEndpoint(8108, 8108, name: "typesense");

var typesenseContainer = typesense.GetEndpoint("typesense");

var questionDb  = postgres.AddDatabase( "questionDb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("r-data")
    .WithManagementPlugin(port: 15672);

var questionService = builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak)
    .WithReference(questionDb)
    .WithReference(rabbitmq)
    .WaitFor(keycloak)
    .WaitFor(questionDb)
    .WaitFor(rabbitmq);

var searchService = builder.AddProject<Projects.SearchService>("search-svc")
    .WithEnvironment("typesense-api-key", typesenseApiKey)
    .WithReference(typesenseContainer)
    .WithReference(rabbitmq)
    .WaitFor(typesense)
    .WaitFor(rabbitmq);

#pragma warning disable ASPIRECERTIFICATES001
var yarp = builder.AddYarp("gateway")
    .WithConfiguration(yarpBuilder =>
    {
        yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/test/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
    }) // I use 51734 shown on gateway dashboard instead of 8001 otherwise get "socket hang up" issue on postman
    .WithoutHttpsCertificate()
    .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
    .WithEndpoint(port: 8001, scheme: "http", targetPort: 8001, name: "gateway", isExternal: true)
    .WithEnvironment("VIRTUAL_HOST", "api.overflow.local")
    .WithEnvironment("VIRTUAL_PORT", "8001");

var webapp = builder.AddJavaScriptApp("webapp", "../webapp")
    .WithReference(keycloak)
    .WithHttpEndpoint(env: "PORT", port: 3000, targetPort: 4000)
    .WithEnvironment("VIRTUAL_HOST", "app.overflow.local")
    .WithEnvironment("VIRTUAL_PORT", "4000")
    .PublishAsDockerFile();

if (!builder.Environment.IsDevelopment())
{
    builder.AddContainer("nginx-proxy", "nginxproxy/nginx-proxy", "1.9")
        .WithEndpoint(80, 80, name: "nginx", isExternal: true)
    .WithEndpoint(443, 443, name: "nginx-ssl", isExternal: true)
    .WithBindMount("/var/run/docker.sock", "/tmp/docker.sock", true)
    .WithBindMount("../infra/devcerts", "/etc/nginx/certs", true);

     keycloak.WithEnvironment("KC_HOSTNAME", "https://id.overflow.local")
         .WithEnvironment("KC_HOSTNAME_BACKCHANNEL_DYNAMIC", "true");
}

builder.Build().Run();