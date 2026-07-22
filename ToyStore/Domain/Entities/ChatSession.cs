using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("ChatSession")]
public partial class ChatSession
{
    [Key]
    [Column("SessionId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SessionId { get; set; }

    [Column("CustomerName")]
    [StringLength(100)]
    public string CustomerName { get; set; } = null!;

    [Column("Status")]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
