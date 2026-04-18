namespace GP.Domain.Common;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}