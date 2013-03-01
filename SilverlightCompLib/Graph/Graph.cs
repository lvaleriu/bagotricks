#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SilverlightCompLib.Mathematics;

#endregion

namespace SilverlightCompLib.Graph
{
    public class Graphic : Panel
    {
        public Panel canvas
        {
            get
            {
                if (_canvas == null) _canvas = this;
                return this;
            }
        }

        public Canvas TopCanvas
        {
            get
            {
                if (_topcanvas == null)
                {
                    _topcanvas = new Canvas
                        {
                            Height = Height,
                            Width = Width,
                            Background = new SolidColorBrush(Colors.Transparent),
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };
                }
                return _topcanvas;
            }
        }

        #region Constructors

        public Graphic(Graph g, Size size) : this(size)
        {
            _graph = g;

            var border = new Rectangle
                {
                    Width = size.Width,
                    Height = size.Height,
                    Stroke = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128))
                };
        }

        public Graphic(Size size)
            : this()
        {
            _timer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 0, 10)
                };

            _timer.Tick += _timer_Tick;
            SizeChanged += Graphic_SizeChanged;
            anim_mng = new AnimationManager(this);

            Width = size.Width;
            Height = size.Height;
            Children.Add(TopCanvas);

            _nodeState = new Dictionary<Node, NodeState>();
            _gcpState = new Dictionary<GraphContentPresenter, GCPDragState>();
            _dict_gcp_lines = new Dictionary<GraphContentPresenter, List<KeyValuePair<GraphContentPresenter, Line>>>();
            exp_dict = new Dictionary<GraphContentPresenter, DateTime>();

            GraphContentPresenter.onMouseEvent += GraphContentPresenter_onMouseLeftButtonDown;
            GraphContentPresenter.onMouseLeftButtonDown += GraphContentPresenter_onMouseLeftButtonDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            canvas.MouseLeave += Canvas_MouseLeave;
            FrameRate = .3;
            _timer.Start();
        }

        public Graphic()
        {
            Loaded += Graphic_Loaded;
            _compositionTarget_RenderingCallback = compositionTarget_rendering;

            _nodeTemplateBinding = new Binding(NodeTemplateProperty.ToString());
            _nodeTemplateBinding.Source = this;
            /*
            _nodeTemplateSelectorBinding = new Binding(NodeTemplateSelectorProperty.Name);
            _nodeTemplateSelectorBinding.Source = this;
            */
            _nodesChangedHandler = NodesCollectionChanged;

            _frameTickWired = false;

            _nodePresenters = new List<GraphContentPresenter>();
            _nodePresentersDict = new Dictionary<object, GraphContentPresenter>();
        }

        private void GraphContentPresenter_onMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var gcpCanvas = sender as GraphContentPresenter;

            _DragState.NodeBeingDragged = new GCPState {GCPCanvas = gcpCanvas, Position = gcpCanvas.ActualLocation};
            _DragState.OffsetWithinNode = e.GetPosition(gcpCanvas);
            _DragState.IsDragging = true;
            gcpCanvas.CaptureMouse();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            lock (lock_expdict)
            {
                var toremove = new List<GraphContentPresenter>();
                foreach (KeyValuePair<GraphContentPresenter, DateTime> pair in exp_dict)
                {
                    if (pair.Key == _centerObjectPresenter) continue;
                    if (pair.Value.Add(MaxExpTime) < DateTime.Now)
                    {
                        toremove.Add(pair.Key);
                        KillCenterGCPLines(pair.Key);
                        Debug.WriteLine(pair.Key.Content.ToString());
                    }
                }
                toremove.ForEach(el => exp_dict.Remove(el));
            }
        }

        #endregion

        #region Events

        public delegate void MouseEvent(object sender, RoutedEventArgs e);

        private volatile bool can_HandleChanges = true;
        private volatile bool canstep = true;

        private void Graphic_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout();
            wireFrameTick();
        }

        private void Graphic_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayout();
        }

        public event MouseEvent onMouseEvent;
        public event EventHandler<EventArgs> onMovingStoped;

        private void GraphContentPresenter_onMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (onMouseEvent != null) onMouseEvent(sender, e);
        }

        public void DoLayout()
        {
            _center = new Point(_canvas.Width/2, _canvas.Height/2);

            var r = new Random();

            foreach (Node n in _graph.Nodes)
            {
                var randomPos = new Point(300 + 200*(r.NextDouble() - 0.5), 300 + 150*(r.NextDouble() - 0.5));
                _nodeState.Add(n,
                               new NodeState
                                   {
                                       Position = randomPos,
                                       Velocity = new Point(0, 0)
                                   });
            }

            #region Build visual graph representation

            foreach (KeyValuePair<Node, NodeState> nodeLoc in _nodeState)
            {
                NodeCanvas nCanv = CreateNodeCanvas(nodeLoc.Key);
                nodeLoc.Value.NodeCanvas = nCanv;
                nCanv.NodeState = nodeLoc.Value;

                /* Set the position of the node */
                nCanv.SetValue(Canvas.LeftProperty, nodeLoc.Value.Position.X - nCanv.Width/2);
                nCanv.SetValue(Canvas.TopProperty, nodeLoc.Value.Position.Y - nCanv.Height/2);

                _canvas.Children.Add(nCanv);

                foreach (Node child in _graph.Nodes.Children(nodeLoc.Key))
                {
                    Point a = nodeLoc.Value.Position;
                    Point b = _nodeState[child].Position;

                    var lgb = new LinearGradientBrush();
                    lgb.GradientStops.Add(new GradientStop {Color = Color.FromArgb(255, 0, 0, 0), Offset = 0});
                    lgb.GradientStops.Add(new GradientStop {Color = Color.FromArgb(255, 128, 0, 0), Offset = 0.5});
                    lgb.GradientStops.Add(new GradientStop {Color = Color.FromArgb(255, 0, 0, 0), Offset = 1.0});

                    var edge = new Line
                        {
                            X1 = a.X,
                            X2 = b.X,
                            Y1 = a.Y,
                            Y2 = b.Y,
                            Stroke = lgb,
                            StrokeThickness = 2.0
                        };

                    nodeLoc.Value.ChildLinks.Add(new KeyValuePair<Node, Line>(child, edge));

                    _canvas.Children.Insert(0, edge);
                }
            }

            #endregion
        }

        private void StepLayout(bool a)
        {
            int cnt = VisualChildrenCount;
            if (!canstep)
            {
                //System.Diagnostics.Debug.WriteLine("!canstep");
                return;
            }
            canstep = false;
            compositionTarget_rendering(null, null);
            canstep = true;
        }

        /// <summary>
        ///     Applies one step of the graph layout algorithm, moving nodes to a more stable configuration.
        /// </summary>
        public void StepLayout()
        {
            StepLayout(false);
            return;

            for (int i = 0; i < 15; i++)
            {
                double KE = RunPhysics();

                /* This causes the centering effect, but we disable it
                 * temporarily while dragging so that it doesn't move the
                 * item beneath the user's cursor */

                if (!DragState.IsDragging)
                    ApplyGlobalOffset(_center, 0.5);
                UpdateVisualPositions();
            }
        }

        private void UpdateVisualPositions()
        {
            foreach (KeyValuePair<Node, NodeState> nodeLoc in _nodeState)
            {
                NodeState ns = nodeLoc.Value;

                /* Set the position of the node */
                ns.NodeCanvas.SetValue(Canvas.LeftProperty, ns.Position.X - ns.NodeCanvas.Width/2);
                ns.NodeCanvas.SetValue(Canvas.TopProperty, ns.Position.Y - ns.NodeCanvas.Height/2);

                foreach (KeyValuePair<Node, Line> kvp in ns.ChildLinks)
                {
                    Point childLoc = _nodeState[kvp.Key].Position;
                    kvp.Value.X1 = ns.Position.X;
                    kvp.Value.Y1 = ns.Position.Y;
                    kvp.Value.X2 = childLoc.X;
                    kvp.Value.Y2 = childLoc.Y;
                }
            }
        }

        private void ApplyGlobalOffset(Point DesiredCentroid, double PixelSpeed)
        {
            var centroid = new Point();

            foreach (NodeState state in _nodeState.Values)
            {
                centroid.X += state.Position.X;
                centroid.Y += state.Position.Y;
            }

            centroid.X = centroid.X/_nodeState.Count;
            centroid.Y = centroid.Y/_nodeState.Count;

            var offset = new Point(
                DesiredCentroid.X - centroid.X,
                DesiredCentroid.Y - centroid.Y);

            /* Normalize the offset and only move by the desired pixel speed */
            double length = Math.Sqrt(offset.X*offset.X + offset.Y*offset.Y);

            /* If we're very close, don't move at the full speed anymore */
            if (PixelSpeed > length) PixelSpeed = length;

            offset.X = (offset.X/length)*PixelSpeed;
            offset.Y = (offset.Y/length)*PixelSpeed;

            foreach (NodeState state in _nodeState.Values)
            {
                state.Position.X += offset.X;
                state.Position.Y += offset.Y;
            }
        }

        #endregion

        #region Physics Simulation Components

        /// <summary>
        ///     Simulates 1/r^2 repulsive force as with two similarly-charged electrons.
        ///     Also adds a very weak attractive force proportional to r which keeps
        ///     disparate graph segments from flying apart.
        /// </summary>
        private Point CoulombRepulsion(Point a, Point b, double k)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            double sqDist = dx*dx + dy*dy;
            double d = Math.Sqrt(sqDist);

            double mag = k*1.0/sqDist; // Force magnitude

            mag += -k*0.00000006*d; // plus WEAK attraction

            if (mag > 10) mag = 10; // Clip maximum

            return new Point(mag*(dx/d), mag*(dy/d));
        }

        private Point HookeAttraction(Point a, Point b, double k)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            double sqDist = dx*dx + dy*dy;
            double d = Math.Sqrt(sqDist);

            double mag = -k*0.001*Math.Pow(d, 1.0); // Force magnitude

            return new Point(mag*(dx/d), mag*(dy/d));
        }

        /// <summary>
        ///     Applies simulated electrostatic repulsion between nodes and spring
        ///     attraction along edges, moving nodes to a more stable configuration.
        /// </summary>
        /// <returns></returns>
        private double RunPhysics()
        {
            double totalKE = 0;

            foreach (KeyValuePair<Node, NodeState> kvp in _nodeState)
            {
                if (DragState.IsDragging && DragState.NodeBeingDragged == kvp.Value) continue;

                double dT = 0.95;
                Node n = kvp.Key;
                NodeState state = kvp.Value;

                var F = new Point(0, 0); // Force

                /* for each other node ... */
                foreach (KeyValuePair<Node, NodeState> kvpB in _nodeState)
                {
                    if (kvpB.Key == n) continue;
                    Point coulomb = CoulombRepulsion(state.Position, kvpB.Value.Position, 100.0);
                    F.X += coulomb.X;
                    F.Y += coulomb.Y;
                }

                /* foreach spring connected ... */
                foreach (Node child in _graph.Nodes.Children(n))
                {
                    Point hooke = HookeAttraction(state.Position, _nodeState[child].Position, 0.9);
                    F.X += hooke.X;
                    F.Y += hooke.Y;
                }
                foreach (Node parent in _graph.Nodes.Parents(n))
                {
                    Point hooke = HookeAttraction(state.Position, _nodeState[parent].Position, 0.9);
                    F.X += hooke.X;
                    F.Y += hooke.Y;
                }

                Point v = state.Velocity;

                double damping = 0.90;

                /* Update velocity */
                state.Velocity = new Point(
                    (v.X + dT*F.X)*damping,
                    (v.Y + dT*F.Y)*damping);

                totalKE += state.Velocity.X*state.Velocity.X + state.Velocity.Y*state.Velocity.Y;

                /* Update position */
                state.Position.X += dT*state.Velocity.X;
                state.Position.Y += dT*state.Velocity.Y;
            }

            return totalKE;
        }

        #endregion

        #region Visual Appearance of one Node

        private NodeDragState DragState;
        private GCPDragState _DragState;

        private NodeCanvas CreateNodeCanvas(Node n)
        {
            Color projColor = Color.FromArgb(128, 100, 55, 55);
            Brush projBrush = new SolidColorBrush(projColor);
            Brush textColor = new SolidColorBrush(Color.FromArgb(255, 255, 150, 150));
            Brush borderColor = new SolidColorBrush(Color.FromArgb(255, 255, 100, 100));

            var title = new TextBlock {Foreground = textColor, Text = n.Title, FontSize = 12};

            double Height = 25d;
            double Width = (title.ActualWidth + 4) < Height ? Height : title.ActualWidth + 4;

            var nodeCanvas = new NodeCanvas {Width = Width, Height = Height};

            var nodeBkg = new Rectangle
                {
                    Width = Width,
                    Height = Height,
                    Fill = projBrush,
                    Stroke = borderColor
                };

            switch (n.Type)
            {
                case Node.NodeType.Fact:
                    nodeBkg.RadiusX = 10;
                    nodeBkg.RadiusY = 10;
                    break;
                case Node.NodeType.Projection:
                    nodeBkg.RadiusX = 2;
                    nodeBkg.RadiusY = 2;
                    break;
            }

            nodeCanvas.MouseLeftButtonDown += nodeCanvas_MouseLeftButtonDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            canvas.MouseLeave += Canvas_MouseLeave;

            nodeCanvas.Children.Add(nodeBkg);
            nodeCanvas.Children.Add(title);

            #region Center the title

            double left = (nodeBkg.Width - title.ActualWidth)/2;
            double top = (nodeBkg.Height - title.ActualHeight)/2;
            title.SetValue(Canvas.LeftProperty, left);
            title.SetValue(Canvas.TopProperty, top);

            #endregion

            return nodeCanvas;
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _DragState.IsDragging = false;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _DragState.IsDragging = false;
            (sender as FrameworkElement).ReleaseMouseCapture();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_DragState.IsDragging)
            {
                Point position = e.GetPosition(_DragState.NodeBeingDragged.GCPCanvas);
                position.X += (-_DragState.OffsetWithinNode.X);
                position.Y += (-_DragState.OffsetWithinNode.Y);

                GCPState ns = _DragState.NodeBeingDragged;

                ns.Position.X += position.X;
                ns.Position.Y += position.Y;
                ns.GCPCanvas.X = ns.Position.X - ns.GCPCanvas.DesiredSize.Width/2;
                ns.GCPCanvas.Y = ns.Position.Y - ns.GCPCanvas.DesiredSize.Height/2;

                //UpdateLayout();
                _centerChanged = true;
                _measureInvalidated = true;
                _stillMoving = true;

                //ns.NodeCanvas.SetValue(Canvas.LeftProperty, ns.Position.X - ns.NodeCanvas.Width / 2);
                //ns.NodeCanvas.SetValue(Canvas.TopProperty, ns.Position.Y - ns.NodeCanvas.Height / 2);
            }
        }

        private void nodeCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var nodeCanvas = sender as NodeCanvas;

            //_DragState.NodeBeingDragged = nodeCanvas.NodeState;
            _DragState.OffsetWithinNode = e.GetPosition(nodeCanvas);
            _DragState.IsDragging = true;

            nodeCanvas.CaptureMouse();
        }

        private struct GCPDragState
        {
            public bool IsDragging;
            public GCPState NodeBeingDragged;
            public Point OffsetWithinNode;
        }

        private struct NodeDragState
        {
            public bool IsDragging;
            public NodeState NodeBeingDragged;
            public Point OffsetWithinNode;
        };

        #endregion

        #region overrides

        protected int VisualChildrenCount
        {
            get
            {
                if (!_isChildCountValid)
                {
                    _childCount = 0;
                    if (_centerObjectPresenter != null)
                    {
                        _childCount++;
                    }
                    if (_nodePresenters != null)
                    {
                        _childCount += _nodePresenters.Count;
                    }
                    _childCount += _fadingGCPList.Count;
                    _isChildCountValid = true;
                }

                return _childCount;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            handleChanges();
            _measureInvalidated = true;
            wireFrameTick();

            for (int i = 0; i < _needsMeasure.Count; i++)
            {
                _needsMeasure[i].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }
            _needsMeasure.Clear();

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _controlCenter.X = finalSize.Width/2;
            _controlCenter.Y = finalSize.Height/2;

            for (int i = 0; i < _needsArrange.Count; i++)
            {
                _needsArrange[i].Arrange(EmptyRect);
            }
            _needsArrange.Clear();

            return finalSize;
        }

        protected DependencyObject GetVisualChild(int index)
        {
            if (index < _fadingGCPList.Count)
            {
                Debug.Assert(_fadingGCPListValid);
                return _fadingGCPList[index];
            }
            else
            {
                index -= _fadingGCPList.Count;
            }

            if (_nodePresenters != null)
            {
                if (index < _nodePresenters.Count)
                {
                    return _nodePresenters[index];
                }
                else
                {
                    index -= _nodePresenters.Count;
                }
            }

            if (index == 0)
            {
                return _centerObjectPresenter;
            }
            else
            {
                throw new Exception("not a valid index");
            }
        }

        #endregion

        #region properties

        #region CenterObject

        public static readonly DependencyProperty CenterObjectProperty = DependencyProperty.Register(
            "CenterObject", typeof (object), typeof (Graphic), new PropertyMetadata(CenterObjectPropertyChanged));

        #region CenterObject Impl

        private static void CenterObjectPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            ((Graphic) element).InvalidateMeasure();
            ((Graphic) element).CenterObjectPropertyChanged(args);
        }

        private void CenterObjectPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            if (args.OldValue != args.NewValue)
            {
                _centerChanged = true;
                resetNodesBinding();
            }
        }

        #endregion

        public object CenterObject
        {
            get { return GetValue(CenterObjectProperty); }
            set { SetValue(CenterObjectProperty, value); }
        }

        #endregion

        #region NodesBindingPath

        public static readonly DependencyProperty NodesBindingPathProperty =
            DependencyProperty.Register("NodesBindingPath", typeof (string), typeof (Graphic),
                                        new PropertyMetadata(NodesBindingPathPropertyChanged));

        public string NodesBindingPath
        {
            get { return (string) GetValue(NodesBindingPathProperty); }
            set { SetValue(NodesBindingPathProperty, value); }
        }

        private static void NodesBindingPathPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            var g = (Graphic) element;
            g.resetNodesBinding();
        }

        #endregion

        #region NodeTemplate

        public static readonly DependencyProperty NodeTemplateProperty = DependencyProperty.Register(
            "NodeTemplate", typeof (DataTemplate), typeof (Graphic), new PropertyMetadata(null));

        public DataTemplate NodeTemplate
        {
            get { return (DataTemplate) GetValue(NodeTemplateProperty); }
            set { SetValue(NodeTemplateProperty, value); }
        }

        #endregion

        #region CoefficientOfDampening

        public delegate object CoerceValueCallback(DependencyObject d, object baseValue);

        public static readonly DependencyProperty CoefficientOfDampeningProperty = DependencyProperty.Register(
            "CoefficientOfDampening", typeof (double), typeof (Graphic),
            new PropertyMetadata(CoerceCoefficientOfDampeningPropertyCallback));

        public double CoefficientOfDampening
        {
            get
            {
                return .7;
                //return (double)GetValue(CoefficientOfDampeningProperty);
            }
            set { SetValue(CoefficientOfDampeningProperty, value); }
        }

        private static void CoerceCoefficientOfDampeningPropertyCallback(DependencyObject element, DependencyPropertyChangedEventArgs baseValue)
        {
            if (baseValue.NewValue != null)
            {
                CoerceCoefficientOfDampeningPropertyCallback((double) baseValue.NewValue);
            }
        }

        private static double CoerceCoefficientOfDampeningPropertyCallback(double baseValue)
        {
            if (baseValue <= MinCOD)
            {
                return MinCOD;
            }
            else if (baseValue >= MaxCOD)
            {
                return MaxCOD;
            }
            else
            {
                return baseValue;
            }
        }

        #endregion

        #region FrameRate

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register("FrameRate",
                                                                                                  typeof (double), typeof (Graphic), new PropertyMetadata(CoerceFrameRatePropertyCallback));

        public double FrameRate
        {
            get { return (double) GetValue(FrameRateProperty); }
            set { SetValue(FrameRateProperty, value); }
        }

        private static void CoerceFrameRatePropertyCallback(DependencyObject element, DependencyPropertyChangedEventArgs baseValue)
        {
            if (baseValue.NewValue != null)
            {
                CoerceFrameRatePropertyCallback((double) baseValue.NewValue);
            }
        }

        private static double CoerceFrameRatePropertyCallback(double baseValue)
        {
            if (baseValue <= MinCOD)
            {
                return MinCOD;
            }
            else if (baseValue >= MaxCOD)
            {
                return MaxCOD;
            }
            else
            {
                return baseValue;
            }
        }

        #endregion

        #endregion

        #region implementation

        private readonly EventHandler _compositionTarget_RenderingCallback;
        private volatile bool showlines = true;

        private void resetNodesBinding()
        {
            if (NodesBindingPath == null)
            {
                //BindingOperations.ClearBinding(this, NodesProperty);
            }
            else
            {
                Binding theBinding = GetBinding(NodesBindingPath, CenterObject);
                if (theBinding == null)
                {
                    //BindingOperations.ClearBinding(this, NodesProperty);
                }
                else
                {
                    //BindingOperations.SetBinding(this, NodesProperty, theBinding);
                }
            }
            //NodesPropertyChanged(new DependencyPropertyChangedEventArgs());
        }

        private void wireFrameTick()
        {
            if (!_frameTickWired)
            {
                //Debug.Assert(CheckAccess());
                CompositionTarget.Rendering += _compositionTarget_RenderingCallback;
                _frameTickWired = true;
            }
        }

        private void unwireFrameTick()
        {
            if (_frameTickWired)
            {
                //Debug.Assert(CheckAccess());
                CompositionTarget.Rendering -= _compositionTarget_RenderingCallback;
                _frameTickWired = false;
            }
        }

        private void compositionTarget_rendering(object sender, EventArgs args)
        {
            int cnt = VisualChildrenCount;
            Debug.Assert(_nodePresenters != null);
            if (_springForces == null)
            {
                _springForces = SetupForceVertors(_nodePresenters.Count);
            }
            else if (_springForces.GetLowerBound(0) != _nodePresenters.Count)
            {
                _springForces = SetupForceVertors(_nodePresenters.Count);
            }

            bool _somethingInvalid = false;
            if (_measureInvalidated || _stillMoving)
            {
                if (_measureInvalidated)
                {
                    _ticksOfLastMeasureUpdate = DateTime.Now.Ticks; //m_milliseconds = Environment.TickCount;                    
                }

                #region CenterObject

                if (_centerObjectPresenter != null)
                {
                    if (_centerObjectPresenter.New)
                    {
                        _centerObjectPresenter.ParentCenter = _controlCenter;
                        _centerObjectPresenter.New = false;
                        _somethingInvalid = true;
                    }
                    else
                    {
                        Vector forceVector = GetAttractionForce(ensureNonzeroVector(new Vector(_centerObjectPresenter.Location)));

                        if (updateGraphCP(_centerObjectPresenter, forceVector, CoefficientOfDampening, FrameRate, _controlCenter))
                        {
                            _somethingInvalid = true;
                        }
                    }
                }

                #endregion

                GraphContentPresenter gcp;
                for (int i = 0; i < _nodePresenters.Count; i++)
                {
                    gcp = _nodePresenters[i];

                    if (gcp.New)
                    {
                        gcp.New = false;
                        _somethingInvalid = true;
                    }

                    for (int j = (i + 1); j < _nodePresenters.Count; j++)
                    {
                        Vector distance = ensureNonzeroVector(Vector.Subtract(gcp.Location, _nodePresenters[j].Location));

                        Vector repulsiveForce = GetRepulsiveForce(distance); //GetSpringForce(distance, gcp.Velocity - _nodePresenters[j].Velocity);
                        _springForces[i, j] = repulsiveForce;
                    }
                }

                Point centerLocationToUse = (_centerObjectPresenter != null) ? _centerObjectPresenter.Location : new Point();

                for (int i = 0; i < _nodePresenters.Count; i++)
                {
                    var forceVector = new Vector();
                    forceVector += GetVectorSum(i, _nodePresenters.Count, _springForces);
                    forceVector += GetSpringForce(ensureNonzeroVector(Vector.Subtract(_nodePresenters[i].Location, centerLocationToUse)));
                    forceVector += GetWallForce(RenderSize, _nodePresenters[i].Location);

                    if (updateGraphCP(_nodePresenters[i], forceVector, CoefficientOfDampening, FrameRate, _controlCenter))
                    {
                        _somethingInvalid = true;
                    }
                }

                #region animate all of the fading ones away

                for (int i = 0; i < _fadingGCPList.Count; i++)
                {
                    if (!_fadingGCPList[i].WasCenter)
                    {
                        Vector centerDiff = ensureNonzeroVector(Vector.Subtract(_fadingGCPList[i].Location, centerLocationToUse));
                        centerDiff.Normalize();
                        centerDiff *= 20;
                        if (updateGraphCP(_fadingGCPList[i], centerDiff, CoefficientOfDampening, FrameRate, _controlCenter))
                        {
                            _somethingInvalid = true;
                        }
                    }
                }

                #endregion

                if (_somethingInvalid && belowMaxSettleTime())
                {
                    _stillMoving = true;
                    UpdateLayout();
                }
                else
                {
                    _stillMoving = false;
                    if (onMovingStoped != null) onMovingStoped(DateTime.Now, null);
                    unwireFrameTick();
                }
                if (_nodePresenters != null && _centerObjectPresenter != null)
                {
                    UpdateLines();
                }
                _measureInvalidated = false;
            }
            if (_nodePresenters != null && _centerObjectPresenter != null)
            {
                exp_dict[_centerObjectPresenter] = DateTime.Now;
            }
        }

        private void AddLines()
        {
            if (!showlines) return;
#if DEBUGa
            System.Diagnostics.Debug.WriteLine("AddLines");
#endif
            lock (lock_gcp_lines)
            {
                if (!_dict_gcp_lines.ContainsKey(_centerObjectPresenter))
                {
                    _dict_gcp_lines[_centerObjectPresenter] = new List<KeyValuePair<GraphContentPresenter, Line>>();
                }
                exp_dict[_centerObjectPresenter] = DateTime.Now;
                var oldlist = _dict_gcp_lines[_centerObjectPresenter];
                var same = new List<KeyValuePair<GraphContentPresenter, Line>>();
                var same_dict = new Dictionary<GraphContentPresenter, Line>();
                _nodePresenters.ForEach(el =>
                    {
                        for (int i = 0; i < oldlist.Count; i++)
                        {
                            if (oldlist[i].Key == el)
                            {
                                same.Add(oldlist[i]);
                                same_dict[el] = oldlist[i].Value;
                                break;
                            }
                        }
                    });
                var total = oldlist.Count;
                var cnt = 0;
                while (cnt < total)
                {
                    if (!same.Contains(oldlist[cnt]))
                    {
                        //this.Children.Remove(oldlist[cnt].Value);
                        TopCanvas.Children.Remove(oldlist[cnt].Value);
                        oldlist.RemoveAt(cnt);
                        total--;
                    }
                    else
                        cnt++;
                }

                for (int i = 0; i < _nodePresenters.Count; i++)
                {
                    Point a = _centerObjectPresenter.Center; // ActualLocation;
                    Point b = _nodePresenters[i].Center; //ActualLocation;
                    Line edge;
                    if (same_dict.ContainsKey(_nodePresenters[i]))
                    {
                        edge = same_dict[_nodePresenters[i]];
                        edge.X1 = a.X;
                        edge.Y2 = b.X;
                        edge.Y1 = a.Y;
                        edge.Y2 = b.Y;
                        //if (!this.Children.Contains(edge)) this.Children.Insert(0, edge);
                        if (!TopCanvas.Children.Contains(edge)) TopCanvas.Children.Insert(0, edge);
                        edge.Visibility = Visibility.Collapsed;
                        //edge.Arrange(new Rect(new Point(a.X, a.Y), new Point(b.X, b.Y)));
                    }
                    else
                    {
                        edge = new Line
                            {
                                X1 = a.X,
                                X2 = b.X,
                                Y1 = a.Y,
                                Y2 = b.Y,
                                Stroke = new SolidColorBrush(Colors.Black),
                                StrokeThickness = 1.0,
                                Visibility = Visibility.Collapsed
                            };

                        TopCanvas.Children.Insert(0, edge);
                        //this.Children.Insert(0, edge);
                        //edge.Arrange(new Rect(new Point(a.X, a.Y), new Point(b.X, b.Y)));
                        _dict_gcp_lines[_centerObjectPresenter].Add(new KeyValuePair<GraphContentPresenter, Line>(_nodePresenters[i], edge));
                    }
                }
                Debug.Assert(_dict_gcp_lines[_centerObjectPresenter].Count == _nodePresenters.Count);
            }
        }

        private void UpdateLines()
        {
            if (!showlines) return;
#if DEBUGa
            System.Diagnostics.Debug.WriteLine("UpdateLines");
#endif
            lock (lock_gcp_lines)
            {
                if (_dict_gcp_lines.ContainsKey(_centerObjectPresenter))
                {
                    exp_dict[_centerObjectPresenter] = DateTime.Now;
                    var List = _dict_gcp_lines[_centerObjectPresenter];
                    foreach (var pair in List)
                    {
                        //if (pair.Key.FinishedAnim == false) continue;
                        pair.Value.X1 = _centerObjectPresenter.Center.X; //ActualLocation.X;
                        pair.Value.Y1 = _centerObjectPresenter.Center.Y; //ActualLocation.Y;
                        pair.Value.X2 = pair.Key.Center.X; //ActualLocation.X;
                        pair.Value.Y2 = pair.Key.Center.Y; // ActualLocation.Y;
                        //Point a = new Point(pair.Value.X1, pair.Value.Y1);
                        //Point b = new Point(pair.Value.X2, pair.Value.Y2);
                        //pair.Value.Arrange(new Rect(new Point(a.X, a.Y), new Point(b.X, b.Y)));
                        if (pair.Value.Visibility == Visibility.Collapsed) pair.Value.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void KillCenterGCPLines(GraphContentPresenter gcp)
        {
            if (!showlines) return;
#if DEBUGa
            System.Diagnostics.Debug.WriteLine("KillCenterGCPLines");
#endif
            lock (lock_gcp_lines)
            {
                if (_dict_gcp_lines.ContainsKey(gcp))
                {
                    var List = _dict_gcp_lines[gcp];
                    //List.ForEach(p => this.Children.Remove(p.Value));
                    List.ForEach(p => TopCanvas.Children.Remove(p.Value));
                    List.Clear();
                }
            }
        }

        private void RemoveCenterGCPLines(GraphContentPresenter gcp)
        {
            if (!showlines) return;
#if DEBUGa
            System.Diagnostics.Debug.WriteLine("KillCenterGCPLines");
#endif
            lock (lock_gcp_lines)
            {
                if (_dict_gcp_lines.ContainsKey(gcp))
                {
                    var List = _dict_gcp_lines[gcp];
                    //List.ForEach(p => this.Children.Remove(p.Value));
                    List.ForEach(p => TopCanvas.Children.Remove(p.Value));
                }
            }
        }

        private bool belowMaxSettleTime()
        {
            Debug.Assert(_ticksOfLastMeasureUpdate != long.MinValue);
            return MaxSettleTime > new TimeSpan(DateTime.Now.Ticks - _ticksOfLastMeasureUpdate);
        }

        private bool belowMaxSettleTime(bool OrigVer)
        {
            Debug.Assert(m_milliseconds != int.MinValue);
            return MaxSettleTime > TimeSpan.FromMilliseconds(Environment.TickCount - m_milliseconds);
        }

        private static Vector ensureNonzeroVector(Vector vector)
        {
            if (vector.Length > 0)
            {
                return vector;
            }
            else
            {
                return new Vector(Rnd.NextDouble() - .5, Rnd.NextDouble() - .5);
            }
        }

        private static bool updateGraphCP(GraphContentPresenter graphContentPresenter, Vector forceVector, double coefficientOfDampening, double frameRate, Point parentCenter)
        {
            bool parentCenterChanged = (graphContentPresenter.ParentCenter != parentCenter);
            if (parentCenterChanged)
            {
                graphContentPresenter.ParentCenter = parentCenter;
            }

            //add system drag
            Debug.Assert(coefficientOfDampening > 0);
            Debug.Assert(coefficientOfDampening < 1);
            graphContentPresenter.Velocity *= (1 - coefficientOfDampening*frameRate);

            //add force
            graphContentPresenter.Velocity += (forceVector*frameRate);

            //apply terminalVelocity
            if (graphContentPresenter.Velocity.Length > TerminalVelocity)
            {
                graphContentPresenter.Velocity *= (TerminalVelocity/graphContentPresenter.Velocity.Length);
            }

            if (graphContentPresenter.Velocity.Length > MinVelocity && forceVector.Length > MinVelocity)
            {
                graphContentPresenter.Location = graphContentPresenter.Location.Add(graphContentPresenter.Velocity*frameRate);
                return true;
            }
            else
            {
                graphContentPresenter.Velocity = new Vector();
                return false || parentCenterChanged;
            }
        }

        private static Vector[,] SetupForceVertors(int count)
        {
            return new Vector[count,count];
        }

        private void KillGCP(GraphContentPresenter gcp, bool isCenter)
        {
            Debug.Assert(VisualTreeHelper.GetParent(gcp) == this);

            UpdateLayout(); //this.InvalidateVisual();

            _fadingGCPList.Add(gcp);
            _isChildCountValid = false;

            gcp.IsHitTestVisible = false;
            if (isCenter)
            {
                gcp.WasCenter = true;
                if (_nodePresenters != null && _centerObjectPresenter != null)
                {
                    KillCenterGCPLines(gcp);
                }
            }
#if OLD
            ScaleTransform st = gcp._scaleTransform;
            DoubleAnimation da = GetNewHideAnimation(gcp, this, theKey);
            
            st.BeginAnimation(ScaleTransform.ScaleXProperty, da);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, da);
            gcp.BeginAnimation(OpacityProperty, da);
#endif
            GetNewHideAnimations(gcp, this);
        }

        private void CleanUpGCP(GraphContentPresenter contentPresenter)
        {
            if (_fadingGCPList.Contains(contentPresenter))
            {
                Debug.Assert(VisualTreeHelper.GetParent(contentPresenter) == this);

                RemoveVisualChild(contentPresenter);
                _isChildCountValid = false;
                _fadingGCPList.Remove(contentPresenter);
            }
        }

        private void GetNewHideAnimations(GraphContentPresenter element, Graphic owner)
        {
#if DEBUG_TO_CHECK
            Storyboard sb = Graphic.anim_mng.GetDissapearAnim(TimeSpan.FromSeconds(0), HideDuration.TimeSpan, 1, 0, element, element);
            sb.Completed += delegate(object sender, EventArgs e)
            {
                CleanUpGCP(element);
            };

            sb.Begin();
#else
            var An = new Animations(element, 24, 1, 0);
            An.onCompleted += delegate { CleanUpGCP(element); };
            An.startAnimation();
#endif
        }

        private DoubleAnimation GetNewHideAnimation(GraphContentPresenter element, Graphic owner, int key)
        {
            var da = new DoubleAnimation {To = 0, Duration = HideDuration};
            da.FillBehavior = FillBehavior.Stop;
            //da.SetValue(Timeline.DesiredFrameRateProperty, HideDesiredFrameRate);
            da.Completed += delegate { CleanUpGCP(element); };
            //da.Freeze();
            return da;
        }

        private void handleChanges()
        {
            if (!can_HandleChanges) return;
            can_HandleChanges = false;

            handleNodesChangedWiring();

            #region Optimisation1

            if (_centerChanged && _nodeCollectionChanged && _centerObjectPresenter != null && _nodePresenters != null &&
                CenterObject != null && !CenterObject.Equals(_centerDataInUse))
            {
                Debug.Assert(!CenterObject.Equals(_centerDataInUse));
                Debug.Assert(_centerObjectPresenter.Content == null || _centerObjectPresenter.Content.Equals(_centerDataInUse));

                _centerDataInUse = CenterObject;

                //figure out if we can re-cycle one of the existing children as the center Node
                //if we can, newCenter != null
                GraphContentPresenter newCenterPresenter = null;
                for (int i = 0; i < _nodePresenters.Count; i++)
                {
                    if (_nodePresenters[i].Content.Equals(CenterObject))
                    {
                        //we should re-use this 
                        newCenterPresenter = _nodePresenters[i];
                        _nodePresenters[i] = null;
                        break;
                    }
                }

                //figure out if we can re-cycle the existing center as one of the new child nodes
                //if we can, newChild != null && newChildIndex == indexOf(data in Nodes)
                int newChildIndex = -1;
                GraphContentPresenter newChildPresnter = null;
                for (int i = 0; i < _nodesInUse.Count; i++)
                {
                    if (_nodesInUse[i] != null && _centerObjectPresenter.Content != null && _nodesInUse[i].Equals(_centerObjectPresenter.Content))
                    {
                        newChildIndex = i;
                        newChildPresnter = _centerObjectPresenter;
                        RemoveCenterGCPLines(_centerObjectPresenter);
                        _centerObjectPresenter = null;
                        break;
                    }
                }

                //now we potentially have a center (or not) and one edge(or not)
                var newChildren = new GraphContentPresenter[_nodesInUse.Count];
                if (newChildPresnter != null)
                {
                    newChildren[newChildIndex] = newChildPresnter;
                }

                for (int i = 0; i < _nodesInUse.Count; i++)
                {
                    if (newChildren[i] == null)
                    {
                        for (int j = 0; j < _nodePresenters.Count; j++)
                        {
                            if (_nodePresenters[j] != null)
                            {
                                if (_nodesInUse[i].Equals(_nodePresenters[j].Content))
                                {
                                    Debug.Assert(newChildren[i] == null);
                                    newChildren[i] = _nodePresenters[j];
                                    _nodePresenters[j] = null;
                                    break;
                                }
                            }
                        }
                    }
                }

                //we've no reused everything we can
                if (_centerObjectPresenter == null)
                {
                    if (newCenterPresenter == null)
                    {
                        _centerObjectPresenter = GetGraphContentPresenter(CenterObject,
                                                                          _nodeTemplateBinding, _nodeTemplateSelectorBinding, false
                            );
                        AddVisualChild(_centerObjectPresenter);
                    }
                    else
                    {
                        _centerObjectPresenter = newCenterPresenter;
                        Debug.Assert(VisualTreeHelper.GetParent(newCenterPresenter) == this);
                    }
                }
                else
                {
                    if (newCenterPresenter == null)
                    {
                        RemoveCenterGCPLines(_centerObjectPresenter);
                        _centerObjectPresenter.Content = CenterObject;
                    }
                    else
                    {
                        KillGCP(_centerObjectPresenter, true);
                        _centerObjectPresenter = newCenterPresenter;
                        Debug.Assert(VisualTreeHelper.GetParent(newCenterPresenter) == this);
                    }
                }

                //go through all of the old CPs that are not being used and remove them
                for (int i = 0; i < _nodePresenters.Count; i++)
                {
                    if (_nodePresenters[i] != null)
                    {
                        KillGCP(_nodePresenters[i], false);
                    }
                }

                //go throug and "fill in" all the new CPs
                for (int i = 0; i < _nodesInUse.Count; i++)
                {
                    if (newChildren[i] == null)
                    {
                        GraphContentPresenter gcp = GetGraphContentPresenter(_nodesInUse[i],
                                                                             _nodeTemplateBinding, _nodeTemplateSelectorBinding, true);
                        AddVisualChild(gcp);

                        newChildren[i] = gcp;
                        _nodePresentersDict[gcp.Content] = gcp;
                    }
                }

                _nodePresenters.Clear();
                _nodePresenters.AddRange(newChildren);

                _isChildCountValid = false;

                _centerChanged = false;
                _nodeCollectionChanged = false;

                if (_nodePresenters != null && _centerObjectPresenter != null)
                {
                    AddLines();
                }
            }

            #endregion

            if (_centerChanged)
            {
                _centerDataInUse = CenterObject;
                if (_centerObjectPresenter != null)
                {
                    KillGCP(_centerObjectPresenter, true);
                    _centerObjectPresenter = null;
                }
                if (_centerDataInUse != null)
                {
                    SetUpCleanCenter(_centerDataInUse);
                }
                _centerChanged = false;
            }

            if (_nodeCollectionChanged)
            {
                SetupNodes(Nodes);

                _nodesInUse = Nodes;

                _nodeCollectionChanged = false;

                if (_nodePresenters != null && _centerObjectPresenter != null)
                {
                    AddLines();
                }
            }
#if DEBUG1
            if (CenterObject != null)
            {
                CenterObject.Equals(_centerDataInUse);
                Debug.Assert(_centerObjectPresenter != null);
            }
            else
            {
                Debug.Assert(_centerDataInUse == null);
            }
            if (Nodes != null)
            {
                Debug.Assert(_nodePresenters != null);
                Debug.Assert(Nodes.Count == _nodePresenters.Count);
                Debug.Assert(_nodesInUse == Nodes);
            }
            else
            {
                Debug.Assert(_nodesInUse == null);
                if (_nodePresenters != null)
                {
                    Debug.Assert(_nodePresenters.Count == 0);
                }
            }
#endif
            can_HandleChanges = true;
        }

        private void handleNodesChangedWiring()
        {
            if (_nodesChanged)
            {
                var oldList = _nodesInUse as INotifyCollectionChanged;
                if (oldList != null)
                {
                    oldList.CollectionChanged -= _nodesChangedHandler;
                }

                var newList = Nodes as INotifyCollectionChanged;
                if (newList != null)
                {
                    newList.CollectionChanged += _nodesChangedHandler;
                }

                _nodesInUse = Nodes;
                _nodesChanged = false;
            }
        }

        private void SetupNodes(IList nodes)
        {
#if DEBUG1
            for (int i = 0; i < _nodePresenters.Count; i++)
            {
                Debug.Assert(_nodePresenters[i] != null);
                Debug.Assert(VisualTreeHelper.GetParent(_nodePresenters[i]) == this);
            }
#endif
            int nodesCount = (nodes == null) ? 0 : nodes.Count;

            var newNodes = new GraphContentPresenter[nodesCount];
            for (int i = 0; i < nodesCount; i++)
            {
                for (int j = 0; j < _nodePresenters.Count; j++)
                {
                    if (_nodePresenters[j] != null)
                    {
                        if (nodes[i] == _nodePresenters[j].Content)
                        {
                            newNodes[i] = _nodePresenters[j];
                            _nodePresentersDict[_nodePresenters[j].Content] = null;
                            _nodePresenters[j] = null;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < _nodePresenters.Count; i++)
            {
                if (_nodePresenters[i] != null)
                {
                    KillGCP(_nodePresenters[i], false);
                    _nodePresentersDict.Remove(_nodePresenters[i].Content);
                    _nodePresenters[i] = null;
                }
            }

            for (int i = 0; i < newNodes.Length; i++)
            {
                if (newNodes[i] == null)
                {
                    newNodes[i] = GetGraphContentPresenter(nodes[i], _nodeTemplateBinding, _nodeTemplateSelectorBinding, true);
                    _nodePresentersDict[nodes[i]] = newNodes[i];
                    AddVisualChild(newNodes[i]);
                }
            }

#if DEBUG1
            for (int i = 0; i < _nodePresenters.Count; i++)
            {
                Debug.Assert(_nodePresenters[i] == null);
            }
            for (int i = 0; i < newNodes.Length; i++)
            {
                Debug.Assert(newNodes[i] != null);
                Debug.Assert(VisualTreeHelper.GetParent(newNodes[i]) == this);
                //Debug.Assert(LogicalTreeHelper.GetParent(newNodes[i]) == this);
                Debug.Assert(newNodes[i].Content == nodes[i]);
            }
#endif

            _nodePresentersDict.Clear();
            _nodePresenters.Clear();
            _nodePresenters.AddRange(newNodes);
            _isChildCountValid = false;
        }

        private void SetUpCleanCenter(object newCenter)
        {
            Debug.Assert(_centerObjectPresenter == null);

            _centerObjectPresenter = GetGraphContentPresenter(newCenter, new Binding(), new Binding(), false);

            AddVisualChild(_centerObjectPresenter);

            _isChildCountValid = false;
        }

        private void AddVisualChild(GraphContentPresenter graphContentPresenter)
        {
            Children.Add(graphContentPresenter);
        }

        private void RemoveVisualChild(GraphContentPresenter gcp)
        {
            Children.Remove(gcp);
        }

        private GraphContentPresenter GetGraphContentPresenter(object content, Binding nodeTemplateBinding,
                                                               Binding nodeTemplateSelectorBinding, bool offsetCenter)
        {
            var gcp = new GraphContentPresenter(content, nodeTemplateBinding, nodeTemplateSelectorBinding, offsetCenter);

            _needsMeasure.Add(gcp);
            _needsArrange.Add(gcp);

            return gcp;
        }

        #region Members

        private static readonly object lock_gcp_lines = new object();
        private static volatile bool _stillMoving;
        private static readonly object lock_expdict = new object();
        private readonly Dictionary<GraphContentPresenter, List<KeyValuePair<GraphContentPresenter, Line>>> _dict_gcp_lines;
        private readonly List<GraphContentPresenter> _fadingGCPList = new List<GraphContentPresenter>();
        private readonly List<GraphContentPresenter> _needsArrange = new List<GraphContentPresenter>();
        private readonly List<GraphContentPresenter> _needsMeasure = new List<GraphContentPresenter>();
        private readonly List<GraphContentPresenter> _nodePresenters;
        private readonly Dictionary<object, GraphContentPresenter> _nodePresentersDict;
        private readonly Binding _nodeTemplateBinding;
        private readonly Binding _nodeTemplateSelectorBinding;
        private readonly NotifyCollectionChangedEventHandler _nodesChangedHandler;
        private readonly Dictionary<GraphContentPresenter, DateTime> exp_dict;

        private bool _centerChanged;
        private object _centerDataInUse;

        private GraphContentPresenter _centerObjectPresenter;
        private int _childCount;
        private Point _controlCenter;

        private bool _fadingGCPListValid = false;
        private bool _frameTickWired;
        private bool _isChildCountValid;


        private bool _measureInvalidated;
        private bool _nodeCollectionChanged;
        private bool _nodesChanged;
        private IList _nodesInUse;

        private Vector[,] _springForces;

        private long _ticksOfLastMeasureUpdate = long.MinValue;
        private int m_milliseconds = int.MinValue;

        public bool StillMoving
        {
            get { return _stillMoving; }
        }

        #endregion

        #region Nodes Property

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof (IList), typeof (Graphic), new PropertyMetadata(NodesPropertyChanged));

        #region Nodes Implementation

        private static PropertyMetadata getNodesPropertyMetadata()
        {
            var fpm = new PropertyMetadata(NodesPropertyChanged);
            //fpm.AffectsMeasure = true;
            return fpm;
        }

        private static void NodesPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            ((Graphic) element).InvalidateMeasure();
            ((Graphic) element).NodesPropertyChanged(args);
        }

        private void NodesPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            _nodeCollectionChanged = true;
            _nodesChanged = true;
        }

        #endregion

        public IList Nodes
        {
            get { return (IList) GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        private void NodesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            VerifyAccess();
            InvalidateMeasure();
            _nodeCollectionChanged = true;
        }

        private void VerifyAccess()
        {
            CheckAccess();
        }

        #endregion

        #endregion

        private class GCPState
        {
            public List<KeyValuePair<GraphContentPresenter, Line>> ChildLinks = new List<KeyValuePair<GraphContentPresenter, Line>>();
            public GraphContentPresenter GCPCanvas;
            public Point Position;
            public Point Velocity;
        }

        public class GraphContentPresenter : ContentPresenter //Added by me :)
        {
            public GraphContentPresenter(object content, Binding nodeTemplateBinding, Binding nodeTemplateSelectorBinding, bool offsetCenter)
            {
                //base.SetBinding(ContentPresenter.ContentTemplateProperty, nodeTemplateBinding);
                //base.SetBinding(ContentPresenter.ContentTemplateSelectorProperty, nodeTemplateSelectorBinding);
                Cursor = Cursors.Hand;
                var CP = new ContentPresenter
                    {
                        //Background = new SolidColorBrush(Color.FromArgb(255, 222, 184, 135)),
                        //Padding = new Thickness(10), 
                        Content = content,
                        //FontSize = 16,
                        //FontFamily = new FontFamily("Verdana"),
                    };
                var Bo = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(255, 222, 184, 135)),
                        Padding = new Thickness(5),
                        CornerRadius = new CornerRadius(10),
                        BorderThickness = new Thickness(2),
                        BorderBrush = new SolidColorBrush(Colors.Gray),
                        Child = CP
                    };

                CP.MouseLeftButtonDown += GraphContentPresenter_MouseLeftButtonDown;
                CP.MouseLeftButtonUp += GraphContentPresenter_MouseLeftButtonUp;

                base.Content = Bo;

                _scaleTransform = new ScaleTransform();
                if (offsetCenter)
                {
                    _translateTransform = new TranslateTransform {X = Rnd.NextDouble() - .5, Y = Rnd.NextDouble() - .5};
                }
                else
                {
                    _translateTransform = new TranslateTransform();
                }

                var tg = new TransformGroup();
                tg.Children.Add(_translateTransform);
                tg.Children.Add(_scaleTransform);

                RenderTransform = tg;

                Animate.AnimateDouble(this, "(Opacity)", .5, 1.0, ShowDuration);
                Animate.AnimateScale(this, .5, 1.0, ShowDuration);
                //new Animations(this, 24, .5, 1.0).startAnimation();                
            }

            private void GraphContentPresenter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                //Btn_Click(this, e);
            }

            private void GraphContentPresenter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                Btn_Click(this, e);
                //if (onMouseLeftButtonDown != null) onMouseLeftButtonDown(this, e);
            }

            private void Btn_Click(object sender, RoutedEventArgs e)
            {
                if (onMouseEvent != null) onMouseEvent(sender, e);
            }

            #region Delegates

            public delegate void MouseEvent(object sender, RoutedEventArgs e);

            public static event EventHandler<MouseButtonEventArgs> onMouseLeftButtonDown;

            public static event MouseEvent onMouseEvent;

            #endregion

            #region Overrides

            protected override Size MeasureOverride(Size constraint)
            {
                _actualDesiredSize = base.MeasureOverride(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return new Size();
            }

            protected override Size ArrangeOverride(Size arrangeSize)
            {
                if (!upd_call)
                {
                    _actualRenderSize = base.ArrangeOverride(_actualDesiredSize);

                    _scaleTransform.CenterX = _actualRenderSize.Width/2;
                    _scaleTransform.CenterY = _actualRenderSize.Height/2;

                    _centerVector.X = -_actualRenderSize.Width/2;
                    _centerVector.Y = -_actualRenderSize.Height/2;

                    updateTransform();
                }
                else
                {
                    upd_call = false;
                }
                return new Size();
            }

            #endregion

            #region Properties

            private volatile bool upd_call;

            public new object Content
            {
                get { return ((base.Content as Border).Child as ContentPresenter).Content; }
                set { ((base.Content as Border).Child as ContentPresenter).Content = value; }
            }

            public Point Location
            {
                get { return _location; }
                set
                {
                    if (_location != value)
                    {
                        _location = value;
                        updateTransform();
                    }
                }
            }

            public Point ParentCenter
            {
                get { return _parentCenter; }
                set
                {
                    if (_parentCenter != value)
                    {
                        _parentCenter = value;
                        updateTransform();
                    }
                }
            }

            public Point ActualLocation
            {
                get { return new Point(_location.X + _parentCenter.X, _location.Y + _parentCenter.Y); }
            }

            public double Y
            {
                get { return (double) GetValue(Canvas.TopProperty); }
                set { SetValue(Canvas.TopProperty, value); }
            }

            public double X
            {
                get { return (double) GetValue(Canvas.LeftProperty); }
                set { SetValue(Canvas.LeftProperty, value); }
            }

            public Point Center
            {
                get
                {
                    return new Point(_location.X + _parentCenter.X + _actualDesiredSize.Width/2
                                     , _location.Y + _parentCenter.Y + _actualDesiredSize.Height/2
                        );
                }
            }

            private void updateTransform()
            {
                double X = _centerVector.X + _location.X + _parentCenter.X;
                double Y = _centerVector.Y + _location.Y + _parentCenter.Y;

                //_translateTransform.X = _centerVector.X + _location.X + _parentCenter.X;
                //_translateTransform.Y = _centerVector.Y + _location.Y + _parentCenter.Y;

                upd_call = true;
                Arrange(new Rect(X, Y, _actualRenderSize.Width, _actualRenderSize.Height));
            }

            #endregion

            #region Members

            private readonly TranslateTransform _translateTransform;
            public bool New = true;
            public Vector Velocity;
            public bool WasCenter = false;
            private Size _actualDesiredSize;
            private Size _actualRenderSize;
            private Vector _centerVector;
            private Point _location;
            private Point _parentCenter;
            public ScaleTransform _scaleTransform;

            #endregion
        }

        private class NodeCanvas : Canvas
        {
            public NodeState NodeState;
        }

        /// <summary>
        ///     Helper class to store visual and physical description for the nodes.
        /// </summary>
        private class NodeState
        {
            public readonly List<KeyValuePair<Node, Line>> ChildLinks = new List<KeyValuePair<Node, Line>>();
            public NodeCanvas NodeCanvas;
            public Point Position;
            public Point Velocity;
        }

        #region Static Stuff

        private const double TerminalVelocity = 150;
        private const double MinVelocity = .05;

        private const double MinCOD = .01, MaxCOD = .99;
        private static readonly Vector VerticalVector = new Vector(0, 1);
        private static readonly Vector HorizontalVector = new Vector(1, 0);
        private static readonly Duration HideDuration = new Duration(new TimeSpan(0, 0, 1));
        private static readonly Duration ShowDuration = new Duration(new TimeSpan(0, 0, 0, 0, 500));

        private static readonly TimeSpan MaxSettleTime = new TimeSpan(0, 0, 8);
        private static readonly TimeSpan MaxExpTime = new TimeSpan(0, 1, 0);

        //private const double MinCOD = .001, MaxCOD = .999;

        private static readonly Rect EmptyRect = new Rect();

        private static Binding GetBinding(string bindingPath, object source)
        {
            Binding newBinding = null;
            try
            {
                newBinding = new Binding(bindingPath);
                newBinding.Source = source;
                newBinding.Mode = BindingMode.OneWay;
            }
            catch (InvalidOperationException)
            {
            }
            return newBinding;
        }

        private static PropertyPath ClonePropertyPath(PropertyPath path)
        {
            return new PropertyPath(path.Path, null);
        }

        #region static helpers

        private static readonly Random Rnd = new Random();

        private static Vector GetVectorSum(int itemIndex, int itemCount, Vector[,] vectors)
        {
            Debug.Assert(itemIndex >= 0);
            Debug.Assert(itemIndex < itemCount);

            var vector = new Vector();

            for (int i = 0; i < itemCount; i++)
            {
                if (i != itemIndex)
                {
                    vector += GetVector(itemIndex, i, itemCount, vectors);
                }
            }

            return vector;
        }

        private static Vector GetVector(int a, int b, int count, Vector[,] vectors)
        {
            Debug.Assert(a != b);
            if (a < b)
            {
                return vectors[a, b];
            }
            else
            {
                return -vectors[b, a];
            }
        }

        private static Point GetRandomPoint(Size range)
        {
            return new Point(Rnd.NextDouble()*range.Width, Rnd.NextDouble()*range.Height);
        }

        private static Rect GetCenteredRect(Size elementSize, Point center)
        {
            double x = center.X - elementSize.Width/2;
            double y = center.Y - elementSize.Height/2;

            return new Rect(x, y, elementSize.Width, elementSize.Height);
        }

        private static Vector GetSpringForce(Vector x)
        {
            var force = new Vector();
            //negative is attraction
            force += GetAttractionForce(x);
            //positive is repulsion
            force += GetRepulsiveForce(x);

            Debug.Assert(IsGoodVector(force));

            return force;
        }

        private static Vector GetAttractionForce(Vector x)
        {
            Vector force = -.2*Normalize(x)*x.Length;
            Debug.Assert(IsGoodVector(force));
            return force;
        }

        private static Vector GetRepulsiveForce(Vector x)
        {
            Vector force = .1*Normalize(x)/Math.Pow(x.Length/1000, 2);
            Debug.Assert(IsGoodVector(force));
            return force;
        }

        private static Vector Normalize(Vector v)
        {
            v.Normalize();
            Debug.Assert(IsGoodVector(v));
            return v;
        }

        private static Vector GetWallForce(Size area, Point location)
        {
            var force = new Vector();
            force += (VerticalVector*GetForce(-location.Y - area.Height/2));
            force += (-VerticalVector*GetForce(location.Y - area.Height/2));

            force += (HorizontalVector*GetForce(-location.X - area.Width/2));
            force += (-HorizontalVector*GetForce(location.X - area.Width/2));

            force *= 1000;
            return force;
        }

        private static double GetForce(double x)
        {
            return GetSCurve((x + 100)/200);
        }

        private static bool IsGoodDouble(double d)
        {
            return !double.IsNaN(d) && !double.IsInfinity(d);
        }

        private static bool IsGoodVector(Vector v)
        {
            return IsGoodDouble(v.X) && IsGoodDouble(v.Y);
        }

        #region math

        private static double GetSCurve(double x)
        {
            return 0.5 + Math.Sin(Math.Abs(x*(Math.PI/2)) - Math.Abs((x*(Math.PI/2)) - (Math.PI/2)))/2;
        }

        #endregion

        #endregion

        #endregion

        #region GraficMembers

        public static AnimationManager anim_mng;
        private readonly Graph _graph;
        private readonly Dictionary<Node, NodeState> _nodeState;
        private readonly DispatcherTimer _timer;
        private Panel _canvas;
        private Point _center;
        private Dictionary<GraphContentPresenter, GCPDragState> _gcpState;
        private Canvas _topcanvas;

        #endregion
    }
}