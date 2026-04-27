namespace GP.Application.DTOs.Marketplace;

public class ListTicketRequestDto
{
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public decimal AskingPrice { get; set; }
}
