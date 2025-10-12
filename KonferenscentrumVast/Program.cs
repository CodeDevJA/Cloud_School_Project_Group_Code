using KonferenscentrumVast.Data;
using KonferenscentrumVast.Repository.Implementations;
using KonferenscentrumVast.Repository.Interfaces;
using KonferenscentrumVast.Services;  
using KonferenscentrumVast.Exceptions;       
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
// ADD THESE TWO LINES:
using KonferenscentrumVast.Models;
using KonferenscentrumVast.Validation;

// ============================================================================
// EXISTING CODE - No changes needed in this section
// ============================================================================
var builder = WebApplication.CreateBuilder(args);

// Controllers + JSON (optional: guard against reference loops if any entity slips through)
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Konferenscentrum Väst API", Version = "v1" });

    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });

    // COMMENT OUT THESE LINES:
    // var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // c.IncludeXmlComments(xmlPath);
});

// ============================================================================
// NEW CODE - File Upload Service Registrations
// ============================================================================

// Azure Storage Configuration - binds appsettings.json to AzureStorageConfig model
builder.Services.Configure<AzureStorageConfig>(
    builder.Configuration.GetSection("AzureStorage")
);

// File Upload Services Registration
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFileStorageRepository, FileStorageRepository>();
builder.Services.AddScoped<UploadFileValidator>();

// ============================================================================
// EXISTING CODE - No changes needed in this section
// ============================================================================

// Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingContractRepository, BookingContractRepository>();

// Application services
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<FacilityService>();
builder.Services.AddScoped<BookingContractService>();
builder.Services.AddScoped<CustomerService>();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection_Psql")));

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("dev", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
    
    // Add for production
    opt.AddPolicy("default", policy =>
    {
        policy
              .AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In app configuration
builder.Services.AddApplicationInsightsTelemetry();

// ============================================================================
// APPLICATION BUILD - No changes needed in this section
// ============================================================================

var app = builder.Build();

app.MapGet("/", () => "Konferenscentrum Väst API is running");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // add this
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");
}
else
{
    app.UseCors("default"); // Uses default policy
}

app.UseSwagger();
app.UseSwaggerUI(); 
app.UseExceptionMapping();   
app.UseCors("dev");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
