using System.Threading.Tasks;

namespace SampleInfrastructure.TestTransport
{
    static class TaskEx
    {
        // ReSharper disable once UnusedParameter.Global
        // Used to explicitly suppress the compiler warning about
        // using the returned value from async operations
        public static void Ignore(this Task task)
        {
        }
    }
}