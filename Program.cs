using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace SnippetRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var snippets = DiscoverSnippets();

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
            PrintTree(snippets.Keys);

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
        private static void PrintTree(IEnumerable<string> snippetKeys)
        {
            Console.WriteLine("Available Snippets:");

            var grouped = snippetKeys
                .OrderBy(k => k)
                .GroupBy(k => k.Contains("/") ? k[..k.LastIndexOf("/")] : "");

            foreach (var group in grouped)
            {
                if (!string.IsNullOrEmpty(group.Key))
                    Console.WriteLine($"{group.Key}");
                foreach (var snippet in group.OrderBy(k => k))
                {
                    string nameOnly = snippet.Contains('/') ? snippet[(snippet.LastIndexOf('/') + 1)..] : snippet;
                    Console.WriteLine($" - {snippet} ({nameOnly})");
                }
                Console.WriteLine();
            }


        }
        private static Dictionary<string, ISnippet> DiscoverSnippets()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t =>
                {
                    var instance = (ISnippet)Activator.CreateInstance(t)!;
                    var ns = t.Namespace?.Replace("SnippetRunner.Snippets", "").ToLower() ?? "";
                    string key = string.IsNullOrEmpty(ns)
                        ? instance.Name.ToLower()
                        : $"{ns.Replace('.', '/')}/{instance.Name.ToLower()}";
                    return (key, instance);
                })
                .ToDictionary(x => x.key, x => x.instance);
        }
    }
}