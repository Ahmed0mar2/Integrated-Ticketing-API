using GP.Application.DTOs.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.Interfaces
{
    public interface IWalletService
    {
        Task<string> DepositAsync(int userId, DepositRequestDto request, CancellationToken cancellationToken = default);
        Task<List<WalletTransactionResponseDto>> GetTransactionHistoryAsync(int userId, CancellationToken cancellationToken = default);
    }
}
