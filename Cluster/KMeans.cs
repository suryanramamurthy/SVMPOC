using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSMProject;

// KMeans needs to be perfected. Having exceptions.

namespace Cluster
{
    public class KMeans : PartitionClustering
    {
        private static long serialVersionUID = 1L;
        private double distortion; // Total distortion
        private VSM[] centroids; // The centroids of each cluster
        private static int dimension = 313599;

        public KMeans()
        {
        }

        public double getDistortion()
        {
            return distortion;
        }

        public VSM[] getCentroids()
        {
            return centroids;
        }

        // Cluster a new instance
        // VSM x is a new instance
        // Return the cluster label, which is the index of the nearest centroid
        public override int Predict(VSM x)
        {
            double minDist = Double.MaxValue;
            int bestCluster = 0;
            for (int i = 0; i < k; i++)
            {
                double dist = VSM.squaredDistance(x, centroids[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCluster = i;
                }
            }
            return bestCluster;
        }

        // Constructor to cluster k-means into k clusters in 100 or less iterations
        // params --> input Dictionary for docID mapping to VSMs and k (number of clusters)
        public void buildKMeans(Dictionary<int, BinaryReader> docID2ReaderMap,
            Dictionary<int, long> docID2OffsetMap, int k)
        {
            buildKMeans(docID2ReaderMap, docID2OffsetMap, k, 100);
        }

        // Same as above constructor, except that maxIterations is specifiable
        public void buildKMeans(Dictionary<int, BinaryReader> docID2ReaderMap,
            Dictionary<int, long> docID2OffsetMap, int k, int maxIter)
        {
            buildKMeans(new BBDTreeH5(docID2ReaderMap, docID2OffsetMap, dimension), docID2ReaderMap,
                docID2OffsetMap, k, maxIter);
        }

        private void buildKMeans(BBDTreeH5 bbDTree, Dictionary<int, BinaryReader> docID2ReaderMap,
            Dictionary<int, long> docID2OffsetMap, int k, int maxIter)
        {
            Console.WriteLine("Starting to build the k-mean cluster");
            if (k < 2)
            {
                throw new ArgumentException("Invalid number of clusters: " + k);
            }

            if (maxIter <= 0)
            {
                throw new ArgumentException("Invalid maximum number of iterations: " + maxIter);
            }

            int n = docID2ReaderMap.Count;
            int d = dimension;

            this.k = k;
            distortion = Double.MaxValue;
            Console.WriteLine("Starting the seeding of the centroids");
            y = seed(docID2ReaderMap, docID2OffsetMap, k, ClusteringDistance.EUCLIDEAN);
            size = new int[k];
            centroids = new VSM[k];
            // Determine the cluster size of each cluster based on the seeding done
            for (int i = 0; i < n; i++)
            {
                size[y[i]]++;
            }

            // Optimize here
            for (int i = 0; i < n; i++)
            {
                VSM tempVSM = Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap);
                for (int j = 0; j < d; j++)
                {
                    float weight = centroids[y[i]].weightAt(j);
                    centroids[y[i]].addWeight(j,
                        weight + tempVSM.weightAt(j));
                }
            }

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < d; j++)
                {
                    float weight = centroids[i].weightAt(j) / size[i];
                    centroids[i].addWeight(j, weight);
                }
            }

            VSM[] sums = new VSM[k];
            for (int iter = 1; iter <= maxIter; iter++)
            {
                double dist = bbDTree.clustering(centroids, sums, size, y);
                for (int i = 0; i < k; i++)
                {
                    if (size[i] > 0)
                    {
                        for (int j = 0; j < d; j++)
                        {
                            centroids[i].addWeight(j, sums[i].weightAt(j) / size[i]);
                        }
                    }
                }

                if (distortion <= dist)
                {
                    break;
                }
                else
                {
                    distortion = dist;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format("K-Means distortion: %.5f%n", distortion));
            sb.Append(String.Format("Clusters of %d data points of dimension %d:%n", y.Length, dimension));
            for (int i = 0; i < k; i++)
            {
                int r = (int)Math.Round(1000.0 * size[i] / y.Length);
                sb.Append(String.Format("%3d\t%5d (%2d.%1d%%)%n", i, size[i], r / 10, r % 10));
            }

            return sb.ToString();
        }
    }
}
