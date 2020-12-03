﻿using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public interface ISender
    {
        Task Publish(EventMessage message);
    }
}