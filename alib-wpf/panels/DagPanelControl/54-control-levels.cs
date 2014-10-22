using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using alib.Graph;
using alib.Array;
using alib.Debugging;
using alib.Enumerable;

namespace alib.Wpf
{
	using Math = System.Math;
	using Array = System.Array;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class LevelInfo : IGraphExWpfLayoutLevel, IReadOnlyList<layout_vertex_base>
	{
		public LevelInfo(DagPanelControl g, int i_level, ILogicalLayoutVertexEx[] rg_src)
		{
			(this.g = g).levels[this.i_level = i_level] = this;

			if (rg_src.Length == 0)
				return;

			if ((this.rg = rg_src as layout_vertex_base[]) == null)
			{
				this.rg = new layout_vertex_base[rg_src.Length];
				if (rg_src[0] is layout_vertex_base)
					Array.Copy(rg_src, this.rg, rg_src.Length);
				else
				{
					throw not.tested;
					//for (int i = 0; i < Count; i++)
					//{
					//	var el = DagPanelControl.create_vertex_element(g, rg_src[i]);
					//	this.rg[i] = new LayoutVertexEx((WpfGraphAdapter)g.GraphInstance, el, i);
					//	WpfGraphAdapter.SetLV(
					//}
				}
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly DagPanelControl g;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		RuntimeLayoutData rld;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public RuntimeLayoutData RuntimeData
		{
			get
			{
				var _tmp = this.rld;
				if (_tmp == null || !_tmp.IsCurrent)
					this.rld = _tmp = new RuntimeLayoutData(this);
				return _tmp;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public IGraphExWpfLayoutProvider GraphInstance { get { return g; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IGraphEx IGraphExProxy.GraphInstance { get { return g.GraphInstance; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public readonly int i_level;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int LevelNum { get { return i_level; } }

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		readonly layout_vertex_base[] rg;

		public layout_vertex_base this[int index] { get { return rg[index]; } }

		public int Count { get { return rg.Length; } }

		ILogicalLayoutVertexEx IReadOnlyList<ILogicalLayoutVertexEx>.this[int index] { get { return rg[index]; } }
		ILayoutVertexEx IReadOnlyList<ILayoutVertexEx>.this[int index] { get { return rg[index]; } }

		public IEnumerator<layout_vertex_base> GetEnumerator() { return rg.Enumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return rg.Enumerator(); }
		IEnumerator<layout_vertex_base> IEnumerable<layout_vertex_base>.GetEnumerator() { return rg.Enumerator(); }
		IEnumerator<ILogicalLayoutVertexEx> IEnumerable<ILogicalLayoutVertexEx>.GetEnumerator() { return rg.Enumerator(); }
		IEnumerator<ILayoutVertexEx> IEnumerable<ILayoutVertexEx>.GetEnumerator() { return rg.Enumerator(); }

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public class RuntimeLayoutData
		{
			public RuntimeLayoutData(LevelInfo level)
			{
				this.level = level;
				this.gen = level.g.layout_gen;

				this._c_proxies = -1;
				this._elem_tot = /*this._elem_reserve =*/ Double.NaN;
				this._elem_max = this._elem_avg = Size.Empty;
			}

			readonly int gen;
			public bool IsCurrent { get { return gen == level.g.layout_gen; } }

			readonly LevelInfo level;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			DagPanelControl g { get { return level.g; } }

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Double TotalWidth
			{
				get { return ElementWidthTotal + HorizontalPadTotal; }
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Double[] rgpad;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Double[] DesiredPadding
			{
				get
				{
					Debug.Assert(IsCurrent);
					if (rgpad == null)
						compute_elements();
					return rgpad;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Double HorizontalPadTotal
			{
				get
				{
					var _tmp = DesiredPadding;
					var d = 0.0;
					for (int i = 1; i < _tmp.Length - 1; i++)
						d += _tmp[i];
					return d;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Double _elem_tot;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Double ElementWidthTotal
			{
				get
				{
					Debug.Assert(IsCurrent);
					if (Double.IsNaN(_elem_tot))
						compute_elements();
					return _elem_tot;
				}
			}

			//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			//Double _elem_reserve;
			//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			//public Double ElementReserve
			//{
			//	get
			//	{
			//		Debug.Assert(IsCurrent);
			//		if (Double.IsNaN(_elem_reserve))
			//			compute_elements();
			//		return _elem_reserve;
			//	}
			//}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Size _elem_max;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Size ElementMax
			{
				get
				{
					Debug.Assert(IsCurrent);
					if (_elem_max.IsEmpty)
						compute_elements();
					return _elem_max;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Size _elem_avg;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Size ElementAverage
			{
				get
				{
					Debug.Assert(IsCurrent);
					if (_elem_avg.IsEmpty)
						compute_elements();
					return _elem_avg;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			int _c_proxies;
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int ProxyCount
			{
				get
				{
					Debug.Assert(IsCurrent);
					if (_c_proxies == -1)
						compute_elements();
					return _c_proxies;
				}
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public int NonProxyCount { get { return level.Count - ProxyCount; } }

			void compute_elements()
			{
				int c = level.Count, i = 0;
				Double h = 0;

				this._elem_max = util.zero_size;
				this._elem_tot = 0;
				this._c_proxies = 0;
				this.rgpad = new Double[c + 1];

				layout_vertex_base vx, vx_prev = null;

				for (i = 0; i < c; i++)
				{
					var sz = g.swap((vx = (layout_vertex_base)level[i]).Size);

					if (i == 0)
						rgpad[i] = g.swap(vx.Padding).Left;
					else
					{
						var p1 = g.swap(vx_prev.Padding).Right;
						var p2 = g.swap(vx.Padding).Left;
						rgpad[i] = ((vx_prev is LayoutProxyVertex) == (vx is LayoutProxyVertex)) == (p1 > p2) ?
											p1 : p2;
					}

					if (vx is LayoutProxyVertex)
					{
						Debug.Assert(sz.IsZero());
						_c_proxies++;
					}
					else if (!sz.IsZero())
					{
						this._elem_tot += sz.Width;
						h += sz.Height;
						util.Maximize(ref this._elem_max, sz);
					}

					vx_prev = vx;
				}
				rgpad[i] = g.swap(vx_prev.Padding).Right;
				this._elem_max = g.swap(_elem_max);
				this._elem_avg = g.swap(new Size(_elem_tot / c, h / c));
			}
		};
	};
}