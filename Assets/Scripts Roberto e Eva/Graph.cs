using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    private Node[] allNodes;
    public Node[] AllNodes { get => allNodes; set => allNodes = value; }

    /// <summary>
    /// The Dijkstra algorithm, it returns a array of connections wich has the best path to achieve the goal,
    /// if cannot achieve the goal it returns null
    /// </summary>
    /// <param name="start">Initial node</param>
    /// <param name="goal">Final node</param>
    /// <returns></returns>
    public Connection[] Dijsktra(Node start, Node goal)
    {
        //list of nodes available to be evaluated
        List<Node> open = new List<Node>();

        //list of nodes already evaluated
        List<Node> closed = new List<Node>();

        //setup initial node with cost 0
        start.CostSoFar = 0;
        start.FromConnection = null;

        //add initial node to open list
        open.Add(start);

        //current is the connection being evaluated
        Node current = null;
        while (open.Count != 0)
        {
            //get the connection wich has the light weight
            current = GetMinCostNode(open);

            //if achieve the goal breaks the loop
            if (current == goal) break;

            //loop through the current node connections
            foreach (var connection in current.Connections)
            {
                //calculate cost to connection
                float toNodeCost = current.CostSoFar + connection.Cost;

                //skip this connection if it is already closed
                if (closed.Contains(connection.ToNode))
                    continue;
                
                if (open.Contains(connection.ToNode))
                {
                    //if this connection already has a better path to it
                    if (connection.ToNode.CostSoFar <= toNodeCost)
                        continue;
                }
                else
                {
                    open.Add(connection.ToNode);
                }

                //if it's the best path to this node, set connection to it
                connection.ToNode.CostSoFar = toNodeCost;
                connection.ToNode.FromConnection = connection;
                
            }

            //now, that all node connections is already evaluated, remove the node from
            //open list and add to closed ones
            open.Remove(current);
            closed.Add(current);
        }

        //if cannot achieve the goal return null
        if (current != goal)
        {
            return null;
        }
        else
        {
            //generate the path connections from the end node to start node
            List<Connection> path = new List<Connection>();
            while (current != start)
            {
                path.Add(current.FromConnection);
                current = current.FromConnection.FromNode;
            }

            //this list will be in wrong order, so it will need to be reversed
            path.Reverse();

            //return the connections list
            return path.ToArray();
        }
    }

    private Node GetMinCostNode(IEnumerable<Node> nodes)
    {
        if (nodes == null)
            return null;

        float minCost = float.MaxValue;
        Node minNode = null;
        foreach (var node in nodes)
        {
            if (node.CostSoFar < minCost)
            {
                minNode = node;
                minCost = node.CostSoFar;
            }
        }

        return minNode;
    }
}
