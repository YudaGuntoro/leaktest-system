using MqttBrokerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LeakTestMqttBroker";
});

builder.Services.AddHostedService<MqttBrokerWorker>();

var host = builder.Build();
host.Run();
