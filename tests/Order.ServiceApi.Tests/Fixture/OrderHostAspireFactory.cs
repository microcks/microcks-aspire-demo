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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

namespace Order.ServiceApi.Tests.Fixture;

/// <summary>
/// Factory for creating and managing the Aspire distributed application for integration tests.
/// This fixture is used with IClassFixture per test class.
/// </summary>
public sealed class OrderHostAspireFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the Microcks resource used for API mocking.
    /// </summary>
    public required MicrocksResource MicrocksResource;

    /// <summary>
    /// Gets or sets the Microcks Async Minion resource used for async API testing.
    /// </summary>
    public MicrocksAsyncMinionResource? MicrocksAsyncMinionResource { get; private set; }

    /// <summary>
    /// Gets or sets the Kafka resource.
    /// </summary>
    public KafkaServerResource? KafkaResource { get; private set; }

    /// <summary>
    /// The distributed application under test.
    /// </summary>
    public DistributedApplication App { get; private set; } = default!;

    /// <summary>
    /// Initializes the distributed application for testing.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper for logging.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask InitializeAsync(ITestOutputHelper testOutputHelper)
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Order_AppHost>(TestContext.Current.CancellationToken);

        // Enable resource logging to see container logs in console
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);

            // Add xUnit logging if test output helper is available
            logging.AddXUnit(testOutputHelper, options =>
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

        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}
