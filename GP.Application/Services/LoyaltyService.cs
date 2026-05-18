using GP.Application.Common;
using GP.Application.DTOs.Loyalty;
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
            var now = AppTime.GetScheduleNow();

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
            var now = AppTime.GetScheduleNow();
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
                    CreatedAt = now
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
            // Remove old monthly challenge assignments, but preserve OneTime challenges
            var allOldChallenges = await _dbContext.UserChallenges
                .Where(uc => uc.Challenge.Frequency == ChallengeFrequency.Monthly)
                .ToListAsync(cancellationToken);

            if (allOldChallenges.Count > 0)
            {
                _dbContext.UserChallenges.RemoveRange(allOldChallenges);
            }

            var users = await _dbContext.Users
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            // Only get active Monthly challenges for resetting
            var activeMonthlyChallenge = await _dbContext.Challenges
                .Where(c => c.IsActive && c.Frequency == ChallengeFrequency.Monthly)
                .ToListAsync(cancellationToken);

            var newAssignments = new List<UserChallenge>(users.Count * activeMonthlyChallenge.Count);
            foreach (var userId in users)
            {
                foreach (var challenge in activeMonthlyChallenge)
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

            return ApiResponse.Ok($"Reset complete. Assigned {activeMonthlyChallenge.Count} challenges to {users.Count} users.");
        }

        public async Task<PagedResult<PointTransactionHistoryDto>> GetUserPointHistoryAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _dbContext.PointTransactions
                .Where(pt => pt.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(pt => pt.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(pt => new PointTransactionHistoryDto
                {
                    TransactionId = pt.Id,
                    Amount = pt.Amount,
                    Description = pt.Description,
                    Source = pt.Source.ToString(),
                    Status = pt.Status.ToString(),
                    CreatedAt = pt.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<PointTransactionHistoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = pageNumber
            };
        }

        public async Task<PagedResult<UserChallengeHistoryDto>> GetUserChallengesAsync(int userId, bool? isCompleted, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _dbContext.UserChallenges
                .Include(uc => uc.Challenge)
                .Where(uc => uc.UserId == userId);

            if (isCompleted.HasValue)
            {
                query = query.Where(uc => uc.IsCompleted == isCompleted.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(uc => uc.IsCompleted)
                .ThenBy(uc => uc.Challenge.Frequency)
                .ThenBy(uc => uc.ChallengeId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(uc => new UserChallengeHistoryDto
                {
                    ChallengeId = uc.ChallengeId,
                    Title = uc.Challenge.Title,
                    Description = uc.Challenge.Description,
                    TitleAr = uc.Challenge.TitleAr,
                    DescriptionAr = uc.Challenge.DescriptionAr,
                    Type = uc.Challenge.Type.ToString(),
                    Frequency = uc.Challenge.Frequency.ToString(),
                    CurrentProgress = uc.CurrentProgress,
                    GoalValue = uc.Challenge.GoalValue,
                    RewardPoints = uc.Challenge.RewardPoints,
                    IsCompleted = uc.IsCompleted
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<UserChallengeHistoryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = pageNumber
            };
        }
    }
}
