using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using alib;
using alib.Wpf;
using alib.Enumerable;
using alib.Debugging;

namespace xigt2
{
#if false
	public partial class __main : Window
	{
		static igt_source_line[] _src =
		{
			//new igt_source_line("odin-1", "doc id=397 959 961 L G T"),
			//new igt_source_line("odin-2", "language: korean (kor)"),
			//new igt_source_line("odin-3", "   1 Nay-ka ai-eykey pap-ul mek-i-ess-ta"),
			//new igt_source_line("odin-4", "     I-Nom child-Dat rice-Acc eat-Caus-Pst-Dec"),
			//new igt_source_line("odin-5", "     `I made the child eat rice.'"),

			new igt_source_line("odin-1", "doc id=397 959 961 L G T"),
			new igt_source_line("odin-2", "language: thai (tha)"),
			new igt_source_line("odin-3", "1 เด็กเล็กกินข้าว"),
			new igt_source_line("odin-4", "child small eat rice"),
			new igt_source_line("odin-5", "The small child is eating rice."),
		};

		List<ui_line> lui = new List<ui_line>();

		void old_ui_mode_OnLoaded()
		{
			AttachmentHandles.SetHostParent(w_igt_grid, true);

			w_igt_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			w_igt_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			w_igt_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
			w_igt_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			w_igt_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			foreach (var x in _src)
			{
				lui.Add(new ui_source_line(w_igt_grid, x));
			}
		}

		void start_ui_mode(String filename)
		{
			//var xcorpus = xigt.xaml.Runtime.Load(filename);

			//var corpus = XigtCorpus.Create(xcorpus);

			//w_content.DataContext = corpus;
		}
	}

	public abstract class ui_line
	{
		public ui_line(Grid g, int i_row)
		{
			this.g = g;
			this.i_row = i_row;
			g.RowDefinitions.Insert(i_row, new RowDefinition { MinHeight = 22 });

			foreach (UIElement child in g.Children)
			{
				int r = Grid.GetRow(child);
				if (r >= i_row)
					Grid.SetRow(child, r + 1);
			}

			String txt;
			if (this is ui_source_line)
				txt = "src";
			else if (this is ui_tokenization_line)
				txt = "brk";
			else if (this is ui_alignment_line)
				txt = "aln";
			else if (this is ui_dependency_line)
				txt = "dep";
			else
				throw new Exception();

			var tb_type = new TextBlock
			{
				Text = txt,
				FontFamily = new FontFamily("Arial Black"),
				FontSize = 18,
				VerticalAlignment = VerticalAlignment.Center,
				Padding = new Thickness(2, 0, 2, 0),
			};
			Grid.SetRow(tb_type, i_row);
			Grid.SetColumn(tb_type, 0);
			g.Children.Add(tb_type);
		}

		public ui_line(Grid g)
			: this(g, g.RowDefinitions.Count)
		{
		}

		public Grid g;
		public int i_row;

		//public int insert_grid_row_below()
		//{
		//	int i_new = i_row + 1;
		//	g.RowDefinitions.Insert(i_new, new RowDefinition { MinHeight = 22 });
		//	foreach (UIElement child in g.Children)
		//	{
		//		int r = Grid.GetRow(child);
		//		if (r >= i_new)
		//			Grid.SetRow(child, r + 1);
		//	}
		//	return i_new;
		//}
	};

	public class ui_tokenization_line : ui_line
	{
		public ui_tokenization_line(Grid g, ui_source_line usl)
			: base(g)
		{
			this.usl = usl;

			this.btn_add_alignment = new Button
			{
				Content = "=",
				Width = 28,
				Margin = new Thickness(2, 0, 2, 0),
			};
			btn_add_alignment.Click += (o, e) =>
			{
				var ib = i_row + 1;
				if (ib >= g.RowDefinitions.Count)
					return;
				var utl_b = ui_source_line.tok_lines_all.FirstOrDefault(x => x.i_row == ib);
				if (utl_b == null)
					return;
				if (utl_b == this)
					throw new Exception();
				var ual = new ui_alignment_line(this, utl_b, ib);
				aligns.Add(ual);
			};
			Grid.SetRow(btn_add_alignment, i_row);
			Grid.SetColumn(btn_add_alignment, 1);
			g.Children.Add(btn_add_alignment);

			this.btn_add_dependency = new Button
			{
				Content = "D",
				Width = 28,
				Margin = new Thickness(2, 0, 2, 0),
			};
			btn_add_dependency.Click += (o, e) =>
			{
				var udl = new ui_dependency_line(this, i_row + 1);
				deps.Add(udl);
			};
			Grid.SetRow(btn_add_dependency, i_row);
			Grid.SetColumn(btn_add_dependency, 2);
			g.Children.Add(btn_add_dependency);

			this.sp_toks = new StackPanel
			{
				Orientation = Orientation.Horizontal,
			};
			Grid.SetRow(sp_toks, i_row);
			Grid.SetColumn(sp_toks, 4);
			Grid.SetColumnSpan(sp_toks, 99);
			g.Children.Add(sp_toks);
		}

		ui_source_line usl;

		List<ui_alignment_line> aligns = new List<ui_alignment_line>();
		List<ui_dependency_line> deps = new List<ui_dependency_line>();

		Button btn_add_alignment;
		Button btn_add_dependency;
		public StackPanel sp_toks;

		public void add_tok(lattice_edge edge)
		{
			sp_toks.Children.Add(new tok_display(edge));
		}

		public class tok_display : AttachmentHandles
		{
			public tok_display(String text)
			{
				this.text = text;
				this.Content = new Border
				{
					Margin = new Thickness(3, 0, 3, 0),
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(1),
					CornerRadius = new CornerRadius(2),
					Padding = new Thickness(3, 0, 3, 0),
					Background = Brushes.BlanchedAlmond,
					Child = new TextBlock
					{
						Text = text,
					},
				};
			}
			public tok_display(lattice_edge edge)
				: this(edge.ComputedText)
			{
				this.edge = edge;
			}
			public String text;
			public lattice_edge edge;
		}
	};

	public abstract class _has_sp_area : ui_line
	{
		public _has_sp_area(Grid g, int i_row)
			: base(g, i_row)
		{
			this.sp_area = new StackPanel
			{
				Orientation = Orientation.Horizontal,
			};
			Grid.SetRow(sp_area, i_row);
			Grid.SetColumn(sp_area, 4);
			Grid.SetColumnSpan(sp_area, 99);
			g.Children.Add(sp_area);
		}
		public StackPanel sp_area;
	};

	public class ui_alignment_line : _has_sp_area
	{
		public ui_alignment_line(ui_tokenization_line utl_a, ui_tokenization_line utl_b, int i_row)
			: base(utl_a.g, i_row)
		{
			this.utl_a = utl_a;
			this.utl_b = utl_b;

			g.UpdateLayout();

			foreach (ui_tokenization_line.tok_display a in utl_a.sp_toks.Children)
			{
				foreach (ui_tokenization_line.tok_display b in utl_b.sp_toks.Children)
				{
					var l = AttachmentHandles.ConnectVertical(b, a);
					Grid.SetRowSpan(l, 99);
					Grid.SetColumnSpan(l, 99);
					Panel.SetZIndex(l, 99);
					g.Children.Add(l);
				}
			}
		}
		ui_tokenization_line utl_a, utl_b;
	};

	public class ui_dependency_line : ui_line
	{
		public ui_dependency_line(ui_tokenization_line utl, int i_row)
			: base(utl.g, i_row)
		{
			var b = new Border
			{
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1),
				Margin = new Thickness(3),
				Child = new StackPanel
				{
					Orientation = Orientation.Vertical,
				},
			};
			Grid.SetRow(b, i_row);
			Grid.SetColumn(b, 4);
			Grid.SetColumnSpan(b, 99);
			g.Children.Add(b);
			var sp_updn = (StackPanel)b.Child;

			this.tok_bin = new dip(this)
			{
				Height = 50,
				Background = Brushes.AliceBlue,
			};
			sp_updn.Children.Add(tok_bin);

			this.tree_pan = new TreeLayoutPanel
			{
				MinHeight = 40,
				Background = Brushes.Gainsboro,
			};
			sp_updn.Children.Add(tree_pan);

			ui_tokenization_line.tok_display td;
			double x = 0;
			foreach (ui_tokenization_line.tok_display _td in utl.sp_toks.Children)
			{
				td = new ui_tokenization_line.tok_display(_td.edge);
				DragItemPanel.SetLeft(td, x);
				tok_bin.Children.Add(td);
				td.Measure(util.infinite_size);
				x += td.DesiredSize.Width + 8;
			}
			td = new ui_tokenization_line.tok_display("EC");
			DragItemPanel.SetLeft(td, x);
			tok_bin.Children.Add(td);
		}
		public DragItemPanel tok_bin;
		public TreeLayoutPanel tree_pan;
	};

	public class dip : DragItemPanel
	{
		public dip(ui_dependency_line udl)
		{
			this.udl = udl;
		}
		ui_dependency_line udl;
		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseUp(e);

			var _td = e.Source as ui_tokenization_line.tok_display;
			if (_td == null)
				return;

			ui_tokenization_line.tok_display other = null;
			if (udl.tree_pan.Children.Count > 0)
				other = udl.tree_pan.Children[0] as ui_tokenization_line.tok_display;

			var td = new ui_tokenization_line.tok_display(_td.text);
			udl.tree_pan.Children.Add(td);

			if (other != null)
				TreeLayoutPanel.SetTreeParent(td, other);
		}

	};

	public class ui_source_line : ui_line
	{
		public ui_source_line(Grid g, igt_source_line isl)
			: base(g)
		{
			this.isl = isl;

			create();
			g.Children.Add(btn_add_tokenization);
			g.Children.Add(txt_name);
			g.Children.Add(src_display);

			set_grid_row(i_row);
		}
		public igt_source_line isl;

		Button btn_add_tokenization;
		TextBlock txt_name;
		source_display src_display;

		List<ui_tokenization_line> tok_lines = new List<ui_tokenization_line>();
		public static List<ui_tokenization_line> tok_lines_all = new List<ui_tokenization_line>();

		public ui_tokenization_line utl_cur_edit;

		void create()
		{
			this.btn_add_tokenization = new Button
			{
				Content = "/",
				Width = 28,
				Margin = new Thickness(2, 0, 2, 0),
			};
			Grid.SetColumn(btn_add_tokenization, 1);
			btn_add_tokenization.Click += (o, e) =>
			{
				add_new_tok_line();
			};

			this.txt_name = new TextBlock
			{
				Text = isl.name,
				VerticalAlignment = VerticalAlignment.Center,
				Padding = new Thickness(2, 0, 2, 0),
			};
			Grid.SetColumn(txt_name, 3);

			this.src_display = new source_display(this);
			Grid.SetColumn(src_display, 4);
		}

		void add_new_tok_line()
		{
			utl_cur_edit = new ui_tokenization_line(g, this);
			tok_lines.Add(utl_cur_edit);
			tok_lines_all.Add(utl_cur_edit);
		}

		void set_grid_row(int i_row)
		{
			Grid.SetRow(btn_add_tokenization, i_row);
			Grid.SetRow(txt_name, i_row);
			Grid.SetRow(src_display, i_row);
		}

		public class source_display : Border
		{
			public source_display(ui_source_line usl)
			{
				this.usl = usl;
				this.BorderThickness = new Thickness(1);
				this.BorderBrush = Brushes.LightGray;
				//this.Padding = new Thickness(5);

				this.Child = new TextBox
				{
					Text = usl.isl.text,
					HorizontalAlignment = HorizontalAlignment.Left,
				};
			}
			ui_source_line usl;

			protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
			{
				if (usl.utl_cur_edit == null)
					usl.add_new_tok_line();

				var tb = (TextBox)this.Child;

				String sel;
				int i_from, i_to;

				while ((sel = tb.SelectedText).Length > 0 && Char.IsWhiteSpace(sel[sel.Length - 1]))
					tb.SelectionLength--;

				while (true)
				{
					i_from = tb.SelectionStart;
					i_to = i_from + tb.SelectionLength;
					if (i_from == i_to || !Char.IsWhiteSpace(tb.SelectedText[0]))
						break;
					tb.SelectionStart++;
				}

				var le = usl.isl.get_edge(i_from, i_to);
				if (!String.IsNullOrWhiteSpace(le.ComputedText))
					usl.utl_cur_edit.add_tok(le);

				base.OnPreviewMouseUp(e);
			}
		};
	};
#endif
}