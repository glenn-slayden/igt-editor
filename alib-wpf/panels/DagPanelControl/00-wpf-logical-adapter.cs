using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using alib.Array;
using alib.Collections;
using alib.Debugging;
using alib.Graph;
using alib.Enumerable;

namespace alib.Wpf
{
	using Array = System.Array;
	using arr = alib.Array.arr;
	using String = System.String;

	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <remarks>
	/// note: this is not a DependencyObject and doesn't need to be in the current thinking in which this is
	/// simply a FrameworkElement->IGraph adapter
	/// </remarks>
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public class WpfGraphAdapter : IGraphExLayoutProvider
	{
		public static readonly DependencyProperty FromProperty;
		public static readonly DependencyProperty ToProperty;
		public static readonly DependencyProperty EdgeDirectionProperty;
		public static readonly DependencyProperty TextLabelProperty;
		public static readonly DependencyProperty IgnoreForLayoutProperty;

		static WpfGraphAdapter()
		{
			FromProperty = DependencyProperty.RegisterAttached(
								"From",
								typeof(UIElement),
								typeof(WpfGraphAdapter),
								new FrameworkPropertyMetadata(
									default(UIElement),
				//FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure,
									0
				/*,from_changed*/));

			ToProperty = DependencyProperty.RegisterAttached(
								"To",
								typeof(UIElement),
								typeof(WpfGraphAdapter),
								new FrameworkPropertyMetadata(
									default(UIElement),
				//FrameworkPropertyMetadataOptions.AffectsParentArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure
									0
				/*,to_changed*/));

			TextLabelProperty = DependencyProperty.RegisterAttached(
								"TextLabel",
								typeof(String),
								typeof(WpfGraphAdapter),
								new FrameworkPropertyMetadata(
									default(String),
				//FrameworkPropertyMetadataOptions.AffectsParentArrange
									0
									));

			EdgeDirectionProperty = DependencyProperty.RegisterAttached(
								"EdgeDirection",
								typeof(EdgeDirection),
								typeof(WpfGraphAdapter),
								new FrameworkPropertyMetadata(
									EdgeDirection.Normal,
				//FrameworkPropertyMetadataOptions.AffectsParentArrange
									0
									));

			IgnoreForLayoutProperty = DependencyProperty.RegisterAttached(
					"IgnoreForLayout",
					typeof(bool),
					typeof(WpfGraphAdapter),
					new FrameworkPropertyMetadata(
						false,
				//FrameworkPropertyMetadataOptions.AffectsParentArrange
						0
						));


			_lvProperty = DependencyProperty.RegisterAttached(
								"_lv",
								typeof(LayoutVertexEx),
								typeof(WpfGraphAdapter));
		}

		public static UIElement GetFrom(UIElement edge)
		{
			return edge == null ? null : (UIElement)edge.GetValue(FromProperty);
		}
		public static void SetFrom(UIElement edge, UIElement v_from)
		{
			edge.SetValue(FromProperty, v_from);
		}

		public static UIElement GetTo(UIElement edge)
		{
			return edge == null ? null : (UIElement)edge.GetValue(ToProperty);
		}
		public static void SetTo(UIElement edge, UIElement v_to)
		{
			edge.SetValue(ToProperty, v_to);
		}

		public static String GetTextLabel(UIElement el)
		{
			return el == null ? null : (String)el.GetValue(TextLabelProperty);
		}
		public static void SetTextLabel(UIElement el, String text)
		{
			el.SetValue(TextLabelProperty, text);
		}

		public static EdgeDirection GetEdgeDirection(UIElement edge)
		{
			return (EdgeDirection)edge.GetValue(EdgeDirectionProperty);
		}
		public static void SetEdgeDirection(UIElement edge, EdgeDirection dir)
		{
			edge.SetValue(EdgeDirectionProperty, dir);
		}

		public static bool GetIgnoreForLayout(UIElement edge)
		{
			return (bool)edge.GetValue(IgnoreForLayoutProperty);
		}
		public static void SetIgnoreForLayout(UIElement edge, bool f_ignore)
		{
			edge.SetValue(IgnoreForLayoutProperty, f_ignore);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		internal static readonly DependencyProperty _lvProperty;

		static LayoutVertexEx LvTo(UIElement el) { return GetLv(GetTo(el)); }
		static LayoutVertexEx LvFrom(UIElement el) { return GetLv(GetFrom(el)); }
		static LayoutVertexEx GetLv(UIElement uie)
		{
			return uie == null ? null : (LayoutVertexEx)uie.GetValue(_lvProperty);
		}
		public static void SetLv(UIElement uie, LayoutVertexEx vx)
		{
			uie.SetValue(_lvProperty, vx);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


		public int VertexCount { get { return VV.Length; } }
		public layout_vertex_base[] VV;
		public IReadOnlyList<layout_vertex_base> Verticies { get { return VV; } }
		IReadOnlyList<ILogicalLayoutVertexEx> IGraphExLayoutProvider.Verticies { get { return VV; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Verticies { get { return VV; } }


		public int EdgeCount { get { return EE.Length; } }
		public LayoutEdgeEx[] EE;
		public IReadOnlyList<LayoutEdgeEx> Edges { get { return EE; } }
		IReadOnlyList<ILogicalLayoutEdgeEx> IGraphExLayoutProvider.Edges { get { return EE; } }
		IReadOnlyList<IEdgeEx> IGraphExImpl.Edges { get { return EE; } }

		public IGraphCommon SourceGraph { get { return default(IGraph); } }

		public IGraphExLayoutProvider owner;
		public IGraphEx GraphInstance { get { return owner; } }


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public WpfGraphAdapter(IGraphExLayoutProvider owner, UIElementCollection children)
		{
			this.owner = owner;

			RefList<UIElement> edges;

			this.VV = load_verticies(children, out edges);

			Debug.Assert(edges.IsDistinct());

			this.EE = gather_edges(edges);

			recalculate_levels();

			insert_edge_proxies();

			if (VV.Length == 0)
			{
				this.levels = Collection<LogLevelInfo>.None;
				this.layout = new _logical_layout(this);
				return;
			}

			setup_levels();

			this.layout = find_layouts().First();
			//this.layout = new _logical_layout(this);

			Nop.X();
		}

		public _logical_layout layout;
		public IGraphExLayout ActiveLayout { get { return layout; } }


		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		LayoutVertexEx[] load_verticies(UIElementCollection children, out RefList<UIElement> edges_out)
		{
			var vv = new LayoutVertexEx[children.Count];
			edges_out = new RefList<UIElement>(vv.Length);

			int i, ix;
			for (ix = i = 0; i < vv.Length; i++)
			{
				UIElement el = children[i], vfrom, vto;

				el.ClearValue(WpfGraphAdapter._lvProperty);

				vfrom = GetFrom(el);
				vto = GetTo(el);
				if (vfrom == null && vto == null)
				{
					//if (!el.IsVisible)
					//{
					//	Debug.Print("skipping vertex {0} because it has IsVisible=False", el);
					//}
					//else
					{
						if (!el.IsMeasureValid)
							el.Measure(util.infinite_size);

						if (el.DesiredSize.IsZero())
						{
							Debug.Print("skipping vertex {0} because its 'DesiredSize' is zero", el);
						}
						else
						{
							SetLv(el, vv[ix] = new LayoutVertexEx(this, el, ix));
							ix++;
						}
					}
				}
				else if (vfrom != null && vto != null && vfrom != vto)
				{
					edges_out.Add(el);
				}
			}
			return arr.Resize(vv, ix);
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		LayoutEdgeEx[] gather_edges(RefList<UIElement> ui_edges)
		{
			var rge = new RefList<LayoutEdgeEx>(ui_edges.Count);

			LayoutVertexEx lv_from, lv_to;
			foreach (var uie in ui_edges)
				if ((lv_from = LvFrom(uie)) != null && (lv_to = LvTo(uie)) != null)
					rge.Add(new LayoutEdgeEx(this, uie, rge.Count, lv_from, lv_to));

			return rge.GetTrimmed();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		void insert_edge_proxies()
		{
			LayoutVertexEx vfrom, vto;
			LayoutEdgeEx ex;

			for (int ix = 0, delta; ix < EE.Length; ix++)
			{
				if ((ex = EE[ix]).GetType() == typeof(LayoutEdgeEx))
				{
					vto = ex.To;
					vfrom = ex.From;

					if ((delta = vto.Row - vfrom.Row) > 1)
						EE[ix] = load_proxies(ex, vfrom, vto, delta);
					else if (vfrom.out_edges.Exclude(ex).Any(oe => oe.To == vto))
						EE[ix] = new LayoutMultiEdgeEx(ex);
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		CompoundLayoutEdgeEx load_proxies(LayoutEdgeEx ex, LayoutVertexEx vfrom, LayoutVertexEx vto, int delta)
		{
			var proxies = new layout_vertex_base[delta + 1];

			var ce = new CompoundLayoutEdgeEx(ex, proxies);

			int i, row;
			for (i = 1, row = (proxies[0] = vfrom).Row; i < delta; i++)
				arr.Append(ref VV, proxies[i] = new LayoutProxyVertex(ce, VV.Length, ++row));

			proxies[i] = vto;

			return ce;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		int c_levels;
		LayoutVertexEx[] _roots;
		public IReadOnlyList<ILogicalLayoutVertexEx> Roots { get { return _roots; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Roots { get { return _roots; } }

		LayoutVertexEx[] _leaves;
		public IReadOnlyList<ILogicalLayoutVertexEx> Leaves { get { return _leaves; } }
		IReadOnlyList<IVertexEx> IGraphExImpl.Leaves { get { return _leaves; } }

		void recalculate_levels()
		{
			foreach (var vx in VV)
				vx.Row = -1;

			this.c_levels = -1;
			this._roots = Collection<LayoutVertexEx>.None;
			this._leaves = Collection<LayoutVertexEx>.None;

			foreach (LayoutVertexEx vx in VV)
				if (vx.Row == -1 && vx.OutEdges.Count == 0)
				{
					arr.Append(ref _leaves, vx);
					get_level(vx);
				}

			int i, lev;
			if (owner.ExtendLeafVerticies)
			{
				for (i = 0; i < _leaves.Length; i++)
					_leaves[i].Row = c_levels;
			}

			if (owner.CompactRootVerticies)
			{
				for (i = 0; i < _roots.Length; )
				{
					lev = _roots[i].out_edges.Min(_e => _e.To.Row);
					if (lev > 1)
					{
						_roots[i].Row = lev - 1;
						arr.RemoveAt(ref _roots, i);
					}
					else
						i++;
				}
			}

			c_levels++;
		}

		int get_level(LayoutVertexEx vx)
		{
			int pl, lev;
			if ((lev = vx.Row) == int.MinValue)
				return -1;
			if (lev == -1)
			{
				if (vx.InEdges.Count == 0)
				{
					arr.Append(ref _roots, vx);
				}
				else
				{
					vx.Row = int.MinValue;
					foreach (var f in vx.InEdges)
						if ((pl = get_level(f.From)) > lev)
							lev = pl;
				}
				vx.Row = ++lev;
				if (lev > c_levels)
					c_levels = lev;
			}
			return lev;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public LogLevelInfo[] levels;

		public void setup_levels()
		{
			this.levels = VV.GroupBy(vx => vx.Row)
				.OrderBy(g => g.Key)
				.Select(g => new LogLevelInfo(this, g.Key, g.ToArray()))
				.ToArray();

			for (int i = 0; i < levels.Length; i++)
				levels[i].post_init();
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		int depth_limit;
		int min_seen;
		//int __last_update;
		int c_iter;
		int min_all;
		int max_iter;

		_LayoutEvaluator evaluator;

		public IEnumerable<_logical_layout> find_layouts()
		{
			this.max_iter = 10000;
			this.min_all = int.MaxValue;
			this.min_seen = int.MaxValue;
			this.depth_limit = levels.Length * levels.Length;
			//this.depth_limit = (int)System.Math.Pow(LevelCount, 1.5);

			IEnumerable<_logical_layout> results;
			if (max_iter == 0)
			{
				results = new[] { new _logical_layout(this) };
			}
			else
			{
				this.evaluator = new _LayoutEvaluator(levels);

				var initial_layout = new _eval_layout(this);

				add_hyp(initial_layout, levels.Length / 2);

				eval_loop();

				results = evaluator.Mins.Distinct(evaluator);
			}

			return results;
		}

		void eval_loop()
		{
			int c;

			while ((c = evaluator.Count) > 0 && min_all > 0 && evaluator.QueueCount > 0 && c < max_iter)
			{
				//var d_mem = alib.Memory.Kernel32.MemoryStatus.ullAvailPhys / alib.Int._int_ext.GigabyteD;
				//if (d_mem < 1.6)
				//	break;

				var hyp = evaluator.Dequeue();

				var c_cur = hyp.total_crossings();
				c_iter++;

				start_hyp(hyp, hyp.i_active_level);
			}
		}

		bool add_hyp(_eval_layout hyp, int i_active_level)
		{
			hyp.i_active_level = i_active_level;

			if (!evaluator.Add(hyp))
				return false;

			int c;
			if ((c = hyp.total_crossings()) < min_all)
				min_all = c;
			return true;
		}

		void start_hyp(_eval_layout hyp, int i)
		{
			_eval_layout copy;

			copy = new _eval_layout(hyp);

			if (hyp.total_crossings() <= min_seen + 5)
			{
				bool f_any = false;
				for (int j = 1; j < levels.Length; j++)
					f_any |= copy.set_leaf_positions_from_above(j);

				if (f_any)
				{
					add_hyp(copy, i);
					copy = new _eval_layout(hyp);
				}
			}

			if (copy.set_medians_from_above(i + 1))
			{
				add_hyp(copy, i + 1);
				copy = new _eval_layout(hyp);
			}

			if (copy.set_medians_from_below(i - 1))
			{
				add_hyp(copy, i - 1);
				copy = new _eval_layout(hyp);
			}

			if (copy.set_medians_from_above(i - 1))
			{
				add_hyp(copy, i - 1);
				copy = new _eval_layout(hyp);
			}

			if (copy.set_medians_from_below(i + 1))
			{
				add_hyp(copy, i + 1);
				copy = new _eval_layout(hyp);
			}
		}

		public LayoutDirection LayoutDirection { get { return owner.LayoutDirection; } }

		public bool ExtendLeafVerticies { get { return owner.ExtendLeafVerticies; } }

		public bool CompactRootVerticies { get { return owner.CompactRootVerticies; } }

		public IGraphExLayoutLevel this[int index] { get { return levels[index]; } }

		public int Count { get { return levels.Length; } }

		public IEnumerator<IGraphExLayoutLevel> GetEnumerator() { return levels.Enumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	};
}
