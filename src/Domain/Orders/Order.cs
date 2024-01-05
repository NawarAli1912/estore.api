﻿using Domain.Errors;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Orders.ValueObjects;
using SharedKernel.Enums;
using SharedKernel.Primitives;
namespace Domain.Orders;

public sealed class Order : AggregateRoot, IAuditableEntity
{
    private readonly HashSet<LineItem> _lineItems = [];

    private readonly List<Guid> _requestedOffers = [];

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public ShippingInfo ShippingInfo { get; private set; }

    public decimal TotalPrice { get; private set; }

    public IReadOnlySet<LineItem> LineItems => _lineItems;

    public IReadOnlyList<Guid> RequestedOffers => _requestedOffers;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ModifiedAtUtc { get; set; }

    public static Order Create(
        Guid customerId,
        ShippingCompany shippingCompany,
        string shippingCompanyAddress,
        string phoneNumber)
    {
        var shippingInfo = ShippingInfo.Create(
            shippingCompany,
            shippingCompanyAddress,
            phoneNumber);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            ShippingInfo = shippingInfo
        };

        return order;
    }

    public Result<Updated> AddItems(
        Guid productId,
        decimal productPrice,
        int quantity,
        ItemType type = ItemType.Product,
        Guid? relatedOfferId = default)
    {
        for (var i = 0; i < quantity; ++i)
        {
            var lineItemResult = LineItem.Create(
                Guid.NewGuid(),
                productId,
                Id,
                productPrice,
                type,
                relatedOfferId);

            if (lineItemResult.IsError)
            {
                return lineItemResult.Errors;
            }

            _lineItems.Add(lineItemResult.Value);
        }

        TotalPrice += productPrice * quantity;

        return Result.Updated;
    }

    public Result<Updated> RemoveItems(Guid productId, int Quantity)
    {
        if (_lineItems.Count(li => li.ProductId == productId) < Quantity)
        {
            return DomainError.LineItem.ExceedsAvailableQuantity(productId);
        }

        for (var i = 0; i < Quantity; ++i)
        {
            var lineItem = _lineItems
                .First(item => item.ProductId == productId);

            _lineItems.Remove(lineItem);

            TotalPrice -= lineItem.Price;
        }

        return Result.Updated;
    }

    public void UpdateShippingInfo(
        ShippingCompany? shippingCompany,
        string? shippingCompanyAddress,
        string? phoneNumber)
    {
        ShippingInfo.Update(
            shippingCompany,
            shippingCompanyAddress,
            phoneNumber);
    }

    public void Approve()
    {
        Status = OrderStatus.Approved;
    }

    public void Reject()
    {
        Status = OrderStatus.Rejected;
    }

    public void Cancel()
    {
        Status = OrderStatus.Canceled;
    }

    internal void AddRequestedOffer(Guid itemId)
    {
        _requestedOffers.Add(itemId);
    }

    private Order() : base(Guid.NewGuid())
    {
    }
}
