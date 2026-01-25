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

using Order.ServiceApi.Tests.Fixture;
using Xunit;

namespace Order.ServiceApi.Tests;

/// <summary>
/// Collection definition that disables parallelization and shares the OrderHostAspireFactory
/// across all test classes in the collection. This ensures a single Kafka and Microcks
/// instance is reused for all tests.
/// </summary>
[CollectionDefinition("DisableParallelization", DisableParallelization = true)]
public class DisableParallelizationTestCollection : ICollectionFixture<OrderHostAspireFactory>
{
    // This class has no code, it's only used to define the collection
    // and share the OrderHostAspireFactory fixture across all tests
}
