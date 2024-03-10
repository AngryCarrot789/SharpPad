//
// Copyright (c) 2023-2024 REghZy
//
// This file is part of SharpPad.
//
// SharpPad is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
//
// SharpPad is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using SharpPad.CommandSystem;
using SharpPad.Interactivity.Contexts;
using SharpPad.Utils;

namespace SharpPad.Notepads.Commands
{
    public abstract class DocumentCommand : Command
    {
        public override Executability CanExecute(CommandEventArgs e)
        {
            if (!DataKeys.DocumentKey.TryGetContext(e.ContextData, out NotepadDocument document))
                return Executability.Invalid;
            return this.CanExecute(document, e);
        }

        protected override void Execute(CommandEventArgs e)
        {
            if (DataKeys.DocumentKey.TryGetContext(e.ContextData, out NotepadDocument document))
                this.Execute(document, e);
        }

        public abstract Executability CanExecute(NotepadDocument document, CommandEventArgs e);

        public abstract Task Execute(NotepadDocument document, CommandEventArgs e);
    }

    public class SaveDocumentCommand : DocumentCommand
    {
        public override Executability CanExecute(NotepadDocument document, CommandEventArgs e)
        {
            return Executability.Valid;
        }

        public override Task Execute(NotepadDocument document, CommandEventArgs e)
        {
            SaveOrSaveAs(document);
            return Task.CompletedTask;
        }

        public static void SaveOrSaveAs(NotepadDocument document)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                SaveDocumentAsCommand.SaveDocumentAs(document);
            }
            else
            {
                SaveDocumentAsCommand.SaveDocument(document, document.FilePath);
            }
        }
    }

    public class SaveDocumentAsCommand : DocumentCommand
    {
        public override Executability CanExecute(NotepadDocument document, CommandEventArgs e)
        {
            return Executability.Valid;
        }

        public override Task Execute(NotepadDocument document, CommandEventArgs e)
        {
            SaveDocumentAs(document);
            return Task.CompletedTask;
        }

        public static bool? SaveDocumentAs(NotepadDocument document)
        {
            string filePath = IoC.FilePickService.SaveFile("Select a location to save the file", Filters.TextTypesAndAll);
            if (filePath == null)
                return null;
            document.FilePath = filePath;
            return SaveDocument(document, filePath);
        }

        public static bool SaveDocument(NotepadDocument document, string filePath)
        {
            string text = document.Document.Text;
            try
            {
                File.WriteAllText(filePath, text);
            }
            catch (Exception e)
            {
                IoC.MessageService.ShowMessage("Error", "Error while saving file", e.GetToString());
                return false;
            }

            document.IsModified = false;
            return true;
        }
    }

    public class CopyFilePathCommand : DocumentCommand
    {
        public override Executability CanExecute(NotepadDocument document, CommandEventArgs e)
        {
            return !string.IsNullOrWhiteSpace(document.FilePath) ? Executability.Valid : Executability.ValidButCannotExecute;
        }

        public override Task Execute(NotepadDocument document, CommandEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
                return Task.CompletedTask;
            try
            {
                Clipboard.SetText(document.FilePath);
            }
            catch (Exception ex)
            {
                IoC.MessageService.ShowMessage("Clipboard unavailable", "Could not copy file path to clipboard", ex.GetToString());
            }

            return Task.CompletedTask;
        }
    }
}