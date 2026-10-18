using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SecLog.ETL.Models;

namespace SecLog.ETL.Services;

public class ElasticSettings
{
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "sec-logs";
    public int BatchSize { get; set; } = 10;
    public int FlushIntervalSeconds { get; set; } = 5;
}

public class ElasticService
{
    private readonly HttpClient _httpClient;
    private readonly ElasticSettings _settings;
    private readonly ILogger<ElasticService> _logger;
    private readonly List<EcsEvent> _buffer = new();
    private readonly object _lock = new();
    private DateTime _lastFlush = DateTime.UtcNow;

    public ElasticService(IOptions<ElasticSettings> settings, ILogger<ElasticService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = new HttpClient { BaseAddress = new Uri(_settings.Uri) };
    }

    public async Task AddToBatchAsync(EcsEvent logEvent)
    {
        lock (_lock)
        {
            _buffer.Add(logEvent);
        }

        // Если буфер заполнен или прошло много времени — отправляем
        if (_buffer.Count >= _settings.BatchSize ||
            (DateTime.UtcNow - _lastFlush).TotalSeconds >= _settings.FlushIntervalSeconds)
        {
            await FlushAsync();
        }
    }

    public async Task FlushAsync()
    {
        List<EcsEvent> eventsToSend;
        lock (_lock)
        {
            if (_buffer.Count == 0) return;
            eventsToSend = new List<EcsEvent>(_buffer);
            _buffer.Clear();
            _lastFlush = DateTime.UtcNow;
        }

        var bulkData = new StringBuilder();
        var indexDate = DateTime.UtcNow.ToString("yyyy.MM.dd"); // Индексы по дням

        foreach (var ev in eventsToSend)
        {
            // Метаданные для Elastic (куда и с каким ID писать)
            bulkData.AppendLine($"{{\"index\": {{\"_index\": \"{_settings.IndexName}-{indexDate}\"}}}}");

            // Сами данные
            var json = JsonSerializer.Serialize(ev);
            bulkData.AppendLine(json);
        }

        var content = new StringContent(bulkData.ToString(), Encoding.UTF8, "application/x-ndjson");

        try
        {
            var response = await _httpClient.PostAsync("/_bulk", content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Успешно отправлено {Count} логов в Elasticsearch.", eventsToSend.Count);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ошибка Elasticsearch: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключиться к Elasticsearch. Убедитесь, что он запущен.");
        }
    }
}