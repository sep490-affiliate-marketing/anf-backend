﻿using ANF.Core.Commons;
using System.ComponentModel.DataAnnotations.Schema;

namespace ANF.Core.Models.Entities
{
    public class PublisherOffer : IEntity
    {
        [Column("po_no")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        //[Column("offer_id")]
        //public long OfferId { get; set; }

        //[Column("publisher_id")]
        //public long PublisherId { get; set; }

        //[Column("joining_date")]
        //public DateTime JoiningDate { get; set; }

        //public User Publisher { get; set; } = null!;

        //public Offer Offer { get; set; } = null!;
    }
}
