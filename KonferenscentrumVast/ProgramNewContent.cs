// using KonferenscentrumVast.Data;
// using KonferenscentrumVast.Repository.Implementations;
// using KonferenscentrumVast.Repository.Interfaces;
// using KonferenscentrumVast.Services;  
// using KonferenscentrumVast.Exceptions;       
// using Microsoft.EntityFrameworkCore;
// using Microsoft.OpenApi.Models;
// using System.Reflection;

// var builder = WebApplication.CreateBuilder(args);


// // Controllers + JSON (optional: guard against reference loops if any entity slips through)
// builder.Services.AddControllers();

// // Swagger/OpenAPI
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Konferenscentrum Väst API", Version = "v1" });

//     c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
//     c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
//     c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });

//     var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//     var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
//     c.IncludeXmlComments(xmlPath);
// });

// // Repositories
// builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
// builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
// builder.Services.AddScoped<IBookingRepository, BookingRepository>();
// builder.Services.AddScoped<IBookingContractRepository, BookingContractRepository>();

// // Application services
// builder.Services.AddScoped<BookingService>();
// builder.Services.AddScoped<FacilityService>();
// builder.Services.AddScoped<BookingContractService>();
// builder.Services.AddScoped<CustomerService>();

// // Database
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddCors(opt =>
// {
//     opt.AddPolicy("dev", policy =>
//     {
//         policy
//             .WithOrigins("http://localhost:3000", "http://localhost:5173")
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });

// var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
//     app.UseDeveloperExceptionPage(); // add this
// }

// app.UseSwagger();
// app.UseSwaggerUI(); // optional: c => { c.RoutePrefix = string.Empty; }



// app.UseExceptionMapping();    // our custom exception -> HTTP mapping
// app.UseHttpsRedirection();
// app.UseCors("dev");           // remove or change if not needed
// app.UseAuthorization();

// app.MapControllers();

// app.Run();

using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KonferenscentrumVast.Data;
using KonferenscentrumVast.Repositories;
using KonferenscentrumVast.Services;
using KonferenscentrumVast.Validation;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;
var env = builder.Environment;

// --- Konfiguration (Configuration) ---
// Förväntar sig följande environment variables / app settings:
// - ConnectionStrings:DefaultConnection (PostgreSQL connection string)
// - AZURE_STORAGE_CONNECTION_STRING (alternativt använd Managed Identity)
// - APPINSIGHTS_CONNECTIONSTRING (Application Insights connection string)

// Add controllers
services.AddControllers();

// Add Application Insights (Monitoring)
services.AddApplicationInsightsTelemetry(); // kommer plocka upp APPINSIGHTS_CONNECTIONSTRING från miljön

// Register DbContext (Entity Framework Core with Npgsql)
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing PostgreSQL connection string (ConnectionStrings:DefaultConnection)");
}

services.AddDbContext<ApplicationDbContext>(options =>
{
    // UseNpgsql requires Npgsql.EntityFrameworkCore.PostgreSQL
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // exempelvis konfiguration av retry, etc.
        npgsqlOptions.EnableRetryOnFailure();
    });
});

// BlobServiceClient registration (Azure Blob Storage)
// Försök använda AZURE_STORAGE_CONNECTION_STRING först, annars DefaultAzureCredential (Managed Identity)
var storageConn = configuration["AZURE_STORAGE_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(storageConn))
{
    services.AddSingleton(new BlobServiceClient(storageConn));
}
else
{
    // DefaultAzureCredential stödjar Managed Identity när app körs i Azure (App Service)
    try
    {
        var accountUrl = configuration["AZURE_STORAGE_ACCOUNT_URL"]; // t.ex. https://{account}.blob.core.windows.net
        if (string.IsNullOrWhiteSpace(accountUrl))
            throw new InvalidOperationException("Missing AZURE_STORAGE_ACCOUNT_URL for DefaultAzureCredential usage.");

        services.AddSingleton(new BlobServiceClient(new Uri(accountUrl), new DefaultAzureCredential()));
    }
    catch (Exception)
    {
        // fallback: låt DI försöka senare eller kasta
        throw;
    }
}

// DI registrations (Service & Repository & Validation)
services.AddScoped<IUploadRepository, UploadRepository>();
services.AddScoped<IUploadService, UploadService>();
services.AddSingleton<UploadValidation>();

// Swagger (Developer experience)
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Logging är förkonfigurerat, Application Insights integrerar med ILogger

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

// Rekommenderat: Aktivera HTTPS redirection i Azure App Service
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Kör migrationer automatiskt (optional, kör med försiktighet i produktion)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
