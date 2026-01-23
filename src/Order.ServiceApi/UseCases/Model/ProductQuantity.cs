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
/// Represents a product and its quantity in an order.
/// </summary>
/// <param name="ProductName">The name of the product.</param>
/// <param name="Quantity">The quantity of the product.</param>
public sealed record ProductQuantity(string ProductName, int Quantity);
