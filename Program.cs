using System.Reflection;


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
         private static Dictionary<string, ISnippet> DiscoverSnippets()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t =>
                {
                    var instance = (ISnippet)Activator.CreateInstance(t)!;

                    var ns = t.Namespace?.Replace("SnippetRunner.Snippets.", "").ToLower() ?? "";
                    string key = string.IsNullOrEmpty(ns)
                        ? instance.Name.ToLower()
                        : $"{ns.Replace('.', '/')}/{instance.Name.ToLower()}";

                    return (key, instance);
                })
                .ToDictionary(x => x.key, x => x.instance);
        }

        private static void PrintTree(IEnumerable<string> snippetKeys)
        {
            var sorted = snippetKeys.OrderBy(k => k).ToList();

            // Build tree structure
            var root = new TreeNode("");

            foreach (var key in sorted)
            {
                var parts = key.Split('/');
                var current = root;
                foreach (var part in parts)
                {
                    if (!current.Children.ContainsKey(part))
                        current.Children[part] = new TreeNode(part);
                    current = current.Children[part];
                }
                current.IsLeaf = true;
            }

            // Print recursively
            PrintNode(root, "");
        }

        private static void PrintNode(TreeNode node, string indent, bool isLast = true)
        {
            if (!string.IsNullOrEmpty(node.Name))
            {
                Console.Write(indent);
                Console.Write(isLast ? "└── " : "├── ");
                Console.WriteLine(node.Name);
                indent += isLast ? "    " : "│   ";
            }

            var childCount = node.Children.Count;
            int i = 0;
            foreach (var child in node.Children.Values.OrderBy(c => c.Name))
            {
                PrintNode(child, indent, ++i == childCount);
            }
        }

        private class TreeNode
        {
            public string Name { get; }
            public Dictionary<string, TreeNode> Children { get; }
            public bool IsLeaf { get; set; }

            public TreeNode(string name)
            {
                Name = name;
                Children = new Dictionary<string, TreeNode>();
                IsLeaf = false;
            }
        }
    }
}