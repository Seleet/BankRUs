# Bank-R-Us: Ultimata pedagogiska guiden

Den här guiden är skriven för att du ska kunna:

1. Förstå **vad** du bygger
2. Förstå **varför** varje del finns
3. Förstå **hur** du felsöker när något blir fel
4. Kunna bygga om allt igen utan att gissa

Målet är inte bara att "få det att funka", utan att du ska kunna förklara lösningen tryggt.

---

## 0) Problemformuleringen (tankesättet innan kod)

### Vad är affärsproblemet?
Bank-R-Us vill låta kunder ansöka om lån via webben.

### Vad krävs tekniskt?
Vi behöver:

- En tjänst som ger kreditscore för personnummer (`CreditScore`)
- En tjänst som hanterar låneansökan (`LoanApplication`)
- En frontend där kund fyller i formuläret (`bankrus-frontend`)

### Varför 3 delar i stället för 1?
Separation av ansvar:

- `CreditScore` gör en sak: kreditupplysning
- `LoanApplication` gör en sak: beslutslogik
- Frontend gör en sak: UI + anrop

Detta gör systemet enklare att underhålla, testa och deploya.

---

## 1) Arkitektur (så allt hänger ihop)

```text
Browser (React)
   -> POST /api/loanapplications (LoanApplication)
       -> GET /api/creditscore?ssn=... + x-api-key (CreditScore)
       <- { score: N }
   <- { Status: Approved/Declined }
```

### Varför API-nyckel på CreditScore?
CreditScore är intern och ska inte vara helt öppen. `x-api-key` är ett enkelt skyddslager.

### Varför Key Vault?
API-nycklar och hemligheter ska inte ligga i kod/appsettings. Key Vault är rätt plats i Azure.

---

## 2) Rekommenderad arbetsordning (viktig!)

Bygg i den har ordningen:

1. `CreditScore` lokalt
2. `LoanApplication` lokalt
3. Frontend lokalt
4. End-to-end test lokalt
5. Deploy API:er till Azure
6. Deploy frontend till Azure Static Web App
7. Koppla CORS + env
8. End-to-end test i Azure

### Varför denna ordning?
Om backend inte fungerar lokalt blir frontend-fel svårtolkade. Vi minskar felsökning med "isolerad verifiering".

---

## 3) Skapa projekten (fran tom mapp)

I `C:\dev5\BankRUs`:

```powershell
dotnet new sln -n BankRUs
dotnet new webapi -n CreditScore
dotnet new webapi -n LoanApplication
dotnet sln BankRUs.sln add .\CreditScore\CreditScore.csproj
dotnet sln BankRUs.sln add .\LoanApplication\LoanApplication.csproj

npm create vite@latest bankrus-frontend -- --template react
cd .\bankrus-frontend
npm install
cd ..
```

---

## 4) Implementera CreditScore (först)

## Krav

- Endpoint: `GET /api/creditscore?ssn=...`
- Header krav: `x-api-key`
- Returnera score 0-100

## Data (in-memory)

Anvand t.ex.:

- `19900101-2010` -> 75
- `19900202-2020` -> 65
- `19900303-3030` -> 35

## Tankesätt

- Om `x-api-key` saknas/fel -> `401 Unauthorized`
- Om personnummer saknas -> `400`
- Om personnummer inte finns i listan -> `404`
- Om allt ar ok -> `200 { score: ... }`

## Konfig

`CreditScore/appsettings.json`:

```json
{
  "APIKey": null,
  "KeyVaultUri": null
}
```

---

## 5) Implementera LoanApplication (sen)

## Krav

- Endpoint: `POST /api/loanapplications`
- Body:

```json
{
  "socialSecurityNumber": "19900101-2010",
  "amount": 100000
}
```

- Validera:
  - personnummer krav + format `YYYYMMDD-XXXX`
  - amount > 0
- Anropa CreditScore med `HttpClient`
- Returnera `201` med `{ "Status": "Approved|Declined" }`

## Affärsregel (enkel)

- score >= 50 => `Approved`
- score < 50 => `Declined`

## Tankesätt

Denna service är "orkestreraren":

- Tar emot request
- Validerar
- Pratar med annan service
- Tar beslut
- Returnerar tydligt svar

## Konfig

`LoanApplication/appsettings.json`:

```json
{
  "CreditScore": {
    "URL": "https://creditscore-[id].azurewebsites.net",
    "ApiKey": null
  },
  "Frontend": {
    "URL": "http://localhost:5173"
  },
  "KeyVaultUri": null
}
```

---

## 6) Implementera frontend (React)

## Funktion

- Form med personnummer + belopp
- Klick på `Ansök`
- POST till LoanApplication
- Visa status/fel

## Viktig detalj

Frontend läser backend-URL från:

- `VITE_LOAN_API_URL`

Saknas den blir det ofta `Failed to fetch`.

`bankrus-frontend/.env.example`:

```env
VITE_LOAN_API_URL=http://localhost:5005
```

Skapa riktig `.env` lokalt:

```powershell
cd .\bankrus-frontend
copy .env.example .env
```

---

## 7) Secrets lokalt (för att slippa hårdkoda nycklar)

```powershell
cd C:\dev5\BankRUs

dotnet user-secrets init --project .\CreditScore\CreditScore.csproj
dotnet user-secrets set "APIKey" "SUPER_SECRET_API_KEY" --project .\CreditScore\CreditScore.csproj

dotnet user-secrets init --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "CreditScore:URL" "http://localhost:5108" --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "CreditScore:ApiKey" "SUPER_SECRET_API_KEY" --project .\LoanApplication\LoanApplication.csproj
dotnet user-secrets set "Frontend:URL" "http://localhost:5173" --project .\LoanApplication\LoanApplication.csproj
```

### Varför user-secrets?
Bra utvecklingshygien: hemligheter utanför repo och kod.

---

## 8) Kör lokalt (3 terminaler)

Terminal A:

```powershell
dotnet run --project .\CreditScore\CreditScore.csproj
```

Terminal B:

```powershell
dotnet run --project .\LoanApplication\LoanApplication.csproj
```

Terminal C:

```powershell
cd .\bankrus-frontend
npm run dev
```

### Lokala testpersonnummer

- `19900101-2010` -> Approved
- `19900202-2020` -> Approved
- `19900303-3030` -> Declined

---

## 9) Vanliga fel lokalt + hur du tänker

## Fel: `Failed to fetch`

Frågor att ställa:

1. Finns `.env`?
2. Är `VITE_LOAN_API_URL` rätt?
3. Startade du om `npm run dev` efter env-ändring?

## Fel: `MSB3021/MSB3027` file locked

Orsak: gammal `dotnet run` process låser exe.
Lösning: stoppa gamla processer och kör igen.

## Fel: `401 Unauthorized` mot CreditScore

Orsak: fel `x-api-key` eller mismatch mellan secrets i tjänsterna.

---

## 10) Azure deploy - övergripande strategi

Vi deployar backend först, frontend sist.

### Backend-resurser

- App Service Plan
- 2 Web Apps
- 2 Key Vaults

### Frontend-resurs

- Azure Static Web App (kopplad till GitHub)

---

## 11) Azure backend - konceptuella steg

1. Skapa Web Apps
2. Slå på Managed Identity på varje app
3. Lägg hemligheter i Key Vault
4. Ge Web App-identiteter roll `Key Vault Secrets User`
5. Sätt appsettings:
   - `KeyVaultUri`
   - `CreditScore__URL` (i LoanApplication)
   - `Frontend__URL` (i LoanApplication)
6. Deploya kod med `dotnet publish` + zip deploy

### Viktig tanke
Om `Invoke-RestMethod` mot Azure-API:erna funkar men frontend inte funkar, då är backend frisk och problemet sitter i frontend deploy/CORS/env.

---

## 12) Azure frontend - konceptuella steg

1. Pusha repo till GitHub
2. Skapa Static Web App fran repo
3. Build config:
   - App location: `bankrus-frontend`
   - Output: `dist`
4. Sätt `VITE_LOAN_API_URL` för production
5. Triggera deploy (commit/push)

### Viktig lärdom
Vite env-vars är **build-time**.  
Ändrar du env i Azure måste du trigga ny build för att frontend ska få nya värdet.

---

## 13) CORS i produktion

`Frontend__URL` i `LoanApplication` måste vara exakt samma origin som din SWA-url.

Exempel:

```powershell
az webapp config appsettings set -g rg-bankrus-mseli -n loanapplication-bankrus-mseli123 --settings Frontend__URL="https://victorious-bush-0b9ed7303.7.azurestaticapps.net"
az webapp restart -g rg-bankrus-mseli -n loanapplication-bankrus-mseli123
```

---

## 14) Slutlig verifiering (production)

I live-frontend:

1. `19900101-2010`, amount `100000` -> `Approved`
2. `19900303-3030`, amount `100000` -> `Declined`

Om detta fungerar är lösningen komplett.

---

## 15) Felsökningsmetod som alltid fungerar

När något är fel, jobba lager för lager:

1. Funkar backend direkt med `Invoke-RestMethod`?
2. Funkar preflight `OPTIONS` med CORS-headers?
3. Vilken `Request URL` visar browsern i Network?
4. Ar frontend byggd med ratt `VITE_LOAN_API_URL`?

### Tumregel

- `Approved` i PowerShell men `Failed to fetch` i browser = oftast frontend env/CORS
- `ERR_CONNECTION_REFUSED` = fel URL eller endpoint inte nattbar
- `401` = auth/API-key mismatch

---

## 16) Kort redovisningsscript (1-2 min)

"Vi byggde tre separata delar: frontend, loan-API och credit-API.  
Frontend skickar låneansökan till LoanApplication, som validerar indata och anropar CreditScore med API-nyckel.  
CreditScore returnerar score, och LoanApplication avgör Approved/Declined enligt regel.  
Nycklar lagras i Key Vault i Azure, inte i kod.  
Frontend är deployad via GitHub Actions till Static Web Apps, och CORS är konfigurerad så endast frontend-origin får anropa backend."

---

## 17) Klart-checklista

- [ ] CreditScore fungerar lokalt
- [ ] LoanApplication fungerar lokalt
- [ ] Frontend fungerar lokalt
- [ ] Båda API:er deployade i Azure
- [ ] Key Vault + Managed Identity fungerar
- [ ] Frontend deployad i SWA
- [ ] `VITE_LOAN_API_URL` satt och ny build körd
- [ ] CORS origin satt till SWA-url
- [ ] Live-test Approved/Declined passerar

---

Du har nu en komplett, professionell och förklarbar lösning.
