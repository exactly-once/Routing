using Microsoft.IO;

namespace ExactlyOnce.Routing.AzureController
{
    static class Memory
    {
        public static readonly RecyclableMemoryStreamManager Manager = new RecyclableMemoryStreamManager();
    }
}