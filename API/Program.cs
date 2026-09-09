using API.Data;
using API.Extensions;
using API.Filters;
using API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebView", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var conString = builder.Configuration.GetConnectionString("ApplicationDbContext") ??
     throw new InvalidOperationException("Connection string 'DefaultConnection'" +
    " not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(conString));

builder.Services.AddServices()
                .AddValidators();

builder.Services.AddMemoryCache();
builder.Services.AddApiRateLimiting();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SECRET"]!)),

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:ISSUER"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:AUDIENCE"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.HttpContext.Items["token_error"] = "token_expired";
            }
            else
            {
                context.HttpContext.Items["token_error"] = "token_invalid";
            }

            return Task.CompletedTask;
        },

        OnChallenge = async context =>
        {
            // Impede o comportamento default (401 vazio com só o header WWW-Authenticate)
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            string errorCode = context.HttpContext.Items["token_error"] as string ?? "token_missing";

            string title = errorCode switch
            {
                "token_expired" => "O token de acesso expirou.",
                "token_invalid" => "Token de acesso invalido.",
                _ => "Autenticacao necessaria."
            };

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = title,
                Type = $"error:{errorCode}",
                Extensions =
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["traceId"] = context.HttpContext.TraceIdentifier 
                } 
            };
            
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("AllowWebView");

app.UseExceptionHandler(); // Ativar o handler de excecoes

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
