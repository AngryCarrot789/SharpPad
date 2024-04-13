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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using SharpPad.WPF.Utils;

namespace SharpPad.WPF.Notepads.Controls
{
    // A modified implementation from: https://stackoverflow.com/a/47955290/11034928
    public class SearchResultBackgroundRenderer : IBackgroundRenderer
    {
        private static readonly Brush BgBrush;
        private static readonly Pen BdPen;
        private readonly TextSegmentCollection<TextSegment> myResults = new TextSegmentCollection<TextSegment>();

        public KnownLayer Layer => KnownLayer.Selection; // draw behind selection

        public SearchResultBackgroundRenderer()
        {
        }

        static SearchResultBackgroundRenderer()
        {
            Color bgc = Colors.Orange;
            Color brc = Colors.LightGray;
            BgBrush = new SolidColorBrush(new Color() { R = bgc.R, G = bgc.G, B = bgc.B, A = 175 });
            BdPen = new Pen(new SolidColorBrush(new Color() { R = brc.R, G = brc.G, B = brc.B, A = 255 }), 1.0);

            // big performance helper
            if (BgBrush.CanFreeze)
                BgBrush.Freeze();
            if (BdPen.CanFreeze)
                BdPen.Freeze();
        }

        public void OnSearchUpdated(IEnumerable<TextRange> ranges)
        {
            this.myResults.Clear();
            if (ranges != null)
                this.myResults.AddCollectionRange(ranges.Select(x => new TextSegment() { StartOffset = x.Index, Length = x.Length }));
        }

        /// <summary>Causes the background renderer to draw.</summary>
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (this.myResults.Count < 1 || !textView.VisualLinesValid)
            {
                return;
            }

            ReadOnlyCollection<VisualLine> visualLines = textView.VisualLines;
            if (visualLines.Count < 1)
            {
                return;
            }

            int viewStart = visualLines[0].FirstDocumentLine.Offset;
            int viewEnd = visualLines[visualLines.Count - 1].LastDocumentLine.EndOffset;

            foreach (TextSegment result in this.myResults.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder
                {
                    AlignToWholePixels = true, BorderThickness = 1, CornerRadius = 0
                };

                geoBuilder.AddSegment(textView, result);
                Geometry geometry = geoBuilder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(BgBrush, BdPen, geometry);
                }
            }
        }
    }
}