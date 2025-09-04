

namespace SnippetRunner.MacOnly
{
    [Program.Platform("Mac")]
    public class MacSnippetPlaceholder : ISnippet
    {
        public string Name => "mac-test";
        public string Description => "Mac-only snippet (placeholder on other platforms)";

        public void Run(string[] args)
        {
#if MAC
            Console.WriteLine("✅ Running macOS-specific snippet!");
#else
            throw new PlatformNotSupportedException("This snippet only runs on macOS.");
#endif
        }
    }
}