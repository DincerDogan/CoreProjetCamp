using Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Entity.Dtos
{
    public class ContentsDTO : IDto
    {
        [Key]
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public int HeadingId { get; set; }
        public string HeadingName { get; set; }
        public int WriterId { get; set; }
        public string WriterName { get; set; }
        public string WriterSurname { get; set; }
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

    }
}
