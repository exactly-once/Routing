using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointInstance
    {
        [JsonConstructor]
        public EndpointInstance(string instanceId, string inputQueue, List<MessageHandlerInstance> messageHandlers, Dictionary<string, MessageKind> recognizedMessages, string site)
        {
            InstanceId = instanceId;
            InputQueue = inputQueue;
            MessageHandlers = messageHandlers;
            RecognizedMessages = recognizedMessages;
            Site = site;
        }

        public string InstanceId { get; }
        public string InputQueue { get; private set; }
        public string Site { get; private set; }
        public List<MessageHandlerInstance> MessageHandlers { get; private set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; }

        public void Move(string site)
        {
            Site = site;
        }

        public List<string> Update(List<MessageHandlerInstance> messageHandlers,
            Dictionary<string, MessageKind> newRecognizedMessages, 
            string inputQueue)
        {
            MessageHandlers = messageHandlers;
            InputQueue = inputQueue;
            var modifications = new List<Action<Dictionary<string, MessageKind>>>();
            var modifiedKeys = new List<string>();

            foreach (var (type, kind) in newRecognizedMessages)
            {
                if (!RecognizedMessages.ContainsKey(type))
                {
                    //Message has been added
                    modifications.Add(x => x.Add(type, kind));
                    modifiedKeys.Add(type);
                }
                else if (newRecognizedMessages[type] != RecognizedMessages[type])
                {
                    //Message type has changed
                    modifications.Add(x => x[type] = kind);
                    modifiedKeys.Add(type);
                }
            }

            foreach (var type in RecognizedMessages.Keys)
            {
                if (!newRecognizedMessages.ContainsKey(type))
                {
                    //Message has been removed
                    modifications.Add(x => x.Remove(type));
                    modifiedKeys.Add(type);
                }
            }

            foreach (var modification in modifications)
            {
                modification(RecognizedMessages);
            }

            return modifiedKeys;
        }
    }
}