using System.Threading.Tasks;

namespace SampleInfrastructure.TestTransport
{
    interface ITestTransportTransaction
    {
        string FileToProcess { get; }

        Task<bool> BeginTransaction(string incomingFilePath);

        Task Commit();

        void Rollback();

        void ClearPendingOutgoingOperations();

        Task Enlist(string messagePath, string messageContents);

        bool Complete();
    }
}