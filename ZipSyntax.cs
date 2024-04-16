using System.Text;

namespace zip2;

[Command(name: CommandMaker.HelpText, shortcut: "-?")]
public class Syntax : ICommandMaker
{
    public MyCommand Make()
    {
        return new MyCommand(invoke: (_) =>
        {
            var tmp = new StringBuilder();
            foreach (var (attr, type) in Helper.GetMainCommands()
                .Where((it) => it.Item2.Name != nameof(Syntax))
                .Where((it) => it.Item2.Name != "Version")
                )
            {
                if (string.IsNullOrEmpty(attr.Shortcut))
                {
                    tmp.AppendLine(attr.Help);
                }
                else
                {
                    tmp.AppendLine(
                        $"{type.Name,-7}:  {attr.Shortcut}  {attr.Name}");
                    tmp.AppendLine(attr.Help);
                }
                tmp.AppendLine();
            }
            tmp.Append("""
                Help to create  : zip2 -c?
                Help to list    : zip2 -t?
                Help to extract : zip2 -x?

                https://github.com/ck-yung/zip2/blob/master/README.md
                """
                );
            Console.Write(tmp.ToString());
            return true;
        });
    }
}
