# Azure Services - Moln Applikation (Migration från lokal-server)
## Beskrivning
- Ett företag har en lokal lösning på huvudkontoret, som nu behöver utökas pga fler bokningar/kunder. 

- De vill nu migrera till en moln-lösning. 

- I dagsläget har de ASP.NET Core Web API med PostgreSQL, på en lokal server, med frontend via en React-applikation. 

### Affärsutmaningar
- 1. Kapacitet och skalbarhet
Systemet riskerar att bli överbelastat när företaget växer till fler anläggningar.
Företaget vill undvika dyra investeringar i egen hårdvara.
Lösningen måste klara perioder med extra hög belastning, till exempel under
högsäsong.

- 2. Säkerhet och GDPR
Företaget måste kunna visa var kunddata lagras och att den skyddas enligt GDPR.
Idag ligger känslig information oskyddad i konfigurationsfiler.
Kontrakt som innehåller personuppgifter är inte tillräckligt säkrade.
Det finns ingen säker funktion för uppladdning av nya dokument.

- 3. Drift och tillgänglighet
Om servern eller internet i huvudkontoret går ner slutar bokningssystemet att
fungera.
Det finns ingen insyn i hur systemet mår eller om något håller på att gå fel.
Backup görs manuellt och är både riskabelt och osäkert.

- 4. Expansion
Kostnaderna ska vara förutsägbara och skalas rimligt när systemet utökas.

### Kravspecifikation från kunden
#### Funktionella Krav
- F1: Migrera ASP.NET Core API från lokal server till molnet

- F2: Deploya API:et så det är tillgängligt online med egen URL

- F3: Flytta databasen till en molnbaserad istället för lokal.

- F4: Implementera säker filuppladdning för nya kontrakt och bilder

- F5: Implementera automatiska email-notifieringar vid bokningar och avbokningar

#### Icke-funktionella Krav
- N1: Systemet ska kunna skalas automatiskt vid ökad belastning

- N2: Lösningen ska klara variationer i belastning, till exempel högsäsonger, utan manuell hantering.

- N3: Åtkomst till känsliga filer och funktioner ska vara skyddad med säker autentisering.

- N4: Personuppgifter ska hanteras i enlighet med GDPR och åtkomst till data ska loggas.

- N5: Systemet ska ha övervakning som identifierar problem innan de påverkar användarna.

- N6: Backuper ska ske automatiskt och det ska vara möjligt att återställa systemet till en specifik tidpunkt.

- N7: Systemhändelser ska loggas centralt för att underlätta felsökning.

- N8: Central hantering av hemligheter/konfiguration (t.ex. Key Vault) för connection strings och API-nycklar. 

### Lösning - Azure Cloud Services
- Azure Resource Group (in region: 'Sweden Central' or 'West Europe') 
    - Azure App Service (PaaS)
    - Azure Database for PostgreSQL and nested DB (PaaS)
    - Azure Blob Storage (PaaS)
    - Azure Application Insights (PaaS)

- Azure Key Vault (PaaS) 
Obs! Azure Key Vault är ej inkluderad, men kan ses över i framtiden. 

## Bygga och köra projektet
### Navigera till projektmappen
```bash
cd .\KonferenscentrumVast\  # För att övriga kommandon skall startar i rätt mapp
```

### Grundläggande arbetsflöde
```bash
dotnet clean    # Rensar tidigare byggen och börjar från scratch
dotnet restore  # Hämtar alla NuGet-paket och beroenden
dotnet build    # Kompilerar koden och kontrollerar fel
dotnet run      # Startar applikationen
```

### Alternativa kommandon
**Kör direkt (inkluderar implicit build):**
```bash
dotnet run      # Kompilerar och startar direkt
```

#### Tips
- Använd 'dotnet clean' om du stöter på konstiga byggfel 'dotnet restore' körs automatiskt vid 'build', men kan köras explicit för felsökning 'dotnet run' startar webbapplikationer som körs tills du stoppar dem med 'Ctrl+C'.
