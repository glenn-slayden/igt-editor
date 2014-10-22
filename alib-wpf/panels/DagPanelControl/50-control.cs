using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

using alib.Graph;
using alib.Math;
using alib.Array;
using alib.Debugging;
using alib.Enumerable;
using alib.Collections;

namespace alib.Wpf
{
	using Math = System.Math;
	using math = alib.Math.math;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class DagPanelControl : Panel, IGraphExWpfLayoutProvider
	{
		Rect r_all;

		IGraphExLayoutProvider g_cur;

		public int layout_gen;

		internal LevelInfo[] levels;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void graph_change()
		{
			Children.Clear();
			this.g_cur = null;
			this.r_all = util.zero_rect;

			var gg = this.IGraphEx;
			if (gg != null)
			{
				if (!this.HasLocalValue(TextBlock.FontSizeProperty))
					TextBlock.SetFontSize(this, 13);
				var vertx = new UIElement[gg.VertexCount];

				for (int j = 0; j < gg.EdgeCount; j++)
				{
					var ge = gg.Edges[j];
					IVertexEx vx;
					UIElement uiv_from, uiv_to;
					if ((uiv_from = vertx[(vx = ge.From).Index]) == null)
						this.Children.Add(vertx[vx.Index] = uiv_from = create_vertex_element(this, vx));
					if ((uiv_to = vertx[(vx = ge.To).Index]) == null)
						this.Children.Add(vertx[vx.Index] = uiv_to = create_vertex_element(this, vx));

					this.Children.Add(create_edge_element(ge, uiv_from, uiv_to));
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override Size MeasureOverride(Size _)
		{
			bool f_ok = true;
			foreach (UIElement el in InternalChildren)
			{
				if (!el.IsMeasureValid)
				{
					el.Measure(_);
					f_ok = false;
				}
			}
			if (f_ok)
				return RuntimeSizeWithPadding;

			layout_gen++;

			this.r_all = util.zero_rect;
			if (g_cur == null && InternalChildren.Count == 0)
			{
				this.g_cur = empty_layout_provider.Instance;
				return new Size
				{
					Width = math.Max(Padding.Left, Padding.Right, 1),
					Height = math.Max(Padding.Top, Padding.Bottom, 1),
				};
			}

			//var sw = Stopwatch.StartNew();
			this.g_cur = new WpfGraphAdapter(this, InternalChildren);
			//Debug.Print("graph layout complete: {0:#,###}ms.", sw.ElapsedMilliseconds);

			if (g_cur.Verticies.Count > 0)
			{
				//foreach (layout_vertex_base v in g_cur.Verticies)
				//	v.Location = util.point_NaN;

				var _tmp = g_cur.Verticies
								.GroupBy(vx => vx.LogicalPosition.Row)
								.OrderBy(g => g.Key)
								.ToArray();

				this.levels = new LevelInfo[_tmp.Length];
				for (int i = 0; i < levels.Length; i++)
				{
					var g = _tmp[i];
					levels[i] = new LevelInfo(this, g.Key, g.OrderBy(_v => _v.LogicalPosition.Column).ToArray());
				}

				layout_verticies(levels);

				if (g_cur.Verticies.Cast<layout_vertex_base>().Any(vx => !vx.Location.IsFinite()))
					throw new Exception();

				//foreach (LayoutEdgeEx e in g_cur.Edges)
				//	e.ResetUI();

				if (EdgeContentMode != EdgeContentMode.None)
					layout_edges();
			}
			return RuntimeSizeWithPadding;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override Size ArrangeOverride(Size _)
		{
			UIElement uie;
			LayoutVertexEx vx;
			Rect r;
			bool __vs_design = DesignerProperties.GetIsInDesignMode(this);

			foreach (var v in g_cur.Verticies)
				if ((vx = v as LayoutVertexEx) != null && !(r = vx.LayoutRect).IsZeroSize())
					if (vx.Element.IsVisible || __vs_design)
						vx.Element.Arrange(coord_adjust(vx.LayoutRect));

			foreach (LayoutEdgeEx e in g_cur.Edges)
				if (e.geom == null && (uie = e.el) != null && uie.IsVisible)
					uie.Arrange(coord_adjust(e.LayoutRect));

			return RuntimeSizeWithPadding;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override void OnRender(DrawingContext dc)
		{
			Rect r_render;
			if ((r_render = new Rect(util.coord_origin, RenderSize)).IsZeroSize())
				return;

			render_background(dc, r_render);

			if (ShowWorkArea)
			{
				var rr = r_all;
				rr.Offset(Padding.Left - r_all.X, Padding.Top - r_all.Y);
				dc.DrawGeometry(null, _work_area_pen, new RectangleGeometry(rr));
			}

			var clip = !EdgeContentMode.HasFlag(EdgeContentMode.Text) ? null :
								new CombinedGeometry
								{
									GeometryCombineMode = GeometryCombineMode.Exclude,
									Geometry1 = new RectangleGeometry(r_render),
									Geometry2 = render_edge_labels(dc)
								};

			using (dc.PushSafe(clip))
				render_edges(dc);
		}
#if false
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override void ParentLayoutInvalidated(UIElement child)
		{
			base.ParentLayoutInvalidated(child);
			InvalidateVisual();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			if (this.EdgeTextStyle != null)
				return;
			this.EdgeTextStyle = new Style(typeof(TextBlock))
			{
				Setters =
				{
					new Setter(TextBlock.FontFamilyProperty,TextBlock.GetFontFamily(this)),
					new Setter(TextBlock.FontStretchProperty,TextBlock.GetFontStretch(this)),
					new Setter(TextBlock.FontWeightProperty,TextBlock.GetFontWeight(this)),
					new Setter(TextBlock.FontStyleProperty,TextBlock.GetFontStyle(this)),
					new Setter(TextBlock.FontSizeProperty,TextBlock.GetFontSize(this)),
					new Setter(TextBlock.ForegroundProperty,TextBlock.GetForeground(this)),
				}
			};
		}
#endif
		protected sealed override bool HasLogicalOrientation { get { return true; } }

		protected sealed override Orientation LogicalOrientation { get { return (Orientation)((int)LayoutDirection & 1); } }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		IGraphExLayoutLevel IReadOnlyList<IGraphExLayoutLevel>.this[int index] { get { return levels[index]; } }
		IGraphExWpfLayoutLevel IReadOnlyList<IGraphExWpfLayoutLevel>.this[int index] { get { return levels[index]; } }

		int IReadOnlyCollection<IGraphExLayoutLevel>.Count { get { return levels.Length; } }
		int IReadOnlyCollection<IGraphExWpfLayoutLevel>.Count { get { return levels.Length; } }

		IEnumerator<IGraphExLayoutLevel> IEnumerable<IGraphExLayoutLevel>.GetEnumerator() { return levels.Enumerator(); }
		IEnumerator<IGraphExWpfLayoutLevel> IEnumerable<IGraphExWpfLayoutLevel>.GetEnumerator() { return levels.Enumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return levels.Enumerator(); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IGraphEx GraphInstance { get { return this.g_cur; } }

		public IGraphCommon SourceGraph { get { return this.IGraphEx; } }

		public IReadOnlyList<ILogicalLayoutVertexEx> Roots { get { throw not.impl; } }
		public IReadOnlyList<ILogicalLayoutVertexEx> Leaves { get { throw not.impl; } }
		public IReadOnlyList<ILogicalLayoutVertexEx> Verticies { get { throw not.impl; } }
		public IReadOnlyList<ILogicalLayoutEdgeEx> Edges { get { throw not.impl; } }
		public IGraphExLayout ActiveLayout { get { throw not.impl; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Roots { get { throw not.impl; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Leaves { get { throw not.impl; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { throw not.impl; } }
		IReadOnlyList<IEdgeEx> IGraphExImpl.Edges { get { throw not.impl; } }
		public int EdgeCount { get { throw not.impl; } }
		public int VertexCount { get { throw not.impl; } }
	};
}
