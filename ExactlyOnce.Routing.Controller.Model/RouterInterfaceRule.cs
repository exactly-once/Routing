using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInterfaceRule
    {
        public string Router { get; }
        public string SourceInterfacePattern { get; }
        public string DestinationInterfacePattern { get; }
        public RouterInterfaceRuleAction Action;

        Lazy<Regex> sourceExpression;
        Lazy<Regex> destinationExpression;

        [JsonConstructor]
        public RouterInterfaceRule(string router, string sourceInterfacePattern, string destinationInterfacePattern, RouterInterfaceRuleAction action)
        {
            Router = router;
            SourceInterfacePattern = sourceInterfacePattern;
            DestinationInterfacePattern = destinationInterfacePattern;
            Action = action;
            sourceExpression = new Lazy<Regex>(() => new Regex(SourceInterfacePattern, RegexOptions.Compiled));
            destinationExpression = new Lazy<Regex>(() => new Regex(DestinationInterfacePattern, RegexOptions.Compiled));
        }

        public RouterInterfaceRuleAction? Evaluate(Connection connectionCandidate)
        {
            if (connectionCandidate.Router != Router)
            {
                return null;
            }

            if (!sourceExpression.Value.IsMatch(connectionCandidate.SourceSite) ||
                !destinationExpression.Value.IsMatch(connectionCandidate.DestinationSite))
            {
                return null;
            }

            return Action;
        }
    }
}