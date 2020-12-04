using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IStateStore
    {
        Task Initialize();
        Task<(State, string)> Load(string stateId, Type stateType, CancellationToken cancellationToken = default);
        Task<string> Upsert(string stateId, State value, string version, CancellationToken cancellationToken = default);
    }
}