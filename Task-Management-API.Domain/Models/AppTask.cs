using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Task_Management_API.Domain.Models
{
    public class AppTask
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        [DefaultValue("Pending")]
        public string Status { get; set; }
        [Required]
        public DateTime DueDate { get; set; }
        [ForeignKey("UserId")]
        public string? UserId { get; set; }
        [JsonIgnore]
        public ApplicationUser? User { get; set; }
    }
}
