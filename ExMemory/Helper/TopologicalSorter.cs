using System;
using System.Collections.Generic;

namespace ExternalMemory.Helper
{
    public static class TopologicalSorter
    {
        // Thanks To https://stackoverflow.com/questions/4106862/how-to-sort-depended-objects-by-dependency
        // Thanks To https://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
        public static List<T> Sort<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
        {
            var sorted = new List<T>();
            var visited = new Dictionary<T, bool>();

            foreach (T item in source)
                Visit(item, getDependencies, sorted, visited);

            return sorted;
        }

        private static void Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, ICollection<T> sorted, IDictionary<T, bool> visited)
        {
            var alreadyVisited = visited.TryGetValue(item, out var inProcess);

            if (alreadyVisited)
            {
                if (inProcess)
                    throw new ArgumentException("Cyclic dependency found.");
            }
            else
            {
                visited[item] = true;

                IEnumerable<T> dependencies = getDependencies(item);
                if (dependencies != null)
                {
                    foreach (T dependency in dependencies)
                    {
                        Visit(dependency, getDependencies, sorted, visited);
                    }
                }

                visited[item] = false;
                sorted.Add(item);
            }
        }
    }
}
