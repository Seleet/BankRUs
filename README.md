# Bank-R-Us - Full lösning

Det här repot innehåller en komplett implementation av övningen:

- `CreditScore` - ASP.NET Core Web API
- `LoanApplication` - ASP.NET Core Web API
- `bankrus-frontend` - React + Vite

## 1) Credit Score Service

Endpoint:

- `GET /api/creditscore?ssn=19900101-2010`
- Kräver header: `x-api-key: <token>`

Regler:

- Returnerar score (0-100) från in-memory dictionary.
- Returnerar `401 Unauthorized` om API-nyckeln saknas/fel.
- Returnerar `404` om personnummer inte finns.

Konfiguration (`CreditScore/appsettings.json`):

- `APIKey` (null i fil, läggs som secret/Key Vault)
- `KeyVaultUri` (valfri, används i Azure)

## 2) Loan Application Service

Endpoint:

- `POST /api/loanapplications`

Body-exempel:

```json
{
  "socialSecurityNumber": "19900101-2010",
  "amount": 100000
}
```

Regler:

- Validerar:
  - personnummer måste finnas och matcha `YYYYMMDD-XXXX`
  - amount måste vara > 0
- Anropar `CreditScore` via `HttpClient`.
- Beslut:
  - `Credit Score >= 50` -> `Approved`
  - `Credit Score < 50` -> `Declined`
- Svar: `201 Created` med body `{ "Status": "Approved|Declined" }`
- CORS aktiverat för frontend URL från config.

Konfiguration (`LoanApplication/appsettings.json`):

- `CreditScore:URL`
- `CreditScore:ApiKey` (null i fil, läggs som secret/Key Vault)
- `Frontend:URL` (för CORS)
- `KeyVaultUri` (valfri, används i Azure)

## 3) Frontend (React)

Frontend finns i `bankrus-frontend`.

- Form för personnummer + belopp
- Skickar POST till `LoanApplication`
- Visar beslut (`Approved`/`Declined`) eller valideringsfel

Miljövariabel:

- Kopiera `.env.example` till `.env`
- Sätt `VITE_LOAN_API_URL` till URL för `LoanApplication`

## Köra lokalt - steg för steg

Öppna tre terminaler.

### A. Init user secrets (en gång)

```powershell
dotnet user-secrets init --project .\CreditScore\CreditScore.csproj
dotnet user-secrets set "APIKey" "SUPER_SECRET_API_KEY" --project .\CreditScore\CreditScore.csproj

dotnet user-secrets init --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "CreditScore:URL" "https://localhost:7000" --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "CreditScore:ApiKey" "SUPER_SECRET_API_KEY" --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "Frontend:URL" "http://localhost:5173" --project .\LoanApplication\LoanApplication.csproj
```

> Om portarna skiljer sig hos dig, använd rätt HTTPS-port från respektive `launchSettings.json`.

### B. Starta API 1 (CreditScore)

```powershell
dotnet run --project .\CreditScore\CreditScore.csproj
```

### C. Starta API 2 (LoanApplication)

```powershell
dotnet run --project .\LoanApplication\LoanApplication.csproj
```

### D. Starta frontend

```powershell
cd .\bankrus-frontend
copy .env.example .env
npm install
npm run dev
```

## Snabbtest med curl

Testa CreditScore:

```powershell
curl "https://localhost:7000/api/creditscore?ssn=19900101-2010" -H "x-api-key: SUPER_SECRET_API_KEY" -k
```

Testa LoanApplication:

```powershell
curl "https://localhost:7001/api/loanapplications" -H "Content-Type: application/json" -d "{\"socialSecurityNumber\":\"19900101-2010\",\"amount\":100000}" -k
```

## Azure (checklista)

För båda API:erna:

1. Skapa `App Service Plan` + `Web App`.
2. Skapa `Key Vault`.
3. Lägg in hemligheter i Key Vault:
   - CreditScore: `APIKey`
   - LoanApplication: `CreditScore--ApiKey` (eller motsvarande naming strategy)
4. Sätt `KeyVaultUri` i app settings.
5. Ge Web App Managed Identity rättighet att läsa secrets.
6. Deploya respektive API.

För frontend:

1. Skapa `Azure Static Web App`.
2. Sätt `VITE_LOAN_API_URL` till din deployade `LoanApplication`-URL.
3. Deploya frontend.

## Tips inför redovisning / reverse engineering

Fokusera på detta flöde:

1. Frontend skickar låneansökan.
2. LoanApplication validerar input.
3. LoanApplication anropar CreditScore med API-key.
4. CreditScore middleware godkänner/blockerar request.
5. LoanApplication beslutar Approved/Declined och returnerar till frontend.
