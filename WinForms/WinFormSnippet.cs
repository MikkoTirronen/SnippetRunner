#if WINDOWS
using System.Windows.Forms;

namespace SnippetRunner.Winforms{
[Program.Platform("Windows")]
public class WinFormsSnippet : ISnippet
{
    public string Name => "winforms-test";
    public string Description => "winforms app on mac exception handling test";
    public void Run(string[] args)
    {
        Application.Run(new Form { Text = "Hello from WinForms!" });
    }
}

}
#endif
