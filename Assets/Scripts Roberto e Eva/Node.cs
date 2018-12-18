using System.Collections.Generic;

public class Node
{
    private List<Connection> connections = new List<Connection>();
    private float costSoFar;
    private Connection fromConnection;

    //GET & SET
    public float CostSoFar { get => costSoFar; set => costSoFar = value; }
    public Connection FromConnection { get => fromConnection; set => fromConnection = value; }
    public List<Connection> Connections { get => connections; set => connections = value; }
}
