using System.Reflection;

namespace SnippetRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var (fullPathMap, shortNameMap, brokenSnippets, platformNotes) = DiscoverSnippets();

            if (args.Length > 0)
            {
                RunSnippet(args[0].ToLower(), args[1..], fullPathMap, shortNameMap, brokenSnippets, platformNotes);
                return;
            }

            Console.WriteLine("\n=== Snippet Runner ===\n");
            PrintTree(fullPathMap, brokenSnippets, platformNotes);

            Console.Write("\nEnter snippet name to run: ");
            var inputLine = Console.ReadLine() ?? "";
            var parts = inputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
                RunSnippet(parts[0].ToLower(), parts[1..], fullPathMap, shortNameMap, brokenSnippets, platformNotes);

            Console.WriteLine("\n==== Ending Program ====\n");
        }

        private static (
            Dictionary<string, ISnippet> fullPathMap,
            Dictionary<string, List<string>> shortNameMap,
            HashSet<string> brokenSnippets,
            Dictionary<string, string> platformNotes
        ) DiscoverSnippets()
        {
            var fullPathMap = new Dictionary<string, ISnippet>();
            var shortNameMap = new Dictionary<string, List<string>>();
            var brokenSnippets = new HashSet<string>();
            var platformNotes = new Dictionary<string, string>();

            var snippetTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var t in snippetTypes)
            {
                string ns = t.Namespace?.Replace("SnippetRunner.", "")?.ToLower() ?? "";
                string folder = string.IsNullOrEmpty(ns) ? "snippets" : ns.Replace('.', '/');

                try
                {
                    var instance = (ISnippet)Activator.CreateInstance(t)!;
                    string key = $"{folder}/{instance.Name.ToLower()}";

                    var platformAttr = t.GetCustomAttribute<PlatformAttribute>();
                    if (platformAttr != null)
                    {
                        platformNotes[key] = platformAttr.Platform;
#if !WINDOWS
                        if (platformAttr.Platform == "Windows")
                        {
                            brokenSnippets.Add(key);
                            continue;
                        }
#endif
                    }

                    fullPathMap[key] = instance;
                    if (!shortNameMap.ContainsKey(instance.Name.ToLower()))
                        shortNameMap[instance.Name.ToLower()] = new();
                    shortNameMap[instance.Name.ToLower()].Add(key);
                }
                catch (Exception ex)
                {
                    string brokenKey = $"{folder}/{t.Name.ToLower()}";
                    WriteWarning($"⚠ Skipping snippet '{brokenKey}' (failed to load: {ex.GetType().Name})");
                    brokenSnippets.Add(brokenKey);
                }
            }

            return (fullPathMap, shortNameMap, brokenSnippets, platformNotes);
        }

        private static void RunSnippet(string keyOrName, string[] args,
            Dictionary<string, ISnippet> fullPathMap,
            Dictionary<string, List<string>> shortNameMap,
            HashSet<string> brokenSnippets,
            Dictionary<string, string> platformNotes)
        {
            if (brokenSnippets.Contains(keyOrName))
            {
                WriteError($"❌ Snippet '{keyOrName}' cannot run (failed to load earlier).");
                return;
            }

            if (platformNotes.TryGetValue(keyOrName, out var platform))
            {
                WriteWarning($"⚠ Snippet '{keyOrName}' is platform-restricted ({platform} only).");
                return;
            }

            if (fullPathMap.TryGetValue(keyOrName, out var snippet))
            {
                try { snippet.Run(args); }
                catch (Exception ex) { WriteError($"💥 Snippet '{keyOrName}' crashed: {ex.Message}"); }
                return;
            }

            if (shortNameMap.TryGetValue(keyOrName, out var matches))
            {
                if (matches.Count == 1)
                {
                    var fullKey = matches[0];
                    if (brokenSnippets.Contains(fullKey))
                    {
                        WriteError($"❌ Snippet '{fullKey}' cannot run (failed to load earlier).");
                        return;
                    }

                    try { fullPathMap[fullKey].Run(args); }
                    catch (Exception ex) { WriteError($"💥 Snippet '{fullKey}' crashed: {ex.Message}"); }
                }
                else
                {
                    WriteWarning($"❌ Ambiguous snippet '{keyOrName}'. Matches:");
                    foreach (var match in matches) Console.WriteLine($"   - {match}");
                }
                return;
            }

            WriteError($"❌ Unknown snippet '{keyOrName}'");
            PrintTree(fullPathMap, brokenSnippets, platformNotes);
        }

        private static void PrintTree(Dictionary<string, ISnippet> fullPathMap,
            HashSet<string> brokenSnippets,
            Dictionary<string, string> platformNotes)
        {
            var root = new TreeNode("Applications:");

            foreach (var kvp in fullPathMap)
                AddPath(root, kvp.Key, kvp.Value, false, null);

            foreach (var kvp in platformNotes)
                AddPath(root, kvp.Key, null, false, kvp.Value);

            PrintNode(root, "", true);

            if (brokenSnippets.Count > 0)
            {
                Console.WriteLine("\nBroken Snippets:");
                var brokenRoot = new TreeNode("");
                foreach (var broken in brokenSnippets)
                    AddPath(brokenRoot, broken, null, true, null);
                PrintNode(brokenRoot, "", true);
            }
        }

        private static void AddPath(TreeNode root, string path, ISnippet? snippet, bool isBroken, string? platform)
        {
            var current = root;
            foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!current.Children.TryGetValue(part, out var child))
                    current.Children[part] = child = new TreeNode(part);
                current = child;
            }
            current.Snippet = snippet ?? current.Snippet;
            current.IsBroken |= isBroken;
            current.PlatformRestriction ??= platform;
        }

        private static void PrintNode(TreeNode node, string indent, bool isLast)
        {
            if (!string.IsNullOrEmpty(node.Name))
            {
                Console.Write(indent);
                Console.Write(isLast ? "└── " : "├── ");

                if (node.IsBroken)
                    WriteError(node.Name + " (⚠ load error)");
                else if (node.PlatformRestriction != null)
                    WriteWarning(node.Name + $" (⚠ {node.PlatformRestriction} only)");
                else if (node.Snippet != null && !string.IsNullOrEmpty(node.Snippet.Description))
                    Console.WriteLine($"{node.Name} - {node.Snippet.Description}");
                else
                    Console.WriteLine(node.Name);

                indent += isLast ? "    " : "│   ";
            }

            var children = node.Children.Values.OrderBy(c => c.Name).ToList();
            for (int i = 0; i < children.Count; i++)
                PrintNode(children[i], indent, i == children.Count - 1);
        }


        private sealed class TreeNode(string name)
        {
            public string Name { get; } = name;
            public Dictionary<string, TreeNode> Children { get; } = new();
            public ISnippet? Snippet { get; set; }
            public bool IsBroken { get; set; }
            public string? PlatformRestriction { get; set; }
        }

        private static void WriteError(string msg)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = prev;
        }

        private static void WriteWarning(string msg)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = prev;
        }

        [AttributeUsage(AttributeTargets.Class)]
        public sealed class PlatformAttribute(string platform) : Attribute
        {
            public string Platform { get; } = platform;
        }
    }
}
