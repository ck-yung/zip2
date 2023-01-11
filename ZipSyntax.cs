using System.Text;

namespace zip2;

[Command(name: "--help", shortcut: "-?")]
public class Syntax : ICommandMaker
{
    public MyCommand Make()
    {
        return new MyCommand(invoke: (_) =>
        {
            var tmp = new StringBuilder();
            foreach (var (attr, type) in Helper.GetMainCommands()
                .Where((it) => it.Item2.Name != nameof(Syntax)))
            {
                if (string.IsNullOrEmpty(attr.Shortcut))
                {
                    tmp.AppendLine(attr.Help);
                }
                else
                {
                    tmp.AppendLine(
                        $"{type.Name}: {attr.Name}, {attr.Shortcut}");
                    tmp.AppendLine(attr.Help);
                }
                tmp.AppendLine();
            }

            Console.Write(tmp.ToString());
            return true;
        });
    }
}
