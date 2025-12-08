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

var builder = DistributedApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["AppHost:BrowserToken"] = "",
});

var microcks = builder.AddMicrocks("microcks")
        .WithMainArtifacts(
            "resources/third-parties/apipastries-openapi.yaml",
            "resources/order-service-openapi.yaml"
        )
        .WithSecondaryArtifacts(
            "resources/order-service-postman-collection.json",
            "resources/third-parties/apipastries-postman-collection.json"
        )
        .WithLifetime(ContainerLifetime.Persistent)
        .WithHostNetworkAccess()
        .WithHostNetworkAccess("order-api");

var orderapi = builder.AddProject<Projects.Order_ServiceApi>("order-api")
    .WithEnvironment("PastryApi:BaseUrl", () =>
    {
        // Callback to get the URL once Microcks is started
        var pastryBaseUrl = microcks.Resource.GetRestMockEndpoint("API+Pastries", "0.0.1");

        return pastryBaseUrl.ToString();
    })
    .WaitFor(microcks)
    .WithReferenceRelationship(microcks);

microcks.WithReferenceRelationship(orderapi);

builder.Build().Run();
