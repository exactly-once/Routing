using System.Threading.Tasks;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    class DefaultErrorHandlingPolicy : IErrorHandlingPolicy
    {
        string errorQueue;
        int immediateRetryCount;

        public DefaultErrorHandlingPolicy(string errorQueue, int immediateRetryCount)
        {
            this.errorQueue = errorQueue;
            this.immediateRetryCount = immediateRetryCount;
        }
        
        public Task<ErrorHandleResult> OnError(IErrorHandlingPolicyContext handlingContext, IDispatchMessages dispatcher)
        {
            if (handlingContext.Error.ImmediateProcessingFailures < immediateRetryCount)
            {
                return Task.FromResult(ErrorHandleResult.RetryRequired);
            }
            return handlingContext.MoveToErrorQueue(errorQueue);
        }
    }
}