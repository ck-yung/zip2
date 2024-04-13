namespace zip2;

internal interface ICommandMaker
{
    MyCommand Make();
}

public record FlagedArg(bool Selected, string Arg);

public interface IOption
{
    string Name { get; }
    string Shortcut { get; }
    string Help { get; }
    IEnumerable<FlagedArg> Parse(
        IEnumerable<FlagedArg> args);
    Action<IOption, IEnumerable<string>> Resolve { get; }
}

internal interface IInvokeOption<T, R>
{
    R Invoke(T arg);
}
