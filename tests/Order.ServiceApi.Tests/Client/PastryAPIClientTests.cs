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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Order.ServiceApi.Client;
using Order.ServiceApi.Client.Model;
using Order.ServiceApi.Tests.Fixture;

namespace Order.ServiceApi.Tests.Client;

/// <summary>
/// Integration tests for the PastryAPIClient.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PastryAPIClientTests"/> class.
/// </remarks>
/// <param name="orderHostAspireFactory">The Aspire factory fixture.</param>
[Collection(OrderHostAspireFactory.CollectionName)]
public class PastryAPIClientTests(OrderHostAspireFactory orderHostAspireFactory)
    : IAsyncLifetime
{
    private readonly OrderHostAspireFactory orderHostAspireFactory = orderHostAspireFactory;

    /// <summary>
    /// Gets or sets the web application factory for testing.
    /// </summary>
    public WebApplicationFactory<Order.ServiceApi.Program>? WebApplicationFactory { get; private set; }

    /// <summary>
    /// Tests that ListPastriesAsync returns pastries filtered by size.
    /// </summary>
    [Fact]
    public async Task TestPastryAPIClient_ListPastriesAsync()
    {
        Assert.NotNull(this.WebApplicationFactory);

        // Arrange
        DistributedApplication app = orderHostAspireFactory.App;
        var microcksClient = app.CreateMicrocksClient("microcks");

        var pastryAPIClient = this.WebApplicationFactory
            .Services
            .GetRequiredService<PastryAPIClient>(); // Ensure the client is registered

        List<Pastry> pastries = await pastryAPIClient.ListPastriesAsync("S", TestContext.Current.CancellationToken);
        Assert.Single(pastries); // Assuming there is 1 pastry in the mock data

        pastries = await pastryAPIClient.ListPastriesAsync("M", TestContext.Current.CancellationToken);
        Assert.Equal(2, pastries.Count); // Assuming there are 2 pastries in the mock

        pastries = await pastryAPIClient.ListPastriesAsync("L", TestContext.Current.CancellationToken);
        Assert.Equal(2, pastries.Count); // Assuming there is 1 pastry in the mock

        bool isVerified = await microcksClient.VerifyAsync(
            "API Pastries", "0.0.1", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(isVerified, "Pastry API should be verified successfully");
    }

    /// <summary>
    /// Tests that GetPastryByNameAsync returns the correct pastry by name.
    /// </summary>
    [Fact]
    public async Task TestPastryAPIClient_GetPastryByNameAsync()
    {
        Assert.NotNull(this.WebApplicationFactory);

        // Arrange
        DistributedApplication app = orderHostAspireFactory.App;
        var microcksClient = app.CreateMicrocksClient("microcks");
        double initialInvocationCount = await microcksClient
            .GetServiceInvocationsCountAsync("API Pastries", "0.0.1", cancellationToken: TestContext.Current.CancellationToken);

        var pastryAPIClient = this.WebApplicationFactory
            .Services
            .GetRequiredService<PastryAPIClient>(); // Ensure the client is registered

        // Act & Assert : Millefeuille (disponible)
        var millefeuille = await pastryAPIClient.GetPastryByNameAsync("Millefeuille", TestContext.Current.CancellationToken);
        Assert.NotNull(millefeuille);
        Assert.Equal("Millefeuille", millefeuille.Name);
        Assert.True(millefeuille.IsAvailable());

        // Act & Assert : Éclair au café (disponible)
        var eclairCafe = await pastryAPIClient.GetPastryByNameAsync("Eclair Cafe", TestContext.Current.CancellationToken);
        Assert.NotNull(eclairCafe);
        Assert.Equal("Eclair Cafe", eclairCafe.Name);
        Assert.True(eclairCafe.IsAvailable());

        // Act & Assert : Éclair chocolat (indisponible)
        var eclairChocolat = await pastryAPIClient.GetPastryByNameAsync("Eclair Chocolat", TestContext.Current.CancellationToken);
        Assert.NotNull(eclairChocolat);
        Assert.Equal("Eclair Chocolat", eclairChocolat.Name);
        Assert.False(eclairChocolat.IsAvailable());

        // Vérifier le nombre d'invocations
        double finalInvocationCount = await microcksClient.GetServiceInvocationsCountAsync("API Pastries", "0.0.1", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(initialInvocationCount + 3, finalInvocationCount);
    }

    /// <summary>
    /// Initializes the test by setting up the web application factory.
    /// </summary>
    /// <returns>A completed value task.</returns>
    public ValueTask InitializeAsync()
    {
        // Get Microcks Pastry API mock endpoint
        DistributedApplication app = orderHostAspireFactory.App;

        var microcksClient = app.CreateMicrocksClient("microcks");

        var pastryApiUrl = orderHostAspireFactory.MicrocksResource
            .GetRestMockEndpoint("API Pastries", "0.0.1")
            .ToString();

        // Add services for web/integration tests.
        this.WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test"); // Set environment to Test
                builder.UseSetting("PastryApi:BaseUrl", pastryApiUrl);
            });
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Disposes of the test resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        WebApplicationFactory?.DisposeAsync();
        await Task.CompletedTask;
    }
}
