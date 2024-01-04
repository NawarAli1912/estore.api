﻿using Application.Common.DatabaseAbstraction;
using Domain.Errors;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

namespace Application.Carts.Checkout;
internal sealed class CheckoutCommandHandler(IApplicationDbContext context)
        : IRequestHandler<CheckoutCommand, Result<Created>>
{
    private readonly IApplicationDbContext _context = context;


    public async Task<Result<Created>> Handle(
        CheckoutCommand request,
        CancellationToken cancellationToken)
    {

        var customer = await _context
            .Customers
            .Include(c => c.Cart)
            .ThenInclude(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return DomainError.Customer.NotFound;
        }

        if (customer.Cart.CartItems.Count == 0)
        {
            return DomainError.Cart.EmptyCart;
        }

        var cartProductsIds = customer
            .Cart
            .CartItems
            .Select(ci => ci.ProductId)
            .ToHashSet();

        var cartProductsDict = await _context
            .Products
            .Where(p => cartProductsIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var orderResult = OrderOrchestratorService.CreateOrder(
            customer,
            cartProductsDict,
            request.ShippingCompany,
            request.ShippingCompanyLocation,
            request.PhoneNumber);

        if (orderResult.IsError)
        {
            return orderResult.Errors;
        }

        await _context.Orders.AddAsync(orderResult.Value, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Created;
    }
}
