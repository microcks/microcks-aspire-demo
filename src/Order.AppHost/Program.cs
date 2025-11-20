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

using Microsoft.Extensions.Configuration;
using Order.AppHost;

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["AppHost:BrowserToken"] = "",
});


// When in DockerCompose Launch profile, we also start Microcks and a Kafka broker (Redpanda) as containers
// Similar to docker-compose setup for local dev/testing 
// DOTNET_LAUNCH_PROFILE=DockerCompose
if (builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "DockerCompose")
{
    DockerComposeAppHost.Configure(builder);
}
else
{
    MicrocksUberAutoImport(builder);
}

builder.Build().Run();


/// <summary>
/// Configure Microcks with auto-import of OpenAPI and Postman artifacts and link it to Order API project.
/// </summary>
static void MicrocksUberAutoImport(IDistributedApplicationBuilder builder)
{
    var microcks = builder.AddMicrocks("microcks")
            .WithMainArtifacts(
                "resources/third-parties/apipastries-openapi.yaml",
                "resources/order-service-openapi.yaml"
            )
            .WithSecondaryArtifacts(
                "resources/order-service-postman-collection.json",
                "resources/third-parties/apipastries-postman-collection.json"
            );

    //
    // Microcks reference - link the Order API project to Microcks
    microcks.WithHostNetworkAccess("order-api");

    var orderapi = builder.AddProject<Projects.Order_ServiceApi>("order-api")
        .WithEnvironment("PastryApi:BaseUrl", () =>
        {
            // Callback to get the URL once Microcks is started
            var pastryBaseUrl = microcks.Resource.GetRestMockEndpoint("API+Pastries", "0.0.1");

            return pastryBaseUrl.ToString();
        })
        .WaitFor(microcks);

    microcks.WithReferenceRelationship(orderapi);
}
