//using System;
//using System.Collections.Generic;
//using UnityEngine;

//public class PointGraph : MonoBehaviour
//{
//    public IReadOnlyList<Point> Points { get; }
//    public IReadOnlyList<Connection> Connections { get; }

//    public event Action<Connection> ConnectionCreated;
//    public event Action<Connection> ConnectionRemoved;
//    public event Action<Point> PointCreated;
//    public event Action<Point> PointRemoved;

//    public Point CreatePoint(Vector2 position, int type)
//    {
//        return null;
//    }

//    public void RemovePoint(Point point)
//    {

//    }

//    public Connection Connect(Point a, Point b)
//    {
//        // null if already connected
//        return null;
//    }


//    public void Disconnect(Connection connection)
//    {

//    }

//    public bool AreConnected(Point a, Point b)
//    {
//        return false;
//    }


//    public Point FindNearest(Vector2 position, float maxDistance, Point exclude = null){
//        return null;
//    }
//    public void GetConnectionsAt(Point point, List<Connection> results) { 

//    }  // adjacency
//}
