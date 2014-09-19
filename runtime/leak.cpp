/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <map>

size_t allocated_bytes = 0;

std::map<int32_t, size_t> allocs_by_id;

struct Info {
    size_t size;
    int32_t id;
};

extern "C" {

    void* malloc_leak(size_t size, int32_t id)
    {
        const Info ifo = { size, id };
        const size_t extra = sizeof(Info);

        allocated_bytes += size;
        size_t& val = allocs_by_id[id];
        val += size;

        Info* ptr = (Info*)malloc(size + extra);
        if (!ptr) {
            return nullptr;
        }

        *ptr = ifo;
        return ptr + 1;
    }

    void free_leak(void* ptr)
    {
        Info* sptr = (Info*)ptr - 1;
        const Info ifo = *sptr;
        allocated_bytes -= ifo.size;
        size_t& val = allocs_by_id[ifo.id];
        
        if (val < ifo.size) {
            printf("Trying to free more than was allocated!\n");
            return;
        }
        
        val -= ifo.size;
        free(sptr);
    }

    void dump_leaks()
    {
        if (allocated_bytes == 0) {
            return;
        }

        printf("MEMORY LEAKS\n");
        printf("%d bytes left unfreed\n", allocated_bytes);

        for (auto kv : allocs_by_id) {
            size_t val = kv.second;
            int ratio = val == 0 ? 0 : (100 * val) / allocated_bytes;
            printf("id %3d   allocated %8d bytes  (%d%%)\n", kv.first, val, ratio);
        }
    }

}