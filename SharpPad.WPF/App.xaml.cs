﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SharpPad.WPF.CommandSystem;
using SharpPad.WPF.Logger;
using SharpPad.WPF.Notepads;
using SharpPad.WPF.Notepads.Views;
using SharpPad.WPF.Properties;
using SharpPad.WPF.Services.Messages;
using SharpPad.WPF.Shortcuts.Managing;
using SharpPad.WPF.Shortcuts.WPF;
using SharpPad.WPF.Utils;

namespace SharpPad.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs args)
        {
            // if (true) {
            //     DefaultProgressTracker.TestCompletionRangeFunctionality();
            //     this.Dispatcher.InvokeShutdown();
            //     return;
            // }

            // Pre init stuff
            ToolTipService.ShowDurationProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
            ToolTipService.InitialShowDelayProperty.OverrideMetadata(typeof(DependencyObject), new FrameworkPropertyMetadata(400));
            UIInputManager.IsInheritedFocusAllowedProperty.OverrideMetadata(typeof(TextBox), new PropertyMetadata(BoolBox.False));

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string[] envArgs = Environment.GetCommandLineArgs();
            if (envArgs.Length > 0 && Path.GetDirectoryName(envArgs[0]) is string dir && dir.Length > 0)
            {
                Directory.SetCurrentDirectory(dir);
            }

            try
            {
                // This is where services are registered
                TypeUtils.RunStaticConstructor<ApplicationCore>();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialise application: " + ex.GetToString(), "Fatal App init failure");
                this.Dispatcher.InvokeShutdown();
                return;
            }

            // Most if not all services are available below here
            try
            {
                AppLogger.Instance.PushHeader("SharpPad initialisation");
                this.InitWPFApp();
                AppLogger.Instance.PopHeader();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start SharpPad: " + ex.GetToString(), "Fatal App setup failure");
                this.Dispatcher.InvokeShutdown();
                return;
            }

            // Notepad init
            Notepad notepad = new Notepad();

            int prefWidth = Settings.Default.NotepadWindowWidth;
            if (prefWidth < 20)
                prefWidth = 600;

            int prefHeight = Settings.Default.NotepadWindowHeight;
            if (prefHeight < 20)
                prefHeight = 600;

            NotepadWindow window = new NotepadWindow()
            {
                Width = prefWidth, Height = prefHeight
            };

            window.Show();
            window.Notepad = notepad;
            this.MainWindow = window;
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
            ApplicationCore.Instance.OnApplicationLoaded(notepad, args.Args);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            ApplicationCore.Instance.OnApplicationExiting();
        }

        public void InitWPFApp()
        {
            ShortcutManager.Instance = new WPFShortcutManager();
            TypeUtils.RunStaticConstructor<UIInputManager>();

            ApplicationCore.Instance.RegisterActions(CommandManager.Instance);

            // TODO: user modifiable keymap, and also save it to user documents
            // also, use version attribute to check out of date keymap, and offer to
            // overwrite while backing up old file... or just try to convert file

            string keymapFilePath = Path.GetFullPath(@"Keymap.xml");
            if (File.Exists(keymapFilePath))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(keymapFilePath))
                    {
                        WPFShortcutManager.WPFInstance.DeserialiseRoot(stream);
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
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (ApplicationCore.Instance.Services.TryGetService(out IMessageDialogService service))
            {
                service.ShowMessage("Error", "An unhandled error occurred in the application. It might now shutdown", e.Exception?.GetToString());
            }
            else
            {
                MessageBox.Show("An unhandled error occurred in the application. It might now shutdown\n\n" + e.Exception?.GetToString(), "Error");
            }

            if (this.MainWindow != null && DesignerProperties.GetIsInDesignMode(this.MainWindow))
            {
                return;
            }

            this.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
        }
    }
}