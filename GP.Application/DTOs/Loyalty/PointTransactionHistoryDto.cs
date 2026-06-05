namespace GP.Application.DTOs.Loyalty;

public class PointTransactionHistoryDto
{
    public int TransactionId { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
