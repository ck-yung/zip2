namespace zip2;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string Name { get; init; }
    public string Shortcut { get; init; }
    public string Help { get; init; }

    public CommandAttribute(string name,
        string shortcut = "", string help = "")
    {
        Shortcut = shortcut;
        Name = name;
        Help = help;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DefaultCommandAttribute: Attribute
{

}

public class MyArgumentException: Exception
{
    public MyArgumentException(string message):
        base(message)
    {
    }
}
