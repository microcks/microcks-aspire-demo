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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Order.ServiceApi.UseCases.Model;

namespace Order.ServiceApi.UseCases;

/// <summary>
/// Service responsible for processing order events from Kafka messages.
/// </summary>
public sealed class OrderEventProcessor : IOrderEventProcessor
{
    private readonly ILogger<OrderEventProcessor> _logger;
    private readonly OrderUseCase _orderUseCase;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderEventProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="orderUseCase">The order use case.</param>
    public OrderEventProcessor(ILogger<OrderEventProcessor> logger, OrderUseCase orderUseCase)
    {
        _logger = logger;
        _orderUseCase = orderUseCase;
    }

    /// <inheritdoc/>
    public async Task ProcessOrderEventAsync(string messageValue, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing order event: {MessageValue}", messageValue);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var orderEvent = JsonSerializer.Deserialize<OrderEvent>(messageValue, options);
            if (orderEvent == null)
            {
                _logger.LogWarning("Failed to deserialize order event from message: {MessageValue}", messageValue);
                return;
            }

            await _orderUseCase.UpdateReviewedOrderAsync(orderEvent);

            _logger.LogInformation("Successfully processed order event for order ID: {OrderId}", orderEvent.Order.Id);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize order event from message: {MessageValue}", messageValue);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order event: {MessageValue}", messageValue);
            throw;
        }
    }
}
