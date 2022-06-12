using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BVHMethod 
{
    Middle,
    EqualCounts,
    SAH
}

public struct BVHBuildSrcData
{
    public Bounds bound;
}

public struct BVHBuildPrimitiveInfo 
{
    public int dataIdx;
    public Bounds bound;
}


public class BVHBuildNode 
{
    public Bounds bound = new Bounds(Vector3.zero, Vector3.zero);
    public BVHBuildNode[] childrens = new BVHBuildNode[2];
    public int splitAxis;
    public int firstDataIdx;
    public int dataStride;
    public int nPrimitives;

    public void InitLeaf(int first,int stride,Bounds b) 
    {
        firstDataIdx = first;
        dataStride = stride;
        bound = b;
        nPrimitives = stride;
    }

    public void InitInterior(int axis, BVHBuildNode n1, BVHBuildNode n2) 
    {
        splitAxis = axis;
        childrens[0] = n1;
        childrens[1] = n2;
        bound = BVHBuilderUtil.UnionBounds(n1.bound, n2.bound);
        nPrimitives = 0;
    }
}

public class BVHTree
{
    public BVHBuildNode root;
    public BVHBuildSrcData[] orderedData;
    public int nodeCount = 0;
    public int depth = 0;
}

public class BVHBuilder
{    struct BucketInfo
    {
        public int count;
        public Bounds bound;
        public BucketInfo(int c, Bounds b)
        {
            count = c;
            bound = b;
        }
    };

    class PrimitiveInfoSortingWithCenter : IComparer<BVHBuildPrimitiveInfo> 
    {
        public int splitAxis = 0;
        public int Compare(BVHBuildPrimitiveInfo info1, BVHBuildPrimitiveInfo info2) 
        {
            if (info1.bound == null)
            {
                if (info2.bound == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else 
            {
                if (info2.bound == null)
                {
                    return 1;
                }
                else 
                {
                    return System.Convert.ToInt32(info1.bound.center[splitAxis] < info2.bound.center[splitAxis]);
                }
            }
        }
    }


    public BVHMethod buildMethod = BVHMethod.SAH;
    public int maxPrimsNode = 1;

    public BVHTree DoBuildSceneBoundingBoxBVH(BVHMethod method,int leafNodeSize, BVHBuildSrcData[] buildSrcData) 
    {
        buildMethod = method;
        maxPrimsNode = leafNodeSize;
        //
        List<BVHBuildPrimitiveInfo> primitiveInfo = new List<BVHBuildPrimitiveInfo>();
        //
        for (int i = 0; i < buildSrcData.Length; i++) 
        {
            BVHBuildPrimitiveInfo primInfo;
            primInfo.dataIdx = i;
            primInfo.bound = buildSrcData[i].bound;
            primitiveInfo.Add(primInfo);
        }
        //
        int totalNodes = 0;
        List<BVHBuildSrcData> orderedSrcData = new List<BVHBuildSrcData>();
        BVHBuildNode root = recursiveBuild(
            primitiveInfo,
            0,
            primitiveInfo.Count,
            ref totalNodes,
            orderedSrcData,
            buildSrcData
            ); ;
        BVHTree tree = new BVHTree();
        tree.root = root;
        tree.orderedData = orderedSrcData.ToArray();
        tree.nodeCount = totalNodes;
        int depth = (int)Mathf.Log((float)(totalNodes), 2) + 1;
        depth += Mathf.Pow(2, depth) < totalNodes ? 1 : 0;
        tree.depth = depth;
        return tree;
    }

    private BVHBuildNode recursiveBuild(
        List<BVHBuildPrimitiveInfo> primitivesInfo,
        int start,
        int end,
        ref int totalNodes, 
        List<BVHBuildSrcData> orderedData,
        BVHBuildSrcData[] buildSrcData
        ) 
    {
        BVHBuildNode retNode = new BVHBuildNode();
        ++totalNodes;
        //构建当前节点整体包围盒
        Bounds nodeBound = BVHBuilderUtil.InfinityBound();
        for (int i = start; i < end; ++i) 
        {
            nodeBound = BVHBuilderUtil.UnionBounds(nodeBound,primitivesInfo[i].bound);
        }
        //
        int primCnt = end - start;
        if (primCnt <= 1)
        {
            //直接创建叶节点
            int firstDataIdx = orderedData.Count;
            for (int i = start; i < end; ++i) 
            {
                int dataIdx = primitivesInfo[i].dataIdx;
                orderedData.Add(buildSrcData[dataIdx]);
            }
            retNode.InitLeaf(firstDataIdx, primCnt, nodeBound);
            return retNode;
        }
        else 
        {
            Bounds centriodBounds = BVHBuilderUtil.InfinityBound();
            for (int i = start; i < end; ++i) 
            {
                centriodBounds = BVHBuilderUtil.UnionBounds(centriodBounds, primitivesInfo[i].bound.center);
            }
            int splitAxis = BVHBuilderUtil.BoundMaxAxis(centriodBounds);
            //
            int mid = (start + end) / 2;
            if (centriodBounds.max[splitAxis].Equals(centriodBounds.min[splitAxis]))
            {
                //直接创建叶节点
                int firstDataIdx = orderedData.Count;
                for (int i = start; i < end; ++i)
                {
                    int dataIdx = primitivesInfo[i].dataIdx;
                    orderedData.Add(buildSrcData[dataIdx]);
                }
                retNode.InitLeaf(firstDataIdx, primCnt, nodeBound);
                return retNode;
            }
            else 
            {
                switch (buildMethod)
                {
                    case BVHMethod.Middle:
                        float pmid = (centriodBounds.max[splitAxis] + centriodBounds.min[splitAxis]) / 2;
                        mid = Partition(primitivesInfo, start, end, new PartitionPredicate(
                            (BVHBuildPrimitiveInfo info) =>
                            {
                                return info.bound.center[splitAxis] < pmid;
                            }
                            ));
                        break;
                    case BVHMethod.EqualCounts:
                        mid = (start + end) / 2;
                        PrimitiveInfoSortingWithCenter comp = new PrimitiveInfoSortingWithCenter();
                        comp.splitAxis = splitAxis;
                        primitivesInfo.Sort(start, primCnt, comp);
                        break;
                    case BVHMethod.SAH:
                    default:
                        if (primCnt <= 2)
                        {
                            mid = (start + end) / 2;
                            PrimitiveInfoSortingWithCenter comparer = new PrimitiveInfoSortingWithCenter();
                            comparer.splitAxis = splitAxis;
                            primitivesInfo.Sort(start, primCnt, comparer);
                        }
                        else
                        {
                            const int bucketCnt = 12;
                            BucketInfo[] bucketInfos = new BucketInfo[bucketCnt];
                            //
                            for (int i = start; i < end; ++i)
                            {
                                float relativeDis = BVHBuilderUtil.BoundOffset(centriodBounds, primitivesInfo[i].bound.center)[splitAxis];
                                int bIdx = (int)(bucketCnt * relativeDis);
                                if (bIdx == bucketCnt) { bIdx = bucketCnt - 1; }
                                if (bucketInfos[bIdx].count <= 0)
                                {
                                    bucketInfos[bIdx].bound = BVHBuilderUtil.InfinityBound();
                                }
                                bucketInfos[bIdx].count += 1;
                                bucketInfos[bIdx].bound = BVHBuilderUtil.UnionBounds(bucketInfos[bIdx].bound, primitivesInfo[i].bound);
                            }
                            //
                            float[] cost = new float[bucketCnt - 1];
                            float nodeBoundSurfaceArea = BVHBuilderUtil.BoundSurfaceArena(nodeBound);
                            for (int i = 0; i < cost.Length; ++i)
                            {
                                Bounds b1 = BVHBuilderUtil.ZeroBound();
                                Bounds b2 = BVHBuilderUtil.ZeroBound();
                                int count1 = 0, count2 = 0;
                                //
                                for (int j = 0; j <= i; ++j)
                                {
                                    b1 = BVHBuilderUtil.UnionBounds(b1, bucketInfos[j].bound);
                                    count1 += bucketInfos[j].count;
                                }
                                for (int j = i + 1; j < bucketCnt; ++j)
                                {
                                    b2 = BVHBuilderUtil.UnionBounds(b2, bucketInfos[j].bound);
                                    count2 += bucketInfos[j].count;
                                }
                                //
                                float b1SurfaceArea = BVHBuilderUtil.BoundSurfaceArena(b1);
                                float b2SurfaceArea = BVHBuilderUtil.BoundSurfaceArena(b2);
                                cost[i] = 0.125f + (count1 * b1SurfaceArea + count2 * b2SurfaceArea) / nodeBoundSurfaceArea;
                            }
                            //
                            float minCost = cost[0];
                            int minCostSplitBucket = 0;
                            for (int i = 1; i < bucketCnt - 1; ++i)
                            {
                                if (cost[i] < minCost)
                                {
                                    minCost = cost[i];
                                    minCostSplitBucket = i;
                                }
                            }
                            //
                            float leafCost = primCnt;
                            if (primCnt > maxPrimsNode || minCost < leafCost)
                            {
                                mid = Partition(primitivesInfo, start, end, new PartitionPredicate(
                                    (BVHBuildPrimitiveInfo info) => 
                                    {
                                        Vector3 relativeDis = BVHBuilderUtil.BoundOffset(centriodBounds, info.bound.center);
                                        int bIdx = (int)(bucketCnt * relativeDis[splitAxis]);
                                        if (bIdx == bucketCnt) { bIdx = bucketCnt - 1; };
                                        return bIdx <= minCostSplitBucket;
                                    }
                                    ));
                            }
                            else 
                            {
                                //直接创建叶节点
                                int firstDataIdx = orderedData.Count;
                                for (int i = start; i < end; ++i)
                                {
                                    int dataIdx = primitivesInfo[i].dataIdx;
                                    orderedData.Add(buildSrcData[dataIdx]);
                                }
                                retNode.InitLeaf(firstDataIdx, primCnt, nodeBound);
                                return retNode;
                            }
                        }
                        break;
                }
                //
                retNode.InitInterior(
                    splitAxis,
                    recursiveBuild(primitivesInfo, start, mid, ref totalNodes, orderedData, buildSrcData),
                    recursiveBuild(primitivesInfo, mid, end, ref totalNodes, orderedData, buildSrcData)
                    ) ;
            }
        }
        return retNode;
    }

    delegate bool PartitionPredicate(BVHBuildPrimitiveInfo info);

    int Partition(List<BVHBuildPrimitiveInfo> primitivesInfo, int start, int end, PartitionPredicate predicate) 
    {
        List<BVHBuildPrimitiveInfo> before = new List<BVHBuildPrimitiveInfo>();
        List<BVHBuildPrimitiveInfo> after = new List<BVHBuildPrimitiveInfo>();
        //
        for (int i = start; i < end; ++i) 
        {
            if (predicate(primitivesInfo[i]))
            {
                before.Add(primitivesInfo[i]);
            }
            else 
            {
                after.Add(primitivesInfo[i]);
            }
        }
        //
        for (int i = 0; i < before.Count; i++) 
        {
            primitivesInfo[start + i] = before[i];
        }
        //
        int mid = start + before.Count;
        //
        for (int i = 0; i < after.Count; i++)
        {
            primitivesInfo[mid + i] = after[i];
        }
        //
        return mid;
    }
}
