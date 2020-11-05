//// Copyright (c) .NET Foundation. All rights reserved.
//// Licensed under the MIT License. See License.txt in the project root for license information.

//using System;
//using System.IO;
//using System.Reflection;
//using System.Threading;
//using System.Threading.Tasks;
//using ExactlyOnce.Routing.Controller.Model.Azure;
//using Microsoft.Azure.WebJobs.Description;
//using Microsoft.Azure.WebJobs.Host.Bindings;
//using Microsoft.Azure.WebJobs.Host.Config;
//using Microsoft.Azure.WebJobs.Host.Protocols;
//using Microsoft.Azure.Storage.Queue;
//using Newtonsoft.Json.Linq;

//namespace Microsoft.Azure.WebJobs.Host.Queues.Config
//{
//    [Extension("AzureStorageQueuesExt")]
//    internal class QueuesExtensionConfigProvider : IExtensionConfigProvider
//    {
//        private readonly StorageAccountProvider _storageAccountProvider;

//        public QueuesExtensionConfigProvider(StorageAccountProvider storageAccountProvider)
//        {
//            _storageAccountProvider = storageAccountProvider;
//        }

//        public void Initialize(ExtensionConfigContext context)
//        {
//            if (context == null)
//            {
//                throw new ArgumentNullException(nameof(context));
//            }

//            var config = new PerHostConfig();
//            config.Initialize(context, _storageAccountProvider);
//        }

//        // $$$ Get rid of PerHostConfig part?  
//        // Multiple JobHost objects may share the same JobHostConfiguration.
//        // But queues have per-host instance state (IMessageEnqueuedWatcher). 
//        // so capture that and create new binding rules per host instance. 
//        private class PerHostConfig : IConverter<QueueAttribute, IAsyncCollector<CloudQueueMessage>>
//        {
//            // Fields that the various binding funcs need to close over. 
//            private StorageAccountProvider _accountProvider;


//            public void Initialize(ExtensionConfigContext context, StorageAccountProvider storageAccountProvider)
//            {
//                _accountProvider = storageAccountProvider;

//                // IStorageQueueMessage is the core testing interface 
//                var binding = context.AddBindingRule<QueueAttribute>();
//                binding
//                    .AddConverter<EventMessage, CloudQueueMessage>(ConvertEventMessageToCloudQueueMessage);

//                binding.AddValidator(ValidateQueueAttribute);

//                binding.SetPostResolveHook(ToWriteParameterDescriptorForCollector)
//                        .BindToCollector<CloudQueueMessage>(this);
//            }

//            async Task<CloudQueueMessage> ConvertEventMessageToCloudQueueMessage(EventMessage arg, Attribute attrResolved, ValueBindingContext context)
//            {
//                var attr = (QueueAttribute)attrResolved;
//                var jobj = await SerializeToJobject(arg, attr, context);
//                var msg = ConvertJObjectToCloudQueueMessage(jobj, attr);
//                return msg;
//            }

//            private CloudQueueMessage ConvertJObjectToCloudQueueMessage(JObject obj, QueueAttribute attrResolved)
//            {
//                var json = obj.ToString(); // convert to JSon
//                return ConvertStringToCloudQueueMessage(json, attrResolved);
//            }

//            // Hook JObject serialization to so we can stamp the object with a causality marker. 
//            private static Task<JObject> SerializeToJobject(object input, Attribute attrResolved, ValueBindingContext context)
//            {
//                JObject objectToken = JObject.FromObject(input, JsonSerialization.Serializer);
//                var functionInstanceId = context.FunctionInstanceId;

//                return Task.FromResult<JObject>(objectToken);
//            }

//            // Asyncollector version. Write-only 
//            private ParameterDescriptor ToWriteParameterDescriptorForCollector(QueueAttribute attr, ParameterInfo parameter, INameResolver nameResolver)
//            {
//                return ToParameterDescriptorForCollector(attr, parameter, nameResolver, FileAccess.Write);
//            }

//            private ParameterDescriptor ToParameterDescriptorForCollector(QueueAttribute attr, ParameterInfo parameter, INameResolver nameResolver, FileAccess access)
//            {
//                var account = _accountProvider.Get(attr.Connection, nameResolver);
//                var accountName = account.Name;

//                return new QueueParameterDescriptor
//                {
//                    Name = parameter.Name,
//                    AccountName = accountName,
//                    QueueName = NormalizeQueueName(attr, nameResolver),
//                    Access = access
//                };
//            }

//            private static string NormalizeQueueName(QueueAttribute attribute, INameResolver nameResolver)
//            {
//                string queueName = attribute.QueueName;
//                if (nameResolver != null)
//                {
//                    queueName = nameResolver.ResolveWholeString(queueName);
//                }
//                queueName = queueName.ToLowerInvariant(); // must be lowercase. coerce here to be nice.
//                return queueName;
//            }

//            // This is a static validation (so only %% are resolved; not {} ) 
//            // For runtime validation, the regular builder functions can do the resolution.
//            private void ValidateQueueAttribute(QueueAttribute attribute, Type parameterType)
//            {
//                string queueName = NormalizeQueueName(attribute, null);

//                // Queue pre-existing  behavior: if there are { }in the path, then defer validation until runtime. 
//                if (!queueName.Contains("{"))
//                {
//                    QueueClient.ValidateQueueName(queueName);
//                }
//            }

//            private CloudQueueMessage ConvertByteArrayToCloudQueueMessage(byte[] arg, QueueAttribute attrResolved)
//            {
//                return new CloudQueueMessage(arg);
//            }

//            private CloudQueueMessage ConvertStringToCloudQueueMessage(string arg, QueueAttribute attrResolved)
//            {
//                return new CloudQueueMessage(arg);
//            }

//            public IAsyncCollector<CloudQueueMessage> Convert(QueueAttribute attrResolved)
//            {
//                var queue = GetQueue(attrResolved);
//                return new QueueAsyncCollector(queue);
//            }

//            internal CloudQueue GetQueue(QueueAttribute attrResolved)
//            {
//                var account = _accountProvider.Get(attrResolved.Connection);
//                var client = account.CreateCloudQueueClient();

//                string queueName = attrResolved.QueueName.ToLowerInvariant();
//                QueueClient.ValidateQueueName(queueName);

//                return client.GetQueueReference(queueName);
//            }
//        }

//        private class QueueBuilder :
//            IAsyncConverter<QueueAttribute, CloudQueue>
//        {
//            private readonly PerHostConfig _bindingProvider;

//            public QueueBuilder(PerHostConfig bindingProvider)
//            {
//                _bindingProvider = bindingProvider;
//            }

//            async Task<CloudQueue> IAsyncConverter<QueueAttribute, CloudQueue>.ConvertAsync(
//                QueueAttribute attrResolved,
//                CancellationToken cancellation)
//            {
//                CloudQueue queue = _bindingProvider.GetQueue(attrResolved);
//                await queue.CreateIfNotExistsAsync(cancellation);
//                return queue;
//            }
//        }

//        // The core Async Collector for queueing messages. 
//        internal class QueueAsyncCollector : IAsyncCollector<CloudQueueMessage>
//        {
//            private readonly CloudQueue _queue;

//            public QueueAsyncCollector(CloudQueue queue)
//            {
//                this._queue = queue;
//            }

//            public async Task AddAsync(CloudQueueMessage message, CancellationToken cancellationToken = default(CancellationToken))
//            {
//                if (message == null)
//                {
//                    throw new InvalidOperationException("Cannot enqueue a null queue message instance.");
//                }

//                await _queue.AddMessageAndCreateIfNotExistsAsync(message, cancellationToken);
//            }

//            public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
//            {
//                // Batching not supported. 
//                return Task.FromResult(0);
//            }
//        }
//    }
//}