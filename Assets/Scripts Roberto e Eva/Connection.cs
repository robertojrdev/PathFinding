using static AI;

public class Connection
{
    public Connection(Node from, Node to)
    {
        FromNode = from;
        ToNode = to;
    }

    private Node fromNode;
    private Node toNode;
    private float cost;

    //GET & SET
    public Node FromNode { get => fromNode; set => fromNode = value; }
    public Node ToNode { get => toNode; set => toNode = value; }
    public float Cost { get => cost; set => cost = value; }
}
