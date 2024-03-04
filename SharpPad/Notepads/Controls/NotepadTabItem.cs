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
using System.Windows;
using System.Windows.Controls;
using SharpPad.Controls.Bindings;
using SharpPad.Interactivity.Contexts;

namespace SharpPad.Notepads.Controls {
    public class NotepadTabItem : TabItem {
        public NotepadTabControl TabControl { get; private set; }

        public NotepadDocument Document { get; private set; }

        private readonly IBinder<NotepadDocument> docNameBinder = new GetSetAutoEventPropertyBinder<NotepadDocument>(TextBlock.TextProperty, nameof(NotepadDocument.FilePathChanged), b => b.Model.DocumentName + (b.Model.IsModified ? "*" : ""), null);

        private TextBlock PART_DocNameTextBlock;

        public NotepadTabItem() {
        }

        static NotepadTabItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotepadTabItem), new FrameworkPropertyMetadata(typeof(NotepadTabItem)));
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            this.PART_DocNameTextBlock = this.GetTemplateChild(nameof(this.PART_DocNameTextBlock)) as TextBlock ?? throw new Exception("Missing " + nameof(this.PART_DocNameTextBlock));
            this.docNameBinder.AttachControl(this.PART_DocNameTextBlock);
        }

        public void OnConnecting(NotepadTabControl owner, NotepadDocument model) {
            this.TabControl = owner;
            this.Document = model;
        }

        public void OnConnected() {
            DataManager.SetContextData(this, new ContextData().Set(DataKeys.DocumentKey, this.Document));
            this.docNameBinder.AttachModel(this.Document);
            this.Document.IsModifiedChanged += this.OnIsModifiedChanged;
        }

        public void OnDisconnecting() {
            DataManager.ClearContextData(this);
            this.docNameBinder.DetachModel();
            this.Document.IsModifiedChanged -= this.OnIsModifiedChanged;
        }

        public void OnDisconnected() {
            this.TabControl = null;
            this.Document = null;
        }

        private void OnIsModifiedChanged(NotepadDocument document) {
            this.docNameBinder.OnModelValueChanged();
        }
    }
}