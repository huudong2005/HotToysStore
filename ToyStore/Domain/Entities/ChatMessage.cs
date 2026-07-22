using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("ChatMessage")]
public partial class ChatMessage
{
    [Key]
    [Column("MessageId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MessageId { get; set; }

    [Column("SessionId")]
    public int SessionId { get; set; }

    [Column("Sender")]
    [StringLength(50)]
    public string Sender { get; set; } = null!;

    [Column("Message")]
    [StringLength(2000)]
    public string MessageText { get; set; } = null!;

    [Column("SentAt")]
    public DateTime? SentAt { get; set; }

    public virtual ChatSession? ChatSession { get; set; }
}
