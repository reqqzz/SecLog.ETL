using SecLog.ETL.Models;
using SecLog.ETL.Parsers;
using SecLog.ETL.Services;
using System.Threading.Channels;

namespace SecLog.ETL;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ElasticService _elasticService;
    private readonly Channel<EcsEvent> _channel;

    public Worker(ILogger<Worker> logger, ElasticService elasticService)
    {
        _logger = logger;
        _elasticService = elasticService;
        var options = new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<EcsEvent>(options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SecLog ETL Сервис запущен.");

        // Запускаем чтение и обработку параллельно
        _ = Task.Run(() => ReadLogsAsync(stoppingToken), stoppingToken);
        _ = Task.Run(() => ProcessAndSendAsync(stoppingToken), stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ReadLogsAsync(CancellationToken stoppingToken)
    {
        var samplesPath = Path.Combine(Directory.GetCurrentDirectory(), "samples");

        var parser = new SysmonParser();

        while (!stoppingToken.IsCancellationRequested)
        {
            var files = Directory.GetFiles(samplesPath, "*.xml");

            if (files.Length == 0)
            {
                await Task.Delay(2000, stoppingToken);
                continue;
            }

            foreach (var file in files)
            {
                var xml = await File.ReadAllTextAsync(file, stoppingToken);
                var ecsEvent = parser.Parse(xml);

                if (ecsEvent != null)
                {
                    await _channel.Writer.WriteAsync(ecsEvent, stoppingToken);

                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessAndSendAsync(CancellationToken stoppingToken)
    {
        // Забираем из канала и "отправляем" (пока выводим в консоль)
        await foreach (var logEvent in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _elasticService.AddToBatchAsync(logEvent);
                _logger.LogInformation("Событие добавлено в буфер Elastic.");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Ошибка при отправке в Elasticsearch");
            }
        }
    }
}
