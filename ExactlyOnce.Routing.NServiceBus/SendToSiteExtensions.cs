using System;
using ExactlyOnce.Routing.NServiceBus;
using NServiceBus.Extensibility;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    /// <summary>
    /// Allows routing a given message to remote sites, similar to Gateway.
    /// </summary>
    public static class SendToSiteExtensions
    {
        /// <summary>
        /// Instructs NServiceBus to send a given message a given site.
        /// The site routing policy for the destination endpoint must be set to Explicit.
        /// </summary>
        public static void SendToSite(this SendOptions options, string site)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrEmpty(site))
            {
                throw new ArgumentException("Site name cannot be null or empty.", nameof(site));
            }
            var state = options.GetExtensions().GetOrCreate<ExplicitSite>();
            state.Site = site;
        }
    }
}