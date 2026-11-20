using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GentleHealersApi.Services;
using GentleHealersApi.Services.Interfaces;
using GentleHealersApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
GentleHealersApi.Extensions.TreatmentCodeServiceExtensions.AddTreatmentCodeImport(builder.Services);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:4173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<IAuthService, AuthService>();
var app = builder.Build();


// using (var scope = app.Services.CreateScope())
// {
//     try
//     {
//         string adminConnectionString = "Server=localhost,1433;Database=GentleHealers;User Id=SA;Password=StrongPassword123;TrustServerCertificate=True;";
        
//         var seeder = new DatabaseSeeder(adminConnectionString);
//         seeder.SeedDatabase();
        
//         Console.WriteLine("Database seeding is succesvol voltooid!");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Fout tijdens het seeden van de database: {ex.Message}");
//     }
// }

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowVueClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
