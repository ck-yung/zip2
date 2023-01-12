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
                .Where((it) => it.Item2.Name != nameof(Syntax)))
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
            tmp.Append(
            """
            For example:
            Extract a zip to dir 'restoreToDir'
              zip2 -xf ..\backup.zip -O restoreToDir

            Store files, which timestamp is within 2 days, into a new zip file.
              dir2 src\ -bsp --within 2day | zip2 -cf ..\new.zip -3T -
            """);
            Console.Write(tmp.ToString());
            return true;
        });
    }
}
