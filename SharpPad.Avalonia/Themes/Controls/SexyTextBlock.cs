using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace SharpPad.Avalonia.Themes.Controls;

public class SexyTextBlock : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<Thickness> PaddingProperty = Decorator.PaddingProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<FontFamily> FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<FontStyle> FontStyleProperty = TextElement.FontStyleProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<FontStretch> FontStretchProperty = TextElement.FontStretchProperty.AddOwner<SexyTextBlock>();
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<SexyTextBlock>();

    /// <summary>
    /// Gets or sets the padding to place around the <see cref="Text"/>.
    /// </summary>
    public Thickness Padding
    {
        get => this.GetValue(PaddingProperty);
        set => this.SetValue(PaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets a brush used to paint the control's background.
    /// </summary>
    public IBrush? Background
    {
        get => this.GetValue(BackgroundProperty);
        set => this.SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the font family used to draw the control's text.
    /// </summary>
    public FontFamily FontFamily
    {
        get => this.GetValue(FontFamilyProperty);
        set => this.SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the size of the control's text in points.
    /// </summary>
    public double FontSize
    {
        get => this.GetValue(FontSizeProperty);
        set => this.SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the font style used to draw the control's text.
    /// </summary>
    public FontStyle FontStyle
    {
        get => this.GetValue(FontStyleProperty);
        set => this.SetValue(FontStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the font weight used to draw the control's text.
    /// </summary>
    public FontWeight FontWeight
    {
        get => this.GetValue(FontWeightProperty);
        set => this.SetValue(FontWeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the font stretch used to draw the control's text.
    /// </summary>
    public FontStretch FontStretch
    {
        get => this.GetValue(FontStretchProperty);
        set => this.SetValue(FontStretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used to draw the control's text and other foreground elements.
    /// </summary>
    public IBrush? Foreground
    {
        get => this.GetValue(ForegroundProperty);
        set => this.SetValue(ForegroundProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<SexyTextBlock, string>("Text");

    public string Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    public SexyTextBlock()
    {
    }

    static SexyTextBlock()
    {
        AffectsRender<SexyTextBlock>(
            BackgroundProperty, PaddingProperty, FontFamilyProperty, FontSizeProperty,
            FontStyleProperty, FontWeightProperty, FontStretchProperty, ForegroundProperty);

        AffectsMeasure<SexyTextBlock>(
            BackgroundProperty, PaddingProperty, FontFamilyProperty, FontSizeProperty,
            FontStyleProperty, FontWeightProperty, FontStretchProperty, ForegroundProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        FormattedText formatted = new FormattedText(this.Text ?? "", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch), this.FontSize, this.Foreground);
        context.DrawText(formatted, new Point(0, 0));
    }
}