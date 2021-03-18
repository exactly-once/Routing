using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;

namespace ExactlyOnce.Routing.CommandLineTool
{
    abstract class PrintCommand : Command
    {
        protected PrintCommand(string name, string? description = null)
            : base(name, description)
        {
        }
     
        protected void Print(InvocationContext context, IEnumerable<View> views)
        {
            var console = context.Console;

            if (console is ITerminal terminal)
            {
                terminal.Clear();
            }

            var stack = new StackLayoutView(Orientation.Vertical);
            foreach (var view in views)
            {
                stack.Add(view);
            }

            console.Append(stack);
        }
    }
}