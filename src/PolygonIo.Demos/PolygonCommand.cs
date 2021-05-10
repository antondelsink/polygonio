using System.Text;

namespace PolygonIo.Demos
{
    public class PolygonCommand
    {
        public string Action { get; set; }
        public string Params { get; set; }

        public override string ToString()
        {
            var cmd = new StringBuilder();
            cmd.Append(@"{""action"":""");
            cmd.Append(Action);
            cmd.Append(@""",""params"":""");
            cmd.Append(Params);
            cmd.Append(@"""}");
            return cmd.ToString();
        }
    }
}