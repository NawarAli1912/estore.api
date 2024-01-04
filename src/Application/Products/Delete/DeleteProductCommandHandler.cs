﻿using Application.Common.DatabaseAbstraction;
using Domain.Errors;
using Domain.Orders.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

namespace Application.Products.Delete;
internal sealed class DeleteProductCommandHandler(IApplicationDbContext context)
        : IRequestHandler<DeleteProductCommand, Result<Deleted>>
{
    private readonly IApplicationDbContext _context = context;

    public async Task<Result<Deleted>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var result = await _context
            .Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (result is null)
            return DomainError.Product.NotFound;

        var productsInOrder = await _context
            .Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .AnyAsync(o => o.LineItems.Any(li => li.ProductId == request.Id), cancellationToken);

        if (productsInOrder)
        {
            return DomainError.Product.InOrder;
        }

        result.MarkAsDeleted();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
