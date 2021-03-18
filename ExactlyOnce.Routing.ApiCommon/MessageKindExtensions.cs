using System;
using MessageKind = ExactlyOnce.Routing.Controller.Model.MessageKind;

namespace ExactlyOnce.Routing.ApiCommon
{
    public static class MessageKindExtensions
    {
        public static MessageKind MapMessageKind(this ApiContract.MessageKind value)
        {
            return value switch
            {
                ApiContract.MessageKind.Command => MessageKind.Command,
                ApiContract.MessageKind.Event => MessageKind.Event,
                ApiContract.MessageKind.Message => MessageKind.Message,
                ApiContract.MessageKind.Undefined => MessageKind.Undefined,
                _ => throw new Exception($"Unrecognized message kind: {value}")
            };
        }

        public static ApiContract.MessageKind MapMessageKind(this MessageKind value)
        {
            return value switch
            {
                MessageKind.Command => ApiContract.MessageKind.Command,
                MessageKind.Event => ApiContract.MessageKind.Event,
                MessageKind.Message => ApiContract.MessageKind.Message,
                MessageKind.Undefined => ApiContract.MessageKind.Undefined,
                _ => throw new Exception($"Unrecognized message kind: {value}")
            };
        }
    }
}
