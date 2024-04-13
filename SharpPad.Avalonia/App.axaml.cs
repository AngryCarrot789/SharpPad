using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using SharpPad.Avalonia.Notepads;

namespace SharpPad.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow window = new MainWindow();

            desktop.MainWindow = window;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Notepad notepad = new Notepad();
                notepad.AddNewEditorForDocument(new NotepadDocument(new TextDocument("Doc 11111111!!")) { DocumentName = "My doc 1" });
                window.Notepad = notepad;
                notepad.AddNewEditorForDocument(new NotepadDocument(new TextDocument("Doc 22!!")) { DocumentName = "My doc 2" });
                notepad.AddNewEditorForDocument(new NotepadDocument(new TextDocument("Doc 33333!!")) { DocumentName = "My doc 3" });
            }, DispatcherPriority.Background);
        }

        base.OnFrameworkInitializationCompleted();
    }
}