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
using Aspire.Hosting.Testing;
using Microcks.Aspire;
using Projects;

namespace Order.ServiceApi.Tests.Fixture;

public class OrderHostAspireFactory : IAsyncLifetime
{
    /// <summary>
    /// CollectionName for ICollectionFixture
    /// </summary>
    public const string CollectionName = "Microcks Aspire Collection";

    public required MicrocksResource MicrocksResource;

    /// <summary>
    /// The distributed application under test.
    /// </summary>
    public DistributedApplication App { get; private set; } = default!;

    public async ValueTask DisposeAsync()
    {
        await this.App.StopAsync();
        await this.App.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await this.InitializeDistributedApplication();
    }

    private async Task InitializeDistributedApplication()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Order_AppHost>(TestContext.Current.CancellationToken);

        this.MicrocksResource = builder.Resources.OfType<MicrocksResource>().Single();

        this.App = await builder.BuildAsync(TestContext.Current.CancellationToken);

        await this.App.StartAsync()
            .ConfigureAwait(true);

        // Wait for microcks readiness
        await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
            MicrocksResource.Name, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // Wait for Order API readiness
        await this.App.ResourceNotifications.WaitForResourceHealthyAsync(
            "order-api",
            TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }
}
