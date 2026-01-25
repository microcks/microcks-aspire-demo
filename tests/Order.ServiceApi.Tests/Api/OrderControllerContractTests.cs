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

using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Text.Json;
using Microcks.Aspire.Clients.Model;
using Order.ServiceApi.Tests.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Order.ServiceApi.Tests.Api;

/// <summary>
/// Contract tests for the Order API using Microcks.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderControllerContractTests"/> class.
/// </remarks>
/// <param name="fixture">The Aspire factory fixture (shared via collection).</param>
/// <param name="testOutputHelper">The test output helper.</param>
[Collection("DisableParallelization")]
public sealed class OrderControllerContractTests(
    OrderHostAspireFactory fixture,
    ITestOutputHelper testOutputHelper)
    : IDisposable
{
    private readonly OrderHostAspireFactory _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Sets up the test output helper for logging.
    /// </summary>
    public OrderHostAspireFactory Fixture
    {
        get
        {
            // Route logs to this test's output
            _fixture.OutputHelper = _testOutputHelper;
            return _fixture;
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
    /// Tests the OpenAPI contract of the Order API.
    /// Uses GetEndpointForNetwork to get the endpoint that Microcks (running in a container)
    /// can access from the Aspire container network.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task TestOpenApiContract()
    {
        // Arrange
        var app = Fixture.App;

        // Use GetEndpointForNetwork with the container network context so that Microcks (running in a container)
        // can access the order-api service from the Aspire container network
        Uri endpoint = app.GetEndpointForNetwork("order-api", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);

        TestRequest request = new()
        {
            ServiceId = "Order Service API:0.1.0",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = $"{endpoint.Scheme}://{endpoint.Host}:{endpoint.Port}/api",
        };

        var microcksClient = app.CreateMicrocksClient("microcks");

        var logger = app.Services.GetRequiredService<ILogger<OrderControllerContractTests>>();
        logger.LogInformation("Testing Order API via endpoint '{Endpoint}'", endpoint);
        var testResult = await microcksClient.TestEndpointAsync(request, TestContext.Current.CancellationToken);

        // Assert
        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _testOutputHelper.WriteLine(json);

        Assert.False(testResult.InProgress, "Test should not be in progress");
        Assert.True(testResult.Success, "Test should be successful");

        Assert.Single(testResult.TestCaseResults!);
    }

    /// <summary>
    /// Tests the OpenAPI contract and business conformance of the Order API.
    /// </summary>
    [Fact]
    public async Task TestOpenAPIContractAndBusinessConformance()
    {
        // Arrange
        var app = Fixture.App;

        // Use GetEndpointForNetwork with the container network context so that Microcks (running in a container)
        // can access the order-api service from the Aspire container network (aspire.dev.internal)
        Uri endpoint = app.GetEndpointForNetwork("order-api", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);

        TestRequest request = new()
        {
            ServiceId = "Order Service API:0.1.0",
            RunnerType = TestRunnerType.OPEN_API_SCHEMA,
            TestEndpoint = $"{endpoint.Scheme}://{endpoint.Host}:{endpoint.Port}/api",
        };
        var microcksClient = app.CreateMicrocksClient("microcks");
        var testResult = await microcksClient.TestEndpointAsync(request, TestContext.Current.CancellationToken);

        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _testOutputHelper.WriteLine(json);

        Assert.False(testResult.InProgress, "Test should not be in progress");
        Assert.True(testResult.Success, "Test should be successful");

        Assert.Single(testResult.TestCaseResults!);

        // You may also check business conformance.
        // Here we use JsonElement for direct navigation in the JSON structure.
        // This is efficient for simple checks and avoids creating intermediate types.
        // However, you can also deserialize into a final type (e.g., a C# class or List<Dictionary<string, object>>)
        // if you need to process or validate the data in a more complex way.
        var messages = await microcksClient.GetMessagesForTestCaseAsync(testResult, "POST /orders", TestContext.Current.CancellationToken);
        foreach (var message in messages)
        {
            if ("201".Equals(message.Response!.Status))
            {
                // Parse the request and response content as JsonDocument
                var responseDocument = JsonDocument.Parse(message.Response.Content!);
                var requestDocument = JsonDocument.Parse(message.Request!.Content!);

                // Exemple: If you want to deserialize into a final type for more advanced processing
                // var requestContent = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Request.Content);
                // or with Order class if you have it defined
                // var requestContent = JsonSerializer.Deserialize<Order>(message.Request.Content);

                // Directly access the 'productQuantities' array in both request and response
                var requestProductQuantities = requestDocument.RootElement.GetProperty("productQuantities");
                var responseProductQuantities = responseDocument.RootElement.GetProperty("productQuantities");

                // Compare the number of items in both arrays
                Assert.Equal(requestProductQuantities.GetArrayLength(), responseProductQuantities.GetArrayLength());
                for (int i = 0; i < requestProductQuantities.GetArrayLength(); i++)
                {
                    // Compare the 'productName' property for each item
                    var reqProductName = requestProductQuantities[i].GetProperty("productName").GetString();
                    var respProductName = responseProductQuantities[i].GetProperty("productName").GetString();
                    Assert.Equal(reqProductName, respProductName);
                }

                // Example: If you want to deserialize into a final type for more advanced processing
                // var requestList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(requestProductQuantities.GetRawText());
                // var responseList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(responseProductQuantities.GetRawText());
                // You can then use LINQ or other C# features to process the data
            }
        }
    }
}
