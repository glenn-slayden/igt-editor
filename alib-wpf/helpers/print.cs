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

namespace alib.Wpf
{
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public static class PNG
	{
		const Double Prescale = 2.0;

		public static RenderTargetBitmap GetImage(UIElement el, Brush bkgnd, Double dpi)
		{
			if (!el.IsMeasureValid)
				el.Measure(util.infinite_size);

			var sz = el.DesiredSize;
			if (sz.Width < math.ε || sz.Height < math.ε)
			{
				throw new Exception();
				//return new RenderTargetBitmap(1, 1, dpi, dpi, PixelFormats.Pbgra32);
			}

			var dv = new DrawingVisual();

			dv.Transform = new ScaleTransform(Prescale, Prescale);

			TextOptions.SetTextHintingMode(dv, TextHintingMode.Fixed);
			TextOptions.SetTextRenderingMode(dv, TextRenderingMode.Aliased);
			RenderOptions.SetEdgeMode(dv, EdgeMode.Unspecified);

			using (DrawingContext ctx = dv.RenderOpen())
			{
				Rect r = new Rect(0, 0, sz.Width + 1, sz.Height + 1);

				if (bkgnd != null && bkgnd != Brushes.Transparent)
					ctx.DrawRectangle(bkgnd, null, r);

				VisualBrush br = new VisualBrush(el);
				br.AutoLayoutContent = true;
				ctx.DrawRectangle(br, null, r);
			}

			Double f = dpi / 96.0;

			RenderTargetBitmap bitmap = new RenderTargetBitmap(
				(int)(sz.Width * f),
				(int)(sz.Height * f),
				dpi / Prescale,
				dpi / Prescale,
				PixelFormats.Pbgra32);
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

		public static void CopyToClipboard(UIElement fe, Brush background, int dpi)
		{
			Clipboard.SetImage(GetImage(fe, background, dpi));
		}

		public static void Save(String fn, UIElement fe, Brush background, int dpi)
		{
			using (var sw = new FileStream(fn, FileMode.Create, FileAccess.ReadWrite))
				PNG.Write(sw, fe, background, dpi);
		}

		public static void Write(this Stream stream, UIElement fe, Brush background, int dpi)
		{
			//fe.Measure(util.infinite_size);
			//fe.UpdateLayout();

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

		public static String FmtDi(this Size sz) { return util.szFmt(sz).PadRight(12) + " (" + util.szFmt(sz.Divide(96.0)) + ")"; }

		public static String FmtDi(this Rect r) { return util.rFmt(r) + "  (" + util.rFmt(r.Divide(96.0)) + ")"; }

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