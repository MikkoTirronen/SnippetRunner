namespace SnippetRunner.FirstWeek
{
    public class BrokenSnippet : ISnippet
    {
        public string Name => "Broken";
        public string Description => "Testing exception handling";
        public BrokenSnippet()
        {
            // Boom: this runs when DiscoverSnippets tries to create the instance
            throw new InvalidOperationException("This snippet is broken on purpose!");
        }

        public void Run(string[] args)
        {
            Console.WriteLine("You should never see this, constructor throws first.");
        }
    }
}
