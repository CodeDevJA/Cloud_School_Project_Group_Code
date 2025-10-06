# Viktiga konfigurationsnoteringar (så att koden fungerar när du kör / deployar)

- ConnectionStrings: Lägg till ConnectionStrings:DefaultConnection i appsettings.json eller (hellre) i App Service Application Settings som en Connection String. Exempel connection string för Azure Database for PostgreSQL Flexible Server:

```Host=myserver.postgres.database.azure.com;Database=mydb;Username=myuser@myserver;Password=SECRET;Ssl Mode=Require;Trust Server Certificate=False```

(Använd ```Ssl Mode=Require``` och ```Trust Server Certificate=false``` för säker TLS-anslutning.)

- Blob storage konfig:

 - Alternativ 1 (enkelt): Sätt AZURE_STORAGE_CONNECTION_STRING i App Service Application Settings (secret). Då används connection string i Program.cs.

 - Alternativ 2 (säker): Sätt AZURE_STORAGE_ACCOUNT_URL (t.ex. https://mystorageaccount.blob.core.windows.net) och aktivera System-assigned Managed Identity för App Service. Ge identity rollen Storage Blob Data Contributor på container/account. Koden använder DefaultAzureCredential (Azure.Identity) för autentisering.

- Application Insights:

 - Sätt APPINSIGHTS_CONNECTIONSTRING i App Service Application Settings. AddApplicationInsightsTelemetry() plockar upp detta automatiskt.

- App Service Application Settings (security):

 - Som du tidigare sa: du kan inte använda Key Vault med ditt Student-konto. Lösning: lägg känsliga värden (DB password, storage connection string, AppInsights conn string) i App Service Application Settings (Configuration → Application settings / Connection strings). Dessa lagras säkrare än hårdkod i källkoden och exponeras som environment variables.

---

# Testning (kort)

- Testa lokalt med ```dotnet run```. För att testa upload: använd Postman eller Swagger UI:

 - ```POST /api/upload``` → Body form-data, key ```file``` (type file), samt eventuellt ```UploadedBy``` i ett textfält.

- Kontrollera att bloben skapas i Storage-kontot och att en rad sparas i PostgreSQL (tabell ```upload_files```).
