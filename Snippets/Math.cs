namespace SnippetRunner
{
    class MathSnippet : ISnippet
    {
        public string Name => "math";
        public string Description => "adds two numbers together";

        public void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: math <a> <b>");
                return;
            }

            if (int.TryParse(args[0], out int a) && int.TryParse(args[1], out int b))
                Console.WriteLine($"{a} plus {b} = {a + b}");
            else
                Console.WriteLine("Invalid Numbers");
        }
    }
}