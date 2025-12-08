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
using System.Text.Json;
using Microcks.Aspire.Clients.Model;
using Order.ServiceApi.Tests.Fixture;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Order.ServiceApi.Tests.Api;

/// <summary>
/// Postman contract tests for the Order API using Microcks.
/// This test validates business conformance using Postman Collection scripts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderControllerPostmanContractTests"/> class.
/// </remarks>
/// <param name="fixture">The Aspire factory fixture.</param>
/// <param name="testOutputHelper">The test output helper.</param>
public sealed class OrderControllerPostmanContractTests(
    OrderHostAspireFactory fixture,
    ITestOutputHelper testOutputHelper)
    : IClassFixture<OrderHostAspireFactory>, IAsyncLifetime
{
    private readonly OrderHostAspireFactory _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    /// <summary>
    /// Initialize the fixture before any test runs.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous initialization operation.</returns>
    public async ValueTask InitializeAsync()
    {
        await _fixture.InitializeAsync(_testOutputHelper);
    }

    /// <summary>
    /// Dispose resources used by the fixture.
    /// </summary>
    /// <returns>ValueTask representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    /// <summary>
    /// Tests the Postman Collection contract of the Order API.
    /// This validates business conformance using Postman scripts defined in the collection.
    /// 
    /// The Postman collection contains scripts that check business rules like:
    /// - Correct products and quantities in order response
    /// - Product names match between request and response
    /// </summary>
    [Fact]
    public async Task TestPostmanCollectionContract()
    {
        // Arrange
        var app = _fixture.App;
        var endpoint = app.GetEndpoint("order-api");
        int port = endpoint.Port;

        // Act - Ask for a Postman Collection script conformance to be launched.
        TestRequest request = new()
        {
            ServiceId = "Order Service API:0.1.0",
            RunnerType = TestRunnerType.POSTMAN, // 👈 Use POSTMAN runner for business conformance
            TestEndpoint = $"http://host.docker.internal:{port}/api",
            Timeout = TimeSpan.FromSeconds(5)
        };

        var microcksClient = app.CreateMicrocksClient("microcks");

        var logger = app.Services.GetRequiredService<ILogger<OrderControllerPostmanContractTests>>();
        logger.LogInformation("Testing Order API Postman contract via hostname 'host.docker.internal' on port {Port}", port);

        var testResult = await microcksClient.TestEndpointAsync(request, TestContext.Current.CancellationToken);

        // Assert
        // You may inspect complete response object with following:
        var json = JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true });
        _testOutputHelper.WriteLine(json);

        Assert.False(testResult.InProgress, "Test should not be in progress");
        Assert.True(testResult.Success, "Postman contract test should be successful - check Postman Collection scripts");

        Assert.Single(testResult.TestCaseResults!);
    }
}
