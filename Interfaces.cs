namespace zip2;

internal interface ICommandMaker
{
    MyCommand Make();
}

public interface IOption
{
    string Name { get; }
    string Shortcut { get; }
    string Help { get; }
    IEnumerable<(bool, string)> Parse(
        IEnumerable<(bool, string)> args);
    Action<IOption, IEnumerable<string>> Resolve { get; }
}

internal interface IInvokeOption<T, R>
{
    R Invoke(T arg);
}
