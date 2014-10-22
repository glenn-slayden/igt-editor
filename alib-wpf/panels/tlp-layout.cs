//#define LEAF_HEIGHT_EXEMPT

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

using alib.Enumerable;
using alib.Collections;

namespace alib.Wpf
{
	using Math = System.Math;
	using String = System.String;
	using math = alib.Math.math;

	public partial class TreeLayoutPanel : Panel
	{
		class NodeLayoutInfo : IReadOnlyList<FrameworkElement>
		{
			public NodeLayoutInfo(TreeLayoutPanel tlp, FrameworkElement fe)
			{
				this.tlp = tlp;
				this.fe = fe;
				this.children = new List<FrameworkElement>();
				this.lstPosLBoundaryRelativeToRoot = new List<Double>();
				this.lstPosRBoundaryRelativeToRoot = new List<Double>();

				if (!fe.IsMeasureValid)
					fe.Measure(util.infinite_size);

				r_final = new Rect(util.coord_origin, fe.DesiredSize);
			}

			readonly TreeLayoutPanel tlp;
			readonly public FrameworkElement fe;

			readonly List<FrameworkElement> children;
			public NodeLayoutInfo nli_parent;

			public void Add(FrameworkElement child) { children.Add(child); }

			public FrameworkElement this[int index] { get { return children[index]; } }

			public int Count { get { return children.Count; } }

			public IEnumerator<FrameworkElement> GetEnumerator()
			{
				return fe.Visibility == Visibility.Collapsed ? Collection<FrameworkElement>.NoneEnumerator : children.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

			public Rect r_final;

			readonly List<Double> lstPosLBoundaryRelativeToRoot;
			readonly List<Double> lstPosRBoundaryRelativeToRoot;

			Double SubTreeWidth;
			public Double pxLeftPosRelativeToParent;
			public Double pxLeftPosRelativeToBoundingBox;
			public Double pxToLeftSibling;

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			Double CalcJustify(Double height, Double pxRowHeight)
			{
				switch (tlp.VerticalContentAlignment)
				{
					case VerticalAlignment.Top:
						return 0;

					case VerticalAlignment.Center:
						return (pxRowHeight - height) / 2;

					case VerticalAlignment.Bottom:
						return pxRowHeight - height;
				}
				throw new InvalidOperationException();
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			public Size DetermineFinalPositions(List<Double> layer_heights, int iLayer, Double pxFromTop, Double pxParentFromLeft)
			{
				var pxRowHeight = layer_heights[iLayer];
				r_final.X = pxLeftPosRelativeToParent + pxParentFromLeft;
				r_final.Y = pxFromTop + CalcJustify(fe.DesiredSize.Height, pxRowHeight);

				if (Count == 0 &&
					//!GetNodeLink(fe)
					nli_parent != null && nli_parent.All(x => !GetNodeLink(x))
					)
					r_final.Y -= Math.Max(tlp.VerticalBuffer - 10.0, 0.0);

				Double pxBottom = r_final.Y + fe.DesiredSize.Height;

				iLayer++;
				foreach (FrameworkElement tnCur in this)
				{
					var y = pxFromTop + pxRowHeight + tlp.VerticalBuffer;
					var b = tlp.nli_dict[tnCur].DetermineFinalPositions(layer_heights, iLayer, y, r_final.X).Height;
					math.Maximize(ref pxBottom, b);
				}
				return new Size(SubTreeWidth, pxBottom);
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			void RepositionSubtree(int ix, IReadOnlyList<FrameworkElement> tngSiblings, List<Double> lstLeftToBB, List<int> lsttnResponsible)
			{
				if (ix == 0)
				{
					foreach (Double pxRelativeToRoot in lstPosRBoundaryRelativeToRoot)
					{
						lstLeftToBB.Add(pxRelativeToRoot + pxLeftPosRelativeToBoundingBox);
						lsttnResponsible.Add(0);
					}
					return;
				}

				boundary_calc bc_max = MergeBoundaryEnums(lstLeftToBB, lstPosLBoundaryRelativeToRoot, lsttnResponsible)
										.ArgMax(bc => bc.delta);

				Double buf_x = bc_max.i_cur == 0 ? tlp.HorizontalBuffer : tlp.HorizontalBufferSubtree;

				FrameworkElement tnLeft = tngSiblings[ix - 1];

				pxToLeftSibling = bc_max.delta - lstLeftToBB[0] + tnLeft.DesiredSize.Width + buf_x;

				int i, cLevels = Math.Min(lstPosRBoundaryRelativeToRoot.Count, lstLeftToBB.Count);

				for (i = 0; i < cLevels; i++)
				{
					lstLeftToBB[i] = lstPosRBoundaryRelativeToRoot[i] + bc_max.delta + buf_x;
					lsttnResponsible[i] = ix;
				}
				for (i = lstLeftToBB.Count; i < lstPosRBoundaryRelativeToRoot.Count; i++)
				{
					lstLeftToBB.Add(lstPosRBoundaryRelativeToRoot[i] + bc_max.delta + buf_x);
					lsttnResponsible.Add(ix);
				}

				Double pxSlop = pxToLeftSibling - tnLeft.DesiredSize.Width - tlp.HorizontalBuffer;
				if (pxSlop > 0)
				{
					for (i = bc_max.i_resp + 1; i < ix; i++)
						tlp.nli_dict[tngSiblings[i]].pxToLeftSibling += pxSlop * (i - bc_max.i_resp) / (ix - bc_max.i_resp);

					pxToLeftSibling -= (ix - bc_max.i_resp - 1) * pxSlop / (ix - bc_max.i_resp);
				}
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			public void CalculateLayout(List<Double> layer_heights, int iLayer)
			{
				Double width = fe.DesiredSize.Width;

				if (Count == 0)
				{
					SubTreeWidth = width;
					lstPosLBoundaryRelativeToRoot.Add(0.0);
					lstPosRBoundaryRelativeToRoot.Add(width);
				}
				else
				{
					List<Double> lstLeftToBB = new List<Double>();
					List<int> lstResponsible = new List<int>();
					for (int i = 0; i < Count; i++)
					{
						var tn = this[i];
						var nli = tlp.nli_dict[tn];

						nli.CalculateLayout(layer_heights, iLayer + 1);
						nli.RepositionSubtree(i, this, lstLeftToBB, lstResponsible);
					}

					// If a subtree extends deeper than it's left neighbors then at that lower level it could potentially extend beyond those neighbors
					// on the left.  We have to check for this and make adjustements after the loop if it occurred.

					Double pxWidth = 0.0, pxUndercut = 0.0, x_cur = Double.NaN;

					foreach (FrameworkElement tn in this)
					{
						NodeLayoutInfo nlic = tlp.nli_dict[tn];
						if (Double.IsNaN(x_cur))
							x_cur = nlic.pxLeftPosRelativeToBoundingBox;
						x_cur += nlic.pxToLeftSibling;

						math.Maximize(ref pxUndercut, nlic.pxLeftPosRelativeToBoundingBox - x_cur);

						// pxWidth might already be wider than the current node's subtree if earlier nodes "undercut" on the
						// right hand side so we have to take the Max here...
						math.Maximize(ref pxWidth, x_cur + nlic.SubTreeWidth - nlic.pxLeftPosRelativeToBoundingBox);

						// After this next statement, the BoundingBox we're relative to is the one of our parent's subtree rather than
						// our own subtree (with the exception of undercut considerations)
						nlic.pxLeftPosRelativeToBoundingBox = x_cur;
					}

					if (pxUndercut > 0.0)
					{
						foreach (FrameworkElement tn in this)
							tlp.nli_dict[tn].pxLeftPosRelativeToBoundingBox += pxUndercut;

						pxWidth += pxUndercut;
					}

					// We are never narrower than our root node's width which we haven't taken into account yet so
					// we do that here.
					SubTreeWidth = Math.Max(width, pxWidth);

					// ...so that this centering may place the parent node negatively while the "width" is the width of
					// all the child nodes.

					// We should be centered between  the connection points of our children...
					FrameworkElement feL = this[0];
					FrameworkElement feR = this[this.Count - 1];
					Double pxLeftChild = tlp.nli_dict[feL].pxLeftPosRelativeToBoundingBox + feL.DesiredSize.Width / 2;
					Double pxRightChild = tlp.nli_dict[feR].pxLeftPosRelativeToBoundingBox + feR.DesiredSize.Width / 2;

					pxLeftPosRelativeToBoundingBox = (pxLeftChild + pxRightChild - width) / 2;

					// If the root node was wider than the subtree, then we'll have a negative position for it.  We need
					// to readjust things so that the left of the root node represents the left of the bounding box and
					// the child distances to the Bounding box need to be adjusted accordingly.
					if (pxLeftPosRelativeToBoundingBox < 0)
					{
						foreach (FrameworkElement tnChildCur in this)
							tlp.nli_dict[tnChildCur].pxLeftPosRelativeToBoundingBox -= pxLeftPosRelativeToBoundingBox;

						pxLeftPosRelativeToBoundingBox = 0;
					}

					foreach (FrameworkElement tn in this)
					{
						NodeLayoutInfo ltiCur = tlp.nli_dict[tn];
						ltiCur.pxLeftPosRelativeToParent = ltiCur.pxLeftPosRelativeToBoundingBox - pxLeftPosRelativeToBoundingBox;
					}

					lstPosLBoundaryRelativeToRoot.Add(0.0);
					lstPosRBoundaryRelativeToRoot.Add(width);

					DetermineBoundary(this, true, lstPosLBoundaryRelativeToRoot);
					DetermineBoundary(this.Reverse(), false, lstPosRBoundaryRelativeToRoot);
				}

				while (layer_heights.Count <= iLayer)
					layer_heights.Add(0.0);

				if (fe.DesiredSize.Height > layer_heights[iLayer]
#if LEAF_HEIGHT_EXEMPT
 && ni.m_children.Count > 0
#endif
)
				{
					layer_heights[iLayer] = fe.DesiredSize.Height;
				}
			}

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			void DetermineBoundary(IEnumerable<FrameworkElement> entn, bool fLeft, List<Double> lstPos)
			{
				int cLayersDeep = 1;
				foreach (FrameworkElement tnChild in entn)
				{
					NodeLayoutInfo ltiChild = tlp.nli_dict[tnChild];

					List<Double> lstPosCur = fLeft ?
												ltiChild.lstPosLBoundaryRelativeToRoot :
												ltiChild.lstPosRBoundaryRelativeToRoot;

					if (lstPosCur.Count >= lstPos.Count)
					{
						foreach (var e in lstPosCur.Skip(cLayersDeep - 1))
						{
							lstPos.Add(e + ltiChild.pxLeftPosRelativeToParent);
							cLayersDeep++;
						}
					}
				}
			}

			struct boundary_calc
			{
				public int i_cur;
				public Double delta;
				public int i_resp;
			};

			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///
			///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			static IEnumerable<boundary_calc> MergeBoundaryEnums(IEnumerable<Double> eL, IEnumerable<Double> eR, IEnumerable<int> eX)
			{
				return MergeBoundaryEnums(eL.GetEnumerator(), eR.GetEnumerator(), eX.GetEnumerator());
			}
			static IEnumerable<boundary_calc> MergeBoundaryEnums(IEnumerator<Double> enL, IEnumerator<Double> enR, IEnumerator<int> enX)
			{
				int i = 0;
				while (enL.MoveNext() && enR.MoveNext() && enX.MoveNext())
				{
					yield return new boundary_calc
					{
						i_cur = i++,
						delta = enL.Current - enR.Current,
						i_resp = enX.Current
					};
				}
			}
		};
	};
}