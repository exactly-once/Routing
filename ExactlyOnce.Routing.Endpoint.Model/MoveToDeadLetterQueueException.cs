using System;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    [Serializable]
    public class MoveToDeadLetterQueueException : Exception
    {
        public MoveToDeadLetterQueueException(string reason) 
            : base(reason)
        {

        }
    }
}