using Microsoft.IO;

namespace ExactlyOnce.Routing.SelfHostedController
{
    static class Memory
    {
        public static readonly RecyclableMemoryStreamManager Manager = new RecyclableMemoryStreamManager();
    }
}