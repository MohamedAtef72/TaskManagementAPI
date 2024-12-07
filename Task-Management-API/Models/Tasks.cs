using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace Task_Management_API.Models
{
    public class Tasks
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