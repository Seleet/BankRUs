## ÖVNING

# Bank-R-Us

[**Beskrivning	2**](#beskrivning)

[Uppgift 1: Credit Score Service	3](#uppgift-1:-credit-score-service)

[Uppgift 2: Loan Application Service	6](#uppgift-2:-loan-application-service)

[Uppgift 3: Bank-R-Us Frontend	9](#uppgift-3:-bank-r-us-frontend)

[**FAQ	10**](#faq)

[Kan man lösa uppgiften på egen hand	10](#kan-man-lösa-övningen-på-egen-hand)

[Är uppgiften obligatorisk?	10](#är-övningen-obligatorisk?)

[Ska uppgiften lämnas in?	10](#ska-övningen-lämnas-in?)

# Beskrivning {#beskrivning}

I denna valfria övning ska ni sätta upp en lösning för Bank-R-Us som gör det möjligt för deras kunder att ansöka om lån via deras webbsida, som ni ska bygga.

Ni ska bygga följande:

* **Credit Score Service**  
  * Används för att göra kreditupplysning på kund  
  * Hostas i Azure (Azure Web App)  
* **Loan Application Service**  
  * Används för att genomföra en låneansökan  
  * Hostas i Azure (Azure Web App)  
* **bankrus.se**  
  * Webbsida för Bank-R-Us  
  * React-applikation (Azure Static Web App)

| TIPS |
| ----- |
| Använd förslagsvis Live Share för att samarbeta. |

## 

## **Uppgift 1: Credit Score Service** {#uppgift-1:-credit-score-service}

Bank-R-Us har bestämt sig för att bygga en intern kreditupplyssningstjänst som kommer användas av interna system, däribland Loan Application Service (se uppgift 2).

Skapa ett ASP.NET Core Web API-projekt:

| Namn: | CreditScore |
| :---- | :---- |

Implementera följande endpoint:

| GET /api/creditscore?ssn=19900101-2010 |  |
| :---- | :---- |
| **Headers** | x-api-key: \<token\> |
| **Body** |  {   "score": 75 }  |

Score kan vara mellan 0-100. Skapa en lista med 2-3 olika personnummer mappade till olika poäng, exempelvis:

| Personnummer | Poäng |
| :---- | :---- |
| 19900101-2010 | 75 |
| 19900202-2020 | 65 |
| 19900303-3030 | 35 |

Notera HTTP-headern "x-api-key" ovan \- för att få lov att göra ett anrop till API:et behöver man skicka med en API-nyckel.  
Ni bestämmer själva värdet av denna.  
Lägg till följande sektion i appsettings.json, för att dokumentera att applikationen använder ett sådant värde:

| {   ...   "APIKey": null } |
| :---- |

Lagra värdet som en user secret när ni kör lokalt, och sen i Azure Key Vault när ni kör applikationen i Azure.

Skapa ett middleware som kontrollerar att värdet av "x-api-key" är korrekt, i samband med att ett anrop görs. Är värdet inte korrekt ska **401 Unauthorized** skickas tillbaka.

Skapa nödvändiga resurser för att köra lösningen i Azure med Azure Web App. Ni behöver inte använda en databas \- håll information i minnet för att hålla lösningen enkel.

När ni laddat upp applikationen till Azure ska det gå att nå den via följande URL, där ni byter ut "\[unik ID" mot ett unikt ID ni väljer själva:  
[https://creditscore-\[unikt ID\].azurewebsites.net](https://creditscore-[unikt)

Exempelvis:  
[https://creditscore-bankrus-12345.azurewebsites.net](https://creditscore-bankrus-12345.azurewebsites.net)

Det unika ID:et krävs för att ni inte ska "krocka" med andra gruppers lösning.

| NOTERA |
| ----- |
| Den första delen av URL:en kommer att vara namnet på er Web App, exempelvis "creditscore-bankrus-12345" \- tänk på det när ni skapar denna. |

## 

## **Uppgift 2: Loan Application Service** {#uppgift-2:-loan-application-service}

Ni behöver bygga en service som kan hantera låneansökningar.

Skapa ett ASP.NET Core Web API-projekt:

| Namn: | LoanApplication |
| :---- | :---- |

Implementera följande endpoint:

| POST /api/loanapplications |  |
| :---- | :---- |
| **Body** |  {   "socialSecurityNumber": "19900101-2010",   "amount": "1000000" }  |

Följande statuskoder kan returneras:

| 201 Created |  |
| :---- | :---- |
| **Body** |  {   "Status": "Approved" }  Status kan vara "Approved" eller "Declined". Ni väljer själv hur affärsreglerna ska se ut för detta, exempelvis: Credit Score \< 50 och amount \>= 100000 → "Decline" Credit Score \>= 50 och amount \<= 1000000 → "Approve" etc. Vill ni hålla det enkelt, kör med följande affärsregel: Credit Score \< 50 → "Decline" Credit Score \>= 50 → "Approved" |

| 400 Bad Request |  |
| :---- | :---- |
| **Body** |  (Standard felsvar från ASP.NET Core)  Fel ska returneras vid följande scenario: Personnummer är tom (\*) Amount är 0 eller negativt  (\*) Vill ni ta det ett steg längre kan ni lägga till validering av personnummer (YYYYMMDD-XXXX). |

I samband med att en ansökan görs ska en kreditkontroll genomföras \- detta görs genom att anropa Credit Score Service som ni utvecklade i Uppgift 1\.

Detta gör man genom att använda HttpClient (sök på YouTube).

Lägg till följande i appsettings.json:

| {   ...   "CreditScore": {     "URL": "[https://creditscore-\[unikt ID\].](https://creditscore-[unikt)[azurewebsites.net](http://azurewebsites.net)",     "ApiKey": null   } } |
| :---- |

Notera att ni behöver byta ut "\[unikt ID\]" mot ert unika ID.

API-nycklar är hemligheter som inte hör hemma i appsettings.json \- lagra värdet av ApiKey inledningsvis lokalt som en user secret:

| dotnet user-secrets set "CreditScore:ApiKey" "SUPER\_SECRET\_API\_KEY" |
| :---- |

Lagra värdet i Azure Key Vault när ni sen väl kör applikationen i Azure.

Ni behöver lägga till stöd för CORS, så att frontend ([bankrus.se](http://bankrus.se) \- se Uppgift 3\) får lov att interagera med API:et som servicen exponerar.

Skapa nödvändiga resurser i Azure för att hosta servicen:

* Azure App Service Plan  
* Azure Web App  
* Azure Key Vault

Deploya servicen och verifiera att det går att göra en låneansökan, genom att exempelvis använda Postman.

Notera att ni behöver använda ett av de personnummer som hårdkodats i kreditupplysningstjänsten (Credit Score).

## 

## **Uppgift 3: Bank-R-Us Frontend** {#uppgift-3:-bank-r-us-frontend}

Ni ska bygga en React-baserad webbapplikationen som kommer att vara nåbar via [bankrus.se](http://bank-r-us.se).

Använd förslagsvis [Vite](https://vite.dev/guide/) för att generera en React-applikation.

På sajten kommer det finnas ett enkelt formulär som låter kunden ansöka om ett lån, genom att ange personnummer samt belopp man önskar låna.

När man trycker på knappen "Ansök" skickas en POST-förfrågan till Loan Application Service, som kommer svara med en body som innehåller antingen "Approved" eller "Declined".

| NOTERA |
| ----- |
| Ni kommer inte faktiskt använda domännamnet [bankrus.se](http://bank-r-us.se) \- detta är enbart för att göra exemplet mer realistiskt. Er webbapplikation kommer istället få en URL liknande https://kind-moss-0b8bfbd03-preview.westeurope.2.azurestaticapps.net. |

Skapa nödvändiga resurser i Azure:

* Azure Static Web App

# 

# **FAQ** {#faq}

## **Kan man lösa övningen på egen hand** {#kan-man-lösa-övningen-på-egen-hand}

Du kan lösa den i grupp eller på egen hand.

## **Är övningen obligatorisk?** {#är-övningen-obligatorisk?}

Nej

## **Ska övningen lämnas in?** {#ska-övningen-lämnas-in?}

Nej  
