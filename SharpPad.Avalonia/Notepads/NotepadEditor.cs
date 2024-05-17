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

using AvaloniaEdit;

namespace SharpPad.Avalonia.Notepads;

public delegate void NotepadEditorEventHandler(NotepadEditor editor);

public delegate void ActiveDocumentChangedEventHandler(NotepadEditor editor, NotepadDocument? oldDoc, NotepadDocument? newDoc);

public delegate void TextEditorChangedEventHandler(NotepadEditor editor, TextEditor? oldEditor, TextEditor? newEditor);

/// <summary>
/// A class that represents a text editor in a notepad window. Each editor has one active
/// document associated with it; a document can be associated with any number of editors.
/// <para>
/// In the current UI implementation, there is only one text editor CONTROL object per notepad window,
/// and its properties are updated when the active editor (aka selected tab) changes. This class is used
/// to mimic the behaviour of a text editor control for each notepad document in a specific window.
/// By window, I loosely reference how microsoft calls each view a "window". Currently there's only one notepad
/// window per shell/root window, but if I implement split-screen then there could be multiple 'windows' per shell
/// </para>
/// </summary>
public class NotepadEditor
{
    private NotepadDocument? document;
    private TextEditor? textEditor;
    private bool isFindPanelOpen;

    /// <summary>
    /// Gets the <see cref="Notepad"/> object that this editor currently exists in
    /// </summary>
    public Notepad? Owner { get; private set; }

    /// <summary>
    /// Gets or sets the document that this editor is presenting
    /// </summary>
    public NotepadDocument? Document
    {
        get => this.document;
        set
        {
            NotepadDocument? doc = this.document;
            if (doc == value)
                return;

            this.document = value;
            this.DocumentChanged?.Invoke(this, doc, value);
        }
    }

    /// <summary>
    /// Gets the find model associated with the current document
    /// </summary>
    public FindAndReplaceModel FindModel { get; }

    /// <summary>
    /// Gets or sets the current text editor that is presenting this notepad editor object
    /// </summary>
    public TextEditor? TextEditor
    {
        get => this.textEditor;
        set
        {
            TextEditor? oldEditor = this.textEditor;
            if (oldEditor == value)
                return;
            this.textEditor = value;
            this.TextEditorChanged?.Invoke(this, oldEditor, value);
        }
    }

    public bool IsFindPanelOpen
    {
        get => this.isFindPanelOpen;
        set
        {
            if (this.isFindPanelOpen == value)
                return;
            this.isFindPanelOpen = value;
            this.IsFindPanelOpenChanged?.Invoke(this);

            if (value)
            {
                this.FindModel.UpdateSearch();
            }
            else
            {
                this.FindModel.ClearResults();
            }
        }
    }

    /// <summary>
    /// An event that gets raised when <see cref="Document"/> changes
    /// </summary>
    public event ActiveDocumentChangedEventHandler? DocumentChanged;

    public event TextEditorChangedEventHandler? TextEditorChanged;
    public event NotepadEditorEventHandler? IsFindPanelOpenChanged;

    /// <summary>
    /// Creates a notepad editor instance
    /// </summary>
    public NotepadEditor()
    {
        this.FindModel = new FindAndReplaceModel(this);
    }

    /// <summary>
    /// Creates a notepad editor with the given initial document. This is
    /// equivalent to setting <see cref="Document"/> after this constructor
    /// </summary>
    /// <param name="document">The initial document</param>
    public NotepadEditor(NotepadDocument document) : this()
    {
        this.Document = document;
    }

    /// <summary>
    /// Checks if this editor is currently being viewed in the given notepad
    /// </summary>
    /// <param name="notepad">The notepad</param>
    /// <returns>A bool</returns>
    public bool IsViewedBy(Notepad notepad) => notepad.ActiveEditor == this;

    public bool IsOwnedBy(Notepad notepad) => notepad == this.Owner;

    internal static void SetOwner(NotepadEditor editor, Notepad notepad)
    {
        editor.Owner = notepad;
    }
}