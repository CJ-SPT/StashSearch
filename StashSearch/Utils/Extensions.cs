using System;
using System.Collections.Generic;

namespace StashSearch.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Returns a duplicate element from a single search parameter uses the default comparer if
        /// none is provided.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="collection2"></param>
        /// <param name="comparer"></param>
        /// <returns>The duplicate object</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T CheckForDuplicateElement<T>(
            this IEnumerable<T> collection,
            IEnumerable<T> collection2,
            Func<T, T, bool> comparer = null
            ) where T : class
        {
            if (collection == null)
                throw new ArgumentNullException("Items cannot be null");

            // Use default comparer if none provided
            comparer ??= EqualityComparer<T>.Default.Equals;

            using var enumerator1 = collection.GetEnumerator();
            using var enumerator2 = collection2.GetEnumerator();
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                if (comparer(enumerator1.Current, enumerator2.Current))
                {
                    // Found a match
                    return enumerator1.Current;
                }
            }

            // No matching element found
            return default;
        }
    }
}