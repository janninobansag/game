// Painted terrain tree avoidance for MonsterAI_New.
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public partial class MonsterAI_New
{
    private const float PaintedTreeCellSize = 10f;
    private readonly Dictionary<Vector2Int, List<PaintedTreeObstacle>> paintedTreeGrid =
        new Dictionary<Vector2Int, List<PaintedTreeObstacle>>();
    private float largestPaintedTreeRadius;
    private float nextPaintedTreeDestinationUpdate;

    private struct PaintedTreeObstacle
    {
        public Vector3 position;
        public float radius;

        public PaintedTreeObstacle(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }


    // Reads Terrain tree instances once because painted trees are not consistently
    // available as ordinary scene objects or NavMesh obstacles at runtime.
    [ContextMenu("Refresh Painted Tree Cache")]
    public void CachePaintedTrees()
    {
        paintedTreeGrid.Clear();
        largestPaintedTreeRadius = 0f;

        Terrain[] terrains = Terrain.activeTerrains;
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData terrainData = terrain.terrainData;
            TreeInstance[] instances = terrainData.treeInstances;
            for (int treeIndex = 0; treeIndex < instances.Length; treeIndex++)
            {
                TreeInstance instance = instances[treeIndex];
                Vector3 localPosition = Vector3.Scale(instance.position, terrainData.size);
                Vector3 worldPosition = terrain.transform.TransformPoint(localPosition);
                float radius = Mathf.Max(0.5f, instance.widthScale * 0.5f);
                AddPaintedTreeToGrid(new PaintedTreeObstacle(worldPosition, radius));
            }
        }
    }

    // Groups static painted trees by world-space cell so route checks do not scan
    // every tree on a large terrain.
    private void AddPaintedTreeToGrid(PaintedTreeObstacle tree)
    {
        Vector2Int cell = GetPaintedTreeCell(tree.position);
        if (!paintedTreeGrid.TryGetValue(cell, out List<PaintedTreeObstacle> treesInCell))
        {
            treesInCell = new List<PaintedTreeObstacle>();
            paintedTreeGrid.Add(cell, treesInCell);
        }

        treesInCell.Add(tree);
        largestPaintedTreeRadius = Mathf.Max(largestPaintedTreeRadius, tree.radius);
    }

    private Vector2Int GetPaintedTreeCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / PaintedTreeCellSize),
            Mathf.FloorToInt(position.z / PaintedTreeCellSize));
    }

    // Uses a point beside the first tree blocking White Lady's direct route.
    private bool SetDestinationAvoidingPaintedTrees(Vector3 destination)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (avoidPaintedTrees && TrySetPaintedTreeDetour(destination))
            return true;

        agent.SetDestination(destination);
        return true;
    }

    private bool TrySetPaintedTreeDetour(Vector3 destination)
    {
        if (paintedTreeGrid.Count == 0)
            return false;

        Vector3 start = transform.position;
        start.y = 0f;
        Vector3 end = destination;
        end.y = 0f;
        Vector3 route = end - start;
        float routeLength = route.magnitude;
        if (routeLength < 0.1f)
            return false;

        Vector3 routeDirection = route / routeLength;
        float lookAhead = Mathf.Min(routeLength, Mathf.Max(1f, paintedTreeLookAheadDistance));
        PaintedTreeObstacle closestTree = default;
        float closestDistance = float.MaxValue;
        bool foundBlockingTree = false;

        Vector3 routeEnd = start + routeDirection * lookAhead;
        float searchPadding = largestPaintedTreeRadius + paintedTreeClearance;
        Vector2Int minCell = GetPaintedTreeCell(new Vector3(
            Mathf.Min(start.x, routeEnd.x) - searchPadding, 0f,
            Mathf.Min(start.z, routeEnd.z) - searchPadding));
        Vector2Int maxCell = GetPaintedTreeCell(new Vector3(
            Mathf.Max(start.x, routeEnd.x) + searchPadding, 0f,
            Mathf.Max(start.z, routeEnd.z) + searchPadding));

        for (int cellX = minCell.x; cellX <= maxCell.x; cellX++)
        {
            for (int cellZ = minCell.y; cellZ <= maxCell.y; cellZ++)
            {
                if (!paintedTreeGrid.TryGetValue(new Vector2Int(cellX, cellZ), out List<PaintedTreeObstacle> treesInCell))
                    continue;

                for (int i = 0; i < treesInCell.Count; i++)
                {
                    PaintedTreeObstacle tree = treesInCell[i];
                    Vector3 toTree = tree.position - start;
                    toTree.y = 0f;
                    float alongRoute = Vector3.Dot(toTree, routeDirection);
                    if (alongRoute <= 0f || alongRoute > lookAhead)
                        continue;

                    Vector3 closestPoint = start + routeDirection * alongRoute;
                    Vector3 flatTreePosition = tree.position;
                    flatTreePosition.y = 0f;
                    float clearance = tree.radius + paintedTreeClearance;
                    if ((flatTreePosition - closestPoint).sqrMagnitude > clearance * clearance ||
                        alongRoute >= closestDistance)
                        continue;

                    closestTree = tree;
                    closestDistance = alongRoute;
                    foundBlockingTree = true;
                }
            }
        }

        if (!foundBlockingTree)
            return false;

        Vector3 side = Vector3.Cross(Vector3.up, routeDirection).normalized;
        float detourDistance = closestTree.radius + paintedTreeClearance;
        return TrySetDetourPath(closestTree.position + side * detourDistance) ||
               TrySetDetourPath(closestTree.position - side * detourDistance);
    }

    private bool TrySetDetourPath(Vector3 candidate)
    {
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit detourPoint,
                                    paintedTreeClearance + 1f, agent.areaMask))
            return false;

        NavMeshPath detourPath = new NavMeshPath();
        if (!agent.CalculatePath(detourPoint.position, detourPath) ||
            detourPath.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.SetPath(detourPath);
        return true;
    }

}