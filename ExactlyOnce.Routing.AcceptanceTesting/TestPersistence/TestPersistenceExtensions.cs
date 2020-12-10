using ExactlyOnce.Routing.AcceptanceTesting.TestPersistence;
using NServiceBus.Configuration.AdvancedExtensibility;

namespace NServiceBus
{
    public static class TestPersistenceExtensions
    {
        public static void StorageDirectory(this PersistenceExtensions<TestPersistence> extensions, string path)
        {
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            extensions.GetSettings().Set(TestSubscriptionPersistence.StorageLocationKey, path);
        }
    }
}