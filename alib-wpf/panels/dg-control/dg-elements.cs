//#define SPECIAL_SPAN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using alib.Collections;
using alib.Debugging;
using alib.Graph;
using alib.Enumerable;
using alib.String;
using alib.Wpf;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	static class _eb_ext
	{
		static _eb_ext()
		{
			mi_dc_ic = typeof(DrawingCollection).GetMethod("get_InheritanceContext", (BindingFlags)0x24);

#if GRAPH_ELEMENT_DP
			dp_key = DependencyProperty.RegisterAttachedReadOnly("GraphElement", typeof(element_base), typeof(DrawingGroup), null);
#endif
		}

		static readonly MethodInfo mi_dc_ic;

		public static DrawingGroup GetOwner(this DrawingCollection dc)
		{
			return mi_dc_ic.Invoke(dc, null) as DrawingGroup;
		}

#if GRAPH_ELEMENT_DP
		static readonly DependencyPropertyKey dp_key;

		public static DependencyProperty GraphElementProperty
		{
			get { return dp_key.DependencyProperty; }
		}
		public static element_base GetGraphElement(this DrawingGroup dxg)
		{
			var el = dxg.GetValue(GraphElementProperty) as element_base;
			if (el == null)
				Nop.X();
			return el;
		}
		public static void SetGraphElement(this DrawingGroup dxg, element_base el)
		{
			dxg.SetValue(dp_key, el);
		}
		public static element_base GetGraphElement(this DrawingCollection dc)
		{
			var dxg = dc.GetOwner();
			return dxg == null ? null : dxg.GetGraphElement();
		}
#endif
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class graph_item_base
	{
		public graph_item_base(graph_control_binder gcb)
		{
			Debug.Assert(gcb != gcb.SourceGraph);
			this.gcb = gcb;
		}

		public const Double RestDistance = 20;
		public const Double NodeSpacing = graph_defaults.NodeMinWidth + RestDistance;

		public const Double LevelElasticity = 1;
		public const Double EdgeElasticity = 1;

		public readonly graph_control_binder gcb;

		protected IGraph g { get { return (IGraph)gcb.SourceGraph; } }

		public graph_control_binder Graph { get { return gcb; } }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class element_base : graph_item_base, INotifyPropertyChanged
	{
		public element_base(graph_control_binder ctrl)
			: base(ctrl)
		{
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(String s_prop)
		{
			var _tmp = PropertyChanged;
			if (_tmp != null)
				_tmp(this, new PropertyChangedEventArgs(s_prop));
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class element_drawing : element_base
	{
		public element_drawing(graph_control_binder gcb)
			: base(gcb)
		{
			this.drawing = new DrawingGroup { Transform = new TranslateTransform() };
		}

		readonly DrawingGroup drawing;
		public DrawingGroup _get_drawing() { return drawing; }

		public virtual void reset_drawings() { }

		public void RenderTo(DrawingContext dc) { dc.DrawDrawing(drawing); }

		protected DrawingCollection Children { get { return drawing.Children; } }

		TranslateTransform Transform { get { return (TranslateTransform)drawing.Transform; } }

		public Point Origin
		{
			get { return Transform.Origin(); }
			set
			{
				Debug.Assert(value.IsFinite());

				var tt = Transform;
				if (value != tt.Origin())
				{
					tt.SetOrigin(value);
					RaisePropertyChanged(null);
				}
			}
		}

		public Rect Bounds
		{
			get
			{
				var r = drawing.Bounds;
				/// finally, the pesky bug: because Rect.Empty is permanently located at (+∞, +∞), we can't count 
				/// on the drawing to remember the origin if its extent is zero.
				/// **FOR THIS REASON** be sure to always use 'this.Bounds' instead of 'Drawing.Bounds' (or getting
				/// bounds from the drawing directly)
				return r.IsZeroSize() ? new Rect(Transform.Origin(), util.zero_size) : r;
			}
		}

		/// <summary>
		/// Confusing: the drawing may not have upper-left origin at 0,0. If this is the case, the 'Bounds' property will
		/// under-represent the size required to display it. We report the extent here, which entails that there may
		/// be whitespace to the left and top, but never the right and bottom. Also, Margin, if any, is added by WPF and 
		/// should not be reserved here. To avoid confusion, the root drawing should probably be drawn at 0,0.
		/// </summary>
		public Size OriginExtent
		{
			get
			{
				var r = this.Bounds;
				return new Size(Math.Max(r.Width, r.Right), Math.Max(r.Height, r.Bottom));
			}
		}

		public double Width { get { return this.Bounds.Width; } }
		public double Height { get { return this.Bounds.Height; } }

		public Double Top { get { return this.Bounds.Top; } }
		public Double Bottom { get { return this.Bounds.Bottom; } }
		public Double HCenter { get { var r = this.Bounds; return r.Left + r.Width / 2; } }
		public Double VCenter { get { var r = this.Bounds; return r.Top + r.Height / 2; } }

		public Point Center { get { return new Point(HCenter, VCenter); } }
		public Point TopCenter { get { return new Point(HCenter, this.Bounds.Top); } }
		public Point LeftCenter { get { return new Point(this.Bounds.Left, VCenter); } }
		public Point RightCenter { get { return new Point(this.Bounds.Right, VCenter); } }
		public Point BottomCenter { get { return new Point(HCenter, this.Bounds.Bottom); } }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class unkeyed_element : element_drawing
	{
		protected unkeyed_element(graph_control_binder gcb)
			: base(gcb)
		{
		}

		protected void ClearAllDrawings() { base.Children.Clear(); }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class keyed_element : element_drawing
	{
		protected keyed_element(graph_control_binder gcb, ChildKey num_keys)
			: base(gcb)
		{
			var dc = base.Children;
			for (ChildKey i = 0; i < num_keys; i++)
				dc.Add(util.EmptyDrawing);
		}

		protected enum ChildKey
		{
			ROOT_edges /*				*/ = 0,
			ROOT_verticies /*			*/ = 1,
			ROOT_NumChildKeys /*		*/ = (ROOT_verticies + 1),

			EDGE_line /*				*/ = 0,
			EDGE_label /*				*/ = 1,
			EDGE_NumChildKeys /*		*/ = (EDGE_label + 1),

			VERTEX_background /*		*/ = 0,
			VERTEX_text /*				*/ = 1,
			VERTEX_NumChildKeys /*		*/ = (VERTEX_text + 1),
		};

		protected Drawing this[ChildKey ix]
		{
			get { return base.Children[(int)ix]; }
			set { base.Children[(int)ix] = value; }
		}

		protected void ClearDrawing(ChildKey ix) { base.Children[(int)ix] = util.EmptyDrawing; }
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class logical_element : keyed_element, IGraphExLogicalElement
	{
		protected logical_element(graph_control_binder gcb, ChildKey num_keys)
			: base(gcb, num_keys)
		{
		}

		IGraphEx IGraphExProxy.GraphInstance { get { return gcb; } }

		public abstract int Index { get; }

		public abstract String TextLabel { get; }

		public bool HideContent { get; set; }
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract class logical_edge : logical_element, ILogicalLayoutEdgeEx
	{
		protected logical_edge(graph_control_binder gcb, Eref er)
			: base(gcb, ChildKey.EDGE_NumChildKeys)
		{
			this.er = er;
		}

		public readonly Eref er;
		public override int Index { get { return er; } }

		ILogicalLayoutVertexEx ILogicalLayoutEdgeEx.From { get { return From; } }
		IVertexEx IEdgeEx.From { get { return From; } }
		public VertexElement From
		{
			get
			{
				var v_from = g.edge_v_from(er);

				if (g is IGraphEx && ((IGraphExImpl)g).Edges[er].From.Index != v_from)
					throw new Exception();

				return Graph.Verticies[g.edge_v_from(er)];
			}
		}

		ILogicalLayoutVertexEx ILogicalLayoutEdgeEx.To { get { return To; } }
		IVertexEx IEdgeEx.To { get { return To; } }
		public VertexElement To
		{
			get
			{
				var v_to = g.edge_v_to(er);

				if (g is IGraphEx && ((IGraphExImpl)g).Edges[er].To.Index != v_to)
					throw new Exception();

				return Graph.Verticies[v_to];
			}
		}

		public EdgeDirection EdgeDirection { get; set; }

#if false
		public bool IsMaster { get { return g.edge_is_master(er); } }

		public void SetMasterEdge() { ((VertexElement)To).SetMasterEdge(this); }

		public IEdgeEx[] Peers
		{
			get
			{
				int c = g.edge_peer_count(er) - 1;
				if (c == 0)
					return Collection<IEdgeEx>.None;

				var e = this.er;
				var arr = new IEdgeEx[c];
				while (--c >= 0)
					arr[c] = Graph.Edges[e = g.edge_e_next(e)];

				return arr;
			}
		}
#endif

		public EdgeElement[] PeersAndSelf
		{
			get
			{
				int c = g.edge_peer_count(er);
				if (c == 1)
					return new[] { (EdgeElement)this };

				var cur = this.er;
				var arr = new EdgeElement[c];
				while (--c >= 0)
					arr[c] = Graph.Edges[cur = g.edge_e_next(cur)];

				return arr;
			}
		}

		public override String TextLabel
		{
			get
			{
				var s_value = g.edge_string(er);

				if (g is IGraphEx && ((IGraphExImpl)g).Edges[er].TextLabel != s_value)
					throw new Exception();

				//return Math.Round(((EdgeElement)this).ComputedDelta, 2).ToString();
				//return Math.Round(((EdgeElement)this).Length, 2).ToString();
				return s_value;
				//return graph.edge_peer_count(er).ToString();
			}
		}

		protected bool ShowArrow
		{
			get
			{
				var lg = g as layout_graph;
				return lg == null || !lg.is_layout_proxy_vref(g.edge_v_to(er));
			}
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class EdgeElement : logical_edge, ILayoutEdgeEx
	{
		static readonly PropertyPath pp_bottom_center, pp_top_center;

		static EdgeElement()
		{
			pp_bottom_center = new PropertyPath("BottomCenter");
			pp_top_center = new PropertyPath("TopCenter");
		}

		public EdgeElement(graph_control_binder gcb, Eref er)
			: base(gcb, er)
		{
		}

		ILayoutVertexEx ILayoutEdgeEx.From { get { return From; } }
		IVertexEx IEdgeEx.From { get { return From; } }
		ILayoutVertexEx ILayoutEdgeEx.To { get { return To; } }
		IVertexEx IEdgeEx.To { get { return To; } }

		public EdgeContentMode EdgeContentMode { get; set; }

		Pen _stroke;
		public Pen LineStroke
		{
			[DebuggerStepThrough]
			get { return _stroke ?? gcb.edge_stroke; }
			set { _stroke = value; }
		}

		Brush _brush;
		public Brush LineFill
		{
			get { return _brush ?? Brushes.Black; }
			set { _brush = LineFill; }
		}

		Drawing make_line_drawing(LineGeometry lg)
		{
			var geom = ShowArrow ?
				new GeometryGroup
				{
					Children =
					{
						lg, 
						new PathGeometry { Figures = { LineWithText.ArrowheadFigure(lg.StartPoint, lg.EndPoint) } } 
					}
				} :
				(Geometry)lg;

			GeometryDrawing gd;
			if ((gd = new GeometryDrawing(LineFill, LineStroke, geom)).CanFreeze)
				gd.Freeze();
			return gd;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		LineGeometry ConnectingLine
		{
			get
			{
				var lg = new LineGeometry
				{
					StartPoint = From.BottomCenter,
					EndPoint = To.TopCenter,
				};
				//lg.TranslateToRenderOrigin(stroke_to_use);
				lg.Freeze();
				return lg;
			}
		}

		Drawing make_label_drawing(LineGeometry lg)
		{
			var text = this.TextLabel;

			if (String.IsNullOrEmpty(text))
				return new GeometryDrawing(null, null, new RectangleGeometry(util.zero_rect));

			var drw = gcb._make_text_drawing(text, Brushes.Firebrick, util.coord_origin, Brushes.White);

			var db = drw.Bounds;

			var r = new Rotation(lg);

			return new DrawingGroup
			{
				Children = { drw },
				Transform = new TransformGroup
				{
					Children = 
					{
						new TranslateTransform
						{
							X = r.Center.X - db.Left - (db.Width / 2),
							Y = r.Center.Y - db.Top - (db.Height / 2),
						},
						r.RotateTransform
					}
				}
			};
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Double ComputedDelta
		{
			get { return To.X - From.X; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Double Length
		{
			get { return (To.TopCenter - From.BottomCenter).Length; }
		}

		public override void reset_drawings()
		{
			var lg = ConnectingLine;
			this[ChildKey.EDGE_line] = make_line_drawing(lg);
			this[ChildKey.EDGE_label] = make_label_drawing(lg);
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class logical_vertex : logical_element, IVertexEx
	{
		protected logical_vertex(graph_control_binder gcb, Vref vr)
			: base(gcb, ChildKey.VERTEX_NumChildKeys)
		{
			this.vr = vr;
		}

		public readonly Vref vr;
		public override int Index { get { return vr; } }

		public bool LayoutExempt { get; set; }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsRoot { get { return g.vertex_is_root(vr); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsLeaf { get { return g.vertex_is_leaf(vr); } }

		public VertexElement[] ParentVerticies
		{
			get
			{
				var rge = g.vertex_in_edges(vr);
				if (rge.Length == 0)
					return Collection<VertexElement>.None;
				var arr = new VertexElement[rge.Length];
				for (int i = 0; i < arr.Length; i++)
					arr[i] = Graph.Edges[rge[i]].From;
				return arr;
			}
		}

		public EdgeElement MasterEdge { get { return IsRoot ? null : (EdgeElement)Graph.Edges[g.vertex_e_in(vr)]; } }

		public bool SetMasterEdge(logical_edge e_new)
		{
			return ((IAvm)g).edge_set_master(e_new.er);
		}

		IReadOnlyList<IEdgeEx> IVertexEx.InEdges { get { return InEdges; } }
		public IReadOnlyList<EdgeElement> InEdges
		{
			get { return IsRoot ? Collection<EdgeElement>.None : MasterEdge.PeersAndSelf; }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int OutEdgeCount
		{
			get
			{
				int c = g.vertex_c_out(vr);

				if (g is IGraphEx && ((IGraphExImpl)g).Verticies[vr].OutEdges.Count != c)
					throw new Exception();

				return c;
			}
		}

		IReadOnlyList<IEdgeEx> IVertexEx.OutEdges { get { return OutEdges; } }
		public IReadOnlyList<EdgeElement> OutEdges
		{
			get
			{
				int c;
				var _tmp = g.vertex_out_edges(vr);
				if ((c = _tmp.Length) == 0)
					return Collection<EdgeElement>.None;
				var arr = new EdgeElement[c];
				for (int i = 0; i < c; i++)
					arr[i] = Graph.Edges[_tmp[i]];
				return arr;
			}
		}

		[DebuggerDisplay("{Data,nq}")]
		public override String TextLabel
		{
			get
			{
				var s_label = g.vertex_string(vr);

				if (g is IGraphEx && ((IGraphExImpl)g).Verticies[vr].TextLabel != s_label)
					throw new Exception();

				return s_label;
			}
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public abstract class VertexLayoutElement : logical_vertex, ILayoutVertexEx
	{
		public VertexLayoutElement(graph_control_binder gcb, Vref vr)
			: base(gcb, vr)
		{
		}

		IReadOnlyList<ILayoutEdgeEx> ILayoutVertexEx.InEdges { get { return InEdges; } }
		IReadOnlyList<ILayoutEdgeEx> ILayoutVertexEx.OutEdges { get { return OutEdges; } }
		IReadOnlyList<ILogicalLayoutEdgeEx> ILogicalLayoutVertexEx.InEdges { get { return InEdges; } }
		IReadOnlyList<ILogicalLayoutEdgeEx> ILogicalLayoutVertexEx.OutEdges { get { return OutEdges; } }

		public VertexElement ve_left, ve_right;
		Double _delta;

		public Double delta
		{
			get { return _delta; }
			set
			{
				if (value != _delta)
				{
					var _tmp = _delta;
					_delta = value;
					var iter_deltas = gcb.iter_deltas;	// hack
					if (iter_deltas != null)
						iter_deltas[vr] += value - _tmp;
				}
			}
		}


		Double _y;
		public Double Y
		{
			get { return _y; }
			set { _y = value; }
		}

		public Double X
		{
			get
			{
				if (ve_left == null)
					return this.Bounds.Width / 2 + delta;
				else
					return ve_left.X + NodeSpacing + delta;
			}
		}

		public Point Location { get { return new Point(X, Y); } }

		public Rect LayoutRect { get { return new Rect(Location, Bounds.Size); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public LogicalPosition LogicalPosition
		{
			get
			{
				var lg = g as layout_graph;
				ILayoutVertexEx vx;
				return lg == null || lg.Verticies == null || (vx = lg.Verticies[vr] as ILayoutVertexEx) == null ?
					default(LogicalPosition) :
					vx.LogicalPosition;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool Leftmost { get { return ve_left == null; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool Rightmost { get { return ve_right == null; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Double ComputedLeft { get { return X - this.Bounds.Width / 2; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Double ComputedRight { get { return X + this.Bounds.Width / 2; } }

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public Point ComputedTopCenter { get { return new Point(X, Y); } }

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		//public Point ComputedBottomCenter { get { return new Point(X, Y + this.Bounds.Height); } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool WantLeft { get { return ComputeEnergy() < 0; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool WantRight { get { return ComputeEnergy() > 0; } }

		public Double LeftNodeDistance
		{
			get { return ve_left == null ? 0 : this.X - ve_left.X; }
		}
		public Double RightNodeDistance
		{
			get { return ve_right == null ? 0 : ve_right.X - this.X; }
		}

		public Double LeftPad
		{
			get { return ve_left == null ? ComputedLeft : LeftNodeDistance - graph_defaults.NodeMinWidth; }
		}
		public Double RightPad
		{
			get { return ve_right == null ? 0 : ve_right.LeftPad; }
		}

		public Double ComputeEnergy()
		{
			Double EE = 0, LE = 0;

			foreach (EdgeElement e in InEdges)
				EE -= e.ComputedDelta;

			foreach (EdgeElement e in OutEdges)
				EE += e.ComputedDelta;

			if (ve_left != null)
				LE -= LeftNodeDistance - NodeSpacing;

			if (ve_right != null)
				LE += RightNodeDistance - NodeSpacing;

			return ((EE * EdgeElasticity) + (LE * LevelElasticity)) / 2;
		}

		public override String ToString()
		{
			String s = String.Empty;
			layout_graph lg;

			if ((lg = g as layout_graph) != null)
			{
				var log_pos = this.LogicalPosition;

				s = String.Format("{0,5} {1,-6} ‹{2} {3,3}› ",
					((int)vr).SQRB(),
					"(" + log_pos.Row + "." + log_pos.Column + ")",
					log_pos.Row,
					lg.layout_cur.ix_logical(log_pos));

				s += String.Format(" x1:{0,6:N1} x2:{1,6:N1} {2}",
					ComputedLeft,
					ComputedRight,
					s);
			}

			s += this.TextLabel;

			s += IsRoot ? " (⊤)" : IsLeaf ? " (⊥)" : String.Empty;

			return s;
		}
	};

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	[DebuggerDisplay("{ToString(),nq}")]
	public sealed class VertexElement : VertexLayoutElement
	{
		public VertexElement(graph_control_binder gcb, Vref vr)
			: base(gcb, vr)
		{
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Pen node_stroke { [DebuggerStepThrough] get { return gcb.node_stroke; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Brush node_fill { [DebuggerStepThrough] get { return gcb.node_fill; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Brush leaf_node_fill { [DebuggerStepThrough] get { return gcb.leaf_node_fill; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Brush root_node_fill { [DebuggerStepThrough] get { return gcb.root_node_fill; } }

		Drawing GetBackgroundDrawing(Size sz, out Double x_adj)
		{
			if ((x_adj = graph_defaults.NodeMinWidth - sz.Width) >= 0.0)
			{
				sz.Width = graph_defaults.NodeMinWidth;
				x_adj /= 2.0;
			}
			else
			{
				x_adj = 0.0;
				sz.Width = Math.Round(sz.Width, 1);
			}
			sz.Width += graph_defaults.NodeInnerMargin * 2;

			sz.Height = Math.Max(sz.Height, Math.Max(gcb.line_spacing, graph_defaults.NodeMinHeight)) + graph_defaults.NodeInnerMargin * 2;

			var cp = new drawing_cache.rect_cache_key
			{
				brush = IsLeaf ? leaf_node_fill : IsRoot ? root_node_fill : node_fill,
				sz = sz,
				stroke = node_stroke,
				corner_radius = graph_defaults.NodeCornerRounding,
			};
			return gcb.get_rect_drawing(ref cp);
		}

		public override void reset_drawings()
		{
			//var lcc = ((graph.dg)graph).vertex_in_edge_count(vr) + ((graph.dg)graph).vertex_c_out(vr);
			//var text = ((layout_graph)graph).vertex_edges_can_cross(vr) ? "*" + lcc + "*" : lcc.ToString();

			var text = base.TextLabel;
			//var text = ((int)vr).ToString();
			//var text = vrix.ix_phys.ToString();
			//var text = String.Format("{0}-{1}", ((graph.dg)graph).vertex_in_edge_count(vr), ((graph.dg)graph).vertex_c_out(vr));

			Drawing _new;
			if (String.IsNullOrEmpty(text))
				text = "blarg";

			var lg = g as layout_graph;
			if (lg != null && lg.is_layout_proxy_vref(vr))
			{
#if true
				ClearDrawing(ChildKey.VERTEX_text);
				ClearDrawing(ChildKey.VERTEX_background);
				return;
#else
				_new = ctrl._make_text_drawing(text, Brushes.Black, new Point(31, 3), null);
				this[ChildKey.VERTEX_background] = new GeometryDrawing(
					null,
					node_stroke,
					new RectangleGeometry(new Rect(
								0,
								0,
								graph_defaults.NodeMinWidth + graph_defaults.NodeInnerMargin * 2,
								graph_defaults.NodeMinHeight + graph_defaults.NodeInnerMargin * 2)));
#endif
			}
			else
			{
				var cp = new drawing_cache.text_cache_key
				{
					text = text,
					brush = gcb.text_color,
					bg = gcb.text_background,
					pt = new Point(graph_defaults.NodeInnerMargin, graph_defaults.NodeInnerMargin),
				};

				_new = gcb.get_text_drawing(ref cp);

				if (this[ChildKey.VERTEX_text].Bounds != _new.Bounds)
				{
					Double x_adj;
					this[ChildKey.VERTEX_background] = GetBackgroundDrawing(_new.Bounds.Size, out x_adj);
					if (x_adj > 0.0)
						_new = new DrawingGroup
						{
							Children = { _new },
							Transform = new TranslateTransform(x_adj, 0.0)
						};
				}
			}
			this[ChildKey.VERTEX_text] = _new;
		}
	};


	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public sealed class MasterElement : keyed_element
	{
		public MasterElement(graph_control_binder gcb, out EdgeElement[] _rgee, out VertexElement[] _rgve)
			: base(gcb, ChildKey.ROOT_NumChildKeys)
		{
			/// create verticies before edges
			this[ChildKey.ROOT_verticies] = sub_group(g.VertexAlloc(), out _rgve, f1);
			this[ChildKey.ROOT_edges] = sub_group(g.EdgeAlloc(), out _rgee, f2);

			//var sg = g.SourceGraph as IGraphRaw;
			//var _V = sg != null ? sg._V_Raw : null;
			foreach (var el in _rgve)
			{
				//if (_V != null)
				//	el._get_drawing().SetValue(FrameworkContentElement.DataContextProperty, _V[el.vr]);
				el.reset_drawings();
			}
			foreach (var el in _rgee)
				el.reset_drawings();
		}

		DrawingGroup sub_group<T>(int c, out T[] rg, Func<MasterElement, int, T> _fnew)
			where T : keyed_element
		{
			rg = new T[c];
			var dxg = new DrawingGroup();
			var dc = dxg.Children;
			for (int i = 0; i < c; i++)
			{
				T t = _fnew(this, i);
				if (t != null)
					dc.Add((rg[i] = t)._get_drawing());
			}
			return dxg;
		}

		static VertexElement f1(MasterElement re, int v) { return re._f1(v); }
		static EdgeElement f2(MasterElement re, int e) { return re._f2(e); }

		VertexElement _f1(int v)
		{
			var vr = new Vref(v);
			return g.vertex_is_valid(vr) ? new VertexElement(gcb, new Vref(v)) : default(VertexElement);
		}
		EdgeElement _f2(int e)
		{
			var er = new Eref(e);
			return g.edge_is_valid(er) ? new EdgeElement(gcb, er) : default(EdgeElement);
		}
	};
}
