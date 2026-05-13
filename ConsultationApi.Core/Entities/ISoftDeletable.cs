namespace ConsultationApi.Core.Entities;

public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
