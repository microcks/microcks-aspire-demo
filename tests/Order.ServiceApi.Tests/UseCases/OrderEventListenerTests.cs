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
using Aspire.Hosting.Testing;
using Microcks.Aspire;
using Microcks.Aspire.Async;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.ServiceApi.UseCases;
using Order.ServiceApi.UseCases.Model;
using Projects;
using static Awaitility.Awaitility;
using OrderModel = Order.ServiceApi.UseCases.Model.Order;

namespace Order.ServiceApi.Tests.UseCases;

/// <summary>
/// Tests for verifying that order events are consumed and processed correctly.
/// </summary>
[Collection("DisableParallelization")]
public class OrderEventListenerTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _testOutputHelper;
    private DistributedApplication? _app;
    private MicrocksResource? _microcksResource;
    private MicrocksAsyncMinionResource? _microcksAsyncMinionResource;
    private KafkaServerResource? _kafkaResource;

    /// <summary>
    /// Gets or sets the web application factory for testing.
    /// </summary>
    public WebApplicationFactory<Program>? WebApplicationFactory { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderEventListenerTests"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper for logging.</param>
    public OrderEventListenerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        // Create and start the distributed application
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Order_AppHost>(TestContext.Current.CancellationToken);

        // Enable resource logging
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
            logging.AddXUnit(_testOutputHelper, options =>
            {
                options.TimestampFormat = "HH:mm:ss.fff ";
                options.IncludeScopes = false;
            });
        });

        builder.Services.Configure<DistributedApplicationOptions>(options =>
        {
            options.DisableDashboard = true;
        });

        _microcksResource = builder.Resources.OfType<MicrocksResource>().Single();
        _microcksAsyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().SingleOrDefault();
        _kafkaResource = builder.Resources.OfType<KafkaServerResource>().SingleOrDefault();

        _app = await builder.BuildAsync(TestContext.Current.CancellationToken);
        await _app.StartAsync(TestContext.Current.CancellationToken);

        // Wait for resources readiness
        if (_kafkaResource is not null)
        {
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                _kafkaResource.Name, TestContext.Current.CancellationToken);
        }

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            _microcksResource.Name, TestContext.Current.CancellationToken);

        if (_microcksAsyncMinionResource is not null)
        {
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(
                _microcksAsyncMinionResource.Name, TestContext.Current.CancellationToken);
        }

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            "order-api", TestContext.Current.CancellationToken);

        // Get Microcks Pastry API mock endpoint
        var pastryApiUrl = _microcksResource
            .GetRestMockEndpoint("API Pastries", "0.0.1")
            .ToString();

        // Get Kafka connection string from Aspire resources
        string? kafkaConnectionString = null;
        if (_kafkaResource is not null)
        {
            kafkaConnectionString = await _kafkaResource.ConnectionStringExpression
                .GetValueAsync(TestContext.Current.CancellationToken);
        }

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

        if (_app is not null)
        {
            try
            {
                await _app.StopAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                // swallow, we're tearing down tests
            }

            await _app.DisposeAsync();
        }
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
        var appModel = _app!.Services.GetRequiredService<DistributedApplicationModel>();
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

        var pastryApiUrl = _microcksResource!
            .GetRestMockEndpoint("API Pastries", "0.0.1")
            .ToString();

        string? kafkaConnectionString = null;
        if (_kafkaResource is not null)
        {
            kafkaConnectionString = await _kafkaResource.ConnectionStringExpression
                .GetValueAsync(TestContext.Current.CancellationToken);
        }

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
