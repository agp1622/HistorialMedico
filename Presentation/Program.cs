using System.Collections.Immutable;
using System.Text;
using Core.Entities;
using Infrastructure.Context;
using Presentation.Domain;
using Presentation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Domain.Services;
using User = Core.Entities.User;

var builder = WebApplication.CreateBuilder(args);

var key = "YourSuperSecretKeyHere"u8.ToArray(); // Replace with a secure key

// Services

builder.Services.AddDbContext<HistorialDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HistorialDb")));
builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDb")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<HistorialDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Version = "v1", Title = "Historial Medico API", Description = "Historial Medico",
        });
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();


builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Historial Medico");
        options.RoutePrefix = string.Empty;
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};


// Controllers

app.MapGet("/weatherforecast",
        () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast(
                        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        Random.Shared.Next(-20, 55),
                        summaries[Random.Shared.Next(summaries.Length)]
                    ))
                .ToArray();
            return forecast;
        })
    .WithName("GetWeatherForecast");


// Patients controllers

app.MapGet("/patients",
    async (IPatientService patientService, int pageNumber = 1, int pageSize = 10, int maxPages = 5) =>
    {
        var patients = await patientService.GetPatients(pageNumber, pageSize, maxPages);
        return Results.Ok(patients);
    });

app.MapPost("/patients",
    async (IPatientService patientService, Patient patient) =>
    {
        if (patient == null)
        {
            return Results.BadRequest();
        }
        
        var result = await patientService.CreatePatient(patient); 
        
        return Results.Created($"/patients/{result.Id}", result);
    });


// User Endpoints

app.MapPost("api/auth/login",
    async (LoginModel loginModel,
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IUserService userService) =>
    {
        var user = await userManager.FindByNameAsync(loginModel.Username);
        if (user == null) return Results.Unauthorized();

        var result = await signInManager.PasswordSignInAsync(user, loginModel.Password, false, false);
        if (!result.Succeeded) return Results.Unauthorized();

        var token = userService.GenerateJwtToken(user);
        return Results.Ok(token);
    });

app.MapPost("/api/admin/createUser",
    async (RegisterModel registerModel, UserManager<User> userManager, HttpContext httpContext, IUserService userService) =>
    {
        var result = await userService.CreateUserAsync(registerModel, httpContext.User);

        return result.Succeeded ? Results.Ok("User created successfully") : Results.BadRequest(result.Errors.FirstOrDefault()?.Description);
    });

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}