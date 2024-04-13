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

using System;
using System.Windows;
using System.Windows.Controls;
using SharpPad.WPF.AdvancedMenuService.ContextService.Controls;
using SharpPad.WPF.Controls.Bindings;
using SharpPad.WPF.Interactivity.Contexts;
using SharpPad.WPF.Notepads.Contexts;

namespace SharpPad.WPF.Notepads.Controls
{
    public class NotepadTabItem : TabItem, INotepadTabUI
    {
        public NotepadTabControl TabControl { get; private set; }

        public NotepadEditor Editor { get; private set; }

        public bool IsFindPanelOpen { get; set; }

        private readonly IBinder<NotepadDocument> docNameBinder = new GetSetAutoEventPropertyBinder<NotepadDocument>(TextBlock.TextProperty, nameof(NotepadDocument.FilePathChanged), b => b.Model.DocumentName + (b.Model.IsModified ? "*" : ""), null);

        private TextBlock PART_DocNameTextBlock;
        private readonly ContextData contextData;
        private NotepadDocument activeDocument;

        public NotepadTabItem()
        {
            this.contextData = new ContextData().Set(DataKeys.UINotepadTabKey, this);
            AdvancedContextMenu.SetContextGenerator(this, NotepadTabContextGenerator.Instance);
        }

        static NotepadTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NotepadTabItem), new FrameworkPropertyMetadata(typeof(NotepadTabItem)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.PART_DocNameTextBlock = this.GetTemplateChild(nameof(this.PART_DocNameTextBlock)) as TextBlock ?? throw new Exception("Missing " + nameof(this.PART_DocNameTextBlock));
            this.docNameBinder.AttachControl(this.PART_DocNameTextBlock);
        }

        public void OnConnecting(NotepadTabControl owner, NotepadEditor model)
        {
            this.TabControl = owner;
            this.Editor = model;
        }

        public void OnConnected()
        {
            DataManager.SetContextData(this, this.contextData.Set(DataKeys.NotepadEditorKey, this.Editor).Clone());
            this.Editor.DocumentChanged += this.OnActiveDocumentChanged;
            this.OnDocumentChanged(this.Editor.Document);
        }

        public void OnDisconnecting()
        {
            DataManager.SetContextData(this, this.contextData.Set(DataKeys.DocumentKey, null).Clone());
            this.Editor.DocumentChanged -= this.OnActiveDocumentChanged;
            this.OnDocumentChanged(null);
        }

        public void OnDisconnected()
        {
            this.TabControl = null;
            this.Editor = null;
        }

        private void OnActiveDocumentChanged(NotepadEditor editor, NotepadDocument olddoc, NotepadDocument newdoc)
        {
            this.OnDocumentChanged(newdoc);
        }

        private void OnIsModifiedChanged(NotepadDocument document)
        {
            this.docNameBinder.OnModelValueChanged();
        }

        private void OnDocumentChanged(NotepadDocument document)
        {
            if (this.activeDocument != null)
            {
                this.activeDocument.IsModifiedChanged -= this.OnIsModifiedChanged;

                if (this.docNameBinder.Model != null)
                    this.docNameBinder.DetachModel();
            }

            if ((this.activeDocument = document) != null)
            {
                document.IsModifiedChanged += this.OnIsModifiedChanged;

                this.docNameBinder.AttachModel(document);
            }
        }
    }
}