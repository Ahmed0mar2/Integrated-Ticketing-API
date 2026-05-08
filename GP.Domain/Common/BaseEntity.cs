namespace GP.Domain.Common;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = AppTime.GetScheduleNow();
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}