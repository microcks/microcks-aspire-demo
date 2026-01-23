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

using Order.ServiceApi.Client.Model;

namespace Order.ServiceApi.Client;

/// <summary>
/// PastryAPIClient is responsible for requesting the product/stock management system (aka Pastry registry).
/// </summary>
public class PastryAPIClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PastryAPIClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    public PastryAPIClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Lists all pastries of a given size.
    /// </summary>
    /// <param name="size">The size filter (S, M, L).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of pastries matching the size.</returns>
    public async Task<List<Pastry>> ListPastriesAsync(string size, CancellationToken cancellationToken = default)
    {
        // The leading / cause the path to be replaced after the host
        // so we don't start the request with a slash to append to the base address
        var response = await _httpClient.GetAsync($"pastries?size={size}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Pastry>>(cancellationToken) ?? new List<Pastry>();
    }

    /// <summary>
    /// Gets a pastry by its name.
    /// </summary>
    /// <param name="pastryName">The name of the pastry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pastry with the specified name.</returns>
    public async Task<Pastry> GetPastryByNameAsync(string pastryName, CancellationToken cancellationToken)
    {
        // The leading / cause the path to be replaced after the host
        // so we don't start the request with a slash to append to the base address
        var response = await _httpClient.GetAsync($"pastries/{pastryName}", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Pastry>(cancellationToken) ?? null!;
    }
}
