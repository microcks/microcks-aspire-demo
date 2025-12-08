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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Order.ServiceApi.UseCases;
using Order.ServiceApi.UseCases.Model;
using OrderModel = Order.ServiceApi.UseCases.Model.Order;

namespace Order.ServiceApi.Endpoints;

/// <summary>
/// Extension methods for mapping order-related endpoints.
/// </summary>
public static class OrderEndpoints
{
    /// <summary>
    /// Maps the order management endpoints to the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application with order endpoints mapped.</returns>
    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .WithDescription("Order management endpoints");

        _ = root.MapPost("/", CreateOrder)
            .Produces<OrderModel>();
        return app;
    }


    private static async Task<IResult> CreateOrder(
        OrderInfo orderInfo,
        OrderUseCase orderUseCase,
        CancellationToken cancellationToken)
    {
        OrderModel createdOrder;
        try
        {
            createdOrder = await orderUseCase.PlaceOrderAsync(orderInfo, cancellationToken);

            return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
        }
        catch (UnavailablePastryException upe)
        {
            // We have to return a 422 (unprocessable) with correct expected type.
            return Results.UnprocessableEntity(new UnavailableProduct(upe.Product, upe.Message));
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message, statusCode: StatusCodes.Status500InternalServerError);
        }

    }

}
