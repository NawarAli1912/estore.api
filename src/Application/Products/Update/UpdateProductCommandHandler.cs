﻿using Application.Common.DatabaseAbstraction;
using Domain.Errors;
using Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Primitives;

namespace Application.Products.Update;

internal sealed class UpdateProductCommandHandler(IApplicationDbContext context)
        : IRequestHandler<UpdateProductCommand, Result<Product>>
{
    private readonly IApplicationDbContext _context = context;

    public async Task<Result<Product>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];
        var product = await _context
            .Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return DomainError.Products.NotFound;
        }

        var updateProductResult = product.Update(
            request.Name,
            request.Description,
            request.Quantity,
            request.CustomerPrice,
            request.PurchasePrice);

        if (updateProductResult.IsError)
        {
            return updateProductResult.Errors;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return updateProductResult.Value;
    }
}
