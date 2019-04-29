using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cluster
{
    class HierarchicalClustering
    {
        private static readonly long serialVersionUID = 1L;

        /**
         * An n-1 by 2 matrix of which row i describes the merging of clusters at
         * step i of the clustering. If an element j in the row is less than n, then
         * observation j was merged at this stage. If j ≥ n then the merge
         * was with the cluster formed at the (earlier) stage j-n of the algorithm.
         */
        private int[][] merge;
        /**
         * A set of n-1 non-decreasing real values, which are the clustering height,
         * i.e., the value of the criterion associated with the clustering method
         * for the particular agglomeration.
         */
        private double[] height;

        /**
         * Learn the Agglomerative Hierarchical Clustering with given linkage
         * method, which includes proximity matrix.
         * linkage --> a linkage method to merge clusters. The linkage object
         * includes the proximity matrix of data.
         */
        public HierarchicalClustering(Linkage linkage)
        {
            double[][] proximity = linkage.getProximity();
            int n = proximity.Length;

            merge = new int[n - 1][];
            for (int i = 0; i < merge.Length; i++) merge[i] = new int[2];

            int[] id = new int[n];
            height = new double[n - 1];

            int[] points = new int[n];
            for (int i = 0; i < n; i++)
            {
                points[i] = i;
                id[i] = i;
            }

            FastPair fp = new FastPair(points, proximity);
            for (int i = 0; i < n - 1; i++)
            {
                height[i] = fp.getNearestPair(merge[i]);
                linkage.merge(merge[i][0], merge[i][1]);     // merge clusters into one
                fp.remove(merge[i][1]);           // drop b
                fp.updatePoint(merge[i][0]);      // and tell closest pairs about merger

                int p = merge[i][0];
                int q = merge[i][1];
                merge[i][0] = Math.Min(id[p], id[q]);
                merge[i][1] = Math.Max(id[p], id[q]);
                id[p] = n + i;
            }

            //if (linkage is UPGMCLinkage || linkage is WPGMCLinkage || linkage is WardLinkage)
            if (linkage is WardLinkage)
            {
                for (int i = 0; i < height.Length; i++)
                {
                    height[i] = Math.Sqrt(height[i]);
                }
            }
        }

        /**
         * Returns an n-1 by 2 matrix of which row i describes the merging of clusters at
         * step i of the clustering. If an element j in the row is less than n, then
         * observation j was merged at this stage. If j ≥ n then the merge
         * was with the cluster formed at the (earlier) stage j-n of the algorithm.
         */
        public int[][] getTree()
        {
            return merge;
        }

        /**
         * Returns a set of n-1 non-decreasing real values, which are the clustering height,
         * i.e., the value of the criterion associated with the clustering method
         * for the particular agglomeration.
         */
        public double[] getHeight()
        {
            return height;
        }

        /**
         * Cuts a tree into several groups by specifying the desired number.
         * @param k the number of clusters.
         * @return the cluster label of each sample.
         */
        public int[] partition(int k)
        {
            int n = merge.Length + 1;
            int[] membership = new int[n];

            IntHeapSelect heap = new IntHeapSelect(k);
            for (int i = 2; i <= k; i++)
            {
                heap.add(merge[n - i][0]);
                heap.add(merge[n - i][1]);
            }

            for (int i = 0; i < k; i++)
            {
                bfs(membership, heap.get(i), i);
            }

            return membership;
        }

        /**
         * Cuts a tree into several groups by specifying the cut height.
         * @param h the cut height.
         * @return the cluster label of each sample.
         */
        public int[] partition(double h)
        {
            for (int i = 0; i < height.Length - 1; i++)
            {
                if (height[i] > height[i + 1])
                {
                    throw new Exception("Non-monotonic cluster tree -- the linkage is probably not appropriate!");
                }
            }

            int n = merge.Length + 1;
            int k = 2;
            for (; k <= n; k++)
            {
                if (height[n - k] < h)
                {
                    break;
                }
            }

            if (k <= 2)
            {
                throw new ArgumentException("The parameter h is too large.");
            }

            return partition(k - 1);
        }

        /**
         * BFS the merge tree and identify cluster membership.
         * @param membership the cluster membership array.
         * @param cluster the last merge point of cluster.
         * @param id the cluster ID.
         */
        private void bfs(int[] membership, int cluster, int id)
        {
            int n = merge.Length + 1;
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(cluster.ToString());

            for (string si = queue.Dequeue(); si != null; si = queue.Dequeue())
            {
                int i = int.Parse(si);
                if (i < n)
                {
                    membership[i] = id;
                    continue;
                }

                i -= n;

                int m1 = merge[i][0];

                if (m1 >= n)
                {
                    queue.Enqueue(m1.ToString());
                }
                else
                {
                    membership[m1] = id;
                }

                int m2 = merge[i][1];

                if (m2 >= n)
                {
                    queue.Enqueue(m2.ToString());
                }
                else
                {
                    membership[m2] = id;
                }
            }
        }
    }
}
