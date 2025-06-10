using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

// Clear the default claim mappings FIRST
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    
    // Enhanced debugging events
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"🔵 Received Authorization header: {authHeader?.Substring(0, Math.Min(50, authHeader?.Length ?? 0))}...");
            return Task.CompletedTask;
        },
        
        OnTokenValidated = context =>
        {
            Console.WriteLine("🟢 ✅ TOKEN VALIDATED SUCCESSFULLY!");
            Console.WriteLine("🔍 All claims in validated token:");
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"   📋 {claim.Type} = '{claim.Value}'");
            }
            
            // Specifically check role claims
            var roles = context.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
            Console.WriteLine($"🎭 Roles found: [{string.Join(", ", roles)}]");
            
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"🔴 ❌ AUTHENTICATION FAILED!");
            Console.WriteLine($"🔴 Exception: {context.Exception.Message}");
            Console.WriteLine($"🔴 Exception Type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"🔴 Inner Exception: {context.Exception.InnerException.Message}");
            }
            
            // Fixed: Don't try to set response if already started
            if (!context.Response.HasStarted)
            {
                context.NoResult();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"message\":\"Unauthorized - Token validation failed\"}");
            }
            return Task.CompletedTask;
        },
        
        OnChallenge = context =>
        {
            Console.WriteLine($"🟠 Challenge triggered: {context.Error} - {context.ErrorDescription}");
            // Don't let the default challenge run to avoid response already started error
            context.HandleResponse();
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"message\":\"Unauthorized\"}");
            }
            return Task.CompletedTask;
        }
    };
    
    // Get configuration values
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
    var jwtAudience = builder.Configuration["JwtSettings:Audience"];
    var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
    
    Console.WriteLine("🔧 JWT Configuration:");
    Console.WriteLine($"   Issuer: '{jwtIssuer}'");
    Console.WriteLine($"   Audience: '{jwtAudience}'");
    Console.WriteLine($"   Secret Key Length: {jwtSecret?.Length ?? 0} characters");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        
        ClockSkew = TimeSpan.Zero,
        
        // 🎯 CRITICAL: Map claim types correctly
        RoleClaimType = ClaimTypes.Role,  // Use the full claim type
        NameClaimType = ClaimTypes.Name   // Use the full claim type
    };
});

// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Configure Identity options if needed
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .Build();
});

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 🎯 CORRECT MIDDLEWARE ORDER
// Static files FIRST
app.UseStaticFiles();

// CORS
app.UseCors("AllowAll");

// HTTPS redirection (only once!)
app.UseHttpsRedirection();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Historial Medico");
        options.RoutePrefix = string.Empty;
        options.InjectJavascript("/swagger-ui/custom-auth.js");
    });
    
    app.MapOpenApi();
}

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbContext = scope.ServiceProvider.GetService<HistorialDbContext>();
    context.Database.EnsureCreated();
    dbContext.Database.EnsureCreated();
    DatabaseSeed.Unseed(context, dbContext);
    DatabaseSeed.Seed(context, dbContext);
}

// 🎯 AUTHENTICATION BEFORE AUTHORIZATION
app.UseAuthentication();
app.UseAuthorization();

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
apiV1.MapPost("users/", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
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
// Get all users (Admin only)
apiV1.MapGet("users", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
        IUserService userService,
        UserManager<User> userManager) =>  // Add UserManager
    {
        try
        {
            var users = await userService.GetAllUsersAsync();
        
            var userList = new List<object>();
        
            // Get roles for each user
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    SecondLastName = user.SecondLastName,
                    Roles = roles.ToList()  // ✅ Add roles here
                });
            }

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
apiV1.MapGet("users/{userId}", [Authorize(Roles = "Admin")] async (
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
apiV1.MapPut("users/{userId}", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
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
apiV1.MapDelete("users/{userId}", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
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
apiV1.MapGet("users/me", [Authorize(AuthenticationSchemes = "Bearer")] async (
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