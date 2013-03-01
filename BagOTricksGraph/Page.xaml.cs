using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SilverlightCompLib.Graph;
using System.Collections.ObjectModel;

namespace BagOTricksGraph
{
    public partial class Page : UserControl
    {
        private Graphic _v;
        private NodeCollection<string> _nodes = new NodeCollection<string>();
        private event EventHandler<EventArgs> onLoadData;

        private object readingLock = new object();
        private Dictionary<string, Vertex> grp = new Dictionary<string, Vertex>();
        private Dictionary<string, LinkedList<Vertex>> DictL = new Dictionary<string, LinkedList<Vertex>>();

        public class Vertex
        {
            public string Word;
            public List<Vertex> Neigh = new List<Vertex>();
        }

        public Page()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Page_Loaded);
            this.SizeChanged += new SizeChangedEventHandler(Page_SizeChanged);
            this.KeyDown += new KeyEventHandler(Page_KeyDown);
            this.onLoadData += new EventHandler<EventArgs>(Page_onLoadData);
        }

        void Page_onLoadData(object sender, EventArgs e)
        {
            
        }

        void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    System.Windows.Interop.Content content = App.Current.Host.Content;
                    content.IsFullScreen = !content.IsFullScreen;
                    break;
            }            
        }

        void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_v == null) return;
            double h = e.NewSize.Height;
            if (h <= 40) return;
            _v.Height = e.NewSize.Height;
            _v.Width = e.NewSize.Width;
            _v.UpdateLayout();
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            double h = Application.Current.Host.Content.ActualHeight;
            double w = Application.Current.Host.Content.ActualWidth;
            _v = new Graphic(new Size(w, h));
            _v.onMouseEvent += new Graphic.MouseEvent(_v_onMouseEvent);
            _v.onMovingStoped += new EventHandler<EventArgs>(_v_onMovingStoped);

            _v.NodesBindingPath = "ChildNodes";
            _v.Nodes = new ObservableCollection<Node<string>>();

            InitData(50);

            // Add the result to the current screen //
            this.LayoutRoot.Children.Add(_v);
           
            UpdateCenterEdges(_nodes.Items[0]);
        }
        void InitData(int max)
        {
            lock (readingLock)
            {
                for (int i = 0; i < max; i++)
                {
                    _nodes.AddNode(i.ToString());

                    Vertex v, v2;

                    if (grp.ContainsKey(i.ToString()))
                    {
                        v = grp[i.ToString()];
                    }
                    else
                    {
                        v = new Vertex() { Word = i.ToString() };
                        grp[v.Word] = v;
                    }

                    for (int j = 0; j < max && j != i; j++)
                    {
                        if (grp.ContainsKey(j.ToString()))
                        {
                            v2 = grp[j.ToString()];
                        }
                        else
                        {
                            v2 = new Vertex() { Word = j.ToString() };
                            grp[v2.Word] = v2;
                        }
                        if (!v.Neigh.Contains(v2)) v.Neigh.Add(v2);
                        if (!v2.Neigh.Contains(v)) v2.Neigh.Add(v);
                    }
                }
            }
        }
        void _v_onMovingStoped(object sender, EventArgs e)
        {
            
        }

        Random r = new Random();
        void _v_onMouseEvent(object sender, RoutedEventArgs e)
        {
            object obj = (sender as SilverlightCompLib.Graph.Graphic.GraphContentPresenter).Content;
            Node<string> center = _nodes.GetNode((obj as Node<string>).Item);

            UpdateCenterEdges(center.Item);
        }

        private void UpdateCenterEdges(string item)
        {
            int ItemsCount = (_nodes == null) ? 0 : _nodes.Items.Count;
            if (ItemsCount == 0) return;

            //if (CheckAccess())
            if (true)
            {
                Node<string> oldnode = _v.CenterObject as Node<String>;
                List<Node<String>> OldVertices = new List<Node<string>>();
                if (oldnode != null)
                {
                    OldVertices = _nodes.GetChildren(oldnode.Item);
#if OldVer
                    OldVertices.ForEach(el => _v.Nodes.Remove(el));
#endif
                }

                Node<string> node = _nodes.GetNode(item);
                _v.CenterObject = node;
                if (node != null)
                {
                    lock (readingLock)
                    {
                        List<Node<String>> Vertices = _nodes.GetChildren(node.Item);
                        Vertices.ForEach(el => _nodes.RemoveEdge(node.Item, el.Item));

                        List<Node<String>> LstNGD = new List<Node<string>>();

                        grp[node.Item].Neigh.ForEach(Vois =>
                        {
                            if (_nodes.Items.Contains(Vois.Word))
                            {
                                Node<string> newNode = _nodes.GetNode(Vois.Word);

                                if (!node.ChildNodes.Contains(newNode))
                                {
#if OldVer
                                    _nodes.AddEdge(node.Item, newNode.Item);
                                    _v.Nodes.Add(newNode);
#endif
                                    LstNGD.Add(newNode);
                                }
                            }
                        });
#if OldVer
#else
                        var newlist = LstNGD.Where(el => true).ToList();
                        OldVertices.ForEach(el =>
                        {
                            if (!LstNGD.Remove(el))
                                _v.Nodes.Remove(el);
                        });

                        newlist.ForEach(el =>
                        {
#if DEBUG1
                                Debug.Assert(!_nodes.ContainsEdge(node.Item, el.Item));
#endif
                            if (!_nodes.ContainsEdge(node.Item, el.Item))
                            {
                                _nodes.AddEdge(node.Item, el.Item);
                                if (LstNGD.Contains(el) && !_v.Nodes.Contains(el))
                                {
                                    _v.Nodes.Add(el);
                                }//Recyclage des centres fait apparaitre 2 fois le meme noeud dans les voisins
                            }
                        });
#endif
                    }   //Lock_reading
                }
                if (onLoadData != null) onLoadData(node.Item, null);
            }
        }        
    }
}
