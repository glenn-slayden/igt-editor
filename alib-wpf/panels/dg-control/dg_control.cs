#define FORCE_DIRECTED
//#define animate

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using alib.Debugging;

using alib.dg;
using alib.Collections;
using alib.Enumerable;
using alib.Wpf;
using alib.Array;

namespace alib.Wpf
{
	using String = System.String;
	using Math = System.Math;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// Not really a WPF 'panel' because we aren't going to provide access to the children as DependencyObjects
	/// </summary>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class graph_defaults : FrameworkElement
	{
		//public const Double HorizontalNodePadding = 20;
		public const Double HorizontalNodePadding = 120;
		//public const Double VerticalNodePadding = 100;
		public const Double VerticalNodePadding = 200;

		//public const Double NodeMinWidth = 36;
		//public const Double NodeMinHeight = 0;
		public const Double NodeMinWidth = 75;
		public const Double NodeMinHeight = 27;

		public const Double NodeCornerRounding = 2.0;
		public const Double NodeInnerMargin = 4.0;

		protected graph_defaults()
		{
			this.typeface = new Typeface(new FontFamily("Cambria"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
			this.font_size = 15;
			//this.text_background = Brushes.AliceBlue;
			this.text_background = null;
			this.text_color = Brushes.Black;

			this.HorizontalAlignment = HorizontalAlignment.Left;
			this.VerticalAlignment = VerticalAlignment.Top;

			this.Margin = new Thickness(10);

			this.edge_stroke = new Pen(Brushes.Black, 1);
			this.edge_stroke.Freeze();

			this.master_edge_stroke = new Pen(Brushes.Navy, 1);
			this.master_edge_stroke.Freeze();

			this.node_stroke = edge_stroke;

#if false
			this.leaf_node_fill = Brushes.MistyRose;
			this.top_node_fill = Brushes.Thistle;
			this.node_fill = Brushes.LemonChiffon;
#else
			root_node_fill = leaf_node_fill = node_fill = Brushes.LemonChiffon;
#endif
		}

		public readonly Double font_size;
		public Double line_spacing;
		protected Typeface typeface;

		public readonly Pen edge_stroke;
		public readonly Pen master_edge_stroke;

		public readonly Pen node_stroke;

		public readonly Brush node_fill;
		public readonly Brush leaf_node_fill;
		public readonly Brush root_node_fill;

		public readonly Brush text_background;
		public readonly Brush text_color;

		public Double[] iter_deltas;	// hack hack
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class drawing_cache : graph_defaults
	{
		protected drawing_cache()
		{
			this.h = new render_text_helper();
			this.rect_drawing_cache = new Dictionary<rect_cache_key, Drawing>();
			this.text_drawing_cache = new Dictionary<text_cache_key, Drawing>();
		}

		public struct text_cache_key : IEquatable<text_cache_key>
		{
			public Point pt;
			public Brush brush;
			public Brush bg;
			public String text;

			public override bool Equals(Object o)
			{
				return o is text_cache_key && Equals((text_cache_key)o);
			}
			public bool Equals(text_cache_key o)
			{
				return pt == o.pt && brush == o.brush && bg == o.bg && StringComparer.Ordinal.Equals(text, o.text);
			}
			public override int GetHashCode()
			{
				int h = pt.GetHashCode();
				if (brush != null)
					h ^= brush.GetHashCode();
				if (bg != null)
					h ^= bg.GetHashCode();
				return h ^ StringComparer.Ordinal.GetHashCode(text);
			}
		};

		public struct rect_cache_key : IEquatable<rect_cache_key>
		{
			public Brush brush;
			public Pen stroke;
			public Size sz;
			public Double corner_radius;

			public override bool Equals(Object o)
			{
				return o is rect_cache_key && Equals((rect_cache_key)o);
			}
			public bool Equals(rect_cache_key o)
			{
				return sz == o.sz && brush == o.brush && stroke == o.stroke && corner_radius == o.corner_radius;
			}
			public override int GetHashCode()
			{
				int h = sz.GetHashCode() ^ corner_radius.GetHashCode();
				if (brush != null)
					h ^= brush.GetHashCode();
				if (stroke != null)
					h ^= stroke.GetHashCode();
				return h;
			}
		};

		readonly render_text_helper h;

		readonly Dictionary<text_cache_key, Drawing> text_drawing_cache;

		readonly Dictionary<rect_cache_key, Drawing> rect_drawing_cache;

		public Drawing get_text_drawing(ref text_cache_key cp)
		{
			Debug.Assert(!String.IsNullOrEmpty(cp.text));

			Drawing d;
			if (!text_drawing_cache.TryGetValue(cp, out d))
				text_drawing_cache.Add(cp, d = _make_text_drawing(cp.text, cp.brush, cp.pt, cp.bg));
			return d;
		}

		Typeface __tf;
		public Drawing _make_centered_text_drawing(String text, Brush brush, Point pt, Brush bg = null, Double font_size = 0.0)
		{
			if (font_size == 0)
				font_size = this.font_size;

			if (__tf == null)
				__tf = new Typeface(util.ff_arial_ums, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

			var ft = new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				__tf,//typeface,
				font_size,
				brush,
				null,
				TextFormattingMode.Ideal);

			if (ft.Height > line_spacing)
				line_spacing = ft.Height;

			pt.X -= ft.Width / 2;

			return h.make_text_drawing(ft, pt, bg);
		}

		public Drawing _make_text_drawing(String text, Brush brush, Point pt, Brush bg, Double font_size = 0.0)
		{
			Debug.Assert(!String.IsNullOrEmpty(text));

			if (font_size == 0)
				font_size = this.font_size;

			var ft = new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				typeface,
				font_size,
				brush,
				null,
				TextFormattingMode.Ideal);

			if (ft.Height > line_spacing)
				line_spacing = ft.Height;

			return h.make_text_drawing(ft, pt, bg);
		}

		public Drawing get_rect_drawing(ref rect_cache_key cp)
		{
			Drawing d;
			if (!rect_drawing_cache.TryGetValue(cp, out d))
			{
				/// keep origin at (0,0); i.e. don't let the thickness of the node outline increase the bounds making the TopLeft go negative
				var r = new Rect(cp.sz);
				var nst = cp.stroke.Thickness / -2.0;
				r.Inflate(nst, nst);

				var rg = new RectangleGeometry(r, cp.corner_radius, cp.corner_radius);
				d = new GeometryDrawing(cp.brush, cp.stroke, rg);
				d.Freeze();

				rect_drawing_cache.Add(cp, d);
			}
			return d;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class graph_control_binder : drawing_cache, IGraphExImpl
	{
		public IGraph Graph
		{
			get { return g; }
			set
			{
				var _tmp = this.g;
				if (_tmp == value)
					return;
				if (_tmp != null)
				{
					this.g = null;
					RemoveLogicalChild(_tmp);
				}
				if (value != null)
				{
					//this.g = prepare_graph(value);

					AddLogicalChild(this.g = value);

					OnGraphChanged(rw_graph.GraphEditOp.Unknown, 0);
				}
			}
		}

		//IGraph prepare_graph(IGraph g)
		//{
		//	var epg = new edge_proxy_graph(g);

		//	Nop.X();


		//	return null;
		//}


		protected
		IGraph g;
		public IGraphCommon SourceGraph { get { return g; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public EdgeElement[] Edges;

		public int EdgeCount { get { return g.EdgeCount; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IEdgeEx> IGraphExImpl.Edges { get { return this.Edges; } }

		public int VertexCount { get { return g.VertexCount; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public VertexElement[] Verticies;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { return this.Verticies; } }

		public IReadOnlyList<IVertexEx> Roots { get { throw not.impl; } }

		public IReadOnlyList<IVertexEx> Leaves { get { throw not.impl; } }

		protected virtual void OnGraphChanged(rw_graph.GraphEditOp op, int data)
		{
		}

		protected void deploy()
		{
			foreach (var el in Verticies)
				if (el != null)
					el.Origin = new Point(el.ComputedLeft, el.Y);

			foreach (var ee in Edges)
				if (ee != null)
					ee.reset_drawings();
		}

		protected sealed override IEnumerator LogicalChildren { get { return new UnaryCollection<IGraph>._enum(g); } }

		IComparer<VertexElement> cve;

		[DebuggerDisplay("Length = {rgve.Length}")]
		public VertexElement[] _dbg_rgve
		{
			get
			{
				var lg = g as layout_graph;
				if (lg == null)
					return Verticies;
				if (cve == null)
					this.cve = new _vtx_cmp_logical(lg.layout_cur);
				var dst = new VertexElement[Verticies.Length];
				Verticies.CopyTo(dst, 0);
				alib.Array.arr.qsort(dst, cve);
				return dst;
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	sealed class _vtx_cmp_logical : IComparer<VertexElement>
	{
		public _vtx_cmp_logical(logical_layout hyp)
		{
			this.hyp = hyp;
		}

		readonly logical_layout hyp;

		public int Compare(VertexElement x, VertexElement y)
		{
			var log_pos_x = x.LogicalPosition;
			var log_pos_y = y.LogicalPosition;

			int d = log_pos_x.Row - log_pos_y.Row;
			if (d != 0)
				return d;

			var p2l = hyp.phys2log(log_pos_x.Row);

			return p2l[log_pos_x.Column] - p2l[log_pos_y.Column];
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	sealed class animator : DispatcherTimer
	{
		public animator(graph_layout lg, int max_iter, Double rjmp_start)
			: base(DispatcherPriority.ApplicationIdle, lg.Dispatcher)
		{
			this.lg = lg;
			this.max_iter = max_iter;
			this.rjmp = rjmp_start;

			this.Tick += animate_event;
			this.Start();
		}

		readonly graph_layout lg;
		readonly int max_iter;
		int i_iter;
		Double rjmp;

		void animate()
		{
			int c_changed = lg.animate(rjmp);

			if (c_changed == 0 || ++i_iter >= max_iter)
			{
				this.Stop();
				this.Tick -= animate_event;
			}
		}

		static void animate_event(Object o, EventArgs e) { ((animator)o).animate(); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class graph_layout : graph_control_binder
	{
		public static DependencyProperty LayoutIterationsProperty;

		static graph_layout()
		{
			LayoutIterationsProperty = DependencyProperty.Register("LayoutIterations", typeof(int), typeof(graph_layout), new FrameworkPropertyMetadata(10000));
		}

		public int LayoutIterations
		{
			get { return (int)GetValue(LayoutIterationsProperty); }
			set { SetValue(LayoutIterationsProperty, value); }
		}

		protected MasterElement master_element;

		protected override void OnGraphChanged(rw_graph.GraphEditOp op, int data)
		{
			base.OnGraphChanged(op, data);

			if (SourceGraph.VertexCount > 0)
			{
				var g_raw = (IGraphRaw)SourceGraph;

				layout_graph lg;

				this.g = lg = new layout_graph(g_raw)
				{
					max_iter = LayoutIterations,
				};

				lg.DoLayout();

				this.master_element = new MasterElement(this, out this.Edges, out this.Verticies);

				deploy_layout(lg.layout_cur);
			}
		}

		const int MaxIter = 100;
		const Double StepStart = 9.0;
		const Double StepIncr = double.NaN;

#if FORCE_DIRECTED

		void deploy_layout(logical_layout hyp)
		{
			setup_forces(hyp);

			set_ys(hyp);

#if animate
			deploy();

			new animator(this, MaxIter, StepStart);
#else
			adjust_loop();

			deploy();

			//show_spacing();

			//show_vertex_energy();
#endif
		}

		public int animate(Double rjmp)
		{
			int c_changed = step(rjmp);

			deploy();

			return c_changed;
		}

		void adjust_loop()
		{
			Double rjmp = StepStart;
			for (int i_iter = 0; i_iter < MaxIter /*&& rjmp > 0.0*/; i_iter++)
			{
				Debug.Print("iter {0}", i_iter);
				int c_changed = step(rjmp);

				//Debug.Print("iter:{0,-3}  changed:{1,3}  {{ {2} }}", i_iter, c_changed, iter_deltas.Where(x => x > 0).Select(d => d.ToString("G3")).StringJoin(" "));
				if (c_changed == 0)
					break;

				//if (i % 10 == 0)
				//	rjmp -= 1.0;
			}
		}

		int step(Double rjmp)
		{
			this.iter_deltas = new Double[Verticies.Length];

			foreach (var ve in Verticies)
				if (ve != null)
					adjust_vertex(ve, rjmp);

			return iter_deltas.Count(x => x > 0);
		}

		void setup_forces(logical_layout hyp)
		{
			Double y_incr = Verticies.Where(ve => ve != null).Max(ve => ve.Bounds.Height) + VerticalNodePadding;
			Double y = 0.0;

			for (int i_level = 0; i_level < hyp.Count; i_level++)
			{
				VertexElement ve = null, v_prev = null;

				foreach (Vref vr in hyp.VrefsLogical(i_level))
				{
					ve = Verticies[vr];

					ve.delta = 0;
					ve.Y = y;

					if (v_prev != null)
						(ve.ve_left = v_prev).ve_right = ve;

					v_prev = ve;
				}
				y += y_incr;
			}
		}

		void set_ys(logical_layout hyp)
		{
			Double y_incr = Verticies.Where(ve => ve != null).Max(ve => ve.Bounds.Height) + VerticalNodePadding;
			Double y = 0.0;

			for (int i_level = 0; i_level < hyp.Count; i_level++)
			{
				foreach (Vref vr in hyp.VrefsLogical(i_level))
					Verticies[vr].Y = y;

				y += y_incr;
			}
		}

		void adjust_vertex(VertexElement ve, Double r)
		{
			VertexElement ve_par;

			if (ve.WantRight)
			{
				if (ve.Rightmost)
				{
					ve.delta += r;
				}
				else if (ve.RightPad - HorizontalNodePadding > r)
				{
					ve.delta += r;
					ve.ve_right.delta -= r;
				}
				else if (!ve.Leftmost && ve.ve_left.WantLeft)
				{
					ve.delta += r;
				}
				else if ((ve_par = ve.ParentVerticies.Cast<VertexElement>().FirstOrDefault(_v => _v.WantRight)) != null)
				{
					var cur = ve_par;
					while ((ve_par = cur.ParentVerticies.Cast<VertexElement>().FirstOrDefault(_v => _v.WantRight)) != null)
						cur = ve_par;
					cur.delta += r;
				}
				else if (ve.ve_right.WantLeft)
				{
					//var vrix = ve.vrix;
					//lg.levels[vrix.i_level].arr[vrix.ix_phys]._change_logical_index(ve.ve_right._ix_log);
					//rgve[ve.vr] = ve.ve_right;
					//rgve[ve.ve_right.vr] = ve;

					//var x = lg.levels[vrix.i_level].renormalize_logical(lg.levels[vrix.i_level]);
					//lg.levels[vrix.i_level].SetLogicalOrder();
					Nop.X();
				}
				//else if (ve.ve_right)
				//{
				//	ve.delta += r;
				//}
				else
				{
					//ve.delta += 3;
					Nop.X();
				}
			}
			if (ve.WantLeft)
			{
				//if (ve.Leftmost && ve.ve_right != null)
				//{
				//	ve.ve_right.delta += r;
				//}
				//else
				if (ve.LeftPad - HorizontalNodePadding >= r)
				{
					ve.delta -= r;
				}
			}
		}

		void show_vertex_energy()
		{
			using (var qq = master_element._get_drawing().Append())
			{
				foreach (var el in Verticies)
				{
					if (el == null)
						continue;
					var txt = String.Format("{0:N2}", el.ComputeEnergy());
					var drw = _make_centered_text_drawing(txt, Brushes.Crimson, new Point(el.HCenter, el.Top + 17), null, 12.0);
					qq.DrawDrawing(drw);

					txt = "";
					bool wl = el.WantLeft, wr = el.WantRight;
					if (wl)
						txt += "◀L";
					else if (wr)
						txt += "R▶";
					else
						txt += "=";

					if (el.Rightmost)
						txt += "]";
					if (el.Leftmost)
						txt = "[" + txt;
					drw = _make_centered_text_drawing(txt, Brushes.Crimson, new Point(el.HCenter + 26, el.Top - 1), null, 12.0);
					qq.DrawDrawing(drw);
				}
			}
		}

		void show_spacing()
		{
			using (var qq = master_element._get_drawing().Append())
				foreach (var el in Verticies)
				{
					if (el == null)
						continue;
					var txt = String.Format("{0:N0}", el.X);
					qq.DrawDrawing(_make_text_drawing(txt, Brushes.Black, new Point(el.X - 37, el.Y + 1), null, 9.0));
					txt = String.Format("Δ {0:N0}", el.delta);
					qq.DrawDrawing(_make_text_drawing(txt, Brushes.Black, new Point(el.X - 38, el.Y + 11), null, 9.0));
					txt = String.Format("V" + (int)el.vr);
					qq.DrawDrawing(_make_text_drawing(txt, Brushes.Black, new Point(el.HCenter + 20, el.Y + 11), null, 9.0));
					txt = String.Format("{0,-4} {1,27}", el.LeftPad, el.RightPad);
					qq.DrawDrawing(_make_text_drawing(txt, Brushes.Black, new Point(el.X - 38, el.Y + 21), null, 9.0));
				}
		}
#else
		void deploy_layout(logical_layout hyp)
		{
			Double max_width = 0.0, max_height, x, y = 0.0;

			for (int i_level = 0; i_level < hyp.Count; i_level++)
			{
				x = 0.0;
				max_height = 0.0;

				foreach (Vref vr in hyp.VrefsLogical(i_level))
				{
					var el = rgve[vr];

					el.Origin = new Point(x, y);
					Rect r = el.Bounds;

					x += r.Width + HorizontalNodePadding;
					if (r.Height > max_height)
						max_height = r.Height;
				}
				if (x > max_width)
					max_width = x;

				y += max_height + VerticalNodePadding;
			}

			for (int i_level = 0; i_level < hyp.Count; i_level++)
			{
				Double x_incr;
				int c = lg.levels[i_level].Count;
				if (c > 1)
				{
					x_incr = max_width / (c - 1);
					x = 0.0;
				}
				else
				{
					x_incr = max_width;
					x = x_incr / 2;
				}
				foreach (Vref vr in hyp.VrefsLogical(i_level))
				{
					var el = rgve[vr];
					el.Origin = new Point(x, el.Origin.Y);
					x += Math.Max(el.Width + HorizontalNodePadding, x_incr);
				}
			}
		}
#endif
		void deploy_layout_old(layout_graph lay)
		{
			Double max_width = 0.0, max_height, x, y = 0.0;

			foreach (var LL in lay.levels)
			{
				x = 0.0;
				max_height = 0.0;

				foreach (Vref vr in LL)
				{
					var el = Verticies[vr];

					el.Origin = new Point(x, y);
					Rect r = el.Bounds;

					x += r.Width + HorizontalNodePadding;
					if (r.Height > max_height)
						max_height = r.Height;
				}
				if (x > max_width)
					max_width = x;

				y += max_height + VerticalNodePadding;
			}

			foreach (var LL in lay.levels)
			{
				Double x_incr;
				if (LL.Count > 1)
				{
					x_incr = max_width / (LL.Count - 1);
					x = 0.0;
				}
				else
				{
					x_incr = max_width;
					x = x_incr / 2;
				}
				foreach (Vref vr in LL)
				{
					var el = Verticies[vr];
					el.Origin = new Point(x, el.Origin.Y);
					x += Math.Max(el.Width + HorizontalNodePadding, x_incr);
				}
			}
		}

	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class graph_control : graph_layout
	{
		protected override void OnGraphChanged(rw_graph.GraphEditOp op, int data)
		{
			base.OnGraphChanged(op, data);
			InvalidateVisual();
		}

		protected override Size MeasureOverride(Size _) { return master_element.OriginExtent; }

		protected override void OnRender(DrawingContext dc) { master_element.RenderTo(dc); }

		//protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		//{
		//	base.OnPreviewMouseLeftButtonDown(e);
		//}

		//protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
		//{
		//	return base.HitTestCore(hitTestParameters);
		//}

		protected override HitTestResult HitTestCore(PointHitTestParameters htp)
		{
			return new PointHitTestResult(this, htp.HitPoint);
		}

		sealed class DrawingHitTestResult : PointHitTestResult
		{
			public DrawingHitTestResult(Visual v, Point pt, Drawing d, int ix)
				: base(v, pt)
			{
				this.d = d;
				this.index = ix;
			}
			public readonly Drawing d;
			public readonly int index;
		};

#if false
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs ev)
		{
			var q = EnumDrawingGroup(root_elem, ev.GetPosition(this));
			if (q != null)
			{
				var yy = ((DrawingHitTestResult)q).d;
				var zz = ((DrawingHitTestResult)q).index;
				var di = drawing_index.entries[zz].value;

				Debug.Print("{0} {1}", di.flags, di.ix);

				if (di.IsEdge)
				{
					var er = new Eref(di.ix);

					graph.edge_set_master(er);

					redraw_links();

				}
			}
		}

		DrawingHitTestResult EnumDrawingGroup(DrawingGroup dg, Point pt)
		{
			DrawingGroup d1;
			GeometryDrawing d2;
			GlyphRunDrawing d3;
			int ix;

			if (dg.Bounds.Contains(pt) && (ix = drawing_index.FindEntry(dg)) != -1)
				return new DrawingHitTestResult(this, pt, dg, ix);

			foreach (Drawing drawing in dg.Children)
			{
				if ((d1 = drawing as DrawingGroup) != null)
				{
					var r = EnumDrawingGroup(d1, pt);
					if (r != null)
						return r;
				}
				else if ((d2 = drawing as GeometryDrawing) != null)
				{
					if ((ix = drawing_index.FindEntry(d2)) != -1)
					{
						if (d2.Geometry.FillContains(pt))
							return new DrawingHitTestResult(this, pt, d2, ix);

						var p = drawing_index.entries[ix].value.stroke;
						if (d2.Geometry.StrokeContains(p, pt))
							return new DrawingHitTestResult(this, pt, d2, ix);
					}
				}
				else if ((d3 = drawing as GlyphRunDrawing) != null)
				{
					/// todo: need to change this for rotated text.
					/// investigate: PathGeometry.CreateFromGeometry(...?)
					if ((ix = drawing_index.FindEntry(d3)) != -1)
					{
						if (d3.Bounds.Contains(pt))
							return new DrawingHitTestResult(this, pt, d3, ix);
					}
				}
				else
					throw new Exception();
			}
			return null;
		}
#endif

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public new VertexElement[] _dbg_rgve { get { return base._dbg_rgve; } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class graph_scroll : PanScroller
	{
		public graph_scroll()
		{
			base.Content = new graph_control();
		}

		public IGraph Graph
		{
			get { return ((graph_control)base.Content).Graph; }
			set { ((graph_control)base.Content).Graph = value; }
		}
	};
}
