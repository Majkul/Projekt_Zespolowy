using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System;

public interface ICardFeeService
{
    Task<(bool Success, string? Error)> TryChargeListingFeeAsync(int sellerUserId, string listingTitle, CancellationToken cancellationToken = default);
    Task TryDispatchPayoutAsync(Order order, CancellationToken cancellationToken = default);
    Task<string> CreateTokenizationOrderAsync(int userId, string customerIp, string continueUrl, CancellationToken cancellationToken = default);
}
