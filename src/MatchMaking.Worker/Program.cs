using MatchMaking.Application.UseCases;
using MatchMaking.Domain.Repositories;
using MatchMaking.Infrastructure.Messaging;
using MatchMaking.Infrastructure.Repositories;
using MatchMaking.Worker.Services;
using Confluent.Kafka;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

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
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = configuration["Kafka:BootstrapServers"],
        ClientId = Environment.MachineName
    };
    return new ProducerBuilder<string, string>(producerConfig).Build();
});

// Register repositories
builder.Services.AddSingleton<IPendingPlayersRepository, RedisPendingPlayersRepository>();
builder.Services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();

// Register use cases
builder.Services.AddScoped(sp =>
{
    var pendingPlayersRepository = sp.GetRequiredService<IPendingPlayersRepository>();
    var playersPerMatch = sp.GetRequiredService<IConfiguration>().GetValue("MatchMaking:PlayersPerMatch", 3);
    return new ProcessMatchRequestUseCase(pendingPlayersRepository, playersPerMatch);
});

builder.Services.AddScoped(sp =>
{
    var pendingPlayersRepository = sp.GetRequiredService<IPendingPlayersRepository>();
    var messagePublisher = sp.GetRequiredService<IMessagePublisher>();
    var playersPerMatch = sp.GetRequiredService<IConfiguration>().GetValue("MatchMaking:PlayersPerMatch", 3);
    return new CreateMatchUseCase(pendingPlayersRepository, messagePublisher, playersPerMatch);
});

// Register worker service
builder.Services.AddHostedService<MatchmakingWorkerService>();

var host = builder.Build();
host.Run();
