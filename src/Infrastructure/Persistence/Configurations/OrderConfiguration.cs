﻿using Domain.Customers;
using Domain.Orders;
using Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(TablesNames.Order);

        builder
            .HasKey(o => o.Id);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .IsRequired();

        builder.HasMany(o => o.LineItems)
            .WithOne()
            .HasForeignKey(li => li.OrderId)
            .IsRequired();

        builder
            .ComplexProperty(o => o.ShippingInfo);

        builder.Property(o => o.TotalPrice)
            .HasPrecision(12, 2);

        builder.Property<List<Guid>>("_requestedOffers")
            .HasColumnName("RequestedOffers")
            .HasListOfIdsConverter();
    }
}
