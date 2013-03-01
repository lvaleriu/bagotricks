using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Threading;
using System.Threading;
using System.Collections.Specialized;
using System.Windows;


namespace BagOTricksGraph
{
    internal class Node<T> : Object where T : IComparable<T>, IEquatable<T>
    {
        internal Node(T item, NodeCollection<T> parent)
        {
            if (item == null || parent == null)
            {
                throw new ArgumentNullException();
            }
            _item = item;
            _parent = parent;
        }
        public ReadOnlyCollection<Node<T>> ChildNodes
        {
            get
            {                
                VerifyAccess();
                if (_children == null)
                {
                    Debug.Assert(_roChildren == null);
                    _parent.NodeChildrenChanged += new NodeChildrenChangedHandler<T>(handler);

                    _children = new ObservableCollection<Node<T>>();
                    _parent.GetChildren(this._item).ForEach(nod => _children.Add(nod));                    

                    _roChildren = new ReadOnlyCollection<Node<T>>(_children);
                }
                return _roChildren;
            }
        }

        public override bool Equals(object obj)
        {
            return Item.Equals((obj as Node<T>).Item);
 	        //return base.Equals(obj);
        }
        private void VerifyAccess()
        {
            
        }
                
        public T Item { get { return _item; } }

        public override string ToString()
        {
            //return "Node - '" + _item.ToString() + "'";
            return _item.ToString();
        }

        private void handler(NodeChildrenChangedArgs<T> args)
        {
            if (args.Parent.Equals(this._item))
            {
                Debug.Assert(args.Child != null && !args.Parent.Equals(args.Child));

                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    Debug.Assert(!_children.Contains(_parent.GetNode(args.Child)));
                    _children.Add(_parent.GetNode(args.Child));
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    Debug.Assert(_children.Contains(_parent.GetNode(args.Child)));
                    _children.Remove(_parent.GetNode(args.Child));
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                Debug.Assert(args.Parent != null);
            }
        }
        
        T _item;
        NodeCollection<T> _parent;
        ObservableCollection<Node<T>> _children;
        ReadOnlyCollection<Node<T>> _roChildren;
    }

    internal class NodeChildrenChangedArgs<T>
    {
        public NodeChildrenChangedArgs(T parent, T child, NotifyCollectionChangedAction action)
        {
            if (parent == null || child == null)
            {
                throw new ArgumentException();
            }
            if (!(action == NotifyCollectionChangedAction.Add || action == NotifyCollectionChangedAction.Remove))
            {
                throw new ArgumentException();
            }

            Parent = parent;
            Child = child;
            Action = action;
        }

        public T Parent;
        public T Child;
        public NotifyCollectionChangedAction Action;
    }

    internal delegate void NodeChildrenChangedHandler<T>(NodeChildrenChangedArgs<T> nodeChildrenChanged);

    internal class NodeCollection<T> : Object where T : IComparable<T>, IEquatable<T>
    {
        public NodeCollection()
        {
            _isReading = false;
            _nodes = new Dictionary<T, Node<T>>();
            _nodeConnections = new Dictionary<T, Dictionary<T, object>>();
        }

        public ReadOnlyCollection<T> Items
        {
            get
            {
                VerifyAccess();
                _isReading = true;
                if ((_items == null) || (_items.Count != _nodeConnections.Count))
                {
                    T[] items = new T[_nodeConnections.Count];
                    int i = 0;
                    foreach (T item in _nodeConnections.Keys)
                    {
                        items[i] = item;
                        i++;
                    }
                    _items = new ReadOnlyCollection<T>(items);
                }

                _isReading = false;

                return _items;
            }
        }

        public void AddNode(T node)
        {
            if (_isReading)
            {
                //throw new Exception();
            }
            VerifyAccess();

            if (_nodeConnections.ContainsKey(node))
            {
                //throw new ArgumentException();
            }
            else
            {
                _nodeConnections.Add(node, new Dictionary<T, object>());
            }            
        }

        public Node<T> GetNode(T node)
        {            
            VerifyAccess();
            _isReading = true;

            Node<T> res = null;

            if (!_nodes.ContainsKey(node))
            {
                if (!_nodeConnections.ContainsKey(node))
                {
                    throw new ArgumentException();
                }
                else
                {
                    _nodes[node] = new Node<T>(node, this);
                }
            }
            res = _nodes[node];

            _isReading = false;

            return res;
        }

        public void MoreFriends(T node)
        {
            if (!_nodeConnections.ContainsKey(node))
            {
                throw new ArgumentException();
            }
            else
            {
                Dictionary<T, object> connections = _nodeConnections[node];
                if (connections.Count < (_nodeConnections.Count - 1))
                {
                    foreach (T newNode in _nodeConnections.Keys)
                    {
                        if (!newNode.Equals(node))
                        {
                            if (!connections.ContainsKey(newNode))
                            {
                                AddEdge(node, newNode);
                                break;
                            }
                        }
                    }

                }
            }
        }

        public void LessFriends(T node)
        {
            if (!_nodeConnections.ContainsKey(node))
            {
                throw new ArgumentException();
            }
            else
            {
                Dictionary<T, object> connections = _nodeConnections[node];
                if (connections.Count > MinConnections)
                {
                    foreach (T childNode in connections.Keys)
                    {
                        RemoveEdge(node, childNode);
                        break;
                    }
                }
            }
        }

        public bool ContainsEdge(T node1, T node2)
        {
            return _nodeConnections[node2].ContainsKey(node1);
        }
        public void AddEdge(T node1, T node2)
        {
            VerifyAccess();
            if (node1.Equals(node2))
            {
                Debug.Assert(!_nodeConnections[node1].ContainsKey(node1));
                Debug.Assert(!_nodeConnections[node2].ContainsKey(node2));

                //throw new ArgumentException("One cannot create an edge between the same node.");
            }
            if (_nodeConnections[node1].ContainsKey(node2))
            {
                Debug.Assert(_nodeConnections[node2].ContainsKey(node1));
                //throw new ArgumentException("This edge already exists.");
            }
            else
            {
                Debug.Assert(!_nodeConnections[node2].ContainsKey(node1));

                _nodeConnections[node1].Add(node2, null);
                _nodeConnections[node2].Add(node1, null);

                OnNodeChildrenChanged(new NodeChildrenChangedArgs<T>(node1, node2, NotifyCollectionChangedAction.Add));
                OnNodeChildrenChanged(new NodeChildrenChangedArgs<T>(node2, node1, NotifyCollectionChangedAction.Add));
            }
        }

        public void RemoveEdge(T node1, T node2)
        {
            VerifyAccess();
            if (node1.Equals(node2))
            {
                Debug.Assert(!_nodeConnections[node1].ContainsKey(node1));
                Debug.Assert(!_nodeConnections[node2].ContainsKey(node2));

                throw new ArgumentException("One cannot create an edge between the same node.");
            }
            if (!_nodeConnections[node1].ContainsKey(node2))
            {
                Debug.Assert(!_nodeConnections[node2].ContainsKey(node1));
                throw new ArgumentException("This edge does not exist");
            }
            else
            {
                Debug.Assert(_nodeConnections[node2].ContainsKey(node1));

                _nodeConnections[node1].Remove(node2);
                _nodeConnections[node2].Remove(node1);

                OnNodeChildrenChanged(
                    new NodeChildrenChangedArgs<T>(node1, node2, NotifyCollectionChangedAction.Remove));
                OnNodeChildrenChanged(
                    new NodeChildrenChangedArgs<T>(node2, node1, NotifyCollectionChangedAction.Remove));
            }
        }

        internal void churn()
        {
            VerifyAccess();
            if (_nodeConnections.Count < 3)
            {
                throw new Exception("need at least three nodes to play");
            }
            //step one: pick a node to play with
            int itemIndex = Rnd.Next(_nodeConnections.Count);
            T item = this.Items[itemIndex];

            //stop two: pick something to do. Either add a node or remove a node.
            bool? doAdd = null;
            if (_nodeConnections[item].Count < MinConnections)
            {
                doAdd = true;
            }
            else
            {
                double currentRatio = _nodeConnections[item].Count / (double)(_nodeConnections.Count - 1);
                Debug.Assert(currentRatio <= 1);
                Debug.Assert(currentRatio > 0);
                if (currentRatio <= IdealConnectionRatio)
                {
                    doAdd = true;
                }
                else
                {
                    doAdd = false;
                }
            }

            if (doAdd == true)
            {
                if (_nodeConnections[item].Count == (_nodeConnections.Count - 1))
                {
                    throw new Exception("this should never happen...");
                }
                int indexToAdd = Rnd.Next(_nodeConnections.Count - _nodeConnections[item].Count);
                Debug.Assert(indexToAdd < _nodeConnections.Count);
                Debug.Assert(indexToAdd >= 0);
                Debug.Assert(indexToAdd < (_nodeConnections.Count - (_nodeConnections[item].Count - 1)));

                foreach (T child in _nodeConnections.Keys)
                {
                    IEquatable<T> foo = child;
                    if (!foo.Equals(item))
                    {
                        if (indexToAdd == 0 && !_nodeConnections[item].ContainsKey(child))
                        {
                            AddEdge(item, child);
                            break;
                        }
                        else
                        {
                            indexToAdd--;
                        }
                    }
                }
            }
            else if (doAdd == false)
            {
                if (_nodeConnections[item].Count < 1)
                {
                    throw new Exception("this should never happen");
                }
                int indexToRemove = Rnd.Next(_nodeConnections[item].Count);
                foreach (T child in _nodeConnections[item].Keys)
                {
                    if (indexToRemove == 0)
                    {
                        RemoveEdge(item, child);
                        break;
                    }
                    else
                    {
                        indexToRemove--;
                    }
                }
            }
            else
            {
                throw new Exception("should never be null");
            }

#if DEBUG
            validateConnections();
#endif
        }

        internal List<Node<T>> GetChildren(T item)
        {
            VerifyAccess();
            if (!_nodeConnections.ContainsKey(item))
            {
                throw new ArgumentException();
            }
            Dictionary<T, object> children = _nodeConnections[item];
            List<Node<T>> cArray = new List<Node<T>>(children.Count);
            foreach (T child in children.Keys)
            {
                cArray.Add(GetNode(child));
            }
            return cArray;
        }

        protected void OnNodeChildrenChanged(NodeChildrenChangedArgs<T> args)
        {
            if (NodeChildrenChanged != null) NodeChildrenChanged(args);
        }
        public event NodeChildrenChangedHandler<T> NodeChildrenChanged;

        private void validateConnections()
        {
            VerifyAccess();
            Dictionary<T, object> _verified = new Dictionary<T, object>();
            foreach (T item in _nodeConnections.Keys)
            {
                foreach (T connection in _nodeConnections[item].Keys)
                {
                    if (!_verified.ContainsKey(connection))
                    {
                        Debug.Assert(_nodeConnections[connection].ContainsKey(item));
                    }
                }
                _verified.Add(item, null);
            }
        }

        private void VerifyAccess()
        {
            
        }

        private Dictionary<T, Node<T>> _nodes;
        private Dictionary<T, Dictionary<T, object>> _nodeConnections;
        private ReadOnlyCollection<T> _items;

        private bool _isReading;
        private static object readingLock = new object();

        private static void checkDupe<U>(U node1, U node2) where U : IEquatable<U>, IComparable<U>
        {
            if (node1.Equals(node2))
            {
                throw new ArgumentException();
            }
            Debug.Assert(!node2.Equals(node1));
            Debug.Assert(node1.CompareTo(node2) != 0);
            Debug.Assert(node2.CompareTo(node1) != 0);
            Debug.Assert(node1.CompareTo(node2) == -node2.CompareTo(node1));
        }

        private const int MinConnections = 2;
        private const double IdealConnectionRatio = .4;
        private readonly Random Rnd = new Random();

    }
}