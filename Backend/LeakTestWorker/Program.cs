using LeakTestWorker.MQTT.Clients;
using LeakTestWorker.MQTT.Handlers;
using LeakTestWorker.MQTT.Handlers.Service;
using LeakTestWorker.MQTT.Handlers.SQL;
using LeakTestWorker.MQTT.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LeakTestWorker";
});

builder.Services.AddSingleton<ILeakTestHistoryInsertService, LeakTestHistoryInsertService>();
builder.Services.AddSingleton<ILeakTestMachineHandler, LeakTestHandler>();
builder.Services.AddSingleton<ILeakTestMessageBuffer, FileLeakTestMessageBuffer>();
builder.Services.AddSingleton<ILeakTestMessageHandler, LeakTestMessageRouter>();
builder.Services.AddSingleton<MqttClientService>();
builder.Services.AddSingleton<IMqttClientService>(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());
builder.Services.AddSingleton<IMqttPublisher>(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());

var host = builder.Build();
host.Run();
