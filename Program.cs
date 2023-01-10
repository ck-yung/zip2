namespace zip2;

class Program
{
    static public void Main(string[] args)
    {
		try
		{
            RunMain(args);
		}
		catch (Exception ee)
		{
			Console.WriteLine(ee);
		}
    }

    static public void RunMain(string[] args)
    {
        Console.WriteLine($"RunMain #args:{args.Length}");
    }
}
