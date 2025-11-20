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

using System.Text;
using Microcks.Aspire;

namespace Order.AppHost;

/// <summary>
/// This class is demonstrating how to setup a full Microcks avec Microcks-Importer profile with both HTTP and AsyncAPI services.
/// </summary>
public static class DockerComposeAppHost
{
    public static void Configure(IDistributedApplicationBuilder builder)
    {
        var command = new string[]{
            "redpanda start ",
            " --overprovisioned --smp 1 --memory 1G --reserve-memory 0M --node-id 0 --check=false",
            " --kafka-addr PLAINTEXT://0.0.0.0:19092,EXTERNAL://0.0.0.0:9092",
            " --advertise-kafka-addr PLAINTEXT://kafka:19092,EXTERNAL://localhost:9092"
        };

        var kafkaBuilder = builder.AddContainer("kafka", "redpandadata/redpanda:v22.2.2")
            .WithEndpoint(9092, 9092, "tcp")
            .WithEndpoint(19092, 19092, "internal")
            .WithArgs(command);
        // ----------------------------------
        // Microcks
        //----------------------------------
        var microcksBuilder = builder.AddMicrocks("microcks")
            .WithAsyncFeature(minion =>
            {
                // Configure Minion to use Redpanda as Kafka Broker
                minion.WithKafkaConnection(kafkaBuilder, 19092);
            });

        // ----------------------------------
        // Microcks Resources Importer
        //----------------------------------
        var microcksResourcesArgs = new StringBuilder()
            .Append("/resources/order-service-openapi.yaml:true,")
            .Append("/resources/order-events-asyncapi.yaml:true,")
            .Append("/resources/third-parties/apipastries-openapi.yaml:true,")
            .Append("/resources/third-parties/apipastries-postman-collection.json:false")
            .ToString();

        var microcksImporter = builder.AddContainer("microcks-importer", "quay.io/microcks/microcks-cli:latest")
            .WithEntrypoint("microcks-cli")
            .WithArgs(
                "import",
                microcksResourcesArgs,
                "--microcksURL=http://microcks:8080/api",
                "--insecure",
                "--keycloakClientId=foo",
                "--keycloakClientSecret=bar"
            )
            // Use absolute host path for volume to ensure resolution inside orchestrator.
            .WithBindMount(source: $"{builder.AppHostDirectory}/resources", target: "/resources")
            .WithReferenceRelationship(microcksBuilder)
            .WaitFor(microcksBuilder);


        var orderapi = builder.AddProject<Projects.Order_ServiceApi>("order-api")
            .WithEnvironment("PastryApi:BaseUrl", () =>
            {
                // Callback to get the URL once Microcks is started
                var pastryBaseUrl = microcksBuilder.Resource.GetRestMockEndpoint("API+Pastries", "0.0.1");

                return pastryBaseUrl.ToString();
            })
            .WithReferenceRelationship(microcksBuilder)
            .WaitFor(microcksBuilder)
            .WithOtlpExporter();

    }
}
