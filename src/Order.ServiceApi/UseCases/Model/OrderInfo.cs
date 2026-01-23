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

using System.Collections.Generic;

namespace Order.ServiceApi.UseCases.Model;

/// <summary>
/// Represents the information needed to create an order.
/// </summary>
public sealed record OrderInfo
{
    /// <summary>
    /// Gets the customer identifier who is placing the order.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Gets the list of products and their quantities to order.
    /// </summary>
    public required IReadOnlyList<ProductQuantity> ProductQuantities { get; init; }

    /// <summary>
    /// Gets the total price of the order.
    /// </summary>
    public double TotalPrice { get; init; }
}
