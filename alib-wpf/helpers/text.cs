using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Documents;
using System.Windows.Media;

namespace alib.Wpf
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static partial class util
	{
		public static readonly XmlLanguage xlang_en;
		public static readonly XmlLanguage xlang_th;

		public static readonly FontFamily ff_segoe;
		public static readonly FontFamily ff_arial;
		public static readonly FontFamily ff_arial_ums;
		public static readonly FontFamily ff_tahoma;
		public static readonly FontFamily ff_cambria;
		public static readonly FontFamily ff_calibri;
		public static readonly FontFamily ff_consolas;

		public static readonly Typeface tf_calibri;

		public static Size DrawText(this DrawingContext dc, String s, double x, double y, Typeface f, double emSize, Brush b)
		{
			FormattedText ft = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, f, emSize, b);
			dc.DrawText(ft, new Point(x, y));
			return new Size(ft.Width, ft.Height);
		}

		public static Size DrawText(this DrawingContext dc, String s, Point pt, Typeface f, double emSize, Brush b)
		{
			FormattedText ft = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, f, emSize, b);
			dc.DrawText(ft, pt);
			return new Size(ft.Width, ft.Height);
		}

		public static void DrawText(this DrawingContext dc, FormattedText ft, double x, double y)
		{
			dc.DrawText(ft, new Point(x, y));
		}

		public static FormattedText FormattedText(this String s, Typeface f, double emSize, Brush b = null)
		{
			return new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, f, emSize, b ?? Brushes.Black);
		}

		public static Typeface Typeface(this Control ctrl)
		{
			return new Typeface(ctrl.FontFamily, ctrl.FontStyle, ctrl.FontWeight, ctrl.FontStretch);
		}

		public static FormattedText FormattedText(this Control ctrl, String s, Brush b = null)
		{
			double size = /* (16.0 / 9.0) * */ ctrl.FontSize;
			return new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, ctrl.Typeface(), size, b ?? Brushes.Black);
		}

		public static IEnumerable<Inline> Flatten(this InlineCollection coll)
		{
			foreach (var il in coll)
			{
				yield return il;
				if (il is Span)
					foreach (var sil in ((Span)il).Inlines.Flatten())
						yield return sil;
			}
		}

		//return sp.Inlines.OfType<Span>().SelectMany(cspan => SpanDescendants(cspan).Prepend(cspan));
		public static IEnumerable<Span> SpanDescendants(this Span sp)
		{
			foreach (var cspan in sp.Inlines.OfType<Span>())
			{
				yield return cspan;
				foreach (var c in SpanDescendants(cspan))
					yield return c;
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class render_text_helper : DrawingVisual
	{
		public render_text_helper()
		{
			//base.VisualTextRenderingMode = TextRenderingMode.ClearType;
			//base.VisualClearTypeHint = ClearTypeHint.Auto;
			//base.VisualEdgeMode = EdgeMode.Aliased;
			//base.VisualTextHintingMode = TextHintingMode.Fixed;
		}

		public Drawing make_text_drawing(FormattedText ft, Point pt, Brush bg)
		{
			Debug.Assert(!String.IsNullOrEmpty(ft.Text));

			using (var dc = RenderOpen())
				dc.DrawText(ft, pt);

			var drw = Drawing.Children[0];
			Drawing.Children.Clear();

			//Point ext = new Point(ft.WidthIncludingTrailingWhitespace, ft.Extent);
			//2013905 - the following messes up the text baseline (bottom-alignment) coordinates
#if false
			/// ALERT** ALERT*** the 'Bounds' property changes when you apply the transform so be sure to copy
			/// the rectangle (it's a value-type) so that the transform that we calculate doesn't get ruined
			/// by adding additional children to the DrawingGroup. The previously-calculated transform will 
			/// apply to them even when though they are added later-- so they must not determine their transform
			/// based on a later sampling.

			Rect r_prior = drw.Bounds;
			r_prior.Inflate(1, 1);

			//Debug.Print("{0,8} pt: {1,9}  ft.ext {2,16}  drw.bounds {3}", ft.Text, pt, r_prior, drw.Bounds.show());

			var v = pt - r_prior.TopLeft;

			var tt = new TranslateTransform { X = v.X, Y = v.Y };

			/// here we are counting on the assumption that the text got rendered into a 'DrawingGroup'

			((DrawingGroup)drw).Transform = tt;

			//Debug.Print("{0,8} pt: {1,9}  ft.ext {2,16}  drw.bounds {3}", ft.Text, pt, r_prior, drw.Bounds.show());
			//Debug.Print("===");
#endif
			if (bg != null)
			{
				Rect r_prior = drw.Bounds;
				((DrawingGroup)drw).Children.Insert(0, new GeometryDrawing(bg, null, new RectangleGeometry(r_prior)));
			}

			drw.Freeze();

			return drw;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class TypefacePlus : Typeface
	{
		public static TypefacePlus Create(DependencyObject o, Style style)
		{
			var ff = TextBlock.GetFontFamily(o);
			var fs = TextBlock.GetFontStyle(o);
			var fw = TextBlock.GetFontWeight(o);
			var st = TextBlock.GetFontStretch(o);
			var sz = TextBlock.GetFontSize(o);
			var fg = TextBlock.GetForeground(o);
			var fd = TextBlock.GetFlowDirection(o);

			if (style != null)
				foreach (var setter in style.Setters.OfType<Setter>())
				{
					var p = setter.Property;
					var v = setter.Value;

					if (p == TextBlock.FontFamilyProperty)
						ff = (FontFamily)v;
					else if (p == TextBlock.FontStyleProperty)
						fs = (FontStyle)v;
					else if (p == TextBlock.FontWeightProperty)
						fw = (FontWeight)v;
					else if (p == TextBlock.FontStretchProperty)
						st = (FontStretch)v;
					else if (p == TextBlock.FontSizeProperty)
						sz = (Double)v;
					else if (p == TextBlock.ForegroundProperty)
						fg = (Brush)v;
					else if (p == TextBlock.FlowDirectionProperty)
						fd = (FlowDirection)v;
				}
			return new TypefacePlus(ff, fs, fw, st, sz, fg, fd);
		}
		public TypefacePlus(Typeface tf)
			: base(tf.FontFamily, tf.Style, tf.Weight, tf.Stretch)
		{
			this.NumberSubstitution = null;
			this.TextFormattingMode = TextFormattingMode.Ideal;
			var tfp = tf as TypefacePlus;
			if (tfp != null)
			{
				this.FontSize = tfp.FontSize;
				this.Foreground = tfp.Foreground;
				this.FlowDirection = tfp.FlowDirection;
				this.NumberSubstitution = tfp.NumberSubstitution;
				this.TextFormattingMode = tfp.TextFormattingMode;
			}
		}
		public TypefacePlus(DependencyObject o)
			: base(TextBlock.GetFontFamily(o),
					TextBlock.GetFontStyle(o),
					TextBlock.GetFontWeight(o),
					TextBlock.GetFontStretch(o))
		{
			this.NumberSubstitution = null;
			this.TextFormattingMode = TextFormattingMode.Ideal;
			this.FontSize = TextBlock.GetFontSize(o);
			this.Foreground = TextBlock.GetForeground(o);
			this.FlowDirection = TextBlock.GetFlowDirection(o);
		}
		TypefacePlus(FontFamily ff, FontStyle fs, FontWeight fw, FontStretch st, Double sz, Brush fg, FlowDirection fd)
			: base(ff, fs, fw, st)
		{
			this.NumberSubstitution = null;
			this.TextFormattingMode = TextFormattingMode.Ideal;
			this.FontSize = sz;
			this.Foreground = fg;
			this.FlowDirection = fd;
		}

		public Double FontSize { get; set; }

		public Brush Foreground { get; set; }

		public FlowDirection FlowDirection { get; set; }

		public NumberSubstitution NumberSubstitution { get; set; }

		public TextFormattingMode TextFormattingMode { get; set; }

		public FormattedText FormattedText(String txt)
		{
			return new FormattedText(txt,
							CultureInfo.InvariantCulture,
							FlowDirection,
							this,
							FontSize,
							Foreground,
							NumberSubstitution,
							TextFormattingMode);
		}
	};
}
