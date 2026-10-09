using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using System.IO;
using System.Xml;

namespace Magnetar.UI.Editor;

internal class MacroCodeEditor : TextEditor, IDisposable
{
    public MacroCodeEditor()
        : base(new TextArea() { Document = new ICSharpCode.AvalonEdit.Document.TextDocument() })
    {
        Options.ShowTabs = true;
        Options.ConvertTabsToSpaces = true;

        InstallHighlighting();
        SearchPanel.Install(TextArea);
    }

    void InstallHighlighting()
    {
        using XmlReader reader = new XmlTextReader(new StringReader(Properties.Resources.MacroHighlighting));
        var luaHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting("Macro", [".macro"], luaHighlighting);
        SyntaxHighlighting = luaHighlighting;
    }

    public void Dispose()
    {
    }
}
