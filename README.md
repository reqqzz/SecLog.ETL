# SecLog ETL (Sysmon to Elasticsearch)

## 📌 Problem Statement
SOC-аналитики получают огромные объёмы логов от разных источников. Сырые события Sysmon в формате XML сложно анализировать и строить по ним запросы.  
**Решение:** этот сервис собирает сырые XML-логи Sysmon, нормализует их в единый стандарт **Elastic Common Schema (ECS)** и отправляет в Elasticsearch, подготавливая данные для SIEM-систем.

## 🏗️ Архитектура
Сервис реализует паттерн **Producer-Consumer** с использованием `System.Threading.Channels`:
1. **Ingestor (Producer):** читает XML-файлы из папки `samples`.
2. **Parser:** маппит поля Sysmon (`Image`, `CommandLine`) в поля стандарта ECS (`process.executable`, `process.command_line`).
3. **Queue (Channel):** буферизирует данные в памяти, предотвращая переполнение.
4. **Elastic Client (Consumer):** накапливает логи в буфер (batching) и отправляет их пачками через Elasticsearch Bulk API.

## 🛠️ Стек технологий
- **Язык:** C# .NET 8 (Worker Service)
- **База данных:** Elasticsearch + Kibana (через Docker)
- **Тестирование:** xUnit
- **Библиотеки:** `System.Threading.Channels`, `System.Text.Json`

## 🚀 Как запустить
1. Поднимите инфраструктуру (Elasticsearch и Kibana):
   ```bash
   cd C:\Users\Admin\source\repos\SecLog.ETL
   docker-compose up -d

Подождите около 30–60 секунд, пока Elasticsearch полностью инициализируется, и откройте в браузере:
👉 http://localhost:9200

Если вы видите JSON с полями "name" : "...", "cluster_name" : "docker-cluster", "version" : { "number" : "8.12.0" ... } — значит, база данных готова принимать логи.
2. Запуск приложения
 F5.
В консоли должно появиться:
text

info: SecLog.ETL.Worker[0]
      SecLog ETL Сервис запущен.
info: SecLog.ETL.Services.ElasticService[0]
      Успешно отправлено 10 логов в Elasticsearch.

4. Проверка данных в Elasticsearch

Как только в консоли появится сообщение об успешной отправке, снова откройте браузер и проверьте индексы:
👉 http://localhost:9200/_cat/indices?v

Вы должны увидеть строку с индексом sec-logs-2026.10.18 (или текущую дату) и количеством документов (docs.count), которое будет расти.
5. Самое интересное: Визуализация в Kibana

    Перейдите в Kibana: 👉 http://localhost:5601

    В левом меню выберите Stack Management (Управление стеком) -> Data Views (Представления данных).