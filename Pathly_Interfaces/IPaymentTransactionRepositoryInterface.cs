using Pathly_Models;

namespace Pathly_Interfaces
{
    public interface IPaymentTransactionRepositoryInterface : IGenericInterface<PaymentTransaction>
    {
        Task<PaymentTransaction?> GetByReferenceAsync(string reference);
    }
}
