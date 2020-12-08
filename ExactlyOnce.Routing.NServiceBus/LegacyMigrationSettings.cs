using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public class LegacyMigrationSettings
    {
        internal Dictionary<string, string> LegacyDestinations { get; } = new Dictionary<string, string>();

        public void RouteToEndpoint(Type type, string destinationEndpoint)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (destinationEndpoint == null)
            {
                throw new ArgumentNullException(nameof(destinationEndpoint));
            }
            // ReSharper disable once AssignNullToNotNullAttribute
            LegacyDestinations[type.FullName] = destinationEndpoint;
        }
    }
}