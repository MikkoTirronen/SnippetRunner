using System.Reflection;

namespace SnippetRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var snippets = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => (ISnippet)Activator.CreateInstance(t)!)
            .ToDictionary(s => s.Name.ToLower(), s => s);

            if (snippets.Count == 0)
            {
                Console.WriteLine("No snippets found!");
            }
            if (args.Length > 0)
            {
                string choice = args[0].ToLower();
                if (snippets.TryGetValue(choice, out var snippet))
                {
                    Console.WriteLine($"Running '{snippet.Name}' with args...\n");
                    snippet.Run(args.Skip(1).ToArray());
                }
                else
                {
                    Console.WriteLine("Invalid snippet name.");
                }
                return;
            }

            Console.WriteLine("=== Snippet Runner ===");
            PrintAvailable(snippets);

            Console.Write("\nEnter snippet name to run: ");
            string choiceInteractive = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (snippets.TryGetValue(choiceInteractive, out var chosenSnippet))
            {
                Console.WriteLine($"\nRunning '{chosenSnippet}' ...\n");
                chosenSnippet.Run(Array.Empty<string>());
            }
            else
            {
                Console.WriteLine("Invalid choice, Exiting...");
            }
        }
        private static void PrintAvailable(Dictionary<string, ISnippet> snippets)
        {
            Console.WriteLine("Available Snippets:");

            foreach (var s in snippets.Values.OrderBy(s => s.Name))
            {
                Console.WriteLine($" - {s.Name} {(string.IsNullOrEmpty(s.Description)? "": ("→ " + s.Description))}");
            }
        }
    }
}