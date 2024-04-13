namespace zip2;

static internal partial class My
{
    class SingleValueOption<T, R> : IOption, IInvokeOption<T, R>
    {
        public string Name { get; init; }
        public string Shortcut { get; init; }
        public string Help { get; init; }
        public SingleValueOption(string name,
            Func<T,R> init,
            Func<IOption, string, Func<T, R>?> resolve,
            string shortcut="", string help="")
        {
            Name= name;
            Shortcut= shortcut;
            Help= help;
            InvokeImp = init;
            Resolve = (the, args) =>
            {
                var aa = args.Take(2).ToArray();
                if (aa.Length > 1) throw new MyArgumentException(
                    $"Too many values ({aa[0]},{aa[1]}) to {the.Name}");
                if (aa.Length== 1 )
                {
                    var imp = resolve(the, aa[0]);
                    if (imp != null)
                    {
                        InvokeImp = imp;
                    }
                }
            };
        }

        public Action<IOption, IEnumerable<string>> Resolve { get; init; }

        public IEnumerable<FlagedArg> Parse(
            IEnumerable<FlagedArg> args)
        {
            var it = args.GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (current.Arg == Name)
                {
                    if (it.MoveNext())
                    {
                        yield return new FlagedArg(true, it.Current.Arg);
                    }
                    else
                    {
                        throw new MyArgumentException(
                            $"Value is required to {Name}");
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        Func<T, R> InvokeImp { get; set; }
        public R Invoke(T arg)
        {
            return InvokeImp(arg);
        }
    }

    class ManyValuesOption<T, R> : IOption, IInvokeOption<T, R>
    {
        public string Name { get; init; }
        public string Shortcut { get; init; }
        public string Help { get; init; }
        public ManyValuesOption(string name,
            Func<T, R> init,
            Func<IOption, string[], Func<T, R>?> resolve,
            string shortcut = "", string help = "")
        {
            Name = name;
            Shortcut = shortcut;
            Help = help;
            InvokeImp = init;
            Resolve = (the, args) =>
            {
                var aa = args.ToArray();
                if (aa.Length > 0)
                {
                    var imp = resolve(the, aa);
                    if (imp != null)
                    {
                        InvokeImp = imp;
                    }
                }
            };
        }

        public Action<IOption, IEnumerable<string>> Resolve { get; init; }

        public IEnumerable<FlagedArg> Parse(
            IEnumerable<FlagedArg> args)
        {
            var it = args.GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (current.Arg == Name)
                {
                    if (it.MoveNext())
                    {
                        yield return new FlagedArg(true, it.Current.Arg);
                    }
                    else
                    {
                        throw new MyArgumentException(
                            $"Value is required to {Name}");
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }

        Func<T, R> InvokeImp { get; set; }
        public R Invoke(T arg)
        {
            return InvokeImp(arg);
        }
    }

    class NoValueOption<T, R> : IOption, IInvokeOption<T, R>
    {
        public string Name { get; init; }
        public string Shortcut { get; init; }
        public string Help { get; init; }
        public NoValueOption(string name,
            Func<T, R> init, Func<T, R> alt,
            string shortcut = "", string help = "")
        {
            Name = name;
            Shortcut = shortcut;
            Help = help;
            InvokeImp = init;
            Resolve = (the, args) =>
            {
                if (args.Any())
                {
                    InvokeImp = alt;
                }
            };
        }

        internal void ChangeImp(Func<T,R> alt)
        {
            InvokeImp = alt;
        }

        public Action<IOption, IEnumerable<string>> Resolve { get; init; }

        public IEnumerable<FlagedArg> Parse(
            IEnumerable<FlagedArg> args)
        {
            var it = args.GetEnumerator();
            while (it.MoveNext())
            {
                var current = it.Current;
                if (current.Arg == Name)
                {
                    yield return new FlagedArg(true, Name);
                }
                else
                {
                    yield return current;
                }
            }
        }

        Func<T, R> InvokeImp { get; set; }
        public R Invoke(T arg)
        {
            return InvokeImp(arg);
        }
    }
}
