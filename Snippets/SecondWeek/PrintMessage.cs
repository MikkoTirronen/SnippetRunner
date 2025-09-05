namespace SnippetRunner.Snippets.SecondWeek
{
    class PrintMessageClass : ISnippet
    {
        public string Name => "PrintMessage";
        public string Description => "Prints Hello World by function";

        public void Run(string[] args)
        {
            PrintMessage();
        }

        public void PrintMessage()
        {
            Console.WriteLine("Hello World by Function");
        }
    }
}