using SupportOpsMcp;

var builder = WebApplication.CreateBuilder(args);
const string LocalInspectorCorsPolicy = "LocalInspector";

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://localhost:5058");
}

builder.Services.AddSingleton<SupportOpsDataStore>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalInspectorCorsPolicy, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly(typeof(Program).Assembly, SupportOpsJson.Options);

var app = builder.Build();

app.UseCors(LocalInspectorCorsPolicy);

app.MapGet("/", () => "SupportOps MCP server is running. Use the MCP endpoint at /mcp.");
app.MapMcp("/mcp");

await app.RunAsync();
