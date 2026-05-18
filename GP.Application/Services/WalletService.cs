using GP.Application.DTOs.Wallet;
using GP.Application.Interfaces;
using GP.Domain.Entities;
using GP.Domain.Enums;
using GP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _dbContext;

        public WalletService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> DepositAsync(int userId, DepositRequestDto request, CancellationToken cancellationToken = default)
        {
            
            await Task.Delay(2000, cancellationToken);

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

                try
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
                    if (user == null) throw new Exception("User not found.");

                    // A. Update the Balance
                    user.WalletBalance += request.Amount;

                    // B. Write to the Ledger
                    var maskedCard = request.MockCardNumber.Substring(request.MockCardNumber.Length - 4);
                    var ledgerEntry = new WalletTransaction
                    {
                        UserId = userId,
                        Amount = request.Amount,
                        Type = TransactionType.Deposit,
                        Description = $"Deposit via simulated card ending in {maskedCard}"
                    };

                    _dbContext.WalletTransactions.Add(ledgerEntry);

                    // C. Save & Commit
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return $"Successfully deposited {request.Amount:0.00} EGP. Your new balance is {user.WalletBalance:0.00} EGP.";
                }
                catch
                {
                    try { await transaction.RollbackAsync(cancellationToken); } catch { }
                    throw new Exception("Deposit failed during database transaction. No funds were added.");
                }
            });
        }

        public async Task<List<WalletTransactionResponseDto>> GetTransactionHistoryAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.WalletTransactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Select(t => new WalletTransactionResponseDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = ToTransactionTypeToken(t.Type),
                    Description = t.Description,
                    BookingId = t.BookingId,
                    CreatedAt = DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc)
                })
                .ToListAsync(cancellationToken);
        }

        private static string ToTransactionTypeToken(TransactionType type)
        {
            var name = type.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var builder = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c) && i > 0)
                    builder.Append('_');

                builder.Append(char.ToUpperInvariant(c));
            }

            return builder.ToString();
        }
    }
}
