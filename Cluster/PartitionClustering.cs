using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    public abstract class PartitionClustering : Clustering
    {
        public int k; // The number of clusters
        public int[] y; // The cluster labels of VSM
        public int[] size; // The number of VSMs in each cluster

        // Return the number of clusters
        public int getNumClusters()
        {
            return k;
        }

        // Returns the cluster labels of data
        public int[] getClusterLabel()
        {
            return y;
        }

        // Returns the size of cluster
        public int[] getClusterSize()
        {
            return size;
        }

        /**
        * Initialize cluster membership of input objects with KMeans++ algorithm.
        * Many clustering methods, e.g. k-means, need a initial clustering
        * configuration as a seed.
        * 
        * K-Means++ is based on the intuition of spreading the k initial cluster
        * centers away from each other. The first cluster center is chosen uniformly
        * at random from the data points that are being clustered, after which each
        * subsequent cluster center is chosen from the remaining data points with
        * probability proportional to its distance squared to the point's closest
        * cluster center.
        * 
        * The exact algorithm is as follows:
        * 
        * --> Choose one center uniformly at random from among the data points. 
        * 
        * --> For each data point x, compute D(x), the distance between x and the nearest 
        *     center that has already been chosen. 
        * --> Choose one new data point at random as a new center, using a weighted probability 
        *     distribution where a point x is chosen with probability proportional to D^2(x)
        * --> Repeat Steps 2 and 3 until k centers have been chosen.
        * --> Now that the initial centers have been chosen, proceed using standard k-means clustering. 
        * 
        * This seeding method gives out considerable improvements in the final error
        * of k-means. Although the initial selection in the algorithm takes extra time,
        * the k-means part itself converges very fast after this seeding and thus
        * the algorithm actually lowers the computation time too.
        * 
        * References
        * 
        * - D. Arthur and S. Vassilvitskii. "K-means++: the advantages of careful seeding". ACM-SIAM 
        *   symposium on Discrete algorithms, 1027-1035, 2007.
        * - Anna D. Peterson, Arka P. Ghosh and Ranjan Maitra. A systematic evaluation of different 
        *   methods for initializing the K-means clustering algorithm. 2010.
        *         * 
        * data - data objects to be clustered.
        * k -  the number of cluster.
        * return the cluster labels.
        */
        public static int[] seed(Dictionary<int, BinaryReader> docID2ReaderMap,
            Dictionary<int, long> docID2OffsetMap, int k, ClusteringDistance distance)
        {
            int n = docID2ReaderMap.Count;
            int[] y = new int[n];
            Random random = new Random();
            VSM centroid = Utilities.getVSMAt(random.Next(n), docID2ReaderMap, docID2OffsetMap);

            double[] d = new double[n];
            for (int i = 0; i < n; i++)
            {
                d[i] = Double.MaxValue;
            }

            // pick the next center
            for (int j = 1; j < k; j++)
            {
                // Loop over the samples and compare them to the most recent center.  Store
                // the distance from each sample to its closest center in scores.
                for (int i = 0; i < n; i++)
                {
                    // compute the distance between this sample and the current center
                    double dist = 0.0;
                    switch (distance)
                    {
                        case ClusteringDistance.EUCLIDEAN:
                            dist = VSM.squaredDistance(Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap),
                                centroid);
                            break;
                        case ClusteringDistance.JENSEN_SHANNON_DIVERGENCE:
                            dist = VSM.JensenShannonDivergence(Utilities.getVSMAt(i, docID2ReaderMap,
                                docID2OffsetMap), centroid);
                            break;
                    }

                    if (dist < d[i])
                    {
                        d[i] = dist;
                        y[i] = j - 1;
                    }
                }

                double cutoff = random.NextDouble() * Utilities.sum(d);
                double cost = 0.0;
                int index = 0;
                for (; index < n; index++)
                {
                    cost += d[index];
                    if (cost >= cutoff)
                    {
                        break;
                    }
                }
                centroid = Utilities.getVSMAt(index, docID2ReaderMap, docID2OffsetMap);
            }

            for (int i = 0; i < n; i++)
            {
                // compute the distance between this sample and the current center
                double dist = 0.0;
                switch (distance)
                {
                    case ClusteringDistance.EUCLIDEAN:
                        dist = VSM.squaredDistance(Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap),
                            centroid);
                        break;
                    case ClusteringDistance.JENSEN_SHANNON_DIVERGENCE:
                        dist = VSM.JensenShannonDivergence(Utilities.getVSMAt(i, docID2ReaderMap,
                            docID2OffsetMap), centroid);
                        break;
                }

                if (dist < d[i])
                {
                    d[i] = dist;
                    y[i] = k - 1;
                }
            }

            return y;
        }

        /**
        * Initialize cluster membership of input objects with KMeans++ algorithm.
        * Many clustering methods, e.g. k-means, need a initial clustering
        * configuration as a seed.
        * <p>
        * K-Means++ is based on the intuition of spreading the k initial cluster
        * centers away from each other. The first cluster center is chosen uniformly
        * at random from the data points that are being clustered, after which each
        * subsequent cluster center is chosen from the remaining data points with
        * probability proportional to its distance squared to the point's closest
        * cluster center.
        * <p>
        * The exact algorithm is as follows:
        * <ol>
        * <li> Choose one center uniformly at random from among the data points. </li>
        * <li> For each data point x, compute D(x), the distance between x and the nearest center that has already been chosen. </li>
        * <li> Choose one new data point at random as a new center, using a weighted probability distribution where a point x is chosen with probability proportional to D<sup>2</sup>(x). </li>
        * <li> Repeat Steps 2 and 3 until k centers have been chosen. </li>
        * <li> Now that the initial centers have been chosen, proceed using standard k-means clustering. </li>
        * </ol>
        * This seeding method gives out considerable improvements in the final error
        * of k-means. Although the initial selection in the algorithm takes extra time,
        * the k-means part itself converges very fast after this seeding and thus
        * the algorithm actually lowers the computation time too.
        * 
        * <h2>References</h2>
        * <ol>
        * <li> D. Arthur and S. Vassilvitskii. "K-means++: the advantages of careful seeding". ACM-SIAM symposium on Discrete algorithms, 1027-1035, 2007.</li>
        * <li> Anna D. Peterson, Arka P. Ghosh and Ranjan Maitra. A systematic evaluation of different methods for initializing the K-means clustering algorithm. 2010.</li>
        * </ol>
        * 
        * @param <T> the type of input object.
        * @param data objects array of size n stored in disk. The 2 dictionaries provide access to the VSMs
        * @param medoids an array of size k to store cluster medoids on output.
        * @param y an array of size n to store cluster labels on output.
        * @param d an array of size n to store the distance of each sample to nearest medoid.
        * @return the initial cluster distortion.
        */
        public static double seed(Distance distance, Dictionary<int, BinaryReader> docID2ReaderMap,
            Dictionary<int, long> docID2OffsetMap, VSM[] medoids, int[] y, double[] d)
        {
            Random random = new Random();
            int n = docID2ReaderMap.Count;
            int k = medoids.Length;
            VSM medoid = Utilities.getVSMAt(random.Next(0, n), docID2ReaderMap, docID2OffsetMap);
            medoids[0] = medoid;

            for (int i = 0; i < d.Length; i++) d[i] = Double.MaxValue;

            // pick the next center
            for (int j = 1; j < k; j++)
            {
                // Loop over the samples and compare them to the most recent center.  Store
                // the distance from each sample to its closest center in scores.
                for (int i = 0; i < n; i++)
                {
                    // compute the distance between this sample and the current center
                    double dist = distance.D(Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap), medoid);
                    if (dist < d[i])
                    {
                        d[i] = dist;
                        y[i] = j - 1;
                    }
                }

                double cutoff = random.NextDouble() * Utilities.sum(d);
                double cost = 0.0;
                int index = 0;
                for (; index < n; index++)
                {
                    cost += d[index];
                    if (cost >= cutoff)
                    {
                        break;
                    }
                }

                medoid = Utilities.getVSMAt(index, docID2ReaderMap, docID2OffsetMap);
                medoids[j] = medoid;
            }

            for (int i = 0; i < n; i++)
            {
                // compute the distance between this sample and the current center
                double dist = distance.D(Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap), medoid);
                if (dist < d[i])
                {
                    d[i] = dist;
                    y[i] = k - 1;
                }
            }

            double distortion = 0.0;
            for (int i = 0; i < n; ++i)
            {
                distortion += d[i];
            }

            return distortion;
        }

        public abstract int Predict(VSM vsm);
    }
}
