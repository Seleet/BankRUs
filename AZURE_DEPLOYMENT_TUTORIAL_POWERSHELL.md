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
