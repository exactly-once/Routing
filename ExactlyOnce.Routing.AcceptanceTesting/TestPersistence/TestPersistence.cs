using ExactlyOnce.Routing.AcceptanceTesting.TestPersistence;

namespace NServiceBus
{
    using Features;
    using Persistence;

    /// <summary>
    /// Used to enable test (file-based) persistence.
    /// </summary>
    public class TestPersistence : PersistenceDefinition
    {
        internal TestPersistence()
        {
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<TestSubscriptionPersistence>());
        }
    }
}