using LeakTestWorker.MQTT.Clients;
using LeakTestWorker.MQTT.Handlers;
using LeakTestWorker.MQTT.Handlers.Service;
using LeakTestWorker.MQTT.Handlers.SQL;
using LeakTestWorker.MQTT.Interfaces;
using LeakTestWorker.Logging;

var builder = Host.CreateApplicationBuilder(args);
var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LeakTestWorker";
});

builder.Logging.AddProvider(new FileLoggerProvider(logDirectory));

builder.Services.AddSingleton<ILeakTestHistoryInsertService, LeakTestHistoryInsertService>();
builder.Services.AddSingleton<ILeakTestMachineHandler, LeakTestHandler>();
builder.Services.AddSingleton<ILeakTestMessageBuffer, FileLeakTestMessageBuffer>();
builder.Services.AddSingleton<ILeakTestMessageHandler, LeakTestMessageRouter>();
builder.Services.AddSingleton<MqttClientService>();
builder.Services.AddSingleton<IMqttClientService>(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());
builder.Services.AddSingleton<IMqttPublisher>(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<MqttClientService>());

var host = builder.Build();
host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("LeakTestWorker.Startup")
    .LogInformation("LeakTestWorker starting. LogDirectory={LogDirectory}", logDirectory);
host.Run();
