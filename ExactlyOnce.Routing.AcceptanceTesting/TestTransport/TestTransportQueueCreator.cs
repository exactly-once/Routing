namespace NServiceBus
{
    using System.IO;
    using System.Threading.Tasks;
    using Transport;

    class TestTransportQueueCreator : ICreateQueues
    {
        readonly string storagePath;
        readonly string brokerName;

        public TestTransportQueueCreator(string storagePath, string brokerName)
        {
            this.storagePath = storagePath;
            this.brokerName = brokerName;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            foreach (var queueBinding in queueBindings.ReceivingAddresses)
            {
                var queuePath = GetDirectoryName(queueBinding);
                Directory.CreateDirectory(queuePath);
            }

            foreach (var queueBinding in queueBindings.SendingAddresses)
            {
                var queuePath = GetDirectoryName(queueBinding);
                Directory.CreateDirectory(queuePath);
            }

            return Task.CompletedTask;
        }

        string GetDirectoryName(string queueBinding)
        {
            return queueBinding.IndexOf('@') == -1 
                ? Path.Combine(storagePath, queueBinding + "@" + brokerName) 
                : Path.Combine(storagePath, queueBinding);
        }
    }
}