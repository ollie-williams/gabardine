/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved.
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
#include "C:/mkl/include/mkl.h"
#include <stdint.h>
#include <string.h>
#include <stdio.h>

void kron(double* a, double* b, size_t a_rows, size_t a_cols, size_t b_rows, size_t b_cols, double* dst)
{
	//printf("kron enter\n");
	const size_t ldc = b_rows * a_rows;
	for (size_t ca = 0; ca < a_cols; ++ca) {
		for (size_t ra = 0; ra < a_rows; ++ra) {


			mkl_domatcopy('c', 'n', b_rows, b_cols, *a, b, b_rows, dst, ldc);

			++a;
			dst += b_rows;
		}
		dst += (b_cols - 1) * ldc;
	}
	//printf("kron exit\n");
}

void kron_eye(const double* a, size_t a_rows, size_t a_cols, size_t n, double* dst)
{
	//printf("kron_eye enter\n");
	const size_t ldb = n * a_rows;
	memset(dst, 0, ldb*n*a_cols*sizeof(double));

	double* b = dst;
	for (size_t c = 0; c < a_cols; ++c) {

		for (size_t r = 0; r < a_rows; ++r) {

			// Set diagonal of this block to value of element of a
			double* t = b;
			for (size_t i = 0; i < n; ++i) {
				*t = *a;
				t += ldb + 1;
			}

			// Move b forward by n rows
			b += n;

			// Move a forward by 1 row
			++a;
		}

		// b now points to the top of the second row of this block, so
		// move forward n-1 rows to get to first column of the next
		// block.
		b += ldb * (n - 1);
	}
	//printf("kron_eye exit\n");    
}

void eye_kron(const double* a, size_t a_rows, size_t a_cols, size_t n, double* dst)
{
	//printf("eye_kron enter\n");
	const size_t ldb = n * a_rows;
	memset(dst, 0, ldb*n*a_cols*sizeof(double));

	double* b = dst;
	for (size_t i = 0; i < n; ++i) {
		// Copy a to block of b (i.e., ldb is a long stride)
		mkl_domatcopy('c', 'n', a_rows, a_cols, 1.0, a, a_rows, b, ldb);

		// Move a_cols columns and a_rows rows forward
		b += ldb * a_cols + a_rows;
	}
	//printf("eye_kron exit\n");
}

