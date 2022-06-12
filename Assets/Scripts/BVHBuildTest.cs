using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVHBuildTest : MonoBehaviour
{

    Color[] colorArray = new Color[]
    {
        Color.red,Color.green,Color.blue,Color.magenta,Color.yellow,Color.cyan
    };

    MeshRenderer[] renderers;

    List<BVHBuildSrcData> srcData = new List<BVHBuildSrcData>();

    BVHTree bvhTree;

    BVHBuilder builder = new BVHBuilder();

    int curDrawDepth = 0;

    private void Awake()
    {
        renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++) 
        {
            BVHBuildSrcData data;
            data.bound = renderers[i].bounds;
            srcData.Add(data);
        }
        //srcData.Sort((BVHBuildSrcData data1, BVHBuildSrcData data2) =>
        //{
        //    if (data1.bound == null)
        //    {
        //        if (data2.bound == null)
        //        {
        //            return 0;
        //        }
        //        else
        //        {
        //            return -1;
        //        }
        //    }
        //    else
        //    {
        //        if (data2.bound == null)
        //        {
        //            return 1;
        //        }
        //        else
        //        {
        //            return System.Convert.ToInt32(BVHBuilderUtil.BoundSurfaceArena(data1.bound) < BVHBuilderUtil.BoundSurfaceArena(data2.bound));
        //        }
        //    }
        //});
        //
        bvhTree = builder.DoBuildSceneBoundingBoxBVH(BVHMethod.SAH, 1, srcData.ToArray());
        curDrawDepth = bvhTree.depth;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && bvhTree != null) 
        {
            curDrawDepth++;
            if (curDrawDepth > bvhTree.depth) { curDrawDepth = 0; }
        }
    }

    private void OnDrawGizmos()
    {
        if (bvhTree != null) 
        {
            DrawBVHNodeBound(bvhTree.root,0);
            //
        }

    }

    private void DrawBVHNodeBound(BVHBuildNode node,int depth) 
    {
        if (node == null) 
        {
            return;
        }
        //
        int colorIdx = depth <= curDrawDepth ? depth : curDrawDepth;
        Gizmos.color = colorIdx < colorArray.Length ? colorArray[colorIdx] : Color.black;
        float sizeScale = (bvhTree.depth + 1 - depth) * 0.2f; 
        Gizmos.DrawWireCube(node.bound.center, node.bound.size + Vector3.one * sizeScale);
        //
        for (int i = 0; i < node.childrens.Length; i++)
        {
            DrawBVHNodeBound(node.childrens[i], depth + 1);
        }
    }
}
