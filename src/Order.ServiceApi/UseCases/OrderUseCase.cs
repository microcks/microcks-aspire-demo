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

using Microsoft.Extensions.Logging;
using Order.ServiceApi.Client;
using Order.ServiceApi.Client.Model;
using Order.ServiceApi.UseCases.Model;
using OrderModel = Order.ServiceApi.UseCases.Model.Order;

namespace Order.ServiceApi.UseCases;

/// <summary>
/// Use case for handling order operations.
/// </summary>
public class OrderUseCase
{
    private readonly ILogger<OrderUseCase> _logger;
    private readonly PastryAPIClient _pastryAPIClient;
    private readonly IEventPublisher _eventPublisher;

    private readonly Dictionary<string, List<OrderEvent>> _orderRepository = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderUseCase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="pastryAPIClient">The pastry API client.</param>
    /// <param name="eventPublisher">The event publisher.</param>
    public OrderUseCase(ILogger<OrderUseCase> logger, PastryAPIClient pastryAPIClient, IEventPublisher eventPublisher)
    {
        _logger = logger;
        _pastryAPIClient = pastryAPIClient;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// This method will check that an Order can be actually placed and persisted. A full implementation
    /// will probably check stocks, customer loyalty, payment methods, shipping details, etc... For
    /// sake of simplicity, we'll just check that products (here pastries) are all available.
    /// </summary>
    /// <param name="orderInfo">The order information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A created Order with incoming info, new unique identifier and created status.</returns>
    /// <exception cref="UnavailablePastryException">Thrown when a pastry is unavailable.</exception>
    /// <exception cref="Exception">Thrown for general errors.</exception>
    public async Task<OrderModel> PlaceOrderAsync(OrderInfo orderInfo, CancellationToken cancellationToken)
    {
        // For all products in order, check the availability calling the Pastry API.
        var availabilityTasks = new Dictionary<string, Task<bool>>();

        foreach (var productQuantity in orderInfo.ProductQuantities)
        {
            availabilityTasks[productQuantity.ProductName] = CheckPastryAvailabilityAsync(productQuantity.ProductName, cancellationToken);
        }

        // Wait for all tasks to finish.
        await Task.WhenAll(availabilityTasks.Values);

        // If one pastry is marked as unavailable, throw a business exception.
        foreach (var kvp in availabilityTasks)
        {
            var productName = kvp.Key;
            var isAvailable = await kvp.Value;

            if (!isAvailable)
            {
                throw new UnavailablePastryException(productName, $"Pastry '{productName}' is unavailable.");
            }
        }

        // Everything is available! Create a new order.
        var order = new OrderModel
        {
            CustomerId = orderInfo.CustomerId,
            ProductQuantities = orderInfo.ProductQuantities,
            TotalPrice = orderInfo.TotalPrice
        };

        // Emit OrderEvent for creation
        var orderEvent = new OrderEvent(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            order,
            "Creation"
        );
        // Persist the order event
        PersistOrderEvent(orderEvent);

        await _eventPublisher.PublishOrderCreatedAsync(orderEvent, cancellationToken);

        return order;
    }

    /// <summary>
    /// Persists an order event to the repository.
    /// </summary>
    /// <param name="orderEvent">The order event to persist.</param>
    private void PersistOrderEvent(OrderEvent orderEvent)
    {
        if (_orderRepository.TryGetValue(orderEvent.Order.Id, out var events))
        {
            // Append to existing events
            events.Add(orderEvent);
            return;
        }

        // First event for this order
        _orderRepository.Add(orderEvent.Order.Id, [orderEvent]);
    }

    /// <summary>
    /// Update an order after review.
    /// </summary>
    /// <param name="orderEvent">The order event containing updated order information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateReviewedOrderAsync(OrderEvent orderEvent)
    {
        // Persist the order event
        PersistOrderEvent(orderEvent);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves an order by its ID.
    /// </summary>
    /// <param name="orderId">The order ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order if found.</returns>
    /// <exception cref="OrderNotFoundException">Thrown when the order is not found.</exception>
    public Task<OrderModel> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        List<OrderEvent>? orderEvents = _orderRepository.GetValueOrDefault(orderId);
        if (orderEvents is null || orderEvents.Count == 0)
        {
            throw new OrderNotFoundException(orderId);
        }
        var lastEvent = orderEvents.Last();
        return Task.FromResult(lastEvent.Order);
    }

    private async Task<bool> CheckPastryAvailabilityAsync(string pastryName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking availability for pastry: {PastryName}", pastryName);
            Pastry pastry = await _pastryAPIClient.GetPastryByNameAsync(pastryName, cancellationToken);

            return await Task.FromResult(pastry.IsAvailable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for pastry: {PastryName}", pastryName);
            return false;
        }
    }
}
