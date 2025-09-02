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
                RunSnippet(args[0].ToLower(), [.. args.Skip(1)], fullPathMap, shortNameMap, brokenSnippets, platformNotes); // ToArray simplification
                return;
            }

            Console.WriteLine("\n=== Snippet Runner ===\n");
            PrintTree(fullPathMap, brokenSnippets, platformNotes);

            Console.Write("\nEnter snippet name to run: ");
            string inputLine = Console.ReadLine() ?? "";
            var parts = inputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                string input = parts[0].ToLower();
                string[] snippetArgs = [.. parts.Skip(1)]; //ToArray() simplification
                RunSnippet(input, snippetArgs, fullPathMap, shortNameMap, brokenSnippets, platformNotes);
            }
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

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(ISnippet).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var t in types)
            {
                string ns = t.Namespace ?? "";
                if (ns.StartsWith("SnippetRunner."))
                    ns = ns["SnippetRunner.".Length..];
                ns = ns.ToLower();

                // Always root under "snippets"
                string folder = string.IsNullOrEmpty(ns) ? "snippets/" : $"{ns.Replace('.', '/')}";

                try
                {
                    var instance = (ISnippet)Activator.CreateInstance(t)!;

                    // Normalized: folder + instance.Name
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
                    // Fall back to class name, but still under the same folder
                    string brokenKey = $"{folder}/{t.Name.ToLower()}";
                    WriteWarning($"⚠ Skipping snippet '{brokenKey}' (failed to load: {ex.GetType().Name})");
                    brokenSnippets.Add($"Broken Snippet {t.Name.ToLower()}");
                }
            }

            return (fullPathMap, shortNameMap, brokenSnippets, platformNotes);
        }

        private static void RunSnippet(
         string keyOrName, string[] args,
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

            if (platformNotes.TryGetValue(keyOrName, out string? value))
            {
                WriteWarning($"⚠ Snippet '{keyOrName}' is not supported on this platform ({value} only).");
                return;
            }
            // Try full path first
            if (fullPathMap.TryGetValue(keyOrName, out var snippet))
            {
                try
                {
                    Console.WriteLine($"\n▶ Running '{keyOrName}'...\n");
                    snippet.Run(args);
                }
                catch (Exception ex)
                {
                    WriteError($"💥 Snippet '{keyOrName}' crashed: {ex.Message}");
                }
                return;
            }

            if (shortNameMap.TryGetValue(keyOrName, out var matches))
            {
                if (matches.Count == 1)
                {
                    string fullKey = matches[0];
                    if (brokenSnippets.Contains(fullKey))
                    {
                        WriteError($"❌ Snippet '{fullKey}' cannot run (failed to load earlier).");
                        return;
                    }

                    try
                    {
                        Console.WriteLine($"\n▶ Running '{fullKey}'...\n");
                        fullPathMap[fullKey].Run(args);
                    }
                    catch (Exception ex)
                    {
                        WriteError($"💥 Snippet '{fullKey}' crashed: {ex.Message}");
                    }
                }
                else
                {
                    WriteWarning($"❌ Ambiguous name '{keyOrName}'. Matches:");
                    foreach (var match in matches)
                        Console.WriteLine($"   - {match}");
                }
                return;
            }

            WriteError($"❌ Unknown snippet '{keyOrName}'\n");
            PrintTree(fullPathMap, brokenSnippets, platformNotes);
        }











        private static void PrintTree(
    Dictionary<string, ISnippet> fullPathMap,
    HashSet<string> brokenSnippets,
    Dictionary<string, string> platformNotes)
{
    var root = new TreeNode(""); // empty root for top-level folders

    // Add working snippets
    foreach (var kvp in fullPathMap)
        AddPathToTree(root, kvp.Key, kvp.Value, isBroken: false, platformRestriction: null);

    // Create a dedicated "Broken" node
    var brokenRoot = new TreeNode("Broken");
    root.Children["Broken"] = brokenRoot;

    // Add broken snippets under the "Broken" node
    foreach (var broken in brokenSnippets)
    {
        string snippetName = broken.Contains('/') 
            ? broken.Split('/').Last() 
            : broken;

        AddPathToTree(brokenRoot, snippetName, null, isBroken: true, platformRestriction: null);
    }

    // Add platform-restricted snippets as usual
    foreach (var kvp in platformNotes)
        AddPathToTree(root, kvp.Key, null, isBroken: false, platformRestriction: kvp.Value);

    PrintNode(root, "", isLast: true);
}

private static void AddPathToTree(TreeNode root, string path, ISnippet? snippet,
                                  bool isBroken, string? platformRestriction)
{
    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.ToLowerInvariant())
                    .ToArray();

    var current = root;
    foreach (var part in parts)
    {
        if (!current.Children.TryGetValue(part, out var child))
        {
            child = new TreeNode(part);
            current.Children[part] = child;
        }
        current = child;
    }

    current.Snippet = snippet ?? current.Snippet;
    current.IsBroken |= isBroken;
    current.PlatformRestriction ??= platformRestriction;
}

        private static void PrintNode(TreeNode node, string indent, bool isLast)
        {
            if (!string.IsNullOrEmpty(node.Name))
            {
                Console.Write(indent);
                Console.Write(isLast ? "└── " : "├── ");

                if (node.IsBroken)
                    WriteError($"{node.Name} (⚠ load error)");
                else if (node.PlatformRestriction != null)
                    WriteWarning($"{node.Name} (⚠ {node.PlatformRestriction} only)");
                else
                    Console.WriteLine(node.Name);

                indent += isLast ? "    " : "│   ";
            }

            var children = node.Children.Values.OrderBy(c => c.Name).ToList();
            for (int i = 0; i < children.Count; i++)
            {
                PrintNode(children[i], indent, i == children.Count - 1);
            }
        }

        // TreeNode class
        private sealed class TreeNode(string name)
        {
            public string Name { get; } = name;
            public Dictionary<string, TreeNode> Children { get; } = new();
            public ISnippet? Snippet { get; set; }
            public bool IsBroken { get; set; }
            public string? PlatformRestriction { get; set; }
        }
        private static string BuildSnippetPath(Type t, string? nameOverride = null)
        {
            string ns = t.Namespace ?? "";
            if (ns.StartsWith("SnippetRunner."))
                ns = ns["SnippetRunner.".Length..];
            ns = ns.ToLower();

            string name = nameOverride ?? t.Name.ToLower();

            return string.IsNullOrEmpty(ns)
                ? $"snippets/{name}"
                : $"snippets/{ns.Replace('.', '/')}/{name}";
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
