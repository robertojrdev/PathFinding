using Completed;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField] private float euristicsStrenght = 10f;

    private float walkCost = 1;
    private float foodCost = -1;
    private float sodaCost = -3;

    private List<TileNode> Nodes = new List<TileNode>();
    private Connection[] bestPath;
    private Player player;
    private BoardManager board;
    private GameManager manager;

    private bool isPathFindUpdated = false;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        board = FindObjectOfType<BoardManager>();
        manager = FindObjectOfType<GameManager>();

        //set food cost (+1 because of the movement cost)
        foodCost = -player.pointsPerFood + 1;
        sodaCost = -player.pointsPerSoda + 1;

        DoPathFinding();
    }

    private void Update()
    {
        if (!manager.playersTurn || manager.enemiesMoving || manager.doingSetup)
        {
            isPathFindUpdated = false;
            return;
        }

        if (!isPathFindUpdated)
        {
            DoPathFinding();

            //move player
            if (bestPath.Length > 0)
            {
                Vector2 direction = ((TileNode)bestPath[0].ToNode).position - player.transform.position;
                player.Move(Vector2Int.CeilToInt(direction));
            }
        }
    }

    private void DoPathFinding()
    {
        GenerateNodes(board);
        GenerateConnections();

        //create new graph and pass nodes to it
        Graph graph = new Graph();
        graph.AllNodes = Nodes.ToArray();
        //find current player node
        TileNode playerNode = Nodes.Find(x => x.position == player.transform.position);
        //use dijkstra algorithm
        bestPath = graph.Dijsktra(playerNode, Nodes[Nodes.Count - 1]);

        //it lock the update till next player movement and then call DoPathFinding again
        isPathFindUpdated = true;
    }

    private void GenerateNodes(BoardManager board)
    {
        //clear nodes
        Nodes.Clear();

        //generate all walk positions
        for (int x = 0; x < board.columns; x++)
        {
            for (int y = 0; y < board.rows; y++)
            {
                //float maxDist = Vector2.Distance(Vector2.zero, new Vector2(board.columns, board.rows));
                //float dist = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                //float aditionalCost = 0;
                //if (player.food != 0)
                //    aditionalCost = (Mathf.Sqrt(maxDist - dist) / (player.food)) * euristicsStrenght; //just a try :D
                //Debug.Log(aditionalCost);
                //Nodes.Add(new TileNode(new Vector3(x, y, 0), walkCost + aditionalCost));
                Nodes.Add(new TileNode(new Vector3(x, y, 0), 1));
            }
        }

        //add walls
        Wall[] walls = FindObjectsOfType<Wall>();
        for (int i = 0; i < walls.Length; i++)
        {
            TileNode node = Nodes.Find(x => x.position == walls[i].transform.position);
            if (node != null)
            {
                node.weight += walls[i].hp;
            }
        }

        //add soda
        Vector3[] sodas = GameObject.FindGameObjectsWithTag("Soda").Select(x => x.transform.position).ToArray();
        foreach (var soda in sodas)
        {
            TileNode node = Nodes.Find(x => x.position == soda);
            if (node != null)
            {
                node.weight = sodaCost;
            }
        }

        //add food
        Vector3[] foods = GameObject.FindGameObjectsWithTag("Food").Select(x => x.transform.position).ToArray();
        foreach (var food in foods)
        {
            TileNode node = Nodes.Find(x => x.position == food);
            if (node != null)
            {
                node.weight = foodCost;
            }
        }

        //add enemies - already assign the cost because it change along the game
        //it need to be the last one to be evaluated because it overlay food
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            TileNode node = Nodes.Find(x => x.position == enemies[i].transform.position);
            if (node != null)
            {
                node.weight = float.MaxValue;

                ////find it neighbors to player avoid crossing the path of the enemy
                //float nodeDistance = 4;
                //List<TileNode> neighborhood = GetNeighborNodes(node, nodeDistance); //get the neighbors including in diagonal
                //foreach (var neighbor in neighborhood)
                //{
                //    float distance = Vector2.Distance(neighbor.position, node.position);
                //    neighbor.weight = (1 - (distance / nodeDistance)) * 20;

                //}

                //find it neighbors to player avoid crossing the path of the enemy
                List<TileNode> neighborhood = GetNeighborNodes(node, 1); //get the neighbors including in diagonal
                foreach (var neighbor in neighborhood)
                {
                    neighbor.weight += 5;
                }
            }
        }
    }

    private void GenerateConnections()
    {
        //make connections with neighbors
        foreach (var node in Nodes)
        {
            //find neighbors
            List<TileNode> neighbors = GetNeighborNodes(node);
            foreach (var neighbor in neighbors)
            {
                //creat connection between them
                Connection connection = new Connection(node, neighbor);
                connection.Cost = neighbor.weight;
                node.Connections.Add(connection);
            }
        }
    }

    private List<TileNode> GetNeighborNodes(TileNode node, float radius = 1)
    {
        //start list to return
        List<TileNode> nodes = new List<TileNode>();
        //for each node inside radius on x axys in -radius to +radius range
        for (int x = Mathf.FloorToInt(-radius); x <= Mathf.CeilToInt(radius); x++)
        {
            //for each node inside radius on y axys in -radius to +radius range
            for (int y = Mathf.FloorToInt(-radius); y <= Mathf.CeilToInt(radius); y++)
            {
                //check if this node exists
                Vector3 nodePos = node.position + new Vector3(x, y, 0);
                TileNode toNode = Nodes.Find(n => n.position == nodePos);
                if (toNode != null)
                    if (toNode != node) //not iterate on itself  
                    {
                        //if is not in range (it block diagonal connections with radius equals 1)
                        //diagonal is used to weight nodes wich predict enemy's movement
                        if(Vector3.Distance(nodePos, node.position) <= radius)
                            nodes.Add(toNode);
                    }
            }
        }

        return nodes;
    }

    private void OnDrawGizmos()
    {
        if (Nodes == null || Nodes.Count == 0)
            return;

        float highCost = Nodes.Max(x => x.CostSoFar);
        float lowCost = Nodes.Min(x => x.CostSoFar);
        float fixer = 0;
        if (lowCost < 0)
            fixer = lowCost * -1;

        Gradient gradient = new Gradient();
        GradientColorKey key0 = new GradientColorKey(Color.white, 0);
        GradientAlphaKey alfa0 = new GradientAlphaKey(1,0);
        GradientAlphaKey alfa1 = new GradientAlphaKey(0,1);
        gradient.SetKeys(
            new GradientColorKey[] { key0},
            new GradientAlphaKey[] { alfa0, alfa1 });
        gradient.mode = GradientMode.Blend;

        foreach (var node in Nodes)
        {
            //draw Nodes
            float percentage = 1 - ((node.CostSoFar + fixer) / (highCost + fixer));
            Gizmos.color = gradient.Evaluate(percentage);
            Gizmos.DrawSphere(node.position, 0.15f);

            //draw connections
            //foreach (var connection in node.Connections)
            //{
            //    TileNode fromNode = connection.FromNode as TileNode;
            //    TileNode toNode = connection.ToNode as TileNode;

            //    Gizmos.DrawLine(fromNode.position, toNode.position);
            //}
        }

        //show best path
        if (bestPath == null)
            return;

        Gizmos.color = Color.magenta;
        bool first = true;
        foreach (var connection in bestPath)
        {

            Vector3 from = ((TileNode)connection.FromNode).position;
            Vector3 to = ((TileNode)connection.ToNode).position;

            Gizmos.DrawLine(from, to);

            if (first)
            {
                Gizmos.DrawCube(from, 0.5f * Vector3.one);
                first = false;
            }
        }
    }
}

public class TileNode : Node
{
    public Vector3 position;
    public float weight;

    public TileNode(Vector3 position)
    {
        this.position = position;
    }

    public TileNode(Vector3 position, float weight)
    {
        this.position = position;
        this.weight = weight;
    }
}
