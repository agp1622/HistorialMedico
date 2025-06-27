using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.PatientService;
using Core.Entities;
using Infrastructure;
using Infrastructure.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Presentation.Domain;
using Presentation.Domain.Services;
using Presentation.Services;
using User = Core.Entities.User;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder
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
            
            var roles = context.Principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
            Console.WriteLine($"🎭 Roles found: [{string.Join(", ", roles)}]");
            
            return Task.CompletedTask;
        },
        
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("\ud83d\udd34 \u274c AUTHENTICATION FAILED!");
            Console.WriteLine($"🔴 Exception: {context.Exception.Message}");
            Console.WriteLine($"🔴 Exception Type: {context.Exception.GetType().Name}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"🔴 Inner Exception: {context.Exception.InnerException.Message}");
            }

            if (context.Response.HasStarted) return Task.CompletedTask;
            context.NoResult();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\":\"Unauthorized - Token validation failed\"}");
        },
        
        OnChallenge = context =>
        {
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
        
        RoleClaimType = ClaimTypes.Role, 
        NameClaimType = ClaimTypes.Name   
    };
});

builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
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

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
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
    
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var applicationContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var historialContext = scope.ServiceProvider.GetService<HistorialDbContext>();
    applicationContext.Database.EnsureCreated();
    historialContext.Database.EnsureCreated();
    DatabaseSeed.Unseed(applicationContext, historialContext);
    DatabaseSeed.Seed(applicationContext, historialContext);
}

app.UseAuthentication();
app.UseAuthorization();

var apiV1 = app.MapGroup("/api/v1");

// Controllers
apiV1.MapGet("/weatherforecast",
        () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(_ =>
                    new WeatherForecast(Random.Shared.Next(-20, 55)
                    ))
                .ToArray();
            return forecast;
        })
    .WithName("GetWeatherForecast");


#region Patient Controller

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
    async (IPatientService patientService,  Patient patient) =>
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

apiV1.MapPost("patient/{patientId:guid}/attachments", async (
    Guid patientId,
    IFormFile file,
    IPatientService patientService,
    IWebHostEnvironment environment) =>
{
    try
    {
        var uploadsPath = environment.WebRootPath;
        var attachment = await patientService.AddAttachmentAsync(patientId, file, uploadsPath);

        return Results.Ok(new
        {
            attachment.Id,
            attachment.Name,
            attachment.Size,
            attachment.UploadDate,
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

apiV1.MapGet("patient/{patientId:guid}/attachments", async (
    Guid patientId,
    IPatientService patientService) =>
{
    try
    {
        var attachments = await patientService.GetPatientAttachmentsAsync(patientId);

        var result = attachments.Select(a => new
        {
            a.Id,
            a.Name,
            a.Size,
            a.UploadDate,
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
            attachment.Id,
            attachment.Name,
            attachment.Size,
            attachment.UploadDate,
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

apiV1.MapPost("patient/{patientId:guid}/additionalPhone",
    async (IPatientService patientService, Guid patientId, AdditionalPhone additionalPhone) =>
    {
        if (string.IsNullOrWhiteSpace(additionalPhone.Number))
        {
            return Results.BadRequest("Note content is required");
        }
        
        await patientService.AddAdditionalPhone(additionalPhone, patientId);
        
        var updatedPatient = await patientService.GetPatient(patientId);
        
        return Results.Ok(updatedPatient.AdditionalPhones.FirstOrDefault(x => x.Number == additionalPhone.Number));
    });


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

#endregion

#region User Controller



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

apiV1.MapGet("users", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
        IUserService userService,
        UserManager<User> userManager) =>  // Add UserManager
    {
        try
        {
            var users = await userService.GetAllUsersAsync();
        
            var userList = new List<object>();
        
            foreach (var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    Username = user.UserName,
                    user.Email,
                    user.FullName,
                    user.FirstName,
                    user.LastName,
                    user.MiddleName,
                    user.SecondLastName,
                    Roles = roles.ToList()
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
            user.Id,
            Username = user.UserName,
            user.Email,
            user.FullName,
            user.FirstName,
            user.LastName,
            user.MiddleName,
            user.SecondLastName
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

apiV1.MapPut("users/{userId}", [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")] async (
    string userId,
    UpdateModel updateModel,
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
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.FirstName,
            user.LastName,
            user.MiddleName,
            user.SecondLastName,
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

#endregion

app.Run();

record WeatherForecast(int TemperatureC)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}