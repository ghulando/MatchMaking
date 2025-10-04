using MatchMaking.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<MatchmakingWorkerService>();

var host = builder.Build();
host.Run();