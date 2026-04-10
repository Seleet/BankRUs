# Azure Deployment Tutorial (PowerShell)

Denna guide bygger pa ett fungerande flode for att skapa upp dessa resurser i Azure:

- Resource Group: `RGLAB`
- App Service Plan: `ASP-RGLAB-9515` (Free `F1`, `Sweden Central`)
- Static Web App: `bankrus-frontend-mseli23` (Free, `West Europe`)

---

## 1) Forberedelser

Krav:

- Azure CLI installerad
- Inloggning i ratt Azure-konto

Kora i PowerShell:

```powershell
az login
az account list -o table
```

Valj subscription:

```powershell
$SUBSCRIPTION = "<SUBSCRIPTION_NAME_OR_ID>"
az account set --subscription $SUBSCRIPTION
```

---

## 2) Satt variabler

```powershell
$RG = "RGLAB"
$LOCATION_RG = "swedencentral"

$PLAN = "ASP-RGLAB-9515"
$PLAN_LOCATION = "swedencentral"

$SWA = "bankrus-frontend-mseli23"
$SWA_LOCATION = "westeurope"
```

---

## 3) Skapa Resource Group

```powershell
az group create --name $RG --location $LOCATION_RG
```

Vantat resultat:

- `provisioningState`: `Succeeded`

---

## 4) Skapa App Service Plan (F1)

```powershell
az appservice plan create `
  --name $PLAN `
  --resource-group $RG `
  --location $PLAN_LOCATION `
  --sku F1
```

Vantat resultat:

- `name`: `ASP-RGLAB-9515`
- `sku.name`: `F1`
- `status`: `Ready`
- `provisioningState`: `Succeeded`

---

## 5) Skapa Static Web App (Free)

```powershell
az staticwebapp create `
  --name $SWA `
  --resource-group $RG `
  --location $SWA_LOCATION `
  --sku Free
```

Vantat resultat:

- `name`: `bankrus-frontend-mseli23`
- `sku.name`: `Free`
- `defaultHostname`: genereras automatiskt (t.ex. `*.azurestaticapps.net`)

---

## 6) Verifiera deployment

Lista resurser i resource group:

```powershell
az resource list -g $RG -o table
```

Kolla App Service Plan:

```powershell
az appservice plan show `
  --resource-group $RG `
  --name $PLAN `
  --query "{name:name,sku:sku.name,location:location,status:status}" `
  -o table
```

Kolla Static Web App:

```powershell
az staticwebapp show `
  --resource-group $RG `
  --name $SWA `
  --query "{name:name,defaultHostname:defaultHostname,location:location,sku:sku.name}" `
  -o table
```

---

## 7) Radera och repetera (labbflode)

Nar du vill ova om fran noll:

```powershell
az group delete --name $RG --yes --no-wait

do {
    Start-Sleep -Seconds 10
    $exists = az group exists --name $RG
    Write-Host "RG exists: $exists"
} while ($exists -eq "true")

Write-Host "RG ar raderad."
```

Kora sedan steg 3 -> 6 igen.

---

## 8) Vanliga fel

- **Namn redan upptaget (Static Web App)**  
  Prova nytt namn:

  ```powershell
  $SWA = "bankrus-frontend-mseli23-01"
  ```

- **Fel subscription**  
  Kontrollera:

  ```powershell
  az account show -o table
  ```

- **Resource group finns inte vid create/show**  
  Skapa om:

  ```powershell
  az group create --name $RG --location $LOCATION_RG
  ```

---

## 9) Koppla GitHub deployment (nasta steg)

Efter att resurserna ar skapade behover Static Web App kopplas till repo for automatisk deployment.

Portal-vag:

1. Gå till `Static Web Apps` -> `bankrus-frontend-mseli23`
2. Klicka `Set up deployment source` (eller `Deployment` -> `Source`)
3. Välj `GitHub` och logga in
4. Välj:
   - Organization: ditt GitHub-konto/org
   - Repository: repo med frontend-koden
   - Branch: `main` (eller den branch du deployar fran)
5. Sätt build-konfiguration for detta repo:
   - App location: `bankrus-frontend`
   - Api location: (lamna tom)
   - Output location: `dist`
6. Klicka `Review + Create` och skapa kopplingen

---

## 10) Verifiera forsta GitHub deployment

Kontrollera deployment status:

- I Azure Portal under Static Web App -> `Deployment history`, status ska bli `Succeeded`
- I GitHub under `Actions`, workflow-korning ska bli gron

Testa URL:

```powershell
$HOST = az staticwebapp show `
  --resource-group $RG `
  --name $SWA `
  --query "defaultHostname" `
  -o tsv

Write-Host "URL: https://$HOST"
```

Oppna URL:en i browser och verifiera att frontend laddar.

---

## 11) Klart

Nu ar miljo aterstalld och har automatisk deploy via GitHub for Static Web App.

---

## 12) Deploya backend (CreditScore + LoanApplication)

Nu deployar vi backend-tjansterna till samma App Service Plan (`ASP-RGLAB-9515`).

### 12.1 Satt backend-variabler

```powershell
# Backend app-namn maste vara globalt unika
$CREDIT_APP = "creditscore-bankrus-mseli23"
$LOAN_APP = "loanapplication-bankrus-mseli23"

# Enkel API-nyckel mellan LoanApplication -> CreditScore
$CREDIT_API_KEY = "replace-with-strong-key"
```

### 12.2 Skapa tva Web Apps (Windows, .NET 8)

```powershell
az webapp create `
  --resource-group $RG `
  --plan $PLAN `
  --name $CREDIT_APP `
  --runtime "DOTNETCORE:8.0"

az webapp create `
  --resource-group $RG `
  --plan $PLAN `
  --name $LOAN_APP `
  --runtime "DOTNETCORE:8.0"
```

Om din PowerShell tolkar `|` fel eller runtime-flaggan strular kan du skapa apparna utan runtime:

```powershell
az webapp create --resource-group $RG --plan $PLAN --name $CREDIT_APP
az webapp create --resource-group $RG --plan $PLAN --name $LOAN_APP
```

### 12.3 Hamta frontend-URL och bygg backend-URL:er

```powershell
$SWA_HOST = az staticwebapp show `
  --resource-group $RG `
  --name $SWA `
  --query "defaultHostname" `
  -o tsv

$FRONTEND_URL = "https://$SWA_HOST"
$CREDIT_URL = "https://$CREDIT_APP.azurewebsites.net"
$LOAN_URL = "https://$LOAN_APP.azurewebsites.net"

Write-Host "Frontend: $FRONTEND_URL"
Write-Host "CreditScore: $CREDIT_URL"
Write-Host "LoanApplication: $LOAN_URL"
```

### 12.4 Satt app settings (viktigt)

CreditScore:

```powershell
az webapp config appsettings set `
  --resource-group $RG `
  --name $CREDIT_APP `
  --settings `
    APIKey=$CREDIT_API_KEY
```

LoanApplication:

```powershell
az webapp config appsettings set `
  --resource-group $RG `
  --name $LOAN_APP `
  --settings `
    CreditScore__URL=$CREDIT_URL `
    CreditScore__ApiKey=$CREDIT_API_KEY `
    Frontend__URL=$FRONTEND_URL
```

### 12.5 Publish + deploy CreditScore

```powershell
dotnet publish .\CreditScore\CreditScore.csproj -c Release -o .\publish\CreditScore
Compress-Archive -Path .\publish\CreditScore\* -DestinationPath .\publish\CreditScore.zip -Force

az webapp deploy `
  --resource-group $RG `
  --name $CREDIT_APP `
  --src-path .\publish\CreditScore.zip `
  --type zip
```

### 12.6 Publish + deploy LoanApplication

```powershell
dotnet publish .\LoanApplication\LoanApplication.csproj -c Release -o .\publish\LoanApplication
Compress-Archive -Path .\publish\LoanApplication\* -DestinationPath .\publish\LoanApplication.zip -Force

az webapp deploy `
  --resource-group $RG `
  --name $LOAN_APP `
  --src-path .\publish\LoanApplication.zip `
  --type zip
```

### 12.7 Verifiera backend-endpoints

```powershell
Invoke-RestMethod "$CREDIT_URL/api/creditscore?ssn=19900101-2010" -Headers @{ "x-api-key" = $CREDIT_API_KEY }
Invoke-RestMethod "$LOAN_URL/api/loanapplications" -Method Post -ContentType "application/json" -Body '{"socialSecurityNumber":"19900101-2010","amount":1000}'
```

Forvantat:

- CreditScore svarar med `score`
- LoanApplication svarar med `Status` (`Approved`/`Declined`)

---

## 13) Uppdatera frontend till backend-URL

Frontend anvander `VITE_LOAN_API_URL` i builden. Satt den till nya LoanApplication-URL:en.

Fil: `.github/workflows/azure-static-web-apps-*.yml`

```yaml
env:
  VITE_LOAN_API_URL: https://loanapplication-bankrus-mseli23.azurewebsites.net
```

Commit + push till `main` sa byggs Static Web App om med ratt API-URL.

---

## 14) Slutverifiering (end-to-end)

1. Oppna frontend-URL
2. Testa personnummer `19900101-2010` och valfritt belopp
3. Kontrollera att `Failed to fetch` ar borta
4. Kontrollera att du far `Status: Approved` eller `Status: Declined`

---

## 15) Snabbfelsokning om frontend visar "Failed to fetch"

1. Verifiera att frontend byggs med ratt backend-URL i workflow:

```yaml
env:
  VITE_LOAN_API_URL: https://loanapplication-bankrus-mseli23.azurewebsites.net
```

2. Verifiera backend manuellt:

```powershell
Invoke-RestMethod "https://creditscore-bankrus-mseli23.azurewebsites.net/api/creditscore?ssn=19900101-2010" -Headers @{ "x-api-key" = $CREDIT_API_KEY }
Invoke-RestMethod "https://loanapplication-bankrus-mseli23.azurewebsites.net/api/loanapplications" -Method Post -ContentType "application/json" -Body '{"socialSecurityNumber":"19900101-2010","amount":1000}'
```

3. Om backend svarar men frontend inte fungerar: pusha en ny commit sa att Static Web App byggs om med uppdaterad `VITE_LOAN_API_URL`.
