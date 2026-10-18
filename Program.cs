using SecLog.ETL;
using SecLog.ETL.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
// Добавляем конфигурацию
builder.Services.Configure<ElasticSettings>(builder.Configuration.GetSection("ElasticSettings"));

// Регистрируем наш сервис
builder.Services.AddSingleton<ElasticService>();

var host = builder.Build();
host.Run();
