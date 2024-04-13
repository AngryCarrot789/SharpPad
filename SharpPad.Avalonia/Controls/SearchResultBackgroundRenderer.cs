using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SharpPad.Avalonia.Utils;

namespace SharpPad.Avalonia.Controls;

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
        BgBrush = new SolidColorBrush(new Color(bgc.R, bgc.G, bgc.B, 175));
        BdPen = new Pen(new SolidColorBrush(new Color(brc.R, brc.G, brc.B, 255)), 1.0);
    }

    public void OnSearchUpdated(IEnumerable<TextRange>? ranges)
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

        ReadOnlyCollection<VisualLine>? visualLines = textView.VisualLines;
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