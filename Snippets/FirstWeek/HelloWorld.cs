namespace SnippetRunner.Snippets.FirstWeek
{
    class HelloWorldSnippet : ISnippet
    {
        public string Name => "hello";
        public string Description => "Prints Hello World";
        public void Run(string[] args)
        {
            Console.WriteLine("Hello, world!");
        }
    }
}