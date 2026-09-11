using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Servicess
{
    public static class NetworkAnalyzer
    {
        public const int DefaultMaxSharedAttributeMembers = 50;

        public static GraphAnalysis Analyze(
            IReadOnlyList<GraphNodeInput> nodes,
            IReadOnlyList<IReadOnlyList<Guid>> coOccurrenceGroups,
            int maxSharedAttributeMembers = DefaultMaxSharedAttributeMembers)
        {
            var edges = new List<GraphEdge>();
            var ids = nodes.Select(n => n.PersonId).ToHashSet();

            var circleCounts = CountValues(nodes.Select(n => n.CircleName));
            var channelCounts = CountValues(nodes.Select(n => n.ChannelName));
            var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
                foreach (var tag in ParseTags(node.TagsJson))
                    tagCounts[tag] = tagCounts.TryGetValue(tag, out int c) ? c + 1 : 1;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var a = nodes[i];
                    var b = nodes[j];
                    double weight = 0;
                    var reasons = new List<string>();

                    if (!string.IsNullOrWhiteSpace(a.CircleName)
                        && string.Equals(a.CircleName.Trim(), b.CircleName?.Trim(), StringComparison.OrdinalIgnoreCase)
                        && !IsUbiquitous(a.CircleName, circleCounts, maxSharedAttributeMembers))
                    {
                        weight += 1.0;
                        reasons.Add("Same organization");
                    }

                    var sharedTags = SharedTags(a.TagsJson, b.TagsJson)
                        .Where(t => !IsUbiquitous(t, tagCounts, maxSharedAttributeMembers))
                        .ToList();
                    if (sharedTags.Count > 0)
                    {
                        weight += Math.Min(1.0, 0.5 * sharedTags.Count);
                        reasons.Add("Shared tags");
                    }

                    if (!string.IsNullOrWhiteSpace(a.ChannelName)
                        && string.Equals(a.ChannelName.Trim(), b.ChannelName?.Trim(), StringComparison.OrdinalIgnoreCase)
                        && !IsUbiquitous(a.ChannelName, channelCounts, maxSharedAttributeMembers))
                    {
                        weight += 0.5;
                        reasons.Add("Shared channel");
                    }

                    if (weight > 0)
                        edges.Add(new GraphEdge(a.PersonId, b.PersonId, string.Join("; ", reasons), weight));
                }
            }

            foreach (var group in coOccurrenceGroups ?? Enumerable.Empty<IReadOnlyList<Guid>>())
            {
                var members = group.Where(ids.Contains).Distinct().ToList();
                for (int i = 0; i < members.Count; i++)
                    for (int j = i + 1; j < members.Count; j++)
                        edges.Add(new GraphEdge(members[i], members[j], "Appeared together", 1.0));
            }

            var adjacency = ids.ToDictionary(id => id, _ => new HashSet<Guid>());
            foreach (var e in edges)
            {
                adjacency[e.From].Add(e.To);
                adjacency[e.To].Add(e.From);
            }

            var degrees = adjacency.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
            var bridges = FindArticulationPoints(adjacency);
            var clusters = ConnectedComponents(adjacency);

            return new GraphAnalysis(edges, degrees, bridges, clusters);
        }

        public static IReadOnlyList<string> SharedTags(string? aJson, string? bJson)
        {
            var a = ParseTags(aJson);
            var b = ParseTags(bJson);
            return a.Intersect(b, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static Dictionary<string, int> CountValues(IEnumerable<string?> values)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                string key = value.Trim();
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            }
            return counts;
        }

        private static bool IsUbiquitous(
            string? value, Dictionary<string, int> counts, int maxSharedAttributeMembers)
        {
            return !string.IsNullOrWhiteSpace(value)
                && counts.TryGetValue(value.Trim(), out int c)
                && c > maxSharedAttributeMembers;
        }

        private static HashSet<string> ParseTags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var tags = JsonSerializer.Deserialize<List<string>>(json);
                return new HashSet<string>(tags ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static HashSet<Guid> FindArticulationPoints(Dictionary<Guid, HashSet<Guid>> adjacency)
        {
            var bridges = new HashSet<Guid>();
            var disc = new Dictionary<Guid, int>();
            var low = new Dictionary<Guid, int>();
            int time = 0;

            foreach (var start in adjacency.Keys)
            {
                if (disc.ContainsKey(start))
                    continue;

                int rootChildren = 0;
                var stack = new Stack<(Guid node, Guid parent, IEnumerator<Guid> neighbors)>();
                disc[start] = low[start] = ++time;
                stack.Push((start, Guid.Empty, adjacency[start].GetEnumerator()));

                while (stack.Count > 0)
                {
                    var (node, parent, neighbors) = stack.Peek();
                    bool descended = false;

                    while (neighbors.MoveNext())
                    {
                        var next = neighbors.Current;
                        if (next == parent)
                            continue;
                        if (!disc.ContainsKey(next))
                        {
                            if (parent == Guid.Empty)
                                rootChildren++;
                            disc[next] = low[next] = ++time;
                            stack.Push((next, node, adjacency[next].GetEnumerator()));
                            descended = true;
                            break;
                        }
                        low[node] = Math.Min(low[node], disc[next]);
                    }

                    if (descended)
                        continue;

                    stack.Pop();
                    if (stack.Count > 0)
                    {
                        var caller = stack.Peek().node;
                        low[caller] = Math.Min(low[caller], low[node]);
                        if (stack.Peek().parent != Guid.Empty && low[node] >= disc[caller])
                            bridges.Add(caller);
                    }
                }

                if (rootChildren > 1)
                    bridges.Add(start);
            }

            return bridges;
        }

        private static List<IReadOnlyList<Guid>> ConnectedComponents(Dictionary<Guid, HashSet<Guid>> adjacency)
        {
            var visited = new HashSet<Guid>();
            var clusters = new List<IReadOnlyList<Guid>>();

            foreach (var start in adjacency.Keys)
            {
                if (!visited.Add(start))
                    continue;

                var cluster = new List<Guid>();
                var queue = new Queue<Guid>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    cluster.Add(node);
                    foreach (var next in adjacency[node])
                        if (visited.Add(next))
                            queue.Enqueue(next);
                }
                clusters.Add(cluster);
            }

            return clusters;
        }
    }
}
