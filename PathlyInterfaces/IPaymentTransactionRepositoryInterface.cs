using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IPaymentTransactionRepositoryInterface : IGenericInterface<PaymentTransaction>
    {
        Task<PaymentTransaction?> GetByReferenceAsync(string reference);
    }
}
