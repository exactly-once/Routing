using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class Search
    {
        readonly Dictionary<Type, Func<object, string>> searchKeyGetters = new Dictionary<Type, Func<object, string>>();

        public Search()
        {
            RegisterSearchKey<Endpoint>(x => x.Name);
            RegisterSearchKey<LegacyEndpoint>(x => x.Name);
            RegisterSearchKey<Router>(x => x.Name);
            RegisterSearchKey<MessageRouting>(x => x.MessageType);
            RegisterSearchKey<Topology>(x => null);
            RegisterSearchKey<RoutingTable>(x => null);
        }

        public string GetSearchKey<T>(T entity)
        {
            return searchKeyGetters[typeof(T)](entity);
        }

        public void RegisterSearchKey<T>(Func<T, string> getSearchKey)
        {
            searchKeyGetters[typeof(T)] = o => getSearchKey((T)o);
        }
    }
}