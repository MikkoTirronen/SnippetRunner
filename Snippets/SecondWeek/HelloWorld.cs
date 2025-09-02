namespace SnippetRunner.Snippets.SecondWeek
{
    class HelloWorldSnippet : ISnippet
    {
        public string Name => "hello";
        public string Description => "prints hello world";
        public void Run(string[] args)
        {
            Console.WriteLine("Hello, world!");
        }
    }
}