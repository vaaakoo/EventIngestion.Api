using EventIngestion.Api.Application.Services;
using EventIngestion.Api.Domain.Interfaces;
using EventIngestion.Api.Infrastructure;
using EventIngestion.Api.Infrastructure.Repositories;
using EventIngestion.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// 1) Configuration: Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2) DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3) Services & Repositories (DI)
builder.Services.AddScoped<IMappingRuleRepository, MappingRuleRepository>();
builder.Services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<IEventIngestionService, EventIngestionService>();

// Controllers + Newtonsoft.Json
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed default mapping rules
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.MappingRules.Any())
    {
        db.MappingRules.AddRange(
            new EventIngestion.Api.Domain.Entities.MappingRule { ExternalName = "usr", InternalName = "ActorId", UpdatedAt = DateTime.UtcNow },
            new EventIngestion.Api.Domain.Entities.MappingRule { ExternalName = "amt", InternalName = "Amount", UpdatedAt = DateTime.UtcNow },
            new EventIngestion.Api.Domain.Entities.MappingRule { ExternalName = "curr", InternalName = "Currency", UpdatedAt = DateTime.UtcNow },
            new EventIngestion.Api.Domain.Entities.MappingRule { ExternalName = "ts", InternalName = "OccurredAt", UpdatedAt = DateTime.UtcNow },
            new EventIngestion.Api.Domain.Entities.MappingRule { ExternalName = "etype", InternalName = "EventType", UpdatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
