using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Destination
    {
        [JsonConstructor]
        public Destination(string handler, string endpoint, DestinationState state, MessageKind messageKind, List<string> sites)
        {
            Handler = handler;
            Endpoint = endpoint;
            State = state;
            MessageKind = messageKind;
            Sites = sites;
        }

        public string Endpoint { get; }
        public string Handler { get; private set; }
        public DestinationState State { get; private set; }
        public MessageKind MessageKind { get; private set; }
        public List<string> Sites { get; }

        public bool Deactivate()
        {
            if (State == DestinationState.Inactive)
            {
                throw new Exception("Cannot deactivate an inactive destination");
            }

            if (State == DestinationState.DeadEnd)
            {
                return true; //We can remove this destination now
            }

            State = DestinationState.Inactive;
            return false;
        }

        public void Activate()
        {
            if (State != DestinationState.Inactive)
            {
                throw new Exception("Only inactive destination can be activated");
            }

            State = DestinationState.Active;
        }

        public void HandlerAdded(string handlerSite)
        {
            if (!Sites.Contains(handlerSite))
            {
                Sites.Add(handlerSite);
            }
        }

        public bool HandlerRemoved(string handlerSite)
        {
            Sites.Remove(handlerSite);
            if (Sites.Count == 0)
            {
                if (State == DestinationState.Inactive)
                {
                    return true; //We can remove this direction
                }

                if (State == DestinationState.Active)
                {
                    State = DestinationState.DeadEnd;
                    return false;
                }
            }
            return false;
        }

        public void MessageKindChanged(MessageKind messageKind)
        {
            MessageKind = messageKind;
        }

        public void Migrate(string handlerSite, string handlerType)
        {
            if (!Sites.Contains(handlerSite))
            {
                Sites.Add(handlerSite);
            }

            Handler = handlerType;
        }
    }
}