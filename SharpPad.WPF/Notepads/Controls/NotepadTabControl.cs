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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SharpPad. If not, see <https://www.gnu.org/licenses/>.
//

using System.Windows;
using System.Windows.Controls;
using SharpPad.WPF.Interactivity.Contexts;
using SharpPad.WPF.Utils;

namespace SharpPad.WPF.Notepads.Controls
{
    public class NotepadTabControl : TabControl
    {
        public static readonly DependencyProperty NotepadProperty = DependencyProperty.Register("Notepad", typeof(Notepad), typeof(NotepadTabControl), new PropertyMetadata(null, (d, e) => ((NotepadTabControl) d).OnNotepadChanged((Notepad) e.OldValue, (Notepad) e.NewValue)));

        public Notepad Notepad
        {
            get => (Notepad) this.GetValue(NotepadProperty);
            set => this.SetValue(NotepadProperty, value);
        }

        public NotepadTabControl() { }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (!(this.Notepad is Notepad notepad))
                return;

            int index = this.SelectedIndex;
            notepad.ActiveEditor = index == -1 ? null : notepad.Editors[index];
        }

        static NotepadTabControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotepadTabControl), new FrameworkPropertyMetadata(typeof(NotepadTabControl)));
        }

        private void OnNotepadChanged(Notepad oldNotepad, Notepad newNotepad)
        {
            if (oldNotepad != null)
            {
                oldNotepad.EditorIndexChanged -= this.OnEditorIndexChanged;
                oldNotepad.ActiveEditorChanged -= this.OnActiveEditorChanged;
            }

            if (newNotepad != null)
            {
                newNotepad.EditorIndexChanged += this.OnEditorIndexChanged;
                newNotepad.ActiveEditorChanged += this.OnActiveEditorChanged;
                DataManager.SetContextData(this, new ContextData().Set(DataKeys.NotepadKey, newNotepad));
            }
            else
            {
                DataManager.ClearContextData(this);
            }
        }

        private void OnEditorIndexChanged(Notepad notepad, NotepadEditor editor, int oldIndex, int newIndex)
        {
            if (oldIndex == -1)
            {
                NotepadTabItem item = new NotepadTabItem();
                item.OnConnecting(this, editor);
                this.Items.Insert(newIndex, item);
                item.OnConnected();
            }
            else if (newIndex == -1)
            {
                NotepadTabItem item = (NotepadTabItem) this.Items[oldIndex];
                item.OnDisconnecting();
                this.Items.RemoveAt(oldIndex);
                item.OnDisconnected();
            }
            else
            {
                CollectionUtils.MoveItem(this.Items, oldIndex, newIndex);
            }
        }

        private void OnActiveEditorChanged(Notepad notepad, NotepadEditor oldEditor, NotepadEditor newEditor)
        {
            this.SelectedIndex = newEditor == null ? -1 : notepad.Editors.IndexOf(newEditor);
        }
    }
}