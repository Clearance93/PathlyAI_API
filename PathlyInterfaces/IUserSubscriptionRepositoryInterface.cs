using Pathly_Models;

namespace PathlyInterfaces
{
    public interface IUserSubscriptionRepositoryInterface : IGenericInterface<UserSubscription>
    {
        /// <summary>The user's current active subscription (with its plan), if any.</summary>
        Task<UserSubscription?> GetActiveForUserAsync(string userId);

        Task<UserSubscription?> GetByProviderRefAsync(string providerSubscriptionRef);
    }
}
