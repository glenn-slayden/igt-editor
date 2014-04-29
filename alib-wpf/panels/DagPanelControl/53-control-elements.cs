using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.dg;
using alib.Enumerable;

namespace alib.Wpf
{
	using Array = System.Array;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public enum EdgeContentOrientation { Orthogonal, Rotated };

	[Flags]
	public enum EdgeContentMode { None = 0, Text = 1, Element = 2, Index = 3 };

	public enum ContentAlignment { TowardsRoot, Center, TowardsLeaf };

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public interface ILayoutEdgeEx : ILogicalLayoutEdgeEx
	{
		new ILayoutVertexEx From { get; }
		new ILayoutVertexEx To { get; }
		Pen LineStroke { get; set; }
		Brush LineFill { get; set; }
	};

	public interface ILayoutVertexEx : ILogicalLayoutVertexEx
	{
		new IReadOnlyList<ILayoutEdgeEx> InEdges { get; }
		new IReadOnlyList<ILayoutEdgeEx> OutEdges { get; }
		Rect LayoutRect { get; }
	};

	public interface IGraphExWpfLayoutProvider : IGraphExLayoutProvider, IReadOnlyList<IGraphExWpfLayoutLevel>
	{
		Thickness VertexPadding { get; }
		Thickness EdgePadding { get; }
		EdgeContentOrientation EdgeContentOrientation { get; }
		EdgeContentMode EdgeContentMode { get; }
	};

	public interface IGraphExWpfLayoutLevel : IGraphExLayoutLevel, IReadOnlyList<ILayoutVertexEx>
	{
		new IGraphExWpfLayoutProvider GraphInstance { get; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class layout_element : IGraphExLogicalElement
	{
		public layout_element(WpfGraphAdapter gx, int ix, String label)
		{
			this.gx = gx;
			this.ix = ix;
			this._text = label;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly WpfGraphAdapter gx;
		public WpfGraphAdapter GraphInstance { get { return gx; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IGraphEx IGraphExProxy.GraphInstance { get { return gx; } }

		protected DagPanelControl __ctrl { get { return (DagPanelControl)gx.GraphInstance; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly protected int ix;
		public int Index { get { return ix; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal String _text;
		public String TextLabel { get { return _text; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HideContent { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public abstract Thickness Padding { get; }

		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Point pt;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Point Location
		{
			get { return pt; }
			set { pt = value; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual Size Size { get { throw not.valid; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual Rect LayoutRect { get { return new Rect(this.Location, this.Size); } }
		///
		///////////////////////////////////////////////////////////
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class LayoutEdgeEx : layout_element, ILayoutEdgeEx
	{
		LayoutEdgeEx(WpfGraphAdapter gx, UIElement el, int ix, String text, LayoutVertexEx from, LayoutVertexEx to)
			: base(gx, ix, text)
		{
			this.from = from;
			this.to = to;
			this.el = el;
			var br = el.GetValue(DagPanelControl.StrokeProperty) as Brush;
			if (br != null)
			{
				LineFill = br;
				LineStroke = new Pen(br, (Double)el.GetValue(DagPanelControl.StrokeThicknessProperty));
			}
		}

		public LayoutEdgeEx(WpfGraphAdapter gx, UIElement el, int ix, LayoutVertexEx from, LayoutVertexEx to)
			: this(gx, el, ix, WpfGraphAdapter.GetTextLabel(el) ?? el.FindText(), from, to)
		{
			arr.Append(ref from.out_edges, this);
			arr.Append(ref to.in_edges, this);
		}
		protected LayoutEdgeEx(LayoutEdgeEx to_copy)
			: this(to_copy.GraphInstance, to_copy.el, to_copy.ix, to_copy._text, to_copy.from, to_copy.to)
		{
			this.EdgeDirection = to_copy.EdgeDirection;

			from.ReplaceOutEdge(to_copy, this);
			to.ReplaceInEdge(to_copy, this);
		}

		public UIElement el;

		public Geometry geom;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TransformGroup TransformGroup { get { return (TransformGroup)geom.Transform; } }

		public Rect GetRawGeometryBounds()
		{
			var gt = TransformGroup;
			geom.Transform = null;
			var r_geo = geom.Bounds;
			geom.Transform = gt;
			return r_geo;
		}

		public void AddTransform(Transform xform)
		{
			TransformGroup tg;
			if ((tg = TransformGroup) == null)
				geom.Transform = tg = new TransformGroup();
			tg.Children.Add(xform);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override Size Size
		{
			get
			{
				if (geom != null)
					return geom.Bounds.Size;

				if (el == null)
					return util.zero_size;
				if (!el.IsMeasureValid)
					el.Measure(util.infinite_size);
				return el.DesiredSize;
			}
		}

		public EdgeDirection EdgeDirection
		{
			get { return WpfGraphAdapter.GetEdgeDirection(el); }
			set { WpfGraphAdapter.SetEdgeDirection(el, value); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Pen LineStroke { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Brush LineFill { get; set; }

		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		LayoutVertexEx from;
		public LayoutVertexEx From
		{
			get { return from; }
			set
			{
				if (from == value)
					return;
				if (from != null)
				{
					from.RemoveOutEdge(this);
					from = null;
				}
				if (value == to || (from = value) == null)
					return;
				from.AddOutEdge(this);
			}
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IVertexEx IEdgeEx.From { get { return from; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ILayoutVertexEx ILayoutEdgeEx.From { get { return from; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ILogicalLayoutVertexEx ILogicalLayoutEdgeEx.From { get { return from; } }
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		LayoutVertexEx to;
		public LayoutVertexEx To
		{
			get { return to; }
			set
			{
				if (to == value)
					return;
				if (to != null)
				{
					to.RemoveInEdge(this);
					to = null;
				}
				if (value == from || (to = value) == null)
					return;
				to.AddInEdge(this);
			}
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IVertexEx IEdgeEx.To { get { return to; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ILayoutVertexEx ILayoutEdgeEx.To { get { return to; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ILogicalLayoutVertexEx ILogicalLayoutEdgeEx.To { get { return to; } }
		///
		///////////////////////////////////////////////////////////

		public override String ToString()
		{
			return String.Format("[V{0}] ->  E{1}  ->  [V{2}] {3}",
				From.Index + " " + (From.TextLabel ?? "").Trim(),
				Index,
				To.Index + " " + (To.TextLabel ?? "").Trim(),
				TextLabel ?? "");
		}

		public override Thickness Padding
		{
			get
			{
				Object ep;
				var th = __ctrl.EdgePadding;
				if (el != null && (ep = el.GetValue(Control.PaddingProperty)) != DependencyProperty.UnsetValue)
					util.Maximize(ref th, (Thickness)ep);
				return th;
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class LayoutMultiEdgeEx : LayoutEdgeEx, ILayoutEdgeEx
	{
		//public const Double MultiEdgeSeparation = 40.0;
		public Double MultiEdgeSeparation
		{
			get
			{
				var th = __ctrl.swap(__ctrl.EdgePadding);
				return System.Math.Max(th.Left, th.Right);
			}
		}

		public LayoutMultiEdgeEx(LayoutEdgeEx e)
			: base(e)
		{
			this.grp_ix = -1;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEnumerable<LayoutMultiEdgeEx> EdgeGroup
		{
			get
			{
				return this.From
							.out_edges
							.OfType<LayoutMultiEdgeEx>()
							.Where((oe, ix) =>
								{
									if (oe != this)
										return oe.To == this.To;
									grp_ix = ix;
									return true;
								});
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int grp_ix;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int EdgeGroupIndex
		{
			get
			{
				if (grp_ix == -1)
				{
					var e = EdgeGroup.GetEnumerator();
					while (e.MoveNext() && grp_ix == -1)
						;
				}
				return grp_ix;
			}
		}

		public Point multi_mid_adjust(Point pt1, Point pt2)
		{
			var mid = __ctrl.swap(util.Midpoint(pt1, pt2));

			var w = (EdgeGroup.Count() - 1) * MultiEdgeSeparation;

			return __ctrl.swap(new Point((mid.X - (w / 2)) + (EdgeGroupIndex * MultiEdgeSeparation), mid.Y));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public class CompoundLayoutEdgeEx : LayoutEdgeEx, ILayoutEdgeEx
	{
		public CompoundLayoutEdgeEx(LayoutEdgeEx e, layout_vertex_base[] proxies)
			: base(e)
		{
			if (proxies.Length <= 2)
				throw new Exception();
			this.proxies = proxies;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly public layout_vertex_base[] proxies;

		public int EdgeCount { get { return proxies.Length - 1; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public layout_vertex_base FirstProxy { get { return proxies[1]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public layout_vertex_base LastProxy { get { return proxies[proxies.Length - 2]; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IEnumerable<Point> Points
		{
			get
			{
				for (int ix = 0; ix < proxies.Length; ix++)
				{
					var r = proxies[ix].LayoutRect;

					DagPanelControl g;
					if ((g = base.GraphInstance.GraphInstance as DagPanelControl) != null)
					{
						if (ix == 0)
						{
							yield return g.from_anchor(r);
							continue;
						}
						if (ix == proxies.Length - 1)
						{
							yield return g.to_anchor(r);
							continue;
						}

						r = g.coord_adjust(r);
					}
					yield return r.Center();
				}
			}
		}

		public override String ToString()
		{
			return base.ToString() + "  { " + proxies.Select(x => "V" + x.Index).StringJoin(" ") + " }";
		}
	}

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class layout_vertex_base : layout_element, ILayoutVertexEx
	{
		public layout_vertex_base(WpfGraphAdapter gx, int ix, String text)
			: base(gx, ix, text)
		{
			this.Unary = new[] { this };
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly public layout_vertex_base[] Unary;

		public int layout_gen;

		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool LayoutExempt { get { return false; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LogicalPosition LogicalPosition
		{
			get { return new LogicalPosition(this.Row, this.Column); }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected int i_level;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Row
		{
			get { return i_level; }
			set { i_level = value; }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected int ix_phys;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Column
		{
			get
			{
				var lay = GraphInstance.layout;
				if (lay != null)
					return lay.phys2log(i_level)[ix_phys];
				return ix_phys;
			}
			set { ix_phys = value; }
		}
		public LevelInfo Level { get { return __ctrl.levels[i_level]; } }
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		public int EdgeCount { get { return Above.Length + Below.Length; } }
		public bool IsSimpleLayout { get { return EdgeCount == 1; } }
		public int[] Upper, Lower;
		public void set_level_hints(ref int c_edges_above, ref int c_edges_below)
		{
			layout_vertex_base[] rg;
			int c;

			if ((c = (rg = Above).Length) == 0)
				Upper = IntArray.Empty;
			else
			{
				Upper = rg.Select(_vx => _vx.Column).ToArray();
				c_edges_above += c;

			}
			if ((c = (rg = Below).Length) == 0)
				Lower = IntArray.Empty;
			else
			{
				Lower = rg.Select(_ex => _ex.Column).ToArray();
				c_edges_below += c;
			}
		}
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		[DebuggerDisplay("{_dbg_inf(Above),nq}")]
		public abstract layout_vertex_base[] Above { get; }
		public abstract IReadOnlyList<LayoutEdgeEx> InEdges { get; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IEdgeEx> IVertexEx.InEdges { get { return InEdges; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<ILayoutEdgeEx> ILayoutVertexEx.InEdges { get { return InEdges; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<ILogicalLayoutEdgeEx> ILogicalLayoutVertexEx.InEdges { get { return InEdges; } }
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		[DebuggerDisplay("{_dbg_inf(Below),nq}")]
		public abstract layout_vertex_base[] Below { get; }
		public abstract IReadOnlyList<LayoutEdgeEx> OutEdges { get; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<IEdgeEx> IVertexEx.OutEdges { get { return OutEdges; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<ILayoutEdgeEx> ILayoutVertexEx.OutEdges { get { return OutEdges; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IReadOnlyList<ILogicalLayoutEdgeEx> ILogicalLayoutVertexEx.OutEdges { get { return OutEdges; } }
		///
		///////////////////////////////////////////////////////////


		public override String ToString()
		{
			return String.Format("V{0}  {1}  {2}", Index, LogicalPosition, this.LayoutRect);
		}
		String _dbg_inf(layout_vertex_base[] rg)
		{
			return rg.Select(vx =>
			{
				String s = "V" + vx.Index.ToString();
				if (vx is LayoutVertexEx)
					s += " " + (((LayoutVertexEx)vx).TextLabel ?? "");
				return alib.String._string_ext.SQRB(s.Trim());
			})
				.StringJoin(" ");
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class LayoutProxyVertex : layout_vertex_base
	{
		public LayoutProxyVertex(CompoundLayoutEdgeEx comp, int ix, int row)
			: base(comp.GraphInstance, ix, "proxy for '" + comp.TextLabel + "'")
		{
			this.i_level = row;
			this.comp = comp;
		}

		readonly CompoundLayoutEdgeEx comp;
		public CompoundLayoutEdgeEx ForEdge { get { return comp; } }

		int comp_ix { get { return this.Row - comp.From.Row; } }

		[DebuggerDisplay("{_dbg_inf(Above),nq}")]
		public override layout_vertex_base[] Above { get { return comp.proxies[comp_ix - 1].Unary; } }

		[DebuggerDisplay("{_dbg_inf(Below),nq}")]
		public override layout_vertex_base[] Below { get { return comp.proxies[comp_ix + 1].Unary; } }

		public override IReadOnlyList<LayoutEdgeEx> InEdges { get { throw not.valid; } }

		public override IReadOnlyList<LayoutEdgeEx> OutEdges { get { throw not.valid; } }

		public override String ToString() { return base.ToString() + " (proxy)"; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override Size Size
		{
			get
			{
				//LevelInfo level;
				//var lp = (IReadOnlyList<IGraphExWpfLayoutLevel>)GraphInstance.GraphInstance;
				//if (lp != null && (level = lp[i_level] as LevelInfo) != null)
				//{
				//	var sz = level.RuntimeData.ElementMax;
				//	var lop = lp as IGraphExWpfLayoutProvider;
				//	if (((int)lop.LayoutDirection & 1) == 0)
				//		sz.Height = 0;
				//	else
				//		sz.Width = 0;
				//	return sz;
				//}
				return util.zero_size;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override Thickness Padding { get { return comp.Padding; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class LayoutVertexEx : layout_vertex_base
	{
		public LayoutVertexEx(WpfGraphAdapter gx, UIElement el, int ix)
			: base(gx, ix, WpfGraphAdapter.GetTextLabel(el) ?? el.FindText())
		{
			this.in_edges = Collection<LayoutEdgeEx>.None;
			this.out_edges = Collection<LayoutEdgeEx>.None;
			(this.el = el).SetValue(WpfGraphAdapter._lvProperty, this);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		readonly UIElement el;
		public UIElement Element { get { return el; } }

		public override Thickness Padding
		{
			get
			{
				Object vp;
				var th = __ctrl.VertexPadding;
				if (el != null && (vp = el.GetValue(Control.PaddingProperty)) != DependencyProperty.UnsetValue)
					util.Maximize(ref th, (Thickness)vp);
				return th;
			}
		}

		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LayoutEdgeEx[] in_edges;
		public override IReadOnlyList<LayoutEdgeEx> InEdges { get { return in_edges; } }

		public void AddInEdge(LayoutEdgeEx ex) { arr.Append(ref in_edges, ex); }

		public void RemoveInEdge(LayoutEdgeEx ex) { arr.RemoveOne(ref in_edges, ex); }

		public void ReplaceInEdge(LayoutEdgeEx e_old, LayoutEdgeEx e_new)
		{
			int ix;
			do
				if ((ix = Array.IndexOf(in_edges, e_old)) == -1)
					throw new Exception();
			while (e_old != Interlocked.CompareExchange(ref in_edges[ix], e_new, e_old));
		}
		///
		///////////////////////////////////////////////////////////


		///////////////////////////////////////////////////////////
		///
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LayoutEdgeEx[] out_edges;
		public override IReadOnlyList<LayoutEdgeEx> OutEdges { get { return out_edges; } }

		public void AddOutEdge(LayoutEdgeEx ex) { arr.Append(ref out_edges, ex); }

		public void RemoveOutEdge(LayoutEdgeEx ex) { arr.RemoveOne(ref out_edges, ex); }

		public void ReplaceOutEdge(LayoutEdgeEx e_old, LayoutEdgeEx e_new)
		{
			int ix;
			do
				if ((ix = Array.IndexOf(out_edges, e_old)) == -1)
					throw new Exception();
			while (e_old != Interlocked.CompareExchange(ref out_edges[ix], e_new, e_old));
		}
		///
		///////////////////////////////////////////////////////////

		[DebuggerDisplay("{_dbg_inf(Above),nq}")]
		public override layout_vertex_base[] Above
		{
			get
			{
				if (in_edges.Length == 0)
					return Collection<layout_vertex_base>.None;
				CompoundLayoutEdgeEx ce;
				LayoutEdgeEx e;
				var rg = new layout_vertex_base[in_edges.Length];
				for (int i = 0; i < rg.Length; i++)
					rg[i] = (ce = (e = in_edges[i]) as CompoundLayoutEdgeEx) != null ? ce.LastProxy : e.From;
				return rg;
			}
		}
		[DebuggerDisplay("{_dbg_inf(Below),nq}")]
		public override layout_vertex_base[] Below
		{
			get
			{
				if (out_edges.Length == 0)
					return Collection<layout_vertex_base>.None;
				CompoundLayoutEdgeEx ce;
				LayoutEdgeEx e;
				var rg = new layout_vertex_base[out_edges.Length];
				for (int i = 0; i < rg.Length; i++)
					rg[i] = (ce = (e = out_edges[i]) as CompoundLayoutEdgeEx) != null ? ce.FirstProxy : e.To;
				return rg;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public override Size Size
		{
			get
			{
				if (el == null)
					return util.zero_size;
				if (!el.IsMeasureValid)
					el.Measure(util.infinite_size);
				var sz = el.DesiredSize;
				var gx = GraphInstance.GraphInstance as DagPanelControl;
				if (gx != null)
					util.Maximize(ref sz, new Size(gx.VertexMinWidth, gx.VertexMinHeight));
				return sz;
			}
		}

		public override String ToString()
		{
			var s = TextLabel ?? String.Empty;
			if (s.Length > 0)
				s = " " + s;
			return base.ToString() + s;
		}
	};
}
