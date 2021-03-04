using System.Collections.Generic;
using System.Linq;
using NServiceBus.DeliveryConstraints;

namespace SampleInfrastructure.TestTransport
{
    static class DeliveryConstraintExtensions
    {
        internal static bool TryGet<T>(this List<DeliveryConstraint> list, out T constraint) where T : DeliveryConstraint
        {
            constraint = list.OfType<T>().FirstOrDefault();

            return constraint != null;
        }
    }
}