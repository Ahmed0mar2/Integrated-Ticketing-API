using GP.Application.Events;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Features.Events
{
    public class BookingCompletedEventHandler : INotificationHandler<BookingCompletedEvent>
    {
        private readonly ApplicationDbContext _dbContext;

        public BookingCompletedEventHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(BookingCompletedEvent notification, CancellationToken cancellationToken)
        {
            var activeChallenges = await _dbContext.UserChallenges
                .Include(uc => uc.Challenge)
                .Include(uc => uc.User)
                .Where(uc => uc.UserId == notification.UserId && !uc.IsCompleted)
                .ToListAsync(cancellationToken);

            if (!activeChallenges.Any())
                return;

            foreach (var uc in activeChallenges)
            {
                // Determine how much to increment based on the challenge type
                int incrementAmount = uc.Challenge.Type switch
                {
                    ChallengeType.TotalTrips => notification.DistinctTripsCount,
                    ChallengeType.TotalSpend => (int)notification.FinalPrice,
                    ChallengeType.RoundTrip => notification.DistinctTripsCount == 2 ? 1 : 0,
                    ChallengeType.MultiDestination => notification.DistinctTripsCount >= 3 ? 1 : 0,
                    _ => 0
                };

                if (incrementAmount == 0)
                    continue;

                uc.CurrentProgress += incrementAmount;

                if (uc.CurrentProgress >= uc.Challenge.GoalValue)
                {
                    uc.CurrentProgress = uc.Challenge.GoalValue; // Cap at goal
                    uc.IsCompleted = true;
                    uc.CompletedAt = DateTime.UtcNow;

                    // Award Challenge Points (Available immediately, expires in 4 months per rules)
                    var rewardTransaction = new PointTransaction
                    {
                        UserId = uc.UserId,
                        Amount = uc.Challenge.RewardPoints,
                        AvailableAmount = uc.Challenge.RewardPoints,
                        Description = $"Completed Challenge: {uc.Challenge.Title}",
                        Source = PointSource.ChallengeReward,
                        Status = PointTransactionStatus.Available,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMonths(4)
                    };

                    _dbContext.PointTransactions.Add(rewardTransaction);
                    uc.User.LoyaltyPointsBalance += uc.Challenge.RewardPoints;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
