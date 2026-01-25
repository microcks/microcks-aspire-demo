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

using Order.ServiceApi.UseCases.Model;

namespace Order.ServiceApi.UseCases;

/// <summary>
/// Interface for publishing order events to message broker.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an order created event asynchronously.
    /// </summary>
    /// <param name="orderEvent">The order event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishOrderCreatedAsync(OrderEvent orderEvent, CancellationToken cancellationToken = default);
}
