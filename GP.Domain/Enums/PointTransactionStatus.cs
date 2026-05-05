namespace GP.Domain.Enums
{
    public enum PointTransactionStatus
    {
        Pending = 1,    // Points are locked until the trip departs
        Available = 2,  // Points can be spent
        Voided = 3,     // Trip was refunded or resold
        Spent = 4       // Points were redeemed
    }
}
