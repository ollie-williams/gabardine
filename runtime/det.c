/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
#include "C:/mkl/include/mkl.h"

double det(const double* a, const MKL_INT* piv, MKL_INT n)
{        
    double det = 1.0;
    for (MKL_INT i = 0; i < n; ++i) {
        if (piv[i] != i+1) {
            det *= -a[i*n+i];
        } else {
            det *= a[i*n+i];
        }
    }    

    return det;
}

void domatcopy(char ordering, char trans, size_t rows, size_t cols, double alpha, double* A, size_t lda, double* B, size_t ldb)
{
    mkl_domatcopy(ordering, trans, rows, cols, alpha, A, lda, B, ldb);
}
