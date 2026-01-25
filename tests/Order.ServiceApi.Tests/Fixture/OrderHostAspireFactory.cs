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
using MartinCostello.Logging.XUnit;
using Microcks.Aspire;
using Microcks.Aspire.Async;
using Microcks.Aspire.PostmanRunner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

namespace Order.ServiceApi.Tests.Fixture;

/// <summary>
/// Factory for creating and managing the Aspire distributed application for integration tests.
/// This fixture is shared across all test classes via ICollectionFixture.
/// Implements ITestOutputHelperAccessor to allow per-test log output.
/// </summary>
public sealed class OrderHostAspireFactory : IAsyncLifetime, ITestOutputHelperAccessor
{
    /// <summary>
    /// Gets or sets the test output helper for the current test.
    /// This is swapped per-test to route logs to the correct test output.
    /// </summary>
    public ITestOutputHelper? OutputHelper { get; set; }

    /// <summary>
    /// Gets the Microcks resource used for API mocking.
    /// </summary>
    public MicrocksResource MicrocksResource { get; private set; } = default!;

    /// <summary>
    /// Gets the Microcks Async Minion resource used for async API testing.
    /// </summary>
    public MicrocksAsyncMinionResource? MicrocksAsyncMinionResource { get; private set; }

    /// <summary>
    /// Gets the Kafka resource.
    /// </summary>
    public KafkaServerResource? KafkaResource { get; private set; }

    /// <summary>
    /// The distributed application under test.
    /// </summary>
    public DistributedApplication App { get; private set; } = default!;

    /// <summary>
    /// Initializes the distributed application for testing.
    /// This is called once when the collection fixture is created.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Order_AppHost>(TestContext.Current.CancellationToken);

        // Enable resource logging to see container logs in console
        // Use ITestOutputHelperAccessor (this) so logs route to current test's output
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);

            // Pass 'this' as the accessor - logs will use current OutputHelper
            logging.AddXUnit(this, options =>
            {
                options.TimestampFormat = "HH:mm:ss.fff ";
                options.IncludeScopes = false;
            });
        });

        // Enable streaming resource logs (container logs)
        builder.Services.Configure<DistributedApplicationOptions>(options =>
        {
            options.DisableDashboard = true;
        });

        this.MicrocksResource = builder.Resources.OfType<MicrocksResource>().Single();
        this.MicrocksAsyncMinionResource = builder.Resources.OfType<MicrocksAsyncMinionResource>().SingleOrDefault();
        this.KafkaResource = builder.Resources.OfType<KafkaServerResource>().SingleOrDefault();

        this.App = await builder.BuildAsync(TestContext.Current.CancellationToken);

        await this.App.StartAsync()
            .ConfigureAwait(true);

        // Wait for Kafka readiness if available
        if (KafkaResource is not null)
        {
            await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
                KafkaResource.Name, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        // Wait for microcks readiness
        await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
            MicrocksResource.Name, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // Wait for Async Minion readiness if available
        if (MicrocksAsyncMinionResource is not null)
        {
            await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
                MicrocksAsyncMinionResource.Name, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        // Wait for Order API readiness
        await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
            "order-api",
            TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Gets the Kafka connection string from the Aspire resources.
    /// </summary>
    /// <returns>The Kafka connection string, or null if Kafka is not available.</returns>
    public async Task<string?> GetKafkaConnectionStringAsync()
    {
        if (KafkaResource is null)
        {
            return null;
        }

        return await KafkaResource.ConnectionStringExpression
            .GetValueAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Disposes of the distributed application resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (App is not null)
            {
                await App.StopAsync(TestContext.Current.CancellationToken)
                    .ConfigureAwait(false);
                await App.DisposeAsync();
            }
        }
        catch
        {
            // swallow, we're tearing down tests
        }
    }
}
