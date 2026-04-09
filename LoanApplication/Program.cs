using System.Text.RegularExpressions;
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

builder.Services.Configure<CreditScoreOptions>(builder.Configuration.GetSection("CreditScore"));

builder.Services.AddHttpClient<CreditScoreClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IConfiguration>()
        .GetSection("CreditScore")
        .Get<CreditScoreOptions>();

    if (string.IsNullOrWhiteSpace(options?.Url))
    {
        throw new InvalidOperationException("CreditScore:URL is not configured.");
    }

    client.BaseAddress = new Uri(options.Url);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        var frontendUrl = builder.Configuration["Frontend:URL"] ?? "http://localhost:5173";
        policy.WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.MapPost("/api/loanapplications", async (LoanApplicationRequest request, CreditScoreClient creditScoreClient) =>
{
    var validationError = ValidateRequest(request);
    if (validationError is not null)
    {
        return Results.ValidationProblem(validationError);
    }

    var score = await creditScoreClient.GetCreditScoreAsync(request.SocialSecurityNumber);
    var status = score >= 50 ? "Approved" : "Declined";

    return Results.Created("/api/loanapplications", new { Status = status });
});

app.Run();

static Dictionary<string, string[]>? ValidateRequest(LoanApplicationRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.SocialSecurityNumber))
    {
        errors["socialSecurityNumber"] = ["socialSecurityNumber is required."];
    }
    else if (!Regex.IsMatch(request.SocialSecurityNumber, @"^\d{8}-\d{4}$"))
    {
        errors["socialSecurityNumber"] = ["socialSecurityNumber must match YYYYMMDD-XXXX."];
    }

    if (request.Amount <= 0)
    {
        errors["amount"] = ["amount must be greater than 0."];
    }

    return errors.Count > 0 ? errors : null;
}

internal sealed record LoanApplicationRequest(string SocialSecurityNumber, decimal Amount);

internal sealed class CreditScoreClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<int> GetCreditScoreAsync(string socialSecurityNumber)
    {
        var apiKey = configuration["CreditScore:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("CreditScore:ApiKey is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/creditscore?ssn={Uri.EscapeDataString(socialSecurityNumber)}");

        request.Headers.Add("x-api-key", apiKey);

        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Credit score lookup failed with HTTP {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<CreditScoreResponse>();
        return payload?.Score ?? throw new InvalidOperationException("Credit score response was invalid.");
    }
}

internal sealed class CreditScoreOptions
{
    public string? Url { get; set; }
    public string? ApiKey { get; set; }
}

internal sealed record CreditScoreResponse(int Score);
