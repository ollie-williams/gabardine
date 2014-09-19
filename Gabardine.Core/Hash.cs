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
using System.Diagnostics.Debug;

namespace Gabardine
{
    public static class Hash
    {
        const int nTables = 4;
        const int tableLength = 256;

        static int[,] tables = new int[nTables, tableLength];

        static Hash()
        {
            Random rng = new Random((int)0x0bad1dea);
            for (int t = 0; t < nTables; ++t) {
                for (int i = 0; i < tableLength; ++i) {
                    tables[t, i] = rng.Next();
                }
            }
        }

        public static int MakeHash<T>(T value, int seed = 0)
        {
            Assert(tableLength == 256);
            var bytes = BitConverter.GetBytes(value.GetHashCode());
            int hash = seed;
            for (int i = 0; i < bytes.Length; ++i) {
                hash ^= tables[i % nTables, bytes[i]];
            }
            return hash;
        }

        public static int MakeHash(params int[] constitutents)
        {
            return MakeHash((IEnumerable<int>)constitutents);
        }

        public static int MakeHash(IEnumerable<int> constituents)
        {
            Assert(tableLength == 256);
            int t = 0;
            int hash = 0;
            foreach (var value in constituents) {
                var bytes = BitConverter.GetBytes(value);
                for (int j = 0; j < bytes.Length; ++j) {
                    hash ^= tables[t++ % nTables, bytes[j]];
                }
            }
            return hash;
        }
    }
}
