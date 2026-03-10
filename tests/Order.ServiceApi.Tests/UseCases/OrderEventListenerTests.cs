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

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microcks.Aspire.Async;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Order.ServiceApi.Tests.Fixture;
using Order.ServiceApi.UseCases;
using Order.ServiceApi.UseCases.Model;
using static Awaitility.Awaitility;
using OrderModel = Order.ServiceApi.UseCases.Model.Order;

namespace Order.ServiceApi.Tests.UseCases;

/// <summary>
/// Tests for verifying that order events are consumed and processed correctly.
/// </summary>
/// <param name="fixture">The Aspire factory fixture (shared via collection).</param>
/// <param name="testOutputHelper">The test output helper for logging.</param>
[Collection("DisableParallelization")]
public class OrderEventListenerTests(
    OrderHostAspireFactory fixture,
    ITestOutputHelper testOutputHelper) : IAsyncLifetime, IDisposable
{
    private readonly OrderHostAspireFactory _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Gets or sets the web application factory for testing.
    /// </summary>
    public WebApplicationFactory<Program>? WebApplicationFactory { get; private set; }

    /// <summary>
    /// Gets the fixture with logging configured for this test.
    /// </summary>
    private OrderHostAspireFactory Fixture
    {
        get
        {
            _fixture.OutputHelper = _testOutputHelper;
            return _fixture;
        }
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        // Ensure the fixture OutputHelper is set for logging
        _fixture.OutputHelper = _testOutputHelper;

        // Get Microcks Pastry API mock endpoint
        var pastryApiUrl = Fixture.MicrocksResource
            .GetRestMockEndpoint("API Pastries", "0.0.1")
            .ToString();

        // Get Kafka connection string from shared fixture
        var kafkaConnectionString = await Fixture.GetKafkaConnectionStringAsync();

        // Create WebApplicationFactory for Order.ServiceApi
        this.WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("PastryApi:BaseUrl", pastryApiUrl);

                // Configure Kafka connection if available
                if (!string.IsNullOrEmpty(kafkaConnectionString))
                {
                    builder.UseSetting("ConnectionStrings:kafka", kafkaConnectionString);
                }
            });
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (WebApplicationFactory is not null)
        {
            await WebApplicationFactory.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears the output helper after test completes.
    /// </summary>
    public void Dispose()
    {
        _fixture.OutputHelper = null;
    }

    /// <summary>
    /// Tests that an order event is consumed and processed by the service.
    /// </summary>
    [Fact]
    public async Task TestEventIsConsumedAndProcessedByService()
    {
        // Arrange
        const string expectedOrderId = "123-456-789";
        const string expectedCustomerId = "lbroudoux";
        const int expectedProductCount = 2;

        // Retrieve MicrocksAsyncMinionResource from application
        var appModel = Fixture.App.Services.GetRequiredService<DistributedApplicationModel>();
        var microcksAsyncMinionResource = appModel.Resources
            .OfType<MicrocksAsyncMinionResource>()
            .SingleOrDefault();

        if (microcksAsyncMinionResource is null)
        {
            _testOutputHelper.WriteLine("MicrocksAsyncMinionResource not found, skipping test");
            return;
        }

        // Get the Kafka topic for mock messages
        string kafkaTopic = microcksAsyncMinionResource
            .GetKafkaMockTopic("Order Events API", "0.1.0", "SUBSCRIBE orders-reviewed");

        _testOutputHelper.WriteLine($"Consuming from mock topic: {kafkaTopic}");

        // Ensure WebApplicationFactory is initialized
        Assert.NotNull(this.WebApplicationFactory);

        // Configure the consumer to use the Microcks mock topic
        // We need to reconfigure the WebApplicationFactory with the correct topic
        await this.WebApplicationFactory.DisposeAsync();

        var pastryApiUrl = Fixture.MicrocksResource
            .GetRestMockEndpoint("API Pastries", "0.0.1")
            .ToString();

        var kafkaConnectionString = await Fixture.GetKafkaConnectionStringAsync();

        this.WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("PastryApi:BaseUrl", pastryApiUrl);
                builder.UseSetting("Kafka:OrderEventsTopic", kafkaTopic);

                if (!string.IsNullOrEmpty(kafkaConnectionString))
                {
                    builder.UseSetting("ConnectionStrings:kafka", kafkaConnectionString);
                }
            });

        // Access Services to trigger WebApplicationFactory startup (and HostedService)
        _ = this.WebApplicationFactory.Services;

        // The OrderEventConsumerHostedService is automatically started by WebApplicationFactory
        // Get the OrderUseCase from WebApplicationFactory services
        var orderUseCase = this.WebApplicationFactory.Services.GetRequiredService<OrderUseCase>();
        OrderModel? order = null;

        // Act & Assert - Poll until the order is processed by the HostedService
        try
        {
            Await()
                .AtMost(TimeSpan.FromSeconds(4))
                .PollDelay(TimeSpan.FromMilliseconds(400))
                .PollInterval(TimeSpan.FromMilliseconds(400))
                .Until(() =>
                {
                    try
                    {
                        var retrievedOrder = orderUseCase.GetOrderAsync(expectedOrderId, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
                        if (retrievedOrder != null)
                        {
                            _testOutputHelper.WriteLine($"Order {retrievedOrder.Id} successfully processed!");
                            order = retrievedOrder;
                            return true;
                        }
                        return false;
                    }
                    catch (OrderNotFoundException)
                    {
                        _testOutputHelper.WriteLine($"Order {expectedOrderId} not found yet, continuing to poll...");
                        return false;
                    }
                });

            Assert.NotNull(order);
            // Verify the order properties match expected values
            Assert.Equal(expectedCustomerId, order.CustomerId);
            Assert.Equal(OrderStatus.Validated, order.Status);
            Assert.Equal(expectedProductCount, order.ProductQuantities.Count);
        }
        catch (TimeoutException)
        {
            Assert.Fail("The expected Order was not received/processed in expected delay");
        }
    }
}
