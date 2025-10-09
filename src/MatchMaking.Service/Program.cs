using MatchMaking.Application.UseCases;
using MatchMaking.Domain.Repositories;
using MatchMaking.Infrastructure.Messaging;
using MatchMaking.Infrastructure.Repositories;
using MatchMaking.Service.Services;
using Confluent.Kafka;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis
builder.Services.AddSingleton<IDatabase>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var redisConnectionString = configuration["Redis:ConnectionString"];
    var redis = ConnectionMultiplexer.Connect(redisConnectionString!);
    return redis.GetDatabase();
});

// Configure Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var kafkaConfig = new ProducerConfig
    {
        BootstrapServers = configuration["Kafka:BootstrapServers"],
        ClientId = "matchmaking-service"
    };
    return new ProducerBuilder<string, string>(kafkaConfig).Build();
});

// Register repositories
builder.Services.AddSingleton<IMatchRepository, RedisMatchRepository>();
builder.Services.AddSingleton<IRateLimitRepository, RedisRateLimitRepository>();
builder.Services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();

// Register use cases
builder.Services.AddScoped(sp =>
{
    var rateLimitRepository = sp.GetRequiredService<IRateLimitRepository>();
    var messagePublisher = sp.GetRequiredService<IMessagePublisher>();
    var rateLimitWindowMs = sp.GetRequiredService<IConfiguration>().GetValue("RateLimit:WindowMs", 100);
    return new RequestMatchUseCase(rateLimitRepository, messagePublisher, rateLimitWindowMs);
});
builder.Services.AddScoped<GetMatchInfoUseCase>();
builder.Services.AddScoped<StoreMatchInfoUseCase>();

// Add background service for consuming match completions
builder.Services.AddHostedService<MatchCompletionConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

app.Run();
