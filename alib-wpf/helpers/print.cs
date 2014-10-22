using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using alib.Math;
using alib.Enumerable;

namespace alib.Wpf
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class PNG
	{
		public static RenderTargetBitmap GetImage(UIElement el, Brush bkgnd, Double dpi)
		{
			if (!el.IsMeasureValid)
				el.Measure(util.infinite_size);

			var sz = el.DesiredSize;
			if (sz.Width < math.ε || sz.Height < math.ε)
				throw new Exception();

			Double f = dpi / 96.0;

			sz.Width = (sz.Width + 1) * f;
			sz.Height = (sz.Height + 1) * f;

			var dv = new DrawingVisual { Transform = new ScaleTransform(f, f) };

			TextOptions.SetTextHintingMode(dv, TextHintingMode.Fixed);
			TextOptions.SetTextRenderingMode(dv, TextRenderingMode.Grayscale);
			TextOptions.SetTextFormattingMode(dv, TextFormattingMode.Ideal);
			RenderOptions.SetEdgeMode(dv, EdgeMode.Aliased);
			RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);
			dv.SetValue(Window.UseLayoutRoundingProperty, true);

			using (DrawingContext ctx = dv.RenderOpen())
			{
				if (bkgnd != null && bkgnd != Brushes.Transparent)
					ctx.DrawRectangle(bkgnd, null, new Rect(sz));

				ctx.DrawAllDrawings(el);
			}

			var bitmap = new RenderTargetBitmap((int)(sz.Width * f), (int)(sz.Height * f), dpi, dpi, PixelFormats.Pbgra32);

			bitmap.Render(dv);

			return bitmap;
		}

		public static BitmapSource CreateBitmapSourceFromBitmap(Stream stream)
		{
			var dec = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			var writable = new WriteableBitmap(dec.Frames.Single());
			writable.Freeze();
			return writable;
		}

		public static void CopyToClipboard(UIElement uie, Brush br, int dpi)
		{
			var sv = uie.AllDescendants()
						.OfType<ScrollViewer>()
						.FirstOrDefault(x => x.Content.GetType().FullName.Contains(".Wpf."));	// hack alert: alib.Wpf., agree.Wpf.

			if (sv != null)
			{
				uie = (UIElement)sv.Content;
				sv.Content = null;

				uie.InvalidateVisual();

				if (sv.HorizontalOffset != 0 || sv.VerticalOffset != 0)
				{
					if (uie.RenderTransform != null && uie.RenderTransform != Transform.Identity)
						throw not.impl;
					uie.RenderTransform = new TranslateTransform(sv.HorizontalOffset, sv.VerticalOffset);
				}
			}

#if adjust_for_scale_form
			FrameworkElement fe;
			Transform lx = null;
			if ((fe = uie as FrameworkElement) != null && (lx = fe.LayoutTransform) != null)
			{
				fe.RenderTransform = lx;
			}
#endif

			Clipboard.SetImage(GetImage(uie, br ?? Brushes.White, dpi));

			if (sv != null)
			{
				uie.RenderTransform = null;
				sv.Content = uie;
			}

#if adjust_for_scale_form
			if (lx != null)
			{
				fe.RenderTransform = null;
				fe.LayoutTransform = lx;
			}
#endif
		}

		public static void Save(String fn, UIElement fe, Brush background, int dpi)
		{
			using (var sw = new FileStream(fn, FileMode.Create, FileAccess.ReadWrite))
				PNG.Write(sw, fe, background, dpi);
		}

		public static void Write(this Stream stream, UIElement fe, Brush background, int dpi)
		{
			var bitmap = GetImage(fe, null, dpi);

			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(bitmap));
			encoder.Save(stream);
		}

		public static void Print(UIElement el, String job_name)
		{
			var pd = new _print_dialog(el);

			if (pd.ShowDialog() == true)
			{
				pd.Print(job_name);
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class _print_dialog : PrintDialog
	{
		public _print_dialog(UIElement el)
		{
			this.el = el;
			if ((el = el.StripFrames()) == null)
				throw new Exception();

			this.ticket = (this.PrintQueue = LocalPrintServer.GetDefaultPrintQueue()).UserPrintTicket;
			this.Margin = new Thickness(36);
		}

		UIElement el;

		PrintTicket ticket;
		public PrintTicket Ticket { get { return ticket; } }

		public Thickness Margin { get; set; }

		Size sz_final;
		public Size PrintSize { get { return sz_final; } }

		Transform trx;
		public Transform PageTransform { get { return trx; } }

		Rect setup()
		{
			Debug.WriteLine("===================");

			if (!el.IsMeasureValid)
				el.Measure(util.infinite_size);

			Debug.WriteLine("src di-size:     " + el.DesiredSize.szFmt());
			Debug.WriteLine("src res:         " + el.TargetDpi().szFmt());

			var el_x_pg = ticket.PageResolution.ToSize().Divide(el.TargetDpi());
			Debug.WriteLine("inter-device factor:  " + el_x_pg.szFmt());

			this.trx = Transform.Identity;

			sz_final = el.DesiredSize;

			var r_page = new Rect(Margin.Left, Margin.Top, sz_final.Width, sz_final.Height);

			var r_work = r_page;

			Debug.WriteLine("r_work:           " + r_work.FmtDi());

			return r_work;
		}

		public void Print(String job_name)
		{
			if (String.IsNullOrEmpty(job_name))
				job_name = "Printing...";

			job_name += " " + DateTime.Now;

			var r_page = setup();

			var rg = new RectangleGeometry(r_page);

			for (Double top = 0; top < PrintSize.Height; top += r_page.Height)
			{
				var dv = new DrawingVisual();
				using (var dc = dv.RenderOpen())
				{
					dc.PushClip(rg);
					dc.PushTransform(new TranslateTransform(Margin.Left, -top + Margin.Top));
					dc.PushTransform(PageTransform);

					dc.DrawAllDrawings(el);

					dc.Pop();
					dc.Pop();
					dc.Pop();
				}

				Debug.WriteLine("printing '{0}' top={1}", job_name, top);
				PrintVisual(dv, job_name);
			}
		}
	};

	public static class _print_ext
	{
		public static Size ToSize(this PageMediaSize pms) { return new Size(pms.Width.Value, pms.Height.Value); }

		public static Size ToSize(this PageResolution res) { return new Size(res.X.Value, res.Y.Value); }

		public static String FmtDi(this Size sz) { return sz.szFmt().PadRight(12) + " (" + sz.Divide(96.0).szFmt() + ")"; }

		public static String FmtDi(this Rect r) { return r.rFmt() + "  (" + r.Divide(96.0).rFmt() + ")"; }

		public static Size DpiScaleFactor(this Visual visual)
		{
			var m = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return new Size(m.M11, m.M22);
		}
		public static Size TargetDpi(this Visual visual)
		{
			return visual.DpiScaleFactor().Multiply(96.0);
		}
	};
}