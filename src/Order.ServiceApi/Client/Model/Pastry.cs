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

namespace Order.ServiceApi.Client.Model;

/// <summary>
/// Represents a pastry product.
/// </summary>
/// <param name="Name">The name of the pastry.</param>
/// <param name="Description">The description of the pastry.</param>
/// <param name="Size">The size of the pastry (S, M, L).</param>
/// <param name="Price">The price of the pastry.</param>
/// <param name="Status">The availability status of the pastry.</param>
public record Pastry(string Name,
                     string Description,
                     string Size,
                     decimal Price,
                     string Status)
{
    /// <summary>
    /// Checks if the pastry is available.
    /// </summary>
    /// <returns>True if the pastry is available; otherwise, false.</returns>
    public bool IsAvailable() => Status.Equals("AVAILABLE", System.StringComparison.InvariantCultureIgnoreCase);
}
