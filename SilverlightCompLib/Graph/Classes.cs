#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using File = SilverlightCompLib.Mathematics.File;

#endregion

namespace SilverlightCompLib.Graph
{
    public class GraphReader
    {
        /// <summary>
        ///     Creates a graph by interpreting a specially formatted text file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>The graph created.</returns>
        public static Graph BuildGraph(Stream s)
        {
            string[] graphLines = File.ReadAllLines(s);
            return BuildGraph(graphLines);
        }

        /// <summary>
        ///     Builds a graph represented by the string array encoding.
        /// </summary>
        /// <param name="serverNodes">String array from the graph API.</param>
        /// <returns>The graph as represented by local the graph library.</returns>
        public static Graph BuildGraph(string[] serverNodes)
        {
            /* Instantiate our lightweight graph library */
            var g = new Graph();

            var nodeDict = new Dictionary<long, Node>();

            // First pass, get all the nodes
            foreach (string node in serverNodes)
            {
                string[] nodeData = node.Split('/');

                if (nodeData.Length < 2) continue;

                var n = new Node
                    {
                        Title = nodeData[1]
                    };

                if (nodeData.Length <= 4)
                {
                    n.Type = Node.NodeType.Fact;
                }
                else if (nodeData[4] == "Fact")
                {
                    n.Type = Node.NodeType.Fact;
                }
                else
                {
                    n.Type = Node.NodeType.Projection;
                }

                nodeDict.Add(long.Parse(nodeData[0]), n);

                g.Nodes.Add(n);
            }

            // Second pass, get the edges
            foreach (string node in serverNodes)
            {
                string[] nodeData = node.Split('/');

                long nodeId = long.Parse(nodeData[0]);

                if (nodeData.Length > 2)
                {
                    string[] nodeChildren = nodeData[2].Split(',');
                    foreach (string child in nodeChildren)
                    {
                        if (child.Length == 0) continue;
                        g.Edges[nodeDict[nodeId], nodeDict[long.Parse(child)]] = true;
                    }
                }
            }

            return g;
        }

        /// <summary>
        ///     For testing purposes: Builds a random graph with N nodes.
        /// </summary>
        /// <param name="n">The number of nodes and random edges.</param>
        /// <returns>The random graph as represented by the local graph library.</returns>
        public static Graph BuildGraph(int N)
        {
            var g = new Graph();

            var allNodes = new List<Node>();

            /* Create N nodes */
            for (int i = 0; i < 3*N; i++)
            {
                var n = new Node {Type = Node.NodeType.Projection, Title = i.ToString()};
                allNodes.Add(n);
                g.Nodes.Add(n);
            }

            // Add N Random Edges ------
            var r = new Random();
            for (int i = 0; i < 0; i++)
            {
                int dest = r.Next(allNodes.Count);
                if (dest == i) continue;
                if (!g.Nodes.Children(allNodes[i]).Contains(allNodes[dest]))
                    g.Edges[allNodes[i], allNodes[dest]] = true;
            }

            for (int i = 1; i < N; i++)
            {
                g.Edges[allNodes[0], allNodes[i]] = true;
            }


            for (int i = N; i < 2*N; i++)
            {
                g.Edges[allNodes[N - 1], allNodes[i]] = true;
            }

            for (int i = 2*N; i < 3*N; i++)
            {
                g.Edges[allNodes[2*N - 1], allNodes[i]] = true;
            }

            return g;
        }
    }

    public class Node
    {
        public enum NodeType
        {
            Fact,
            Projection
        }

        public string Title;
        public NodeType Type;
    }

    public class Graph
    {
        public GraphEdges Edges;
        public GraphNodes Nodes;
        protected Dictionary<Node, List<Node>> _children = new Dictionary<Node, List<Node>>();
        protected Dictionary<Node, List<Node>> _parents = new Dictionary<Node, List<Node>>();

        public Graph()
        {
            Edges = new GraphEdges(this);
            Nodes = new GraphNodes(this);
        }

        protected IList<Node> GetEdges(Node a, Dictionary<Node, List<Node>> adjacent)
        {
            List<Node> adj = null;
            if (adjacent.TryGetValue(a, out adj))
                return adj.AsReadOnly();
            else
                return new List<Node>(0).AsReadOnly();
        }

        protected void SetEdge(Node parent, Node child, bool value)
        {
            if (!Nodes.Contains(parent) || !Nodes.Contains(child))
                throw new Exception("One or both of the nodes attached to the edge is not contained in the graph.");

            List<Node> children = SetDirectedEdge(parent, child, _children, value);
            List<Node> parents = SetDirectedEdge(child, parent, _parents, value);
        }

        /// <summary>
        ///     Sets the status of the edge between note A and B and returns the updated adjacency list for node A.
        /// </summary>
        private List<Node> SetDirectedEdge(Node a, Node b, Dictionary<Node, List<Node>> adjacent, bool value)
        {
            List<Node> adj = null;

            if (adjacent.TryGetValue(a, out adj))
            {
                if (value == false)
                {
                    if (!adj.Remove(b))
                        throw new Exception("Edge specified for removal didn't exist.");

                    if (adj.Count == 0) // If this is the last item in the list ...
                    {
                        adjacent.Remove(a); // Remove the list itself
                        return adj;
                    }
                }
                else
                {
                    if (adj.Contains(b))
                        throw new Exception("Edge specified for addition already existed.");
                    else
                        adj.Add(b);
                }
            }
            else
            {
                if (value == false)
                {
                    throw new Exception("Edge specified for removal didn't exist.");
                }
                else // Need an edge where adj list doesn't exist -- so create both the list & edge
                {
                    adj = new List<Node>(new[] {b});
                    adjacent[a] = adj;
                }
            }

            return adj;
        }

        public class GraphEdges
        {
            private readonly Graph _parentGraph;

            public GraphEdges(Graph parentGraph)
            {
                _parentGraph = parentGraph;
            }

            public bool this[Node parent, Node child]
            {
                set { _parentGraph.SetEdge(parent, child, value); }
            }
        }

        public class GraphNodes : IEnumerable<Node>
        {
            private readonly Dictionary<Node, bool> _nodeSet;
            private readonly Graph _parentGraph;

            public GraphNodes(Graph parentGraph)
            {
                _parentGraph = parentGraph;
                _nodeSet = new Dictionary<Node, bool>();
            }

            public bool Contains(Node node)
            {
                return _nodeSet.ContainsKey(node);
            }

            public IList<Node> Children(Node ofNode)
            {
                return _parentGraph.GetEdges(ofNode, _parentGraph._children);
            }

            public IList<Node> Parents(Node ofNode)
            {
                return _parentGraph.GetEdges(ofNode, _parentGraph._parents);
            }

            public void Add(Node node)
            {
                if (Contains(node))
                    throw new Exception("The graph already contains the node specified for addition.");
                else
                    _nodeSet[node] = true;
            }

            public void Remove(Node node)
            {
                if (Parents(node).Count > 0 || Children(node).Count > 0)
                    throw new Exception("The node cannot be removed because it still has connected edges.");

                if (!_nodeSet.Remove(node))
                    throw new Exception("The graph does not contain the node specified for removal.");
            }

            #region IEnumerable<Node> Members

            public IEnumerator<Node> GetEnumerator()
            {
                return _nodeSet.Keys.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }
    }
}