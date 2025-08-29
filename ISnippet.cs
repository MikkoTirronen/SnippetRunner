namespace SnippetRunner
{
    public interface ISnippet
    {
        string Name { get; }
        string Description { get; }
        void Run(string[] args);
    }
}