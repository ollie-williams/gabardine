/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Collections.Generic;

namespace Gabardine
{
    public static class Extensions
    {
        /// <summary>
        /// Pops the top <paramref name="n"/> elements from <paramref
        /// name="stack"/> and returns them as an array.  The array
        /// will contain the elements in the order in which they were
        /// pushed to the stack.
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        public static T[] PopFIFO<T>(this Stack<T> stack, int n)
        {
            T[] retval = new T[n];
            int i = n;
            while (--i >= 0)
            {
                retval[i] = stack.Pop();
            }
            return retval;
        }

        public static IEnumerable<T> PopLIFO<T>(this Stack<T> stack, int n)
        {
            for (int i = 0; i < n; ++i) {
                yield return stack.Pop();
            }
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> first, T second)
        {
            foreach (T element in first) yield return element;
            yield return second;
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> second, T first)
        {
            yield return first;
            foreach (T element in second) yield return element;
        }

        /// <summary>
        /// Enumerates the elements of it replacing the <paramref name="index"/>th member with <paramref name="value"/>
        /// </summary>
        public static IEnumerable<T> ReplaceAt<T>(this IEnumerable<T> it, int index, T value)
        {
            int i = 0;
            foreach(T elem in it) {
                if (i == index) {
                    yield return value;
                } else {
                    yield return elem;
                }
                ++i;
            }
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> it, Func<T,bool> pred, out T first)
        {
            foreach (T elem in it) {
                if (pred(elem)) {
                    first = elem;
                    return true;
                }
            }
            first = default(T);
            return false;
        }
    }
}
