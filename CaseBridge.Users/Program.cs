
using CaseBridge_Users.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

            var app = builder.Build();
            
            //Gloval exception Middleware
            app.UseMiddleware<CaseBridge_Users.Middleware.ExceptionMiddleware>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
