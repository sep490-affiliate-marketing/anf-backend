﻿using ANF.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ANF.Infrastructure.Configs
{
    public class PublisherOfferTypeConfig : IEntityTypeConfiguration<PublisherOffer>
    {
        public void Configure(EntityTypeBuilder<PublisherOffer> builder)
        {
            builder.HasOne(po => po.Publisher)
                .WithMany(p => p.PublisherOffers)
                .HasForeignKey(po => po.PublisherId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(po => po.Offer)
                .WithMany(o => o.PublisherOffers)
                .HasForeignKey(po => po.OfferId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
