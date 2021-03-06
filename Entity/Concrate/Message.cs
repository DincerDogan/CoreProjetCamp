using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Entity.Concrate
{
    public class Message : IEntity
    {
        public int Id { get; set; }
        [StringLength(50)]
        public string sender { get; set; }
        [StringLength(50)]
        public string Receiver { get; set; }
        [StringLength(50)]
        public string Subject { get; set; }
        [AllowHtml]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public bool IsDeleted { get; set; }
        public bool DraftStatus { get; set; }
        public bool IsRead { get; set; }
    }
}
