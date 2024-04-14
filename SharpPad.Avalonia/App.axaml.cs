using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using SharpPad.Avalonia.Notepads;
using SharpPad.Avalonia.Shortcuts.Avalonia;
using SharpPad.Avalonia.Utils;

namespace SharpPad.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AvCore.OnApplicationInitialised();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AvCore.OnFrameworkInitialised();
        UIInputManager.Init();
        
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
        
        string keymapFilePath = Path.GetFullPath(@"Keymap.xml");
        if (File.Exists(keymapFilePath))
        {
            try
            {
                using (FileStream stream = File.OpenRead(keymapFilePath))
                {
                    AvaloniaShortcutManager.AvaloniaInstance.DeserialiseRoot(stream);
                }
            }
            catch (Exception ex)
            {
                IoC.MessageService.ShowMessage("Keymap", "Failed to read keymap file" + keymapFilePath, ex.GetToString());
            }
        }
        else
        {
            IoC.MessageService.ShowMessage("Keymap", "Keymap file does not exist at " + keymapFilePath);
        }

        base.OnFrameworkInitializationCompleted();
    }
}