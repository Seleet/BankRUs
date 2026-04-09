using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddUserSecrets<Program>(optional: true);

var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();

var creditScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["19900101-2010"] = 75,
    ["19900202-2020"] = 65,
    ["19900303-3030"] = 35
};

app.MapGet("/api/creditscore", (string ssn) =>
{
    if (string.IsNullOrWhiteSpace(ssn))
    {
        return Results.BadRequest(new { error = "Query param 'ssn' is required." });
    }

    if (!creditScores.TryGetValue(ssn, out var score))
    {
        return Results.NotFound(new { error = "No credit score found for supplied ssn." });
    }

    return Results.Ok(new { score });
});

app.Run();

internal sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("x-api-key", out var providedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var configuredApiKey = configuration["APIKey"];
        if (string.IsNullOrWhiteSpace(configuredApiKey) || providedApiKey != configuredApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next(context);
    }
}
