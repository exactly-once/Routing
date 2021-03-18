using System.CommandLine.Invocation;
using System.CommandLine.Rendering.Views;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;

namespace ExactlyOnce.Routing.CommandLineTool
{
    abstract class ListCommand : PrintCommand
    {
        protected void Print(InvocationContext context, ListResponse data)
        {
            var table = new TableView<ListItem>
            {
                Items = data.Items,
            };
            table.AddColumn(i => i.Id, new ContentView("Id".Underline()), ColumnDefinition.Fixed(40));
            table.AddColumn(i => i.Name, new ContentView("Name".Underline()), ColumnDefinition.Star(1));

            base.Print(context, new View[]{table});
        }

        protected ListCommand(string name, string? description = null) : base(name, description)
        {
        }
    }
}