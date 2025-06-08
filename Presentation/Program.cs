using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Application.PatientService;
using Core.Entities;
using Infrastructure;
using Infrastructure.Context;
using Presentation.Domain;
using Presentation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Domain.Services;
using User = Core.Entities.User;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


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
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
        };
    });

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Version = "v1", Title = "Historial Medico API", Description = "Historial Medico",
        });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB limit
});

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseCors("AllowAll");
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Historial Medico");
        options.RoutePrefix = string.Empty;
        
        options.InjectJavascript("/swagger-ui/custom-auth.js");
    });
    
    // Configure the HTTP request pipeline.
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbContext = scope.ServiceProvider.GetService<HistorialDbContext>();
    context.Database.EnsureCreated();
    dbContext.Database.EnsureCreated();
    DatabaseSeed.Unseed(context, dbContext);
    DatabaseSeed.Seed(context, dbContext);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// API Endpoints

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var apiV1 = app.MapGroup("/api/v1");


// Controllers

apiV1.MapGet("/weatherforecast",
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

apiV1.MapGet("/patients", 
    async (IPatientService patientService, int pageNumber = 1, int pageSize = 10, int maxPages = 5) =>
    {
        var patients = await patientService.GetPatients(pageNumber, pageSize, maxPages);
        return Results.Ok(patients);
    });

apiV1.MapGet("/patient", 
    async (IPatientService patientService, string id ) =>
    {
        
        
        var patient = await patientService.GetPatient(Guid.Parse(id));
        return Results.Ok(patient);
    });

apiV1.MapPost("/patients", 
    async (IPatientService patientService, Patient patient) =>
    {
        if (patient == null)
        {
            return Results.BadRequest();
        }
        
        var result = await patientService.CreatePatient(patient); 
        
        return Results.Created($"/patients/{result.Id}", result);
    });

apiV1.MapPut("/patients",
     async (IPatientService patientService, [FromBody] Patient patient, Guid id) =>
    {
        var updatedPatient = await patientService.UpdatePatient(patient, id);

        return Results.Ok(updatedPatient);
    });

apiV1.MapDelete("/patient",
    async (IPatientService patientService, [FromQuery] Guid id) =>
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest("Patient id is required.");
        }
        
        var deleted= await patientService.DeletePatient(id);

        return deleted ? Results.NotFound() : Results.NoContent();
    });

apiV1.MapPost("patient/{patientId:guid}/history",
    async (IPatientService patientService, Guid patientId, MedicalHistory history) =>
    {
        if (string.IsNullOrWhiteSpace(history.Nota))
        {
            return Results.BadRequest("Note content is required");
        }
        
        await patientService.AddMedicalHistory(history, patientId);
        
        var updatedPatient = await patientService.GetPatient(patientId);
        return Results.Ok(updatedPatient);
    });

// Attachment Minimal API Endpoints using PatientService

// Upload attachment
apiV1.MapPost("patient/{patientId:guid}/attachments", async (
    Guid patientId,
    IFormFile file,
    IPatientService patientService,
    IWebHostEnvironment environment) =>
{
    try
    {
        var uploadsPath = environment.WebRootPath ?? environment.ContentRootPath;
        var attachment = await patientService.AddAttachmentAsync(patientId, file, uploadsPath);

        return Results.Ok(new
        {
            Id = attachment.Id,
            Name = attachment.Name,
            Size = attachment.Size,
            UploadDate = attachment.UploadDate,
            DownloadUrl = $"/api/patient/{patientId}/attachments/{attachment.Id}/download"
        });
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error uploading file: {ex.Message}");
    }
})
.WithName("UploadAttachment")
.WithTags("Attachments")
.WithOpenApi()
.DisableAntiforgery();

// Get all attachments for a patient
apiV1.MapGet("patient/{patientId:guid}/attachments", async (
    Guid patientId,
    IPatientService patientService) =>
{
    try
    {
        var attachments = await patientService.GetPatientAttachmentsAsync(patientId);

        var result = attachments.Select(a => new
        {
            Id = a.Id,
            Name = a.Name,
            Size = a.Size,
            UploadDate = a.UploadDate,
            DownloadUrl = $"/api/patient/{patientId}/attachments/{a.Id}/download"
        });

        return Results.Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving attachments: {ex.Message}");
    }
})
.WithName("GetPatientAttachments")
.WithTags("Attachments")
.WithOpenApi();

// Download attachment
apiV1.MapGet("patient/{patientId:guid}/attachments/{attachmentId:guid}/download", async (
    Guid patientId,
    Guid attachmentId,
    IPatientService patientService) =>
{
    try
    {
        var fileResult = await patientService.GetAttachmentFileAsync(patientId, attachmentId);

        if (fileResult == null)
        {
            return Results.NotFound("Attachment not found or file not accessible");
        }

        var (fileData, contentType, fileName) = fileResult.Value;
        return Results.File(fileData, contentType, fileName);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error downloading file: {ex.Message}");
    }
})
.WithName("DownloadAttachment")
.WithTags("Attachments")
.WithOpenApi();

// Delete attachment
apiV1.MapDelete("patient/{patientId:guid}/attachments/{attachmentId:guid}", async (
    Guid patientId,
    Guid attachmentId,
    IPatientService patientService) =>
{
    try
    {
        var deleted = await patientService.DeleteAttachmentAsync(patientId, attachmentId);

        if (!deleted)
        {
            return Results.NotFound("Attachment not found");
        }

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error deleting attachment: {ex.Message}");
    }
})
.WithName("DeleteAttachment")
.WithTags("Attachments")
.WithOpenApi();

// Get attachment info (metadata only)
apiV1.MapGet("patient/{patientId:guid}/attachments/{attachmentId:guid}", async (
    Guid patientId,
    Guid attachmentId,
    IPatientService patientService) =>
{
    try
    {
        var attachment = await patientService.GetAttachmentAsync(patientId, attachmentId);

        if (attachment == null)
        {
            return Results.NotFound("Attachment not found");
        }

        return Results.Ok(new
        {
            Id = attachment.Id,
            Name = attachment.Name,
            Size = attachment.Size,
            UploadDate = attachment.UploadDate,
            DownloadUrl = $"/api/patient/{patientId}/attachments/{attachment.Id}/download"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving attachment info: {ex.Message}");
    }
})
.WithName("GetAttachmentInfo")
.WithTags("Attachments")
.WithOpenApi();


// Helper method for validation
static bool IsValidModel<T>(T model, out List<string> errors)
{
    errors = new List<string>();
    var context = new ValidationContext(model);
    var results = new List<ValidationResult>();
    
    if (!Validator.TryValidateObject(model, context, results, true))
    {
        errors = results.Select(r => r.ErrorMessage ?? "Validation error").ToList();
        return false;
    }
    return true;
}

// Login endpoint
apiV1.MapPost("auth/login", async (
    LoginModel loginModel,
    IUserService userService) =>
{
    try
    {
        if (!IsValidModel(loginModel, out var errors))
        {
            return Results.BadRequest(new { message = "Invalid login data", errors });
        }

        var result = await userService.LoginAsync(loginModel);
        
        if (result == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error during login: {ex.Message}");
    }
})
.WithName("Login")
.WithOpenApi();

// Create admin user (for initial setup)
apiV1.MapPost("auth/create-admin", async (
    RegisterModel registerModel,
    IUserService userService) =>
{
    try
    {
        if (!IsValidModel(registerModel, out var errors))
        {
            return Results.BadRequest(new { message = "Invalid registration data", errors });
        }

        var result = await userService.CreateAdminUserAsync(registerModel);

        if (result.Succeeded)
        {
            return Results.Ok(new { message = "Admin user created successfully" });
        }

        return Results.BadRequest(new { 
            message = "Failed to create admin user",
            errors = result.Errors.Select(e => e.Description)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating admin user: {ex.Message}");
    }
})
.WithName("CreateAdmin")
.WithOpenApi();

// Create user (Admin only)
apiV1.MapPost("users/", async (
    RegisterModel registerModel,
    IUserService userService,
    HttpContext httpContext) =>
{
    try
    {
        if (!IsValidModel(registerModel, out var errors))
        {
            return Results.BadRequest(new { message = "Invalid user data", errors });
        }

        var result = await userService.CreateUserAsync(registerModel, httpContext.User);

        if (result.Succeeded)
        {
            return Results.Ok(new { message = "User created successfully" });
        }

        return Results.BadRequest(new {
            message = "Failed to create user",
            errors = result.Errors.Select(e => e.Description)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating user: {ex.Message}");
    }
})
.WithName("CreateUser")
.WithOpenApi();

// Get all users (Admin only)
apiV1.MapGet("users", async (
    IUserService userService) =>
{
    try
    {
        var users = await userService.GetAllUsersAsync();
        
        var userList = users.Select(u => new
        {
            Id = u.Id,
            Username = u.UserName,
            Email = u.Email,
            FullName = u.FullName,
            FirstName = u.FirstName,
            LastName = u.LastName,
            MiddleName = u.MiddleName,
            SecondLastName = u.SecondLastName
        });

        return Results.Ok(userList);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving users: {ex.Message}");
    }
})
.WithName("GetAllUsers")
.WithOpenApi();

// Get user by ID (Admin only)
apiV1.MapGet("users/{userId}",  async (
    string userId,
    IUserService userService) =>
{
    try
    {
        var user = await userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return Results.NotFound(new { message = "User not found" });
        }

        var userInfo = new
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            SecondLastName = user.SecondLastName
        };

        return Results.Ok(userInfo);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving user: {ex.Message}");
    }
})
.WithName("GetUserById")
.WithOpenApi();

// Update user (Admin only)
apiV1.MapPut("users/{userId}", async (
    string userId,
    RegisterModel updateModel,
    IUserService userService) =>
{
    try
    {
        if (!IsValidModel(updateModel, out var errors))
        {
            return Results.BadRequest(new { message = "Invalid user data", errors });
        }

        var result = await userService.UpdateUserAsync(userId, updateModel);

        if (result.Succeeded)
        {
            return Results.Ok(new { message = "User updated successfully" });
        }

        return Results.BadRequest(new {
            message = "Failed to update user",
            errors = result.Errors.Select(e => e.Description)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error updating user: {ex.Message}");
    }
})
.WithName("UpdateUser")
.WithOpenApi();

// Delete user (Admin only)
apiV1.MapDelete("users/{userId}", async (
    string userId,
    IUserService userService) =>
{
    try
    {
        var result = await userService.DeleteUserAsync(userId);

        if (result.Succeeded)
        {
            return Results.Ok(new { message = "User deleted successfully" });
        }

        return Results.BadRequest(new {
            message = "Failed to delete user",
            errors = result.Errors.Select(e => e.Description)
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error deleting user: {ex.Message}");
    }
})
.WithName("DeleteUser")
.WithOpenApi();

// Get current user info (Authenticated users)
apiV1.MapGet("users/me", async (
    HttpContext httpContext,
    UserManager<User> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);

        if (user == null)
        {
            return Results.NotFound(new { message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);

        var userInfo = new
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            SecondLastName = user.SecondLastName,
            Roles = roles
        };

        return Results.Ok(userInfo);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error retrieving current user: {ex.Message}");
    }
})
.WithName("GetCurrentUser")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}