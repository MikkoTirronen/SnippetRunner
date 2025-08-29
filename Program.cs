using System.Reflection;


namespace SnippetRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var (fullPathMap, shortNameMap) = DiscoverSnippets();


            if (args.Length > 0)
            {
                RunSnippet(args[0].ToLower(), args.Skip(1).ToArray(), fullPathMap, shortNameMap);
                return;
            }
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=== Snippet Runner ===\n");
            PrintTree(fullPathMap);

            Console.Write("\nEnter snippet name to run: ");
            string input = Console.ReadLine()?.Trim().ToLower() ?? "";

            RunSnippet(input, Array.Empty<string>(), fullPathMap, shortNameMap);
        }

        private static (Dictionary<string, ISnippet> fullPathMap,
                Dictionary<string, List<string>> shortNameMap) DiscoverSnippets()
        {
            var fullPathMap = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t =>
                {
                    var instance = (ISnippet)Activator.CreateInstance(t)!;

                    var ns = t.Namespace ?? "";
                    if (ns.StartsWith("SnippetRunner."))
                        ns = ns.Substring("SnippetRunner.".Length);

                    ns = ns.ToLower();

                    string key = string.IsNullOrEmpty(ns)
                        ? instance.Name.ToLower()
                        : $"{ns.Replace('.', '/')}/{instance.Name.ToLower()}";

                    return (key, instance);
                })
                .ToDictionary(x => x.key, x => x.instance);

            var shortNameMap = new Dictionary<string, List<string>>();
            foreach (var kvp in fullPathMap)
            {
                string shortName = kvp.Value.Name.ToLower();
                if (!shortNameMap.ContainsKey(shortName))
                    shortNameMap[shortName] = new List<string>();
                shortNameMap[shortName].Add(kvp.Key);
            }

            return (fullPathMap, shortNameMap);
        }



        private static void RunSnippet(string keyOrName, string[] args,
            Dictionary<string, ISnippet> fullPathMap,
            Dictionary<string, List<string>> shortNameMap)
        {
            // Try full path first
            if (fullPathMap.TryGetValue(keyOrName, out var snippet))
            {
                Console.WriteLine($"\n▶ Running '{keyOrName}'...\n");
                snippet.Run(args);
                return;
            }

            // Try short name
            if (shortNameMap.TryGetValue(keyOrName, out var matches))
            {
                if (matches.Count == 1)
                {
                    string fullKey = matches[0];
                    Console.WriteLine($"\n▶ Running '{fullKey}'...\n");
                    fullPathMap[fullKey].Run(args);
                }
                else
                {
                    Console.WriteLine($"❌ Ambiguous name '{keyOrName}'. Matches:");
                    foreach (var match in matches)
                        Console.WriteLine($"   - {match}");
                }
                return;
            }

            Console.WriteLine($"❌ Unknown snippet '{keyOrName}'\n");
            PrintTree(fullPathMap);
        }

        private static void PrintTree(Dictionary<string, ISnippet> snippets)
        {
            var root = new TreeNode("");

            foreach (var kvp in snippets)
            {
                var parts = kvp.Key.Split('/');
                var current = root;
                foreach (var part in parts)
                {
                    if (!current.Children.ContainsKey(part))
                        current.Children[part] = new TreeNode(part);
                    current = current.Children[part];
                }
                current.Snippet = kvp.Value; // leaf
            }

            PrintNode(root, "", isLast: true);
        }

        private static void PrintNode(TreeNode node, string indent, bool isLast)
        {
            if (!string.IsNullOrEmpty(node.Name))
            {
                string branch = isLast ? "└── " : "├── ";
                string linePrefix = indent + branch + node.Name;
                string contPrefix = indent + (isLast ? "    " : "│   ");

                if (node.Snippet != null && !string.IsNullOrWhiteSpace(node.Snippet.Description))
                {
                    string delim = "  -  ";
                    Console.WriteLine(linePrefix + delim + node.Snippet.Description);
                }
                else
                {
                    // just the name
                    Console.WriteLine(linePrefix);
                }

                // indent for children
                indent = contPrefix;
            }

            var children = node.Children.Values.OrderBy(c => c.Name).ToList();
            for (int i = 0; i < children.Count; i++)
            {
                bool childIsLast = (i == children.Count - 1);
                PrintNode(children[i], indent, childIsLast);
            }
        }




        private sealed class TreeNode
        {
            public string Name { get; }
            public Dictionary<string, TreeNode> Children { get; } = new();
            public ISnippet? Snippet { get; set; }
            public TreeNode(string name) => Name = name;
        }

    }
}
