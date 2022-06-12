using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHBuilderUtil
{

    public static Bounds ZeroBound()
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        return b;
    }

    public static Bounds InfinityBound() 
    {
        Bounds b = new Bounds(Vector3.zero,Vector3.zero);
        b.min = Vector3.positiveInfinity;
        b.max = Vector3.negativeInfinity;
        return b;
    }
    public static Bounds UnionBounds(Bounds b1, Bounds b2) 
    {
        Vector3 min = new Vector3(
                Mathf.Min(b1.min.x,b2.min.x),
                Mathf.Min(b1.min.y, b2.min.y),
                Mathf.Min(b1.min.z, b2.min.z)
            );
        Vector3 max = new Vector3(
            Mathf.Max(b1.max.x, b2.max.x),
            Mathf.Max(b1.max.y, b2.max.y),
            Mathf.Max(b1.max.z, b2.max.z)
        );
        Bounds b = new Bounds();
        b.min = min;
        b.max = max;
        return b;
    }

    public static Bounds UnionBounds(Bounds b1, Vector3 p)
    {
        Vector3 min = new Vector3(
                Mathf.Min(b1.min.x, p.x),
                Mathf.Min(b1.min.y, p.y),
                Mathf.Min(b1.min.z, p.z)
            );
        Vector3 max = new Vector3(
            Mathf.Max(b1.max.x, p.x),
            Mathf.Max(b1.max.y, p.y),
            Mathf.Max(b1.max.z, p.z)
        );
        Bounds b = new Bounds();
        b.min = min;
        b.max = max;
        return b;
    }

    public static float BoundSurfaceArena(Bounds b) 
    {
        Vector3 d = b.max - b.min;
        return 2 * (d.x * d.y + d.x * d.z + d.y * d.z);
    }

    public static int BoundMaxAxis(Bounds b) 
    {
        Vector3 d = b.max - b.min;
        if (d.x > d.y && d.x > d.z)
        {
            return 0;
        }
        else if (d.y > d.z)
        {
            return 1;
        }
        else 
        {
            return 2;
        }
    }

    public static Vector3 BoundOffset(Bounds b, Vector3 p) 
    {
        Vector3 o = p - b.min;
        if (b.max.x > b.min.x) { o.x /= (b.max.x - b.min.x); }
        if (b.max.y > b.min.y) { o.y /= (b.max.y - b.min.y); }
        if (b.max.z > b.min.z) { o.z /= (b.max.z - b.min.z); }
        return o;
    }

    public static bool IsBoundsOverlap(Bounds b1, Bounds b2) 
    {
        bool x = (b1.max.x >= b2.min.x) && (b1.min.x <= b2.max.x);
        bool y = (b1.max.y >= b2.min.y) && (b1.min.y <= b2.max.y);
        bool z = (b1.max.z >= b2.min.z) && (b1.min.z <= b2.max.z);
        return x && y && z;
    }
}
