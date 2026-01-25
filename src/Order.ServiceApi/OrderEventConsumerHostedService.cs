//
// Copyright The Microcks Authors.
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//

using Confluent.Kafka;
using Order.ServiceApi.UseCases;

namespace Order.ServiceApi;

/// <summary>
/// Background service that consumes order events from Kafka topic "orders-reviewed".
/// </summary>
public sealed class OrderEventConsumerHostedService : BackgroundService
{
    private readonly ILogger<OrderEventConsumerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IConsumer<string, string> _consumer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderEventConsumerHostedService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public OrderEventConsumerHostedService(
        ILogger<OrderEventConsumerHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        _consumer = serviceProvider.GetRequiredService<IConsumer<string, string>>();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = _configuration.GetValue<string>("Kafka:OrderEventsTopic") ?? "orders-reviewed";
        _consumer.Subscribe(topic);
        _logger.LogInformation("Started consuming from '{Topic}' topic", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var orderEventProcessor = scope.ServiceProvider.GetRequiredService<IOrderEventProcessor>();
                        await orderEventProcessor.ProcessOrderEventAsync(consumeResult.Message.Value, stoppingToken);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Order event consumer was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in order event consumer");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Order event consumer stopped");
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
