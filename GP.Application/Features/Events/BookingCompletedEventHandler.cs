using GP.Application.Common;
using GP.Application.Events;
using GP.Application.Interfaces;
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
        private readonly INotificationService _notificationService;

        public BookingCompletedEventHandler(
            ApplicationDbContext dbContext,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
        }

        public async Task Handle(BookingCompletedEvent notification, CancellationToken cancellationToken)
        {
            var now = AppTime.GetScheduleNow();

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
                    uc.CompletedAt = now;

                    // Award Challenge Points (Available immediately, expires in 4 months per rules)
                    var rewardTransaction = new PointTransaction
                    {
                        UserId = uc.UserId,
                        Amount = uc.Challenge.RewardPoints,
                        AvailableAmount = uc.Challenge.RewardPoints,
                        Description = $"Completed Challenge: {uc.Challenge.Title}",
                        Source = PointSource.ChallengeReward,
                        Status = PointTransactionStatus.Available,
                        CreatedAt = now,
                        ExpiresAt = now.AddMonths(4)
                    };

                    _dbContext.PointTransactions.Add(rewardTransaction);
                    uc.User.LoyaltyPointsBalance += uc.Challenge.RewardPoints;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var uc in activeChallenges.Where(uc => uc.IsCompleted))
            {
                await _notificationService.SendNotificationAsync(
                    uc.UserId,
                    "Challenge Completed! 🏆",
                    $"You earned {uc.Challenge.RewardPoints} points for completing: {uc.Challenge.Title}!",
                    "تم إنجاز المهمة! 🏆",
                    $"لقد كسبت {uc.Challenge.RewardPoints} نقطة لإنجازك: {uc.Challenge.TitleAr}!",
                    "CHALLENGE_COMPLETED",
                    cancellationToken);
            }
        }
    }
}
