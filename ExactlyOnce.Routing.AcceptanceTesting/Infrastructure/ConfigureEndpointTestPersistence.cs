using System;
using System.IO;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NUnit.Framework;
using SampleInfrastructure.TestPersistence;

public class ConfigureEndpointTestPersistence
{
    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var testRunId = TestContext.CurrentContext.Test.ID;

        string tempDir;

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            //can't use bin dir since that will be too long on the build agents
            tempDir = @"c:\temp";
        }
        else
        {
            tempDir = Path.GetTempPath();
        }

        var storageDir = Path.Combine(tempDir, testRunId, "Subscriptions");

        configuration.UsePersistence<TestPersistence>().StorageDirectory(storageDir);
        return Task.FromResult(0);
    }
}