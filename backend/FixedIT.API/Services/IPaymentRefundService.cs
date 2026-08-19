using FixedIT.API.Models;

namespace FixedIT.API.Services;

public interface IPaymentRefundService
{
    Task RefundAsync(Payment payment, CancellationToken cancellationToken);
}
