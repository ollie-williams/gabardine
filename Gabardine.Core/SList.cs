/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System.Collections;
using System.Collections.Generic;

namespace Gabardine
{
    public class SLNode<T>
    {
        T item;
        readonly SLNode<T> next;

        public SLNode(T item, SLNode<T> next)
        {
            this.item = item;
            this.next = next;
        }

        public T Item
        {
            get { return item; }
            set { item = value; }
        }

        public SLNode<T> Next { get { return next; } }            
    }

    /// <summary>
    ///   A number of methods that would be obvious candidates for
    ///   being members of SLNode are perfectly well-defined when the
    ///   node is null, but C# doesn't like calling member functions
    ///   on null objects, so they need to be extension methods.
    /// </summary>
    public static class SListExtensions 
    {
        public static IEnumerable<T> Enumerate<T>(this SLNode<T> list)
        {
            SLNode<T> node = list;
            while (node != null) {
                yield return node.Item;
                node = node.Next;
            }
            yield break;
        }
    
        public static SLNode<T> Append<T>(this SLNode<T> tail, T head)
        {
            return new SLNode<T>(head, tail);
        }
    }    
}
