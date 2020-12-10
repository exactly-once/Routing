using System;
using System.IO;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;

namespace ExactlyOnce.Routing.AcceptanceTesting.TestPersistence
{
    public class TestSubscriptionPersistence : Feature
    {
        public const string StorageLocationKey = "TestPersistence.StoragePath";

        internal TestSubscriptionPersistence()
        {
#pragma warning disable 618
            DependsOn<MessageDrivenSubscriptions>();
#pragma warning restore 618
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.TryGet<string>(StorageLocationKey, out var storagePath))
            {
                var solutionRoot = FindSolutionRoot();
                storagePath = Path.Combine(solutionRoot, ".learningpersistence");
            }

            context.Container.ConfigureComponent(
                b => new TestSubscriptionStorage(storagePath), DependencyLifecycle.SingleInstance);
        }

        static string FindSolutionRoot()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            while (true)
            {
                if (Directory.EnumerateFiles(directory).Any(file => file.EndsWith(".sln")))
                {
                    return directory;
                }

                var parent = Directory.GetParent(directory);

                if (parent == null)
                {
                    // throw for now. if we discover there is an edge then we can fix it in a patch.
                    throw new Exception("Couldn't find the solution directory for the learning transport. If the endpoint is outside the solution folder structure, make sure to specify a storage directory using the 'EndpointConfiguration.UseTransport<TestTransport>().StorageDirectory()' API.");
                }

                directory = parent.FullName;
            }
        }
    }
}