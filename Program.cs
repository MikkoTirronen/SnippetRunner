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

            Console.WriteLine("=== Snippet Runner ===\n");
            PrintTree(fullPathMap.Keys);

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
            PrintTree(fullPathMap.Keys);
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