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

using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using SharpPad.Utils;

namespace SharpPad.Notepads.Controls {
    // Old version. Works, but getting the white outline doesn't work that well
    public class SearchResultColorizingTransformer : ColorizingTransformer {
        private static readonly Brush BgBrush = new SolidColorBrush(new Color() {R = Colors.Orange.R, G = Colors.Orange.G, B = Colors.Orange.B, A = 150});

        private readonly NotepadEditorControl control;

        public FindAndReplaceModel FindModel { get; set; }

        public SearchResultColorizingTransformer(NotepadEditorControl control) {
            this.control = control;
        }

        static SearchResultColorizingTransformer() {
            // big performance helper
            if (BgBrush.CanFreeze)
                BgBrush.Freeze();
        }

        protected override void Colorize(ITextRunConstructionContext context) {
            IReadOnlyList<TextRange> results = this.FindModel?.Results;
            if (results == null || results.Count < 1) {
                return;
            }

            int lineStartOffset = context.VisualLine.FirstDocumentLine.Offset;
            foreach (TextRange range in results) {
                if (range.Index < lineStartOffset) {
                    continue;
                }

                int startColumn = context.VisualLine.GetVisualColumn(range.Index - lineStartOffset);
                int endColumn = context.VisualLine.GetVisualColumn(range.EndIndex - lineStartOffset);

                this.ChangeVisualElements(startColumn, endColumn, element => {
                    element.TextRunProperties.SetBackgroundBrush(BgBrush);
                });
            }
        }
    }
}