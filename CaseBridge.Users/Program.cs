
using CaseBridge_Users.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

namespace CaseBridge.Users
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            //services
            builder.Services.AddSingleton<CaseBridge_Users.Data.DapperContext>();
            builder.Services.AddScoped<CaseBridge_Users.Repositories.UserRepository>();
            builder.Services.AddScoped<TokenService>();
            builder.Services.AddTransient<EmailService>();

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            // Configure Authentication
            builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"]
                    };
                });

            builder.Services.AddSwaggerGen(options =>
            {
                // 1. Set the Swagger UI header info
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CaseBridge_Users API",
                    Version = "v1",
                    Description = "Authentication, Registration, and Firm Hierarchy Management"
                });

                // 2. Define the "Authorize" button and its behavior
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste your JWT Access Token below (Do NOT include 'Bearer ' prefix, just the token)."
                });

                // 3. Ensure the token is sent in the header of every request
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
                    Array.Empty<string>()
                }
              });
                    });

            var app = builder.Build();
            
            //Gloval exception Middleware
            app.UseMiddleware<CaseBridge_Users.Middleware.ExceptionMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors("AllowReactApp");
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
