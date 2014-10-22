using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

using alib.Debugging;
using alib.Enumerable;
using alib.Graph;

namespace alib.Wpf
{
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	///
	///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	public partial class DagPanelControl
	{
		const FrameworkPropertyMetadataOptions MeasureArrangeRender = 0;//(FrameworkPropertyMetadataOptions)0x13;

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		///
		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		public static DependencyProperty OrientationProperty { get { return StackPanel.OrientationProperty; } }
		public static DependencyProperty PaddingProperty { get { return Control.PaddingProperty; } }
		public static DependencyProperty BorderThicknessProperty { get { return Border.BorderThicknessProperty; } }
		public static DependencyProperty BorderBrushProperty { get { return Border.BorderBrushProperty; } }
		public static DependencyProperty CornerRadiusProperty { get { return Border.CornerRadiusProperty; } }
		public static DependencyProperty StrokeProperty { get { return Shape.StrokeProperty; } }
		public static DependencyProperty StrokeThicknessProperty { get { return Shape.StrokeThicknessProperty; } }
		public static readonly DependencyProperty IGraphExProperty;
		public static readonly DependencyProperty EdgePaddingProperty;
		public static readonly DependencyProperty VertexPaddingProperty;
		public static readonly DependencyProperty VertexMinWidthProperty;
		public static readonly DependencyProperty VertexMinHeightProperty;
		public static readonly DependencyProperty LayoutDirectionProperty;
		public static readonly DependencyProperty EdgeContentModeProperty;
		public static readonly DependencyProperty EdgeContentOrientationProperty;
		public static readonly DependencyProperty ContentAlignmentProperty;
		public static readonly DependencyProperty ExtendLeafVerticiesProperty;
		public static readonly DependencyProperty CompactRootVerticiesProperty;
		public static readonly DependencyProperty ShowWorkAreaProperty;
		public static readonly DependencyProperty ShowLayoutProxyPointsProperty;
		public static readonly DependencyProperty ShowLayoutProxyOutlinesProperty;
		public static readonly DependencyProperty EdgeTextStyleProperty;
		public static readonly DependencyProperty SplineTensionProperty;

		static readonly DependencyPropertyKey graph_crossings_pk;
		public static DependencyProperty GraphCrossingsProperty { get { return graph_crossings_pk.DependencyProperty; } }

		static DagPanelControl()
		{
			BorderThicknessProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												new Thickness(0),
												MeasureArrangeRender,
												(o, e) => ((DagPanelControl)o)._brdr_pen = null));

			BorderBrushProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												default(Brush),
												MeasureArrangeRender,
												(o, e) => ((DagPanelControl)o)._brdr_pen = null));

			StrokeProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												Brushes.Black,
												0,//FrameworkPropertyMetadataOptions.AffectsRender,
												(o, e) => ((DagPanelControl)o)._pen = null));

			StrokeThicknessProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												1.0,
												0,//FrameworkPropertyMetadataOptions.AffectsRender,
												(o, e) => ((DagPanelControl)o)._pen = null));

			CornerRadiusProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												new CornerRadius(0.0),
												0//FrameworkPropertyMetadataOptions.AffectsRender
												));

			PaddingProperty.AddOwner(typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												new Thickness(10.0),
												MeasureArrangeRender));

			IGraphExProperty = DependencyProperty.Register(
											"IGraphEx",
											typeof(IGraphExImpl),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												default(IGraphExImpl),
												MeasureArrangeRender,
												(o, e) => ((DagPanelControl)o).graph_change()));

			EdgePaddingProperty = DependencyProperty.Register(
											"EdgePadding",
											typeof(Thickness),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												new Thickness(40.0),
												MeasureArrangeRender));

			VertexPaddingProperty = DependencyProperty.Register(
											"VertexPadding",
											typeof(Thickness),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												new Thickness(40, 60, 40, 60),
												MeasureArrangeRender));

			VertexMinWidthProperty = DependencyProperty.Register(
											"VertexMinWidth",
											typeof(Double),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												0.0,
												MeasureArrangeRender));

			VertexMinHeightProperty = DependencyProperty.Register(
											"VertexMinHeight",
											typeof(Double),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												0.0,
												MeasureArrangeRender));

			LayoutDirectionProperty = DependencyProperty.Register(
											"LayoutDirection",
											typeof(LayoutDirection),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												LayoutDirection.TopToBottom,
												MeasureArrangeRender));

			EdgeContentModeProperty = DependencyProperty.Register(
											"EdgeContentMode",
											typeof(EdgeContentMode),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												EdgeContentMode.Element,
												MeasureArrangeRender));

			EdgeContentOrientationProperty = DependencyProperty.Register(
											"EdgeContentOrientation",
											typeof(EdgeContentOrientation),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												EdgeContentOrientation.Rotated,
												MeasureArrangeRender));

			ContentAlignmentProperty = DependencyProperty.Register(
											"ContentAlignment",
											typeof(ContentAlignment),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												ContentAlignment.Center,
												MeasureArrangeRender));

			ExtendLeafVerticiesProperty = DependencyProperty.Register(
											"ExtendLeafVerticies",
											typeof(bool),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												false,
												MeasureArrangeRender,
												(o, e) => ((DagPanelControl)o).g_cur = null));

			CompactRootVerticiesProperty = DependencyProperty.Register(
											"CompactRootVerticies",
											typeof(bool),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												false,
												MeasureArrangeRender,
												(o, e) => ((DagPanelControl)o).g_cur = null));

			ShowWorkAreaProperty = DependencyProperty.Register(
											"ShowWorkArea",
											typeof(bool),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												false,
												0//FrameworkPropertyMetadataOptions.AffectsRender
												));

			ShowLayoutProxyPointsProperty = DependencyProperty.Register(
											"ShowLayoutProxyPoints",
											typeof(bool),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												false,
												0//FrameworkPropertyMetadataOptions.AffectsRender
												));

			ShowLayoutProxyOutlinesProperty = DependencyProperty.Register(
											"ShowLayoutProxyOutlines",
											typeof(bool),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												false,
												0//FrameworkPropertyMetadataOptions.AffectsRender
												));

			EdgeTextStyleProperty = DependencyProperty.Register(
											"EdgeTextStyle",
											typeof(Style),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												default(Style),
												MeasureArrangeRender));

			graph_crossings_pk = DependencyProperty.RegisterReadOnly(
											"GraphCrossings",
											typeof(int),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(0));

			SplineTensionProperty = DependencyProperty.Register(
											"SplineTension",
											typeof(Double),
											typeof(DagPanelControl),
											new FrameworkPropertyMetadata(
												0.35,
												0//FrameworkPropertyMetadataOptions.AffectsRender
												));
		}

		public Thickness BorderThickness
		{
			get { return (Thickness)GetValue(BorderThicknessProperty); }
			set { SetValue(BorderThicknessProperty, value); }
		}
		public Brush BorderBrush
		{
			get { return (Brush)GetValue(BorderBrushProperty); }
			set { SetValue(BorderBrushProperty, value); }
		}
		public CornerRadius CornerRadius
		{
			get { return (CornerRadius)GetValue(CornerRadiusProperty); }
			set { SetValue(CornerRadiusProperty, value); }
		}
		public Brush Stroke
		{
			get { return (Brush)GetValue(StrokeProperty); }
			set { SetValue(StrokeProperty, value); }
		}
		public Double StrokeThickness
		{
			get { return (Double)GetValue(StrokeThicknessProperty); }
			set { SetValue(StrokeThicknessProperty, value); }
		}
		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}
		public ContentAlignment ContentAlignment
		{
			get { return (ContentAlignment)GetValue(ContentAlignmentProperty); }
			set { SetValue(ContentAlignmentProperty, value); }
		}
		public IGraphExImpl IGraphEx
		{
			get { return (IGraphExImpl)GetValue(IGraphExProperty); }
			set { SetValue(IGraphExProperty, value); }
		}
		public Thickness EdgePadding
		{
			get { return (Thickness)GetValue(EdgePaddingProperty); }
			set { SetValue(EdgePaddingProperty, value); }
		}
		public Thickness VertexPadding
		{
			get { return (Thickness)GetValue(VertexPaddingProperty); }
			set { SetValue(VertexPaddingProperty, value); }
		}
		public Double VertexMinWidth
		{
			get { return (Double)GetValue(VertexMinWidthProperty); }
			set { SetValue(VertexMinWidthProperty, value); }
		}
		public Double VertexMinHeight
		{
			get { return (Double)GetValue(VertexMinHeightProperty); }
			set { SetValue(VertexMinHeightProperty, value); }
		}
		public LayoutDirection LayoutDirection
		{
			get { return (LayoutDirection)GetValue(LayoutDirectionProperty); }
			set { SetValue(LayoutDirectionProperty, value); }
		}
		public EdgeContentMode EdgeContentMode
		{
			get { return (EdgeContentMode)GetValue(EdgeContentModeProperty); }
			set { SetValue(EdgeContentModeProperty, value); }
		}
		public EdgeContentOrientation EdgeContentOrientation
		{
			get { return (EdgeContentOrientation)GetValue(EdgeContentOrientationProperty); }
			set { SetValue(EdgeContentOrientationProperty, value); }
		}
		public bool ExtendLeafVerticies
		{
			get { return (bool)GetValue(ExtendLeafVerticiesProperty); }
			set { SetValue(ExtendLeafVerticiesProperty, value); }
		}
		public bool CompactRootVerticies
		{
			get { return (bool)GetValue(CompactRootVerticiesProperty); }
			set { SetValue(CompactRootVerticiesProperty, value); }
		}
		public bool ShowWorkArea
		{
			get { return (bool)GetValue(ShowWorkAreaProperty); }
			set { SetValue(ShowWorkAreaProperty, value); }
		}
		public bool ShowLayoutProxyPoints
		{
			get { return (bool)GetValue(ShowLayoutProxyPointsProperty); }
			set { SetValue(ShowLayoutProxyPointsProperty, value); }
		}
		public bool ShowLayoutProxyOutlines
		{
			get { return (bool)GetValue(ShowLayoutProxyOutlinesProperty); }
			set { SetValue(ShowLayoutProxyOutlinesProperty, value); }
		}
		public Style EdgeTextStyle
		{
			get { return (Style)GetValue(EdgeTextStyleProperty); }
			set { SetValue(EdgeTextStyleProperty, value); }
		}
		public int GraphCrossings
		{
			get { return (int)GetValue(GraphCrossingsProperty); }
		}
		public Double SplineTension
		{
			get { return (Double)GetValue(SplineTensionProperty); }
			set { SetValue(SplineTensionProperty, value); }
		}
	};
}
