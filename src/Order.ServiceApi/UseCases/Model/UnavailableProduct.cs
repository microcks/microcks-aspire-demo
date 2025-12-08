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

namespace Order.ServiceApi.UseCases.Model;

/// <summary>
/// Represents a product that is unavailable for order.
/// </summary>
public sealed class UnavailableProduct
{
    /// <summary>
    /// Gets the name of the unavailable product.
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// Gets the details about why the product is unavailable.
    /// </summary>
    public string Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnavailableProduct"/> class.
    /// </summary>
    /// <param name="productName">The name of the unavailable product.</param>
    /// <param name="details">The details about why the product is unavailable.</param>
    public UnavailableProduct(string productName, string details)
    {
        ProductName = productName;
        Details = details;
    }
}
