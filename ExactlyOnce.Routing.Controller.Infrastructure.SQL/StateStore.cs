using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Infrastructure.SQL
{
    public class StateStore : IStateStore
    {
        readonly Func<DbConnection> connectionFactory;
        readonly Dialect dialect;
        readonly JsonSerializer serializer = new JsonSerializer();

        public StateStore(Func<DbConnection> connectionFactory, Dialect dialect)
        {
            this.connectionFactory = connectionFactory;
            this.dialect = dialect;
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public async Task<(State, object)> Load(string stateId, Type stateType, CancellationToken cancellationToken = default)
        {
            using (var connection = connectionFactory())
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = dialect.StateLoadQuery;

                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "stateId";
                    idParam.Value = stateId;
                    command.Parameters.Add(idParam);

                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    if (await reader.ReadAsync(cancellationToken))
                    {
                        var version = reader.GetInt32(0);
                        using (var serializedData = reader.GetStream(1))
                        {
                            using (var streamReader = new StreamReader(serializedData))
                            {
                                using (var textReader = new JsonTextReader(streamReader))
                                {
                                    var state = (State)serializer.Deserialize(textReader, stateType);

                                    return (state, version);
                                }
                            }
                        }
                    }
                }
            }

            var newState = (State)Activator.CreateInstance(stateType);
            newState.Id = stateId;
            return (newState, null);
        }

        public async Task<object> Upsert(string stateId, State value, object version, CancellationToken cancellationToken = default)
        {
            using (var connection = connectionFactory())
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = dialect.StateLoadQuery;

                    var idParam = command.CreateParameter();
                    idParam.ParameterName = "stateId";
                    idParam.Value = stateId;
                    command.Parameters.Add(idParam);

                    var versionValue = (int?) version + 1 ?? 0;
                    var versionParam = command.CreateParameter();
                    versionParam.ParameterName = "version";
                    versionParam.Value = versionValue;
                    command.Parameters.Add(versionParam);

                    var stateParam = command.CreateParameter();
                    stateParam.ParameterName = "state";
                    stateParam.Value = stateId;
                    command.Parameters.Add(stateParam);

                    var keywordParam = command.CreateParameter();
                    keywordParam.ParameterName = "searchKey";
                    keywordParam.Value = value.SearchKey;
                    command.Parameters.Add(keywordParam);

                    if (version != null)
                    {
                        var previousVersionParam = command.CreateParameter();
                        previousVersionParam.ParameterName = "prevVersion";
                        previousVersionParam.Value = version;
                        command.Parameters.Add(previousVersionParam);

                        command.CommandText = dialect.StateUpdateCommand;
                    }
                    else
                    {
                        command.CommandText = dialect.StateInsertCommand;
                    }

                    var rowsAffected = (int) await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

                    if (rowsAffected != 1)
                    {
                        throw new OptimisticConcurrencyFailure();
                    }

                    return versionParam.Value;
                }
            }
        }

        public async Task<IReadOnlyCollection<ListResult>> List(Type stateType, string searchKeyword, CancellationToken cancellationToken = default)
        {
            using (var connection = connectionFactory())
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = dialect.StateListQuery;

                    var keywordParam = command.CreateParameter();
                    keywordParam.ParameterName = "searchKey";
                    keywordParam.Value = searchKeyword;
                    command.Parameters.Add(keywordParam);

                    var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                    var result = new List<ListResult>();

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var id = reader.GetString(0);
                        var name = reader.GetString(1);

                        result.Add(new ListResult(id, name));
                    }

                    return result;
                }
            }
        }
    }
}