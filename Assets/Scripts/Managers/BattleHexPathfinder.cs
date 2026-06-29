using System.Collections.Generic;

public sealed class BattleHexPathfinder
{
    private BattleSystem BattleSystem;

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public bool TryFindPath(BattleHexCoord start, BattleHexCoord goal, BattleHexGrid grid, BattleHexOccupation occupation, BattleUnitInstance requester, List<BattleHexCoord> resultPath)
    {
        resultPath.Clear();

        if (!grid.IsInside(start))
        {
            return false;
        }

        if (!grid.CanOccupy(goal, requester, occupation))
        {
            return false;
        }

        Dictionary<BattleHexCoord, BattleHexCoord> cameFrom = new();
        Dictionary<BattleHexCoord, int> gScore = new();

        HexPriorityQueue open = new();
        HashSet<BattleHexCoord> closed = new();

        gScore[start] = 0;
        open.Enqueue(start, grid.Distance(start, goal));

        while (open.Count > 0)
        {
            BattleHexCoord current = open.Dequeue();

            if (closed.Contains(current))
            {
                continue;
            }

            if (current.Equals(goal))
            {
                ReconstructPath(current, cameFrom, resultPath);
                return true;
            }

            closed.Add(current);

            foreach (var neighbor in grid.GetNeighbors(current))
            {
                if (closed.Contains(neighbor))
                {
                    continue;
                }

                if (!grid.CanTraverse(neighbor, requester, occupation))
                {
                    continue;
                }

                int tentativeG = gScore[current] + 1;

                if (gScore.TryGetValue(neighbor, out int oldG) && tentativeG >= oldG)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;

                int fScore = tentativeG + grid.Distance(neighbor, goal);
                open.Enqueue(neighbor, fScore);
            }
        }

        return false;
    }

    private void ReconstructPath(BattleHexCoord current, Dictionary<BattleHexCoord, BattleHexCoord> cameFrom, List<BattleHexCoord> resultPath)
    {
        List<BattleHexCoord> nodes = new();
        nodes.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            nodes.Add(current);
        }

        nodes.Reverse();

        resultPath.Clear();

        for (int i = 1; i < nodes.Count; i++)
        {
            resultPath.Add(nodes[i]);
        }
    }
}

public sealed class HexPriorityQueue
{
    private readonly List<Entry> heap = new();

    public int Count => heap.Count;

    public void Enqueue(BattleHexCoord coord, int priority)
    {
        heap.Add(new Entry(coord, priority));
        BubbleUp(heap.Count - 1);
    }

    public BattleHexCoord Dequeue()
    {
        Entry root = heap[0];

        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);

        if (heap.Count > 0)
        {
            BubbleDown(0);
        }

        return root.Coord;
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;

            if (heap[index].Priority >= heap[parent].Priority)
            {
                break;
            }

            Swap(index, parent);
            index = parent;
        }
    }

    private void BubbleDown(int index)
    {
        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left < heap.Count && heap[left].Priority < heap[smallest].Priority)
            {
                smallest = left;
            }

            if (right < heap.Count && heap[right].Priority < heap[smallest].Priority)
            {
                smallest = right;
            }

            if (smallest == index)
            {
                break;
            }

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
    }

    private readonly struct Entry
    {
        public readonly BattleHexCoord Coord;
        public readonly int Priority;

        public Entry(BattleHexCoord coord, int priority)
        {
            Coord = coord;
            Priority = priority;
        }
    }
}