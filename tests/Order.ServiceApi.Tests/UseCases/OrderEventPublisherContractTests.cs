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
using Aspire.Hosting;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microcks.Aspire.Clients.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Order.ServiceApi.Tests.Fixture;
using Order.ServiceApi.UseCases;
using Order.ServiceApi.UseCases.Model;

namespace Order.ServiceApi.Tests.UseCases;

/// <summary>
/// Contract tests for Kafka event publishing using Aspire distributed application.
/// Validates that events published by the application conform to AsyncAPI specifications.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderEventPublisherContractTests"/> class.
/// </remarks>
/// <param name="fixture">The Aspire factory fixture (shared via collection).</param>
/// <param name="testOutputHelper">The test output helper for logging.</param>
[Collection("DisableParallelization")]
public class OrderEventPublisherContractTests(
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

                // Remove the OrderEventConsumerHostedService for this test
                // We only need to publish, not consume
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ImplementationType == typeof(OrderEventConsumerHostedService));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                });
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
    /// Tests that an order created event is published when an order is placed.
    /// </summary>
    [Fact]
    public async Task PublishesOrderCreatedEvent_WhenOrderIsPlaced()
    {
        // Arrange
        await EnsureTopicExistsAsync("orders-created")
            .ConfigureAwait(true);

        // Prepare a Microcks test request
        var kafkaTest = new TestRequest
        {
            ServiceId = "Order Events API:0.1.0",
            FilteredOperations = ["SUBSCRIBE orders-created"],
            RunnerType = TestRunnerType.ASYNC_API_SCHEMA,
            TestEndpoint = "kafka://kafka:9093/orders-created",
            Timeout = TimeSpan.FromSeconds(5)
        };

        var info = new OrderInfo
        {
            CustomerId = "123-456-789",
            ProductQuantities =
            [
                new ProductQuantity("Millefeuille", 1),
                new ProductQuantity("Eclair Cafe", 1)
            ],
            TotalPrice = 8.4
        };

        // Ensure WebApplicationFactory is initialized
        Assert.NotNull(this.WebApplicationFactory);

        // Get the OrderUseCase from WebApplicationFactory services
        var orderUseCase = this.WebApplicationFactory.Services.GetRequiredService<OrderUseCase>();

        // Create MicrocksClient
        var microcksClient = Fixture.App.CreateMicrocksClient("microcks");

        // Launch the Microcks test and wait a bit to be sure it actually connects to Kafka.
        var testRequestTask = microcksClient.TestEndpointAsync(kafkaTest, TestContext.Current.CancellationToken);
        await Task.Delay(750, TestContext.Current.CancellationToken);

        // Invoke the application to create an order.
        var createdOrder = await orderUseCase.PlaceOrderAsync(
            info,
            TestContext.Current.CancellationToken);

        // Get the Microcks test result.
        var testResult = await testRequestTask;

        // Log the test result for debugging
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _testOutputHelper.WriteLine(json);

        // Assert
        Assert.True(testResult.Success, "Microcks test should succeed.");
        Assert.NotEmpty(testResult.TestCaseResults!);
        Assert.Single(testResult.TestCaseResults![0].TestStepResults!);

        // Check the content of the emitted event, read from Kafka topic.
        var events = await microcksClient.GetEventMessagesForTestCaseAsync(
            testResult, "SUBSCRIBE orders-created", TestContext.Current.CancellationToken);
        Assert.Single(events);

        var message = events[0].EventMessage;
        var messageMap = JsonSerializer.Deserialize<Dictionary<string, object>>(message!.Content!);
        Assert.NotNull(messageMap);
        Assert.True(messageMap.TryGetValue("changeReason", out var changeReason));
        Assert.Equal("Creation", changeReason?.ToString());
        Assert.True(messageMap.TryGetValue("order", out var orderObj));
        var orderElement = (JsonElement)orderObj!;
        var orderDict = JsonSerializer.Deserialize<Dictionary<string, object>>(orderElement.GetRawText());
        Assert.NotNull(orderDict);
        Assert.True(orderDict.TryGetValue("customerId", out var customerId));
        Assert.Equal("123-456-789", customerId?.ToString());
        Assert.True(orderDict.TryGetValue("totalPrice", out var totalPrice));
        Assert.Equal(8.4, double.Parse(totalPrice?.ToString() ?? "0", System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(orderDict.TryGetValue("productQuantities", out var pqObj));
        var pqElement = (JsonElement)pqObj!;
        Assert.Equal(2, pqElement.GetArrayLength());
    }

    private async Task EnsureTopicExistsAsync(string topic)
    {
        if (Fixture.KafkaResource is null)
        {
            throw new InvalidOperationException("Kafka resource is not available.");
        }

        var connectionString = await Fixture.GetKafkaConnectionStringAsync();

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = connectionString
        })
        .Build();

        // Create the topic if it doesn't exist
        var topicMetadata = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(5));
        if (topicMetadata.Topics.Count == 0 || topicMetadata.Topics.All(t => t.Topic != topic))
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);
        }
    }
}
