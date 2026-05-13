namespace ConsultationApi.Core.Entities;

public class ChatMessage : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ConsultationSession Session { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
