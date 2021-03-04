using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using SampleInfrastructure.TestTransport;

namespace SampleInfrastructure.TestPersistence
{
    public static class TestPersistenceExtensions
    {
        public static void StorageDirectory(this PersistenceExtensions<NServiceBus.TestPersistence> extensions, string path)
        {
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            extensions.GetSettings().Set(TestSubscriptionPersistence.StorageLocationKey, path);
        }
    }
}