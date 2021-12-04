// Bzip2 library for .net
// By Jaime Olivares
// Location: http://github.com/jaime-olivares/bzip2
// Ported from the Java implementation by Matthew Francis: https://github.com/MateuszBartosiewicz/bzip2

using System;

namespace Bzip2
{
    /// <summary>
    /// DivSufSort suffix array generator
    /// Based on libdivsufsort 1.2.3 patched to support BZip2
    /// </summary>
    /// <remarks>
	/// This is a simple conversion of the original C with two minor bugfixes applied(see "BUGFIX"
	/// comments within the class). Documentation within the class is largely absent.
    /// </remarks>
	internal class BZip2DivSufSort
    {
        #region Nested classes
        private class StackEntry
        {
            readonly public int a;
            readonly public int b;
            readonly public int c;
            readonly public int d;

            public StackEntry(int a, int b, int c, int d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
            }
        }

        private class PartitionResult
        {
            readonly public int first;
            readonly public int last;

            public PartitionResult(int first, int last)
            {
                this.first = first;
                this.last = last;
            }
        }

        private class TRBudget
        {
            private int budget;
            public int chance;

            public bool update(int size, int n)
            {

                this.budget -= n;
                if (this.budget <= 0)
                {
                    if (--this.chance == 0)
                    {
                        return false;
                    }
                    this.budget += size;
                }

                return true;
            }

            public TRBudget(int budget, int chance)
            {
                this.budget = budget;
                this.chance = chance;
            }
        }
        #endregion

        #region Private fields
        private const int STACK_SIZE = 64;

	    private const int BUCKET_A_SIZE = 256;

	    private const int BUCKET_B_SIZE = 65536;

	    private const int SS_BLOCKSIZE = 1024;

	    private const int INSERTIONSORT_THRESHOLD = 8;

	    private static readonly int[] log2table= {
			-1,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
			5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
			6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
			6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7
		};

		private readonly int[] SA;

		private readonly byte[] T;

		private readonly int n;
        #endregion

        #region Public methods
        /**
         * @param T The input array
         * @param SA The output array
         * @param n The length of the input data
         */
        public BZip2DivSufSort(byte[] T, int[] SA, int n)
        {
            this.T = T;
            this.SA = SA;
            this.n = n;
        }

        /**
         * Performs a Burrows Wheeler Transform on the input array
         * @return the index of the first character of the input array within the output array
         */
        public int BWT()
        {
            if (n == 0)
                return 0;

            if (n == 1)
            {
                SA[0] = T[0];
                return 0;
            }

            var bucketA = new int[BUCKET_A_SIZE];
            var bucketB = new int[BUCKET_B_SIZE];

            int m = this.sortTypeBstar(bucketA, bucketB);
            return 0 < m ? this.constructBWT(bucketA, bucketB) : 0;
        }
        #endregion

        #region Private methods
        // ReSharper disable LoopVariableIsNeverChangedInsideLoop

		private static void swapElements (int[] array1,  int index1,  int[] array2,  int index2) 
		{
			var temp = array1[index1];
			array1[index1] = array2[index2];
			array2[index2] = temp; 
		}

		private int ssCompare (int p1,  int p2,  int depth) 
        {
			int U1n, U2n; // pointers within T
			int U1, U2;

			for (
				U1 = depth + SA[p1], U2 = depth + SA[p2], U1n = SA[p1 + 1] + 2, U2n = SA[p2 + 1] + 2;
				(U1 < U1n) && (U2 < U2n) && (T[U1] == T[U2]);
				++U1, ++U2
            ) { }

		    return U1 < U1n ? (U2 < U2n ? (T[U1] & 0xff) - (T[U2] & 0xff) : 1) : (U2 < U2n ? -1 : 0);
		}
			
		private int ssCompareLast (int PA, int p1, int p2, int depth, int size) 
        {
			int U1, U2, U1n, U2n;

			for (
				U1 = depth + SA[p1], U2 = depth + SA[p2], U1n = size, U2n = SA[(p2 + 1)] + 2;
				(U1 < U1n) && (U2 < U2n) && (T[U1] == T[U2]);
				++U1, ++U2
            ) { }

		    if (U1 < U1n) 
				return (U2 < U2n) ? (T[U1] & 0xff) - (T[U2] & 0xff) : 1;

            if (U2 == U2n) 
		        return 1;

		    for (
				U1 = U1 % size, U1n = SA[PA] + 2;
				(U1 < U1n) && (U2 < U2n) && (T[U1] == T[U2]);
				++U1, ++U2
            ) { }

		    return U1 < U1n ?
				(U2 < U2n ? (T[U1] & 0xff) - (T[U2] & 0xff) : 1)
					: (U2 < U2n ? -1 : 0);
		}
			
		private void ssInsertionSort (int PA, int first, int last, int depth) 
        {
		    int i; // pointer within SA

		    for (i = last - 2; first <= i; --i) {
			    int j; // pointer within SA
			    int t;
			    int r;
			    for (t = SA[i], j = i + 1; 0 < (r = this.ssCompare (PA + t, PA + SA[j], depth));) {
					do {
						SA[j - 1] = SA[j];
					} while ((++j < last) && (SA[j] < 0));
					if (last <= j) {
						break;
					}
				}
				if (r == 0) {
					SA[j] = ~SA[j];
				}
				SA[j - 1] = t;
			}
		}

		private void ssFixdown (int Td, int PA, int sa, int i, int size) 
        {
			int j, k;
			int v;
		    int c;

		    for (v = SA[sa + i], c = (T[Td + SA[PA + v]]) & 0xff; (j = 2 * i + 1) < size; SA[sa + i] = SA[sa + k], i = k) {
				int d = T[Td + SA[PA + SA[sa + (k = j++)]]] & 0xff;
		        int e;
		        if (d < (e = T[Td + SA[PA + SA[sa + j]]] & 0xff)) {
					k = j;
					d = e;
				}
				if (d <= c) break;
			}
			SA[sa + i] = v;
		}

		private void ssHeapSort (int Td, int PA, int sa, int size) 
		{
			int i;

		    int m = size;
			if ((size % 2) == 0) {
				m--;
				if ((T[Td + SA[PA + SA[sa + (m / 2)]]] & 0xff) < (T[Td + SA[PA + SA[sa + m]]] & 0xff)) {
					swapElements (SA, sa + m, SA, sa + (m / 2));
				}
			}

			for (i = m / 2 - 1; 0 <= i; --i) {
				this.ssFixdown (Td, PA, sa, i, m);
			}

			if ((size % 2) == 0) {
				swapElements (SA, sa, SA, sa + m);
				this.ssFixdown (Td, PA, sa, 0, m);
			}

			for (i = m - 1; 0 < i; --i) {
				int t = SA[sa];
				SA[sa] = SA[sa + i];
				this.ssFixdown (Td, PA, sa, 0, i);
				SA[sa + i] = t;
			}
		}

		private int ssMedian3 (int Td,  int PA, int v1, int v2, int v3) 
		{
			var T_v1 = T[Td + SA[PA + SA[v1]]] & 0xff;
			var T_v2 = T[Td + SA[PA + SA[v2]]] & 0xff;
			var T_v3 = T[Td + SA[PA + SA[v3]]] & 0xff;

			if (T_v1 > T_v2) {
				 var temp = v1;
				v1 = v2;
				v2 = temp;
				 var T_vtemp = T_v1;
				T_v1 = T_v2;
				T_v2 = T_vtemp;
			}
			if (T_v2 > T_v3)
			{
			    return T_v1 > T_v3 ? v1 : v3;
			}
		    return v2;
		}

		private int ssMedian5 (int Td,  int PA, int v1, int v2, int v3, int v4, int v5) 
		{
			var T_v1 = T[Td + SA[PA + SA[v1]]] & 0xff;
			var T_v2 = T[Td + SA[PA + SA[v2]]] & 0xff;
			var T_v3 = T[Td + SA[PA + SA[v3]]] & 0xff;
			var T_v4 = T[Td + SA[PA + SA[v4]]] & 0xff;
			var T_v5 = T[Td + SA[PA + SA[v5]]] & 0xff;
			int temp;
			int T_vtemp;

            // ReSharper disable RedundantAssignment
			if (T_v2 > T_v3) {
				temp = v2;
				v2 = v3;
				v3 = temp;
				T_vtemp = T_v2;
				T_v2 = T_v3;
				T_v3 = T_vtemp;
			}
			if (T_v4 > T_v5) {
				temp = v4;
				v4 = v5;
				v5 = temp;
				T_vtemp = T_v4;
				T_v4 = T_v5;
				T_v5 = T_vtemp;
			}
			if (T_v2 > T_v4) {
				temp = v2;
				v2 = v4;
				v4 = temp;
				T_vtemp = T_v2;
				T_v2 = T_v4;
				T_v4 = T_vtemp;
				temp = v3;
				v3 = v5;
				v5 = temp;
				T_vtemp = T_v3;
				T_v3 = T_v5;
				T_v5 = T_vtemp;
			}
			if (T_v1 > T_v3) {
				temp = v1;
				v1 = v3;
				v3 = temp;
				T_vtemp = T_v1;
				T_v1 = T_v3;
				T_v3 = T_vtemp;
			}
			if (T_v1 > T_v4) {
				temp = v1;
				v1 = v4;
				v4 = temp;
				T_vtemp = T_v1;
				T_v1 = T_v4;
				T_v4 = T_vtemp;
				temp = v3;
				v3 = v5;
				v5 = temp;
				T_vtemp = T_v3;
				T_v3 = T_v5;
				T_v5 = T_vtemp;
			}
            // ReSharper restore RedundantAssignment

			return T_v3 > T_v4 ? v4 : v3;
		}
			
		private int ssPivot (int Td,  int PA,  int first,  int last) 
        {
		    int t = last - first;
			int middle = first + t / 2;

			if (t <= 512) {
				if (t <= 32) {
					return this.ssMedian3 (Td, PA, first, middle, last - 1);
				}
				t >>= 2;
				return this.ssMedian5 (Td, PA, first, first + t, middle, last - 1 - t, last - 1);
			}
			t >>= 3;
			return this.ssMedian3 (
				Td, PA,
				this.ssMedian3 (Td, PA, first, first + t, first + (t << 1)),
				this.ssMedian3 (Td, PA, middle - t, middle, middle + t),
				this.ssMedian3 (Td, PA, last - 1 - (t << 1), last - 1 - t, last - 1)
			);
		}
			
		private static int ssLog (int x) 
		{
			return ((x & 0xff00) != 0) ? 8 + log2table[(x >> 8) & 0xff] : log2table[x & 0xff];
		}

		private int ssSubstringPartition (int PA,  int first,  int last,  int depth) 
		{
			int a, b;

		    for (a = first - 1, b = last;;) {
				for (; (++a < b) && ((SA[PA + SA[a]] + depth) >= (SA[PA + SA[a] + 1] + 1));) {
					SA[a] = ~SA[a];
				}
				for (; (a < --b) && ((SA[PA + SA[b]] + depth) <  (SA[PA + SA[b] + 1] + 1));) {}
				if (b <= a) {
					break;
				}
				int t = ~SA[b];
				SA[b] = SA[a];
				SA[a] = t;
			}
			if (first < a) 
				SA[first] = ~SA[first];

			return a;
		}

		private void ssMultiKeyIntroSort (int PA, int first, int last, int depth) 
        {
			var stack = new StackEntry[STACK_SIZE];

		    int ssize;
			int limit;
		    int x = 0;

		    for (ssize = 0, limit = ssLog (last - first);;) {
				if ((last - first) <= INSERTIONSORT_THRESHOLD) {
					if (1 < (last - first)) {
						this.ssInsertionSort (PA, first, last, depth);
					}
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					last = entry.b;
					depth = entry.c;
					limit = entry.d;
					continue;
				}

				int Td = depth;
				if (limit-- == 0) {
					this.ssHeapSort (Td, PA, first, last - first);
				}
			    int a;
		        int v;
		        if (limit < 0) {
					for (a = first + 1, v = T[Td + SA[PA + SA[first]]] & 0xff; a < last; ++a) {
						if ((x = (T[Td + SA[PA + SA[a]]] & 0xff)) != v) {
							if (1 < (a - first)) { break; }
							v = x;
							first = a;
						}
					}
					if ((T[Td + SA[PA + SA[first]] - 1] & 0xff) < v) {
						first = this.ssSubstringPartition (PA, first, a, depth);
					}
					if ((a - first) <= (last - a)) {
						if (1 < (a - first)) {
							stack[ssize++] = new StackEntry (a, last, depth, -1);
							last = a;
							depth += 1;
							limit = ssLog (a - first);
						} else {
							first = a;
							limit = -1;
						}
					} else {
						if (1 < (last - a)) {
							stack[ssize++] = new StackEntry (first, a, depth + 1, ssLog (a - first));
							first = a;
							limit = -1;
						} else {
							last = a;
							depth += 1;
							limit = ssLog (a - first);
						}
					}
					continue;
				}

				a = this.ssPivot (Td, PA, first, last);
				v = T[Td + SA[PA + SA[a]]] & 0xff;
				swapElements (SA, first, SA, a);

			    int b;
			    for (b = first; (++b < last) && ((x = (T[Td + SA[PA + SA[b]]] & 0xff)) == v); ) { }
				if (((a = b) < last) && (x < v)) {
					for (; (++b < last) && ((x = (T[Td + SA[PA + SA[b]]] & 0xff)) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a);
							++a;
						}
					}
				}
			    int c;
			    for (c = last; (b < --c) && ((x = (T[Td + SA[PA + SA[c]]] & 0xff)) == v);) { }
			    int d;
			    if ((b < (d = c)) && (x > v)) {
					for (; (b < --c) && ((x = (T[Td + SA[PA + SA[c]]] & 0xff)) >= v);) {
						if (x == v) {
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}
				for (; b < c;) {
					swapElements (SA, b, SA, c);
					for (; (++b < c) && ((x = (T[Td + SA[PA + SA[b]]] & 0xff)) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a);
							++a;
						}
					}
					for (; (b < --c) && ((x = (T[Td + SA[PA + SA[c]]] & 0xff)) >= v);) {
						if (x == v) { 
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}

				if (a <= d) {
					c = b - 1;

				    int s;
				    int t;
				    if ((s = a - first) > (t = b - a)) {
						s = t;
					}
				    int e;
				    int f;
				    for (e = first, f = b - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}
					if ((s = d - c) > (t = last - d - 1)) {
						s = t;
					}
					for (e = b, f = last - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}

					a = first + (b - a);
					c = last - (d - c);
					b = (v <= (T[Td + SA[PA + SA[a]] - 1] & 0xff)) ? a : this.ssSubstringPartition (PA, a, c, depth);

					if ((a - first) <= (last - c)) {
						if ((last - c) <= (c - b)) {
							stack[ssize++] = new StackEntry (b, c, depth + 1, ssLog (c - b));
							stack[ssize++] = new StackEntry (c, last, depth, limit);
							last = a;
						} else if ((a - first) <= (c - b)) {
							stack[ssize++] = new StackEntry (c, last, depth, limit);
							stack[ssize++] = new StackEntry (b, c, depth + 1, ssLog (c - b));
							last = a;
						} else {
							stack[ssize++] = new StackEntry (c, last, depth, limit);
							stack[ssize++] = new StackEntry (first, a, depth, limit);
							first = b;
							last = c;
							depth += 1;
							limit = ssLog (c - b);
						}
					} else {
						if ((a - first) <= (c - b)) {
							stack[ssize++] = new StackEntry (b, c, depth + 1, ssLog (c - b));
							stack[ssize++] = new StackEntry (first, a, depth, limit);
							first = c;
						} else if ((last - c) <= (c - b)) {
							stack[ssize++] = new StackEntry (first, a, depth, limit);
							stack[ssize++] = new StackEntry (b, c, depth + 1, ssLog (c - b));
							first = c;
						} else {
							stack[ssize++] = new StackEntry (first, a, depth, limit);
							stack[ssize++] = new StackEntry (c, last, depth, limit);
							first = b;
							last = c;
							depth += 1;
							limit = ssLog (c - b);
						}
					}
				} else {
					limit += 1;
					if ((T[Td + SA[PA + SA[first]] - 1] & 0xff) < v) {
						first = this.ssSubstringPartition (PA, first, last, depth);
						limit = ssLog (last - first);
					}
					depth += 1;
				}
			}
		}
			
		private static void ssBlockSwap (int[] array1,  int first1,  int[] array2,  int first2,  int size) 
		{
			int a, b;
			int i;
			for (i = size, a = first1, b = first2; 0 < i; --i, ++a, ++b) {
				swapElements (array1, a, array2, b);
			}
		}

		private void ssMergeForward (int PA, int[] buf,  int bufoffset,  int first,  int middle,  int last,  int depth) 
        {
		    int i, j, k;
			int t;

		    int bufend = bufoffset + (middle - first) - 1;
			ssBlockSwap (buf, bufoffset, SA, first, middle - first);

			for (t = SA[first], i = first, j = bufoffset, k = middle;;)
			{
			    int r = this.ssCompare (PA + buf[j], PA + SA[k], depth);
			    if (r < 0) {
					do {
						SA[i++] = buf[j];
						if (bufend <= j) {
							buf[j] = t;
							return;
						}
						buf[j++] = SA[i];
					} while (buf[j] < 0);
				} else if (r > 0) {
					do {
						SA[i++] = SA[k];
						SA[k++] = SA[i];
						if (last <= k) {
							while (j < bufend) { SA[i++] = buf[j]; buf[j++] = SA[i]; }
							SA[i] = buf[j]; buf[j] = t;
							return;
						}
					} while (SA[k] < 0);
				} else {
					SA[k] = ~SA[k];
					do {
						SA[i++] = buf[j];
						if (bufend <= j) {
							buf[j] = t;
							return;
						}
						buf[j++] = SA[i];
					} while (buf[j] < 0);

					do {
						SA[i++] = SA[k];
						SA[k++] = SA[i];
						if (last <= k) {
							while (j < bufend) {
								SA[i++] = buf[j];
								buf[j++] = SA[i];
							}
							SA[i] = buf[j]; buf[j] = t;
							return;
						}
					} while (SA[k] < 0);
				}
			}
        }

		private void ssMergeBackward (int PA, int[] buf,  int bufoffset,  int first,  int middle,  int last,  int depth) 
		{
			int p1, p2;
		    int i, j, k;
			int t;

		    int bufend = bufoffset + (last - middle);
			ssBlockSwap (buf, bufoffset, SA, middle, last - middle);

			int x = 0;
			if (buf[bufend - 1] < 0) {
				x |=  1;
				p1 = PA + ~buf[bufend - 1];
			} else {
				p1 = PA +  buf[bufend - 1];
			}
			if (SA[middle - 1] < 0) {
				x |=  2;
				p2 = PA + ~SA[middle - 1];
			} else {
				p2 = PA +  SA[middle - 1];
			}
			for (t = SA[last - 1], i = last - 1, j = bufend - 1, k = middle - 1;;)
			{
			    int r = this.ssCompare (p1, p2, depth);
			    if (r > 0) {
					if ((x & 1) != 0) {
						do {
							SA[i--] = buf[j];
							buf[j--] = SA[i];
						} while (buf[j] < 0);
						x ^= 1;
					}
					SA[i--] = buf[j];
					if (j <= bufoffset) {
						buf[j] = t;
						return;
					}
					buf[j--] = SA[i];

					if (buf[j] < 0) {
						x |=  1;
						p1 = PA + ~buf[j];
					} else {
						p1 = PA +  buf[j];
					}
				} else if (r < 0) {
					if ((x & 2) != 0) {
						do {
							SA[i--] = SA[k];
							SA[k--] = SA[i];
						} while (SA[k] < 0);
						x ^= 2;
					}
					SA[i--] = SA[k];
					SA[k--] = SA[i];
					if (k < first) {
						while (bufoffset < j) {
							SA[i--] = buf[j];
							buf[j--] = SA[i];
						}
						SA[i] = buf[j];
						buf[j] = t;
						return;
					}

					if (SA[k] < 0) {
						x |=  2;
						p2 = PA + ~SA[k];
					} else {
						p2 = PA +  SA[k];
					}
				} else {
					if ((x & 1) != 0) {
						do {
							SA[i--] = buf[j];
							buf[j--] = SA[i];
						} while (buf[j] < 0);
						x ^= 1;
					}
					SA[i--] = ~buf[j];
					if (j <= bufoffset) {
						buf[j] = t;
						return;
					}
					buf[j--] = SA[i];

					if ((x & 2) != 0) {
						do {
							SA[i--] = SA[k];
							SA[k--] = SA[i];
						} while (SA[k] < 0);
						x ^= 2;
					}
					SA[i--] = SA[k];
					SA[k--] = SA[i];
					if (k < first) {
						while (bufoffset < j) {
							SA[i--] = buf[j];
							buf[j--] = SA[i];
						}
						SA[i] = buf[j];
						buf[j] = t;
						return;
					}

					if (buf[j] < 0) {
						x |=  1;
						p1 = PA + ~buf[j];
					} else {
						p1 = PA +  buf[j];
					}
					if (SA[k] < 0) {
						x |=  2;
						p2 = PA + ~SA[k];
					} else {
						p2 = PA +  SA[k];
					}
				}
			}
		}

		private  static int getIDX (int a) 
		{
			return (0 <= a) ? a : ~a;
		}

		private void ssMergeCheckEqual (int PA,  int depth,  int a) 
		{
			if ((0 <= SA[a]) && (this.ssCompare (PA + getIDX (SA[a - 1]), PA + SA[a], depth) == 0))
			{
				SA[a] = ~SA[a];
			}
		}

		private void ssMerge (int PA, int first, int middle, int last, int[] buf,  int bufoffset,  int bufsize,  int depth) 
		{
			var stack = new StackEntry[STACK_SIZE];

		    int ssize;
		    int check;

		    for (check = 0, ssize = 0;;) 
            {
				if ((last - middle) <= bufsize) {
					if ((first < middle) && (middle < last)) {
						this.ssMergeBackward (PA, buf, bufoffset, first, middle, last, depth);
					}

					if ((check & 1) != 0) {
						this.ssMergeCheckEqual (PA, depth, first);
					}
					if ((check & 2) != 0) {
						this.ssMergeCheckEqual (PA, depth, last);
					}
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					middle = entry.b;
					last = entry.c;
					check = entry.d;
					continue;
				}

				if ((middle - first) <= bufsize) {
					if (first < middle) {
						this.ssMergeForward ( PA, buf, bufoffset, first, middle, last, depth);
					}
					if ((check & 1) != 0) {
						this.ssMergeCheckEqual (PA, depth, first);
					}
					if ((check & 2) != 0) {
						this.ssMergeCheckEqual (PA, depth, last);
					}
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					middle = entry.b;
					last = entry.c;
					check = entry.d;
					continue;
				}

			    int m;
			    int len;
			    int half;
			    for (
					m = 0, len = Math.Min (middle - first, last - middle), half = len >> 1;
					0 < len;
					len = half, half >>= 1
				)
				{
					if (this.ssCompare (PA + getIDX (SA[middle + m + half]),
						PA + getIDX (SA[middle - m - half - 1]), depth) < 0) {
						m += half + 1;
						half -= (len & 1) ^ 1;
					}
				}

				if (0 < m) {
					ssBlockSwap (SA, middle - m, SA, middle, m);
				    int j;
				    int i = j = middle;
					int next = 0;
					if ((middle + m) < last) {
						if (SA[middle + m] < 0) {
							for (; SA[i - 1] < 0; --i)
							{
							}
						    SA[middle + m] = ~SA[middle + m];
						}
						for (j = middle; SA[j] < 0; ++j)
						{
						}
					    next = 1;
					}
					if ((i - first) <= (last - j)) {
						stack[ssize++] = new StackEntry (j, middle + m, last, (check &  2) | (next & 1));
						middle -= m;
						last = i;
						check = (check & 1);
					} else {
						if ((i == middle) && (middle == j)) {
							next <<= 1;
						}
						stack[ssize++] = new StackEntry (first, middle - m, i, (check & 1) | (next & 2));
						first = j;
						middle += m;
						check = (check & 2) | (next & 1);
					}
				} else {
					if ((check & 1) != 0) {
						this.ssMergeCheckEqual (PA, depth, first);
					}
					this.ssMergeCheckEqual (PA, depth, middle);
					if ((check & 2) != 0) {
						this.ssMergeCheckEqual (PA, depth, last);
					}
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					middle = entry.b;
					last = entry.c;
					check = entry.d;
				}
			}
		}

		private void subStringSort (int PA, int first,  int last,  int[] buf,  int bufoffset,  int bufsize,  int depth,  bool lastsuffix,  int size) 
		{
		    int a;
		    int i;
		    int k;

		    if (lastsuffix) 
				++first;
			
			for (a = first, i = 0; (a + SS_BLOCKSIZE) < last; a += SS_BLOCKSIZE, ++i) {
				this.ssMultiKeyIntroSort (PA, a, a + SS_BLOCKSIZE, depth);
				int[] curbuf = SA;
				int curbufoffset = a + SS_BLOCKSIZE;
				int curbufsize = last - (a + SS_BLOCKSIZE);
				if (curbufsize <= bufsize) {
					curbufsize = bufsize;
					curbuf = buf;
					curbufoffset = bufoffset;
				}
			    int b;
			    int j;
			    for (b = a, k = SS_BLOCKSIZE, j = i; (j & 1) != 0; b -= k, k <<= 1, j >>= 1) 
					this.ssMerge (PA, b - k, b, b + k, curbuf, curbufoffset, curbufsize, depth);				
			}

			this.ssMultiKeyIntroSort (PA, a, last, depth);

			for (k = SS_BLOCKSIZE; i != 0; k <<= 1, i >>= 1) {
				if ((i & 1) != 0) {
					this.ssMerge (PA, a - k, a, last, buf, bufoffset, bufsize, depth);
					a -= k;
				}
			}

			if (lastsuffix) {
				int r;
				for (
					a = first, i = SA[first - 1], r = 1;
					(a < last) && ((SA[a] < 0) || (0 < (r = this.ssCompareLast (PA, PA + i, PA + SA[a], depth, size))));
					++a
				)
				{
					SA[a - 1] = SA[a];
				}
				if (r == 0) {
					SA[a] = ~SA[a];
				}
				SA[a - 1] = i;
			}
		}

		private int trGetC (int ISA,  int ISAd,  int ISAn,  int p) 
		{
			return (((ISAd + p) < ISAn) ? this.SA[ISAd + p] : this.SA[ISA + ((ISAd - ISA + p) % (ISAn - ISA))]);
		}

		private void trFixdown (int ISA,  int ISAd,  int ISAn,  int sa, int i,  int size) 
		{
			int j, k;
			int v;
		    int c;

		    for (v = SA[sa +i], c = this.trGetC (ISA, ISAd, ISAn, v); (j = 2 * i + 1) < size; SA[sa + i] = SA[sa + k], i = k) {
				k = j++;
				int d = this.trGetC (ISA, ISAd, ISAn, SA[sa + k]);
		        int e;
		        if (d < (e = this.trGetC (ISA, ISAd, ISAn, SA[sa + j]))) {
					k = j;
					d = e;
				}
				if (d <= c) {
					break;
				}
			}
			SA[sa + i] = v;
		}

		private void trHeapSort (int ISA,  int ISAd,  int ISAn,  int sa,  int size) 
		{
			int i;

		    int m = size;
			if ((size % 2) == 0) {
				m--;
				if (this.trGetC (ISA, ISAd, ISAn, SA[sa + (m / 2)]) < this.trGetC (ISA, ISAd, ISAn, SA[sa + m])) {
					swapElements (SA, sa + m, SA, sa + (m / 2));
				}
			}

			for (i = m / 2 - 1; 0 <= i; --i) {
				this.trFixdown (ISA, ISAd, ISAn, sa, i, m);
			}

			if ((size % 2) == 0) {
				swapElements (SA, sa + 0, SA, sa + m);
				this.trFixdown (ISA, ISAd, ISAn, sa, 0, m);
			}

			for (i = m - 1; 0 < i; --i) {
				int t = SA[sa + 0];
				SA[sa + 0] = SA[sa + i];
				this.trFixdown (ISA, ISAd, ISAn, sa, 0, i);
				SA[sa + i] = t;
			}
		}
			
		private void trInsertionSort (int ISA,  int ISAd,  int ISAn, int first, int last) 
        {
		    int a;

		    for (a = first + 1; a < last; ++a) {
			    int b;
		        int t;
		        int r;
		        for (t = SA[a], b = a - 1; 0 > (r = this.trGetC (ISA, ISAd, ISAn, t) - this.trGetC (ISA, ISAd, ISAn, SA[b]));) {
					do {
						SA[b + 1] = SA[b];
					} while ((first <= --b) && (SA[b] < 0));
					if (b < first) {
						break;
					}
				}
				if (r == 0) {
					SA[b] = ~SA[b];
				}
				SA[b + 1]= t;
			}
		}
			
		private static int trLog (int x) 
		{
			return ((x & 0xffff0000) != 0) ?
				(((x & 0xff000000) != 0) ? 24 + log2table[(x >> 24) & 0xff] : 16 + log2table[(x >> 16) & 0xff])
					: (((x & 0x0000ff00) != 0) ? 8 + log2table[(x >>  8) & 0xff] : 0 + log2table[(x >>  0) & 0xff]);
		}

		private int trMedian3 (int ISA,  int ISAd,  int ISAn, int v1, int v2, int v3) 
		{
			var SA_v1 = this.trGetC (ISA, ISAd, ISAn, SA[v1]);
			var SA_v2 = this.trGetC (ISA, ISAd, ISAn, SA[v2]);
			var SA_v3 = this.trGetC (ISA, ISAd, ISAn, SA[v3]);

			if (SA_v1 > SA_v2) {
				 var temp = v1;
				v1 = v2;
				v2 = temp;
				 var SA_vtemp = SA_v1;
				SA_v1 = SA_v2;
				SA_v2 = SA_vtemp;
			}
			if (SA_v2 > SA_v3)
			{
			    return SA_v1 > SA_v3 ? v1 : v3;
			}

		    return v2;
		}

		private int trMedian5 (int ISA,  int ISAd,  int ISAn, int v1, int v2, int v3, int v4, int v5) 
		{
			var SA_v1 = this.trGetC (ISA, ISAd, ISAn, SA[v1]);
			var SA_v2 = this.trGetC (ISA, ISAd, ISAn, SA[v2]);
			var SA_v3 = this.trGetC (ISA, ISAd, ISAn, SA[v3]);
			var SA_v4 = this.trGetC (ISA, ISAd, ISAn, SA[v4]);
			var SA_v5 = this.trGetC (ISA, ISAd, ISAn, SA[v5]);
			int temp;
			int SA_vtemp;

            // ReSharper disable RedundantAssignment
            if (SA_v2 > SA_v3)
            {
				temp = v2;
				v2 = v3;
				v3 = temp;
				SA_vtemp = SA_v2;
				SA_v2 = SA_v3;
				SA_v3 = SA_vtemp;
			}
			if (SA_v4 > SA_v5) {
				temp = v4;
				v4 = v5;
				v5 = temp;
				SA_vtemp = SA_v4;
				SA_v4 = SA_v5;
				SA_v5 = SA_vtemp;
			}
			if (SA_v2 > SA_v4) {
				temp = v2;
				v2 = v4;
				v4 = temp;
				SA_vtemp = SA_v2;
				SA_v2 = SA_v4;
				SA_v4 = SA_vtemp;
				temp = v3;
				v3 = v5;
				v5 = temp;
				SA_vtemp = SA_v3;
				SA_v3 = SA_v5;
				SA_v5 = SA_vtemp;
			}
			if (SA_v1 > SA_v3) {
				temp = v1;
				v1 = v3;
				v3 = temp;
				SA_vtemp = SA_v1;
				SA_v1 = SA_v3;
				SA_v3 = SA_vtemp;
			}
			if (SA_v1 > SA_v4) {
				temp = v1;
				v1 = v4;
				v4 = temp;
				SA_vtemp = SA_v1;
				SA_v1 = SA_v4;
				SA_v4 = SA_vtemp;
				temp = v3;
				v3 = v5;
				v5 = temp;
				SA_vtemp = SA_v3;
				SA_v3 = SA_v5;
				SA_v5 = SA_vtemp;
			}
            // ReSharper restore RedundantAssignment
            
            return SA_v3 > SA_v4 ? v4 : v3;
		}
			
		private int trPivot (int ISA,  int ISAd,  int ISAn,  int first,  int last) 
		{
		    int t = last - first;
			int middle = first + t / 2;

			if (t <= 512) {
				if (t <= 32) {
					return this.trMedian3 (ISA, ISAd, ISAn, first, middle, last - 1);
				}
				t >>= 2;
				return this.trMedian5 (
					ISA, ISAd, ISAn,
					first, first + t,
					middle,
					last - 1 - t, last - 1
				);
			}
			t >>= 3;
			return this.trMedian3 (
				ISA, ISAd, ISAn,
				this.trMedian3 (ISA, ISAd, ISAn, first, first + t, first + (t << 1)),
				this.trMedian3 (ISA, ISAd, ISAn, middle - t, middle, middle + t),
				this.trMedian3 (ISA, ISAd, ISAn, last - 1 - (t << 1), last - 1 - t, last - 1)
			);
		}

		private void lsUpdateGroup (int ISA,  int first,  int last) 
		{
		    int a;

		    for (a = first; a < last; ++a) {
		        int b;
		        if (0 <= SA[a]) {
					b = a;
					do {
						SA[ISA + SA[a]] = a;
					} while ((++a < last) && (0 <= SA[a]));
					SA[b] = b - a;
					if (last <= a) {
						break;
					}
				}
				b = a;
				do {
					SA[a] = ~SA[a];
				} while (SA[++a] < 0);
				int t = a;
				do {
					SA[ISA + SA[b]] = t;
				} while (++b <= a);
			}
		}
			
		private void lsIntroSort (int ISA,  int ISAd,  int ISAn, int first, int last) 
		{
			var stack = new StackEntry[STACK_SIZE];

		    int limit;
		    int x = 0;
		    int ssize;

			for (ssize = 0, limit = trLog (last - first);;) {

				if ((last - first) <= INSERTIONSORT_THRESHOLD) {
					if (1 < (last - first)) {
						this.trInsertionSort (ISA, ISAd, ISAn, first, last);
						this.lsUpdateGroup (ISA, first, last);
					} else if ((last - first) == 1) {
						SA[first] = -1;
					}
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					last = entry.b;
					limit = entry.c;
					continue;
				}

			    int a;
			    int b;
			    if (limit-- == 0) {
					this.trHeapSort (ISA, ISAd, ISAn, first, last - first);
					for (a = last - 1; first < a; a = b) {
						for (
							x = this.trGetC (ISA, ISAd, ISAn, SA[a]), b = a - 1;
							(first <= b) && (this.trGetC (ISA, ISAd, ISAn, SA[b]) == x);
							--b
						)
						{
							SA[b] = ~SA[b];
						}
					}
					this.lsUpdateGroup (ISA, first, last);
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					last = entry.b;
					limit = entry.c;
					continue;
				}

				a = this.trPivot (ISA, ISAd, ISAn, first, last);
				swapElements (SA, first, SA, a);
				int v = this.trGetC (ISA, ISAd, ISAn, SA[first]);

				for (b = first; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) == v);)
				{
				}
			    if (((a = b) < last) && (x < v)) {
					for (; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a);
							++a;
						}
					}
				}
			    int c;
			    for (c = last; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) == v);)
				{
				}
			    int d;
			    if ((b < (d = c)) && (x > v)) {
					for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
						if (x == v) {
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}
				for (; b < c;) {
					swapElements (SA, b, SA, c);
					for (; (++b < c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a);
							++a;
						}
					}
					for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
						if (x == v) {
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}

				if (a <= d) {
					c = b - 1;

				    int s;
				    int t;
				    if ((s = a - first) > (t = b - a)) {
						s = t;
					}
				    int e;
				    int f;
				    for (e = first, f = b - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}
					if ((s = d - c) > (t = last - d - 1)) {
						s = t;
					}
					for (e = b, f = last - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}

					a = first + (b - a);
					b = last - (d - c);

					for (c = first, v = a - 1; c < a; ++c) {
						SA[ISA + SA[c]] = v;
					}
					if (b < last) {
						for (c = a, v = b - 1; c < b; ++c) {
							SA[ISA + SA[c]] = v;
						}
					}
					if ((b - a) == 1) {
						SA[a] = - 1;
					}

					if ((a - first) <= (last - b)) {
						if (first < a) {
							stack[ssize++] = new StackEntry (b, last, limit, 0);
							last = a;
						} else {
							first = b;
						}
					} else {
						if (b < last) {
							stack[ssize++] = new StackEntry (first, a, limit, 0);
							first = b;
						} else {
							last = a;
						}
					}
				} else {
					if (ssize == 0) return;
					var entry = stack[--ssize];
					first = entry.a;
					last = entry.b;
					limit = entry.c;
				}
			}
		}

		private void lsSort (int ISA,  int x, int depth) 
		{
			int ISAd;

		    for (ISAd = ISA + depth; -x < SA[0]; ISAd += (ISAd - ISA)) {
				int first = 0;
				int skip = 0;
			    int last;
		        int t;
		        do {
					if ((t = SA[first]) < 0) {
						first -= t;
						skip += t;
					} else {
						if (skip != 0) {
							SA[first + skip] = skip;
							skip = 0;
						}
						last = SA[ISA + t] + 1;
						this.lsIntroSort (ISA, ISAd, ISA + x, first, last);
						first = last;
					}
				} while (first < x);

				if (skip != 0) 
					SA[first + skip] = skip;
				
				if (x < (ISAd - ISA)) {
					first = 0;
					do {
						if ((t = SA[first]) < 0) 
							first -= t;
						else {
							last = SA[ISA + t] + 1;
						    int i;
						    for (i = first; i < last; ++i) 
								SA[ISA + SA[i]] = i;
							first = last;
						}
					} while (first < x);
					break;
				}
			}
		}

		private PartitionResult trPartition (int ISA,  int ISAd,  int ISAn, int first, int last,  int v) 
		{
		    int a, b, c, d;
		    var x = 0;

			for (b = first - 1; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) == v);)
			{
			}
		    if (((a = b) < last) && (x < v)) {
				for (; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
					if (x == v) {
						swapElements (SA, b, SA, a); ++a;
					}
				}
			}
			for (c = last; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) == v);)
			{
			}
		    if ((b < (d = c)) && (x > v)) {
				for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
					if (x == v) {
						swapElements (SA, c, SA, d); --d;
					}
				}
			}
			for (; b < c;) {
				swapElements (SA, b, SA, c);
				for (; (++b < c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
					if (x == v) {
						swapElements (SA, b, SA, a); ++a;
					}
				}
				for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
					if (x == v) {
						swapElements (SA, c, SA, d); --d;
					}
				}
			}

			if (a <= d) {
				c = b - 1;
			    int t;
			    int s;
			    if ((s = a - first) > (t = b - a)) 
					s = t;
				
			    int e;
			    int f;
			    for (e = first, f = b - s; 0 < s; --s, ++e, ++f) {
					swapElements (SA, e, SA, f);
				}
				if ((s = d - c) > (t = last - d - 1)) {
					s = t;
				}
				for (e = b, f = last - s; 0 < s; --s, ++e, ++f) {
					swapElements (SA, e, SA, f);
				}
				first += (b - a);
				last -= (d - c);
			}

			return new PartitionResult (first, last);
		}

		private void trCopy (int ISA,  int ISAn,  int first,  int a,  int b,  int last,  int depth) 
		{
			int c, d, e;
			int s;
			int v = b - 1;

			for (c = first, d = a - 1; c <= d; ++c) {
				if ((s = SA[c] - depth) < 0) {
					s += ISAn - ISA;
				}
				if (SA[ISA +s] == v) {
					SA[++d] = s;
					SA[ISA +s] = d;
				}
			}
			for (c = last - 1, e = d + 1, d = b; e < d; --c) {
				if ((s = SA[c] - depth) < 0) {
					s += ISAn - ISA;
				}
				if (SA[ISA +s] == v) {
					SA[--d] = s;
					SA[ISA + s] = d;
				}
			}
		}
			
		private void trIntroSort (int ISA, int ISAd, int ISAn, int first, int last,  TRBudget budget,  int size) 
		{
			 var stack = new StackEntry[STACK_SIZE];

		    int s;
		    int x = 0;
		    int limit;
		    int ssize;

			for (ssize = 0, limit = trLog (last - first);;) {
			    int a;
			    int b;
			    int c;
			    int v;
			    int next;
			    if (limit < 0) {
					if (limit == -1) {
						if (!budget.update (size, last - first)) break; 
						var result = this.trPartition (ISA, ISAd - 1, ISAn, first, last, last - 1);
						a = result.first;
						b = result.last;
						if ((first < a) || (b < last)) {
							if (a < last) {
								for (c = first, v = a - 1; c < a; ++c) {
									SA[ISA + SA[c]] = v;
								}
							}
							if (b < last) {
								for (c = a, v = b - 1; c < b; ++c) {
									SA[ISA + SA[c]] = v;
								}
							}

							stack[ssize++] = new StackEntry (0, a, b, 0);
							stack[ssize++] = new StackEntry (ISAd - 1, first, last, -2);
							if ((a - first) <= (last - b)) {
								if (1 < (a - first)) {
									stack[ssize++] = new StackEntry (ISAd, b, last, trLog (last - b));
									last = a; limit = trLog (a - first);
								} else if (1 < (last - b)) {
									first = b; limit = trLog (last - b);
								} else {
									if (ssize == 0) return;
									var entry = stack[--ssize];
									ISAd = entry.a;
									first = entry.b;
									last = entry.c;
									limit = entry.d;
								}
							} else {
								if (1 < (last - b)) {
									stack[ssize++] = new StackEntry (ISAd, first, a, trLog (a - first));
									first = b;
									limit = trLog (last - b);
								} else if (1 < (a - first)) {
									last = a;
									limit = trLog (a - first);
								} else {
									if (ssize == 0) return;
									var entry = stack[--ssize];
									ISAd = entry.a;
									first = entry.b;
									last = entry.c;
									limit = entry.d;
								}
							}
						} else {
							for (c = first; c < last; ++c) {
								SA[ISA + SA[c]] = c;
							}
							if (ssize == 0) return;
							var entry = stack[--ssize];
							ISAd = entry.a;
							first = entry.b;
							last = entry.c;
							limit = entry.d;
						}
					} else if (limit == -2) {
						a = stack[--ssize].b;
						b = stack[ssize].c;
						this.trCopy (ISA, ISAn, first, a, b, last, ISAd - ISA);
						if (ssize == 0) return;
						var entry = stack[--ssize];
						ISAd = entry.a;
						first = entry.b;
						last = entry.c;
						limit = entry.d;
					} else {
						if (0 <= SA[first]) {
							a = first;
							do {
								SA[ISA + SA[a]] = a;
							} while ((++a < last) && (0 <= SA[a]));
							first = a;
						}
						if (first < last) {
							a = first;
							do {
								SA[a] = ~SA[a];
							} while (SA[++a] < 0);
							next = (SA[ISA + SA[a]] != SA[ISAd + SA[a]]) ? trLog (a - first + 1) : -1;
							if (++a < last) {
								for (b = first, v = a - 1; b < a; ++b) {
									SA[ISA + SA[b]] = v;
								}
							}

							if ((a - first) <= (last - a)) {
								stack[ssize++] = new StackEntry (ISAd, a, last, -3);
								ISAd += 1; last = a; limit = next;
							} else {
								if (1 < (last - a)) {
									stack[ssize++] = new StackEntry (ISAd + 1, first, a, next);
									first = a; limit = -3;
								} else {
									ISAd += 1; last = a; limit = next;
								}
							}
						} else {
							if (ssize == 0) return;
							var entry = stack[--ssize];
							ISAd = entry.a;
							first = entry.b;
							last = entry.c;
							limit = entry.d;
						}
					}
					continue;
				}

				if ((last - first) <= INSERTIONSORT_THRESHOLD) {
					if (!budget.update (size, last - first)) break;
					this.trInsertionSort (ISA, ISAd, ISAn, first, last);
					limit = -3;
					continue;
				}

				if (limit-- == 0) {
					if (!budget.update (size, last - first)) break;
					this.trHeapSort (ISA, ISAd, ISAn, first, last - first);
					for (a = last - 1; first < a; a = b) {
						for (
							x = this.trGetC (ISA, ISAd, ISAn, SA[a]), b = a - 1;
							(first <= b) && (this.trGetC (ISA, ISAd, ISAn, SA[b]) == x);
							--b
						)
						{
							SA[b] = ~SA[b];
						}
					}
					limit = -3;
					continue;
				}

				a = this.trPivot (ISA, ISAd, ISAn, first, last);

				swapElements (SA, first, SA, a);
				v = this.trGetC (ISA, ISAd, ISAn, SA[first]);
				for (b = first; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) == v);)
				{
				}
			    if (((a = b) < last) && (x < v)) {
					for (; (++b < last) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a); ++a;
						}
					}
				}
				for (c = last; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) == v);)
				{
				}
			    int d;
			    if ((b < (d = c)) && (x > v)) {
					for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
						if (x == v) {
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}
				for (; b < c;) {
					swapElements (SA, b, SA, c);
					for (; (++b < c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[b])) <= v);) {
						if (x == v) {
							swapElements (SA, b, SA, a);
							++a;
						}
					}
					for (; (b < --c) && ((x = this.trGetC (ISA, ISAd, ISAn, SA[c])) >= v);) {
						if (x == v) {
							swapElements (SA, c, SA, d);
							--d;
						}
					}
				}

				if (a <= d) {
					c = b - 1;

				    int t;
				    if ((s = a - first) > (t = b - a)) {
						s = t;
					}
				    int e;
				    int f;
				    for (e = first, f = b - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}
					if ((s = d - c) > (t = last - d - 1)) {
						s = t;
					}
					for (e = b, f = last - s; 0 < s; --s, ++e, ++f) {
						swapElements (SA, e, SA, f);
					}

					a = first + (b - a);
					b = last - (d - c);
					next = (SA[ISA + SA[a]] != v) ? trLog (b - a) : -1;

					for (c = first, v = a - 1; c < a; ++c) {
						SA[ISA + SA[c]] = v;
					}
					if (b < last) {
						for (c = a, v = b - 1; c < b; ++c) {
							SA[ISA + SA[c]] = v; }
					}

					if ((a - first) <= (last - b)) {
						if ((last - b) <= (b - a)) {
							if (1 < (a - first)) {
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								last = a;
							} else if (1 < (last - b)) {
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								first = b;
							} else if (1 < (b - a)) {
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								if (ssize == 0) return;
								var entry = stack[--ssize];
								ISAd = entry.a;
								first = entry.b;
								last = entry.c;
								limit = entry.d;
							}
						} else if ((a - first) <= (b - a)) {
							if (1 < (a - first)) {
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								last = a;
							} else if (1 < (b - a)) {
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								first = b;
							}
						} else {
							if (1 < (b - a)) {
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								last = a;
							}
						}
					} else {
						if ((a - first) <= (b - a)) {
							if (1 < (last - b)) {
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								first = b;
							} else if (1 < (a - first)) {
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								last = a;
							} else if (1 < (b - a)) {
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								stack[ssize++] = new StackEntry (ISAd, first, last, limit);
							}
						} else if ((last - b) <= (b - a)) {
							if (1 < (last - b)) {
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								stack[ssize++] = new StackEntry (ISAd + 1, a, b, next);
								first = b;
							} else if (1 < (b - a)) {
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								last = a;
							}
						} else {
							if (1 < (b - a)) {
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								stack[ssize++] = new StackEntry (ISAd, b, last, limit);
								ISAd += 1;
								first = a;
								last = b;
								limit = next;
							} else {
								stack[ssize++] = new StackEntry (ISAd, first, a, limit);
								first = b;
							}
						}
					}
				} else {
					if (!budget.update (size, last - first)) break; // BUGFIX : Added to prevent an infinite loop in the original code
					limit += 1; ISAd += 1;
				}
			}

			for (s = 0; s < ssize; ++s) {
				if (stack[s].d == -3) {
					this.lsUpdateGroup (ISA, stack[s].b, stack[s].c);
				}
			}
		}

		private void trSort (int ISA,  int x,  int depth) 
        {
		    int first = 0;

		    if (-x < SA[0]) {
				var budget = new TRBudget (x, trLog (x) * 2 / 3 + 1);
				do
				{
				    int t;
				    if ((t = SA[first]) < 0) {
						first -= t;
					}
					else {
						int last = SA[ISA + t] + 1;
						if (1 < (last - first)) {
							this.trIntroSort (ISA, ISA + depth, ISA + x, first, last, budget, x);
							if (budget.chance == 0) {
								/* Switch to Larsson-Sadakane sorting algorithm. */
								if (0 < first) {
									SA[0] = -first;
								}
								this.lsSort (ISA, x, depth);
								break;
							}
						}
						first = last;

					}
				} while (first < x);
			}
		}

		private static  int BUCKET_B (int c0,  int c1) 
		{
			return (c1 << 8) | c0;
		}
			
		private static  int BUCKET_BSTAR (int c0,  int c1) 
		{
			return (c0 << 8) | c1;
		}

		private int sortTypeBstar (int[] bucketA,  int[] bucketB) 
		{
			var tempbuf = new int[256];

		    int i, j, k, t;
			int c0, c1;
			int flag;

			for (i = 1, flag = 1; i < n; ++i) {
				if (T[i - 1] != T[i]) {
					if ((T[i - 1] & 0xff) > (T[i] & 0xff)) {
						flag = 0;
					}
					break;
				}
			}
			i = n - 1;
			int m = n;

			int ti,ti1, t0;
			if (((ti = (T[i] & 0xff)) < (t0 = (T[0] & 0xff))) || ((T[i] == T[0]) && (flag != 0)))
            {
				if (flag == 0) {
					++bucketB[BUCKET_BSTAR (ti, t0)];
					SA[--m] = i;
				} else {
					++bucketB[BUCKET_B (ti, t0)];
				}
				for (--i; (0 <= i) && ((ti = (T[i] & 0xff)) <= (ti1 = (T[i + 1] & 0xff))); --i) {
					++bucketB[BUCKET_B (ti, ti1)];
				}
			}

			for (; 0 <= i;) {
				do {
					++bucketA[T[i] & 0xff];
				} while ((0 <= --i) && ((T[i] & 0xff) >= (T[i + 1] & 0xff)));

				if (0 <= i) {
					++bucketB[BUCKET_BSTAR (T[i] & 0xff, T[i + 1] & 0xff)];
					SA[--m] = i;
					for (--i; (0 <= i) && ((ti = (T[i] & 0xff)) <= (ti1 = (T[i + 1] & 0xff))); --i) {
						++bucketB[BUCKET_B (ti, ti1)];
					}
				}
			}
			m = n - m;
			if (m == 0) {
				for (i = 0; i < n; ++i) {
					SA[i] = i;
				}
				return 0;
			}

			for (c0 = 0, i = -1, j = 0; c0 < 256; ++c0) {
				t = i + bucketA[c0];
				bucketA[c0] = i + j;
				i = t + bucketB[BUCKET_B (c0, c0)];
				for (c1 = c0 + 1; c1 < 256; ++c1) {
					j += bucketB[BUCKET_BSTAR (c0, c1)];
					bucketB[(c0 << 8) | c1] = j;
					i += bucketB[BUCKET_B (c0, c1)];
				}
			}

			int PAb = n - m;
			int ISAb = m;
			for (i = m - 2; 0 <= i; --i) {
				t = SA[PAb + i];
				c0 = T[t] & 0xff;
				c1 = T[t + 1] & 0xff;
				SA[--bucketB[BUCKET_BSTAR (c0, c1)]] = i;
			}
			t = SA[PAb + m - 1];
			c0 = T[t] & 0xff;
			c1 = T[t + 1] & 0xff;
			SA[--bucketB[BUCKET_BSTAR (c0, c1)]] = m - 1;

			int[] buf = this.SA;
			int bufoffset = m;
			int bufsize = n - (2 * m);
			if (bufsize <= 256) {
				buf = tempbuf;
				bufoffset = 0;
				bufsize = 256;
			}

			for (c0 = 255, j = m; 0 < j; --c0) {
				for (c1 = 255; c0 < c1; j = i, --c1) {
					i = bucketB[BUCKET_BSTAR (c0, c1)];
					if (1 < (j - i)) {
						this.subStringSort (PAb, i, j, buf, bufoffset, bufsize, 2, SA[i] == (m - 1), n);
					}
				}
			}

			for (i = m - 1; 0 <= i; --i) {
				if (0 <= SA[i]) {
					j = i;
					do {
						SA[ISAb + SA[i]] = i;
					} while ((0 <= --i) && (0 <= SA[i]));
					SA[i + 1] = i - j;
					if (i <= 0) {
						break;
					}
				}
				j = i;
				do {
					SA[ISAb + (SA[i] = ~SA[i])] = j;
				} while (SA[--i] < 0);
				SA[ISAb + SA[i]] = j;
			}

			this.trSort (ISAb, m, 1);

			i = n - 1; j = m;
			if (((T[i] & 0xff) < (T[0] & 0xff)) || ((T[i] == T[0]) && (flag != 0)))
            {
				if (flag == 0) {
					SA[SA[ISAb + --j]] = i;
				}
				for (--i; (0 <= i) && ((T[i] & 0xff) <= (T[i + 1] & 0xff)); --i)
				{
				}
			}
			for (; 0 <= i;)
            {
				for (--i; (0 <= i) && ((T[i] & 0xff) >= (T[i + 1] & 0xff)); --i)
				{
				}
			    if (0 <= i) {
					SA[SA[ISAb + --j]] = i;
					for (--i; (0 <= i) && ((T[i] & 0xff) <= (T[i + 1] & 0xff)); --i)
					{
					}
			    }
			}

			for (c0 = 255, i = n - 1, k = m - 1; 0 <= c0; --c0)
            {
				for (c1 = 255; c0 < c1; --c1) {
					t = i - bucketB[BUCKET_B (c0, c1)];
					bucketB[BUCKET_B (c0, c1)] = i + 1;

					for (i = t, j = bucketB[BUCKET_BSTAR (c0, c1)]; j <= k; --i, --k) {
						SA[i] = SA[k];
					}
				}
				t = i - bucketB[BUCKET_B (c0, c0)];
				bucketB[BUCKET_B (c0, c0)] = i + 1;
				if (c0 < 255) {
					bucketB[BUCKET_BSTAR (c0, c0 + 1)] = t + 1;
				}
				i = bucketA[c0];
			}

			return m;
		}

		private int constructBWT (int[] bucketA,  int[] bucketB) 
		{
		    int i;
		    int t = 0;
		    int s, s1;
			int c0, c1, c2 = 0;
			var orig = -1;

			for (c1 = 254; 0 <= c1; --c1)
			{
			    int j;
			    for (i = bucketB[BUCKET_BSTAR (c1, c1 + 1)], j = bucketA[c1 + 1], t = 0, c2 = -1; i <= j; --j)
				{
					if (0 <= (s1 = s = SA[j]))
                    {
						if (--s < 0) 
							s = n - 1;

						if ((c0 = (T[s] & 0xff)) <= c1)
                        {
							SA[j] = ~s1;
							if ((0 < s) && ((T[s - 1] & 0xff) > c0)) {
								s = ~s;
							}
							if (c2 == c0) {
								SA[--t] = s;
							} else {
								if (0 <= c2) 
									bucketB[BUCKET_B (c2, c1)] = t;
								
								SA[t = bucketB[BUCKET_B (c2 = c0, c1)] - 1] = s;
							}
						}
					} else {
						SA[j] = ~s;
					}
				}
			}

		    for (i = 0; i < n; ++i)
            {
				if (0 <= (s1 = s = SA[i]))
                {
					if (--s < 0) 
						s = n - 1;

                    if ((c0 = (T[s] & 0xff)) >= (T[s + 1] & 0xff))
                    {
						if ((0 < s) && ((T[s - 1] & 0xff) < c0)) 
							s = ~s;
						
						if (c0 == c2)
                        {
							SA[++t] = s;
						} else {
							if (c2 != -1) // BUGFIX: Original code can write to bucketA[-1]
								bucketA[c2] = t;
							SA[t = bucketA[c2 = c0] + 1] = s;
						}
					}

				} else {
					s1 = ~s1;
				}

				if (s1 == 0) {
					SA[i] = T[n - 1];
					orig = i;
				} else {
					SA[i] = T[s1 - 1];
				}
			}

			return orig;
        }
        #endregion
    }
}
