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

namespace Order.ServiceApi.UseCases;

/// <summary>
/// Exception thrown when a pastry is unavailable for order.
/// </summary>
public class UnavailablePastryException : Exception
{
    /// <summary>
    /// Gets the name of the unavailable product.
    /// </summary>
    public string Product { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnavailablePastryException"/> class.
    /// </summary>
    /// <param name="product">The name of the unavailable product.</param>
    /// <param name="message">The exception message.</param>
    public UnavailablePastryException(string product, string message) : base(message)
    {
        Product = product;
    }
}
