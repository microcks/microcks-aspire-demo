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

using System;
using System.Collections.Generic;

namespace Order.ServiceApi.UseCases.Model;

/// <summary>
/// Represents an order in the system.
/// </summary>
public sealed class Order
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier who placed the order.
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the list of products and their quantities in the order.
    /// </summary>
    public required IReadOnlyList<ProductQuantity> ProductQuantities { get; set; }

    /// <summary>
    /// Gets or sets the total price of the order.
    /// </summary>
    public double TotalPrice { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Order"/> class.
    /// </summary>
    public Order()
    {
        Id = Guid.NewGuid().ToString();
        Status = OrderStatus.Created;
    }
}
