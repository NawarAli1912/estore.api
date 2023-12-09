﻿using Domain.Kernal;
using FluentValidation;
using MediatR;

namespace Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    private readonly IValidator<TRequest>? _validator;

    public ValidationBehavior(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validator is null)
        {
            return await next();
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (validationResult.IsValid)
        {
            return await next();

        }

        var errors = validationResult.Errors
            .ConvertAll(e => Error.Validation(
                e.ErrorCode,
                e.ErrorMessage));

        // Totally safe to use dynamic because of the generic constraints
        // the result can always be mapped to <Type> Error </Type>
        return (dynamic)errors;
    }
}
