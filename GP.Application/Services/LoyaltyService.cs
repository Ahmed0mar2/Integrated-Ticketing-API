using GP.Application.Common;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GP.Application.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ApplicationDbContext _dbContext;

        public LoyaltyService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiResponse> ExpireOldPointsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var expiring = await _dbContext.PointTransactions
                .Where(p => !p.IsExpired && p.ExpiresAt != null && p.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            if (expiring.Count == 0)
            {
                return ApiResponse.Ok("Expired 0 point transaction(s).");
            }

            foreach (var transaction in expiring)
            {
                transaction.IsExpired = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Expired {expiring.Count} point transaction(s).");
        }

        public async Task<bool> DeductPointsFifoAsync(int userId, int pointsToDeduct, string description, int? bookingId = null, CancellationToken cancellationToken = default)
        {
            var earnTransactions = await _dbContext.PointTransactions
                .Where(pt => pt.UserId == userId
                             && pt.Amount > 0
                             && pt.AvailableAmount > 0
                             && !pt.IsExpired)
                .OrderBy(pt => pt.ExpiresAt)
                .ToListAsync(cancellationToken);

            var totalAvailable = earnTransactions.Sum(pt => pt.AvailableAmount);
            if (totalAvailable < pointsToDeduct)
            {
                throw new InvalidOperationException("Insufficient points.");
            }

            var remaining = pointsToDeduct;
            foreach (var transaction in earnTransactions)
            {
                if (remaining == 0)
                {
                    break;
                }

                var deduction = Math.Min(transaction.AvailableAmount, remaining);
                transaction.AvailableAmount -= deduction;
                remaining -= deduction;

                var spendTransaction = new PointTransaction
                {
                    UserId = userId,
                    Amount = -deduction,
                    AvailableAmount = 0,
                    Description = $"{description} (Consumed from Batch #{transaction.Id})",
                    BookingId = bookingId,
                    ParentTransactionId = transaction.Id,
                    Status = PointTransactionStatus.Spent,
                    Source = PointSource.Redemption,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.PointTransactions.Add(spendTransaction);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
            if (user != null)
            {
                user.LoyaltyPointsBalance -= pointsToDeduct;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<ApiResponse> ResetMonthlyChallengesAsync(CancellationToken cancellationToken = default)
        {
            var allOldChallenges = await _dbContext.UserChallenges.ToListAsync(cancellationToken);

            if (allOldChallenges.Count > 0)
            {
                _dbContext.UserChallenges.RemoveRange(allOldChallenges);
            }

            var users = await _dbContext.Users
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            var activeChallenges = await _dbContext.Challenges
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            var newAssignments = new List<UserChallenge>(users.Count * activeChallenges.Count);
            foreach (var userId in users)
            {
                foreach (var challenge in activeChallenges)
                {
                    newAssignments.Add(new UserChallenge
                    {
                        UserId = userId,
                        ChallengeId = challenge.Id,
                        CurrentProgress = 0,
                        IsCompleted = false
                    });
                }
            }

            if (newAssignments.Count > 0)
            {
                _dbContext.UserChallenges.AddRange(newAssignments);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Reset complete. Assigned {activeChallenges.Count} challenges to {users.Count} users.");
        }
    }
}
