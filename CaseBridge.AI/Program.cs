#pragma warning disable SKEXP0070
using CaseBridge_AI.Consumers;
using CaseBridge_AI.Services;
using MassTransit;
using Microsoft.SemanticKernel;
using Minio;
using Qdrant.Client;

namespace CaseBridge_AI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register Services
            builder.Services.AddScoped<IAiChatService, AiChatService>();

            // MinIO Configuration
            builder.Services.AddMinio(configureClient => configureClient
                .WithEndpoint(builder.Configuration["MinIO:Endpoint"])
                .WithCredentials(
                    builder.Configuration["MinIO:AccessKey"],
                    builder.Configuration["MinIO:SecretKey"]
                )
                .WithSSL(false)
                .Build());

            // Qdrant Vector DB Configuration
            
            builder.Services.AddSingleton<QdrantClient>(sp =>
            {
                return new QdrantClient(
                    builder.Configuration["Qdrant:Host"],
                    int.Parse(builder.Configuration["Qdrant:Port"]),
                    bool.Parse(builder.Configuration["Qdrant:Https"])
                );
            });



            // Semantic Kernel Configuration (Google Gemini)
            #pragma warning disable SKEXP0070

            builder.Services.AddKernel().AddGoogleAIGeminiChatCompletion(
               modelId: "models/gemini-2.0-flash",
                apiKey: builder.Configuration["Gemini:ApiKey"]!,
               apiVersion: Microsoft.SemanticKernel.Connectors.Google.GoogleAIVersion.V1_Beta
            ).AddGoogleAIEmbeddingGeneration(
                modelId: builder.Configuration["Gemini:EmbeddingModel"]!,
                apiKey: builder.Configuration["Gemini:ApiKey"]!
            );

            #pragma warning restore SKEXP0070

            // MassTransit (RabbitMQ) Configuration
            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<DocumentUploadedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(builder.Configuration["RabbitMQ:Username"]);
                        h.Password(builder.Configuration["RabbitMQ:Password"]);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Add your React port here
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.UseCors("AllowFrontend");

            app.MapControllers();

            app.Run();
        }
    }
}
