using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BIRCHTree : Clustering
    {
        /**
        * Balanced Iterative Reducing and Clustering using Hierarchies. BIRCH performs
        * hierarchical clustering over particularly large datasets. An advantage of
        * BIRCH is its ability to incrementally and dynamically cluster incoming,
        * multi-dimensional metric data points in an attempt to produce the high
        * quality clustering for a given set of resources (memory and time constraints).
        * 
        * BIRCH has several advantages. For example, each clustering decision is made
        * without scanning all data points and currently existing clusters. It
        * exploits the observation that data space is not usually uniformly occupied
        * and not every data point is equally important. It makes full use of
        * available memory to derive the finest possible sub-clusters while minimizing
        * I/O costs. It is also an incremental method that does not require the whole
        * data set in advance.
        * 
        * This implementation produces a clustering in three steps. 
        * 
        * First step builds a CF (clustering feature) tree by a single scan of database.
        * Second step clusters the leaves of CF tree by hierarchical clustering.
        * Then the user can use the learned model to cluster input data in the final
        * step. In total, we scan the database twice.
        */

        private static long serialVersionUID = 1L;
        public static int B; // Branching factor. Maximum number of children nodes.
        public static double T; // Maximum radius of a sub-cluster.
        public static BIRCHNode root; // The root of CF tree.
        private VSM[] centroids; // Leaves of CF tree as representatives of all data points.
        int[] y; // Cluster label for each centroid (for the BIRCH leaves)
        private readonly int OUTLIER = -1;

        /**
        * Constructor.
        * B the branching factor. Maximum number of children nodes.
        * T the maximum radius of a sub-cluster.
        */
        public BIRCHTree(int B, double T)
        {
            BIRCHTree.B = B;
            BIRCHTree.T = T;
        }

        // Add a data point into CF tree.
        public void add(VSM x)
        {
            if (root == null)
            {
                root = new BIRCHNode();
                root.add(new BIRCHLeaf(x));
                root.update(x);
            }
            else
            {
                root.add(x);
            }
        }

        // Returns the branching factor, which is the maximum number of children nodes.
        public int getBrachingFactor()
        {
            return B;
        }

        // Returns the maximum radius of a sub-cluster.
        public double getMaxRadius()
        {
            return T;
        }

        // Clustering leaves of CF tree into k clusters.
        // k the number of clusters.
        // return the number of non-outlier leaves.
        public int partition(int k)
        {
            return partition(k, 0);
        }

        // Clustering leaves of CF tree into k clusters.
        // k the number of clusters.
        // minPts a CF leaf will be treated as outlier if the number of its points is less than minPts.
        // return the number of non-outlier leaves.
        public int partition(int k, int minPts)
        {
            List<BIRCHLeaf> leaves = new List<BIRCHLeaf>();
            List<VSM> centers = new List<VSM>();
            Queue<BIRCHNode> queue = new Queue<BIRCHNode>();
            queue.Enqueue(root);

            for (BIRCHNode node = queue.Dequeue(); node != null; node = queue.Dequeue())
            {
                if (node.numChildren == 0)
                {
                    if (node.n >= minPts)
                    {
                        VSM x = VSM.divideVSM(node.sum, node.n);
                        centers.Add(x);
                        leaves.Add((BIRCHLeaf)node);
                    }
                    else
                    {
                        BIRCHLeaf leaf = (BIRCHLeaf)node;
                        leaf.y = OUTLIER;
                    }
                }
                else
                {
                    for (int i = 0; i < node.numChildren; i++)
                    {
                        queue.Enqueue(node.children[i]);
                    }
                }
            }

            int n = centers.Count;
            this.centroids = centers.ToArray();

            if (n > k)
            {
                double[][] proximity = new double[n][];
                for (int i = 0; i < n; i++)
                {
                    proximity[i] = new double[i + 1];
                    for (int j = 0; j < i; j++)
                    {
                        proximity[i][j] = Math.Sqrt(VSM.squaredDistance(centroids[i], centroids[j]));
                    }
                }

                Linkage linkage = new WardLinkage(proximity);
                HierarchicalClustering hc = new HierarchicalClustering(linkage);

                y = hc.partition(k);
                for (int i = 0; i < n; i++)
                {
                    leaves.ElementAt(i).y = y[i];
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    leaves.ElementAt(i).y = i;
                }
            }

            return n;
        }

        /**
         * Cluster a new instance to the nearest CF leaf. After building the 
         * CF tree, the user should call {@link #partition(int)} method first
         * to clustering leaves. Then they call this method to clustering new
         * data.
         * 
         * @param x a new instance.
         * @return the cluster label, which is the label of nearest CF leaf.
         * Note that it may be {@link #OUTLIER}.
         */
        public int Predict(VSM x)
        {
            if (centroids == null)
            {
                throw new Exception("Call partition() first!");
            }

            BIRCHLeaf leaf = root.search(x);
            return leaf.y;
        }

        /**
         * Returns the representatives of clusters.
         * 
         * @return the representatives of clusters
         */
        public VSM[] getCentroids()
        {
            if (centroids == null)
            {
                throw new Exception("Call partition() first!");
            }

            return centroids;
        }

        public void printTreeDetails()
        {
            // This should be used to test the CF Tree implemenation with a known sample data
            // of a small set and dimension
            Console.WriteLine("Root node details");
            Console.WriteLine("Number of Children: {0}, LS {1}", 
                root.numChildren, root.sum.getIndexedWeights());
            List<BIRCHNode> nodes = new List<BIRCHNode>();
            foreach (BIRCHNode node in root.children) nodes.Add(node); // Queue the nodes to be processed
            BIRCHNode birchNode = null;
            while ((birchNode = nodes.ElementAt(0)) != null)
            {
                nodes.RemoveAt(0); // The current node is removed form queue as it is being processed
                Console.WriteLine("Number of children {0}, LS {1}", birchNode.numChildren,
                    birchNode.sum.getIndexedWeights());
                if (birchNode.numChildren != 0) // Add the children nodes for processing
                    foreach (BIRCHNode node in birchNode.children) nodes.Add(node);
            }
            // Print number of levels
            // Print total number of BIRCHLeaves
            // Print Maximum/Min/Average/Standard deviation of observations in BIRCHLeaves
            // Print (optional) number of observations, sum/average for each BIRCHLeaf
        }
    }
}
