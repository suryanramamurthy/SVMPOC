﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BBDTree
    {
        private BBDNode root; // Root node
        private int[] index; // The index of the docIDs
        private int dim; // dimension of the corpus. Total number of unique terms
        private Dictionary<int, BinaryReader> docID2ReaderMap;
        private Dictionary<int, long> docID2OffsetMap;

        // Construct the tree out of the given VSMs in the N-dimensional space
        public BBDTree(Dictionary<int, BinaryReader> docIDFileMap, Dictionary<int, long> docIDFilePosMap,
            int dimension)
        {
            Console.WriteLine("Starting the BBD Tree build");
            int n = docIDFileMap.Count;
            index = new int[n];
            for (int i = 0; i < n; i++)
                index[i] = i;

            dim = dimension; // Assign the dimension to build the k-d tree
            docID2ReaderMap = docIDFileMap;
            docID2OffsetMap = docIDFilePosMap;

            // Build the tree
            Console.WriteLine("Starting at the root node");
            root = buildNode(0, n);
        }

        /**
        * Build a k-d tree from the given set of data.
        */
        private BBDNode buildNode(int begin, int end)
        {
            // class variable dim contains the dimension value
            Console.WriteLine("Starting to Build the current node"); // debugging
            // Allocate the node
            BBDNode node = new BBDNode();

            // Fill in basic info
            node.count = end - begin;
            node.index = begin;

            // Calculate the bounding box
            VSM lowerBound = Utilities.getVSMAt(index[begin], docID2ReaderMap, docID2OffsetMap);
            VSM upperBound = Utilities.getVSMAt(index[begin], docID2ReaderMap, docID2OffsetMap);

            int count = 0; // debugging
            for (int i = begin + 1; i < end; i++)
            {
                VSM currVSM = Utilities.getVSMAt(i, docID2ReaderMap, docID2OffsetMap);
                lowerBound = VSM.min(lowerBound, currVSM);
                upperBound = VSM.max(upperBound, currVSM);
                count++; // debugging
                if (count % 1000 == 0)
                    Console.WriteLine("VSM {0}", count); // debuggin
            }

            // Calculate bounding box stats
            double maxRadius = -1;
            int splitIndex = -1;
            node.center = VSM.addVSMs(lowerBound, upperBound);
            node.center = VSM.divideVSM(node.center, 2);
            node.radius = VSM.subtractVSMs(upperBound, lowerBound);
            node.radius = VSM.divideVSM(node.radius, 2);

            Console.WriteLine("Finished creating upper bound and lower bound for the node"); // debugging
            foreach (int key in node.radius.getWeights().Keys)
                if (node.radius.weightAt(key) > maxRadius)
                {
                    maxRadius = node.radius.weightAt(key);
                    splitIndex = key;
                }
            Console.WriteLine("Max Radius is {0} and Split Index is {1}", maxRadius, splitIndex); // debugging

            // If the max spread is 0, make this a leaf node
            if (maxRadius < 1E-10)
            {
                Console.WriteLine("Creating leaf node"); // debugging
                node.lower = node.upper = null;
                node.sum = Utilities.getVSMAt(index[begin], docID2ReaderMap, docID2OffsetMap);

                if (end > begin + 1)
                {
                    int len = end - begin;
                    node.sum = VSM.multiplyVSM(node.sum, len);
                }

                node.cost = 0;
                return node;
            }

            // Partition the data around the midpoint in this dimension. The
            // partitioning is done in-place by iterating from left-to-right and
            // right-to-left in the same way that partioning is done in quicksort.
            float splitCutoff = node.center.weightAt(splitIndex);
            int i1 = begin, i2 = end - 1, size = 0;
            while (i1 <= i2)
            {
                bool i1Good = (Utilities.getVSMAt(index[i1], docID2ReaderMap, docID2OffsetMap).weightAt(splitIndex) < splitCutoff);
                bool i2Good = (Utilities.getVSMAt(index[i2], docID2ReaderMap, docID2OffsetMap).weightAt(splitIndex) >= splitCutoff);

                if (!i1Good && !i2Good)
                {
                    int temp = index[i1];
                    index[i1] = index[i2];
                    index[i2] = temp;
                    i1Good = i2Good = true;
                }

                if (i1Good)
                {
                    i1++;
                    size++;
                }

                if (i2Good)
                {
                    i2--;
                }
            }

            Console.WriteLine("Creating child nodes."); // debugging
            Console.WriteLine("Lower node is from {0} to {1}", begin, begin + size);
            Console.WriteLine("Upper node is from {0} to {1}", begin + size, end);
            // Create the child nodes
            node.lower = buildNode(begin, begin + size);
            node.upper = buildNode(begin + size, end);

            // Calculate the new sum and opt cost
            node.sum = VSM.addVSMs(node.lower.sum, node.upper.sum);

            VSM mean = VSM.divideVSM(node.sum, node.count);

            node.cost = getNodeCost(node.lower, mean) + getNodeCost(node.upper, mean);
            return node;
        }

        /**
        * Returns the total contribution of all data in the given kd-tree node,
        * assuming they are all assigned to a mean at the given location.
        *
        *   sum_{x \in node} ||x - mean||^2.
        *
        * If c denotes the mean of mass of the data in this node and n denotes
        * the number of data in it, then this quantity is given by
        *
        *   n * ||c - mean||^2 + sum_{x \in node} ||x - c||^2
        *
        * The sum is precomputed for each node as cost. This formula follows
        * from expanding both sides as dot products.
        */
        // Need to optimize this
        private double getNodeCost(BBDNode node, VSM center)
        {
            double scatter = 0.0;
            VSM vsm = VSM.subtractVSMs(VSM.divideVSM(node.sum, node.count), center);
            foreach (int key in vsm.getWeights().Keys)
                scatter += vsm.weightAt(key) * vsm.weightAt(key);
            return node.cost + node.count * scatter;
        }

        /**
        * Given k cluster centroids, this method assigns data to nearest centroids.
        * The return value is the distortion to the centroids. The parameter sums
        * will hold the sum of data for each cluster. The parameter counts hold
        * the number of data of each cluster. If membership is
        * not null, it should be an array of size n that will be filled with the
        * index of the cluster [0 - k] that each data point is assigned to.
        */
        public double clustering(VSM[] centroids, VSM[] sums, int[] counts, int[] membership)
        {
            int k = centroids.Length;

            for (int j = 0; j < counts.Length; j++) counts[j] = 0;

            int[] candidates = new int[k];
            for (int i = 0; i < k; i++)
            {
                candidates[i] = i;
                sums[i].clear();
            }

            return filter(root, centroids, candidates, k, sums, counts, membership);
        }

        /**
        * This determines which clusters all data that are rooted node will be
        * assigned to, and updates sums, counts and membership (if not null)
        * accordingly. Candidates maintains the set of cluster indices which
        * could possibly be the closest clusters for data in this subtree.
        */
        private double filter(BBDNode node, VSM[] centroids, int[] candidates, int k, VSM[] sums,
            int[] counts, int[] membership)
        {
            // Determine which centroid (closest) the node mean is closest to
            double minDist = VSM.ParallelSquaredDistance(node.center, centroids[candidates[0]]);
            int closest = candidates[0];
            for (int i = 1; i < k; i++)
            {
                double dist = VSM.ParallelSquaredDistance(node.center, centroids[candidates[i]]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = candidates[i];
                }
            }
            // If this is a non-leaf node, recurse if necessary
            if (node.lower != null)
            {
                // Build the new list of candidates
                int[] newCandidates = new int[k];
                int newk = 0;

                for (int i = 0; i < k; i++)
                {
                    if (!prune(node.center, node.radius, centroids, closest, candidates[i]))
                    {
                        newCandidates[newk++] = candidates[i];
                    }
                }

                // Recurse if there's at least two
                if (newk > 1)
                {
                    double result = filter(node.lower, centroids, newCandidates, newk, sums, counts, membership) + filter(node.upper, centroids, newCandidates, newk, sums, counts, membership);
                    return result;
                }
            }

            // Assigns all data within this node to a single mean
            sums[closest] = VSM.addVSMs(sums[closest], node.sum);
            counts[closest] += node.count;

            if (membership != null)
            {
                int last = node.index + node.count;
                for (int i = node.index; i < last; i++)
                {
                    membership[index[i]] = closest;
                }
            }

            return getNodeCost(node, centroids[closest]);
        }

        /**
        * Determines whether every point in the box is closer to centroids[bestIndex] than to
        * centroids[testIndex].
        *
        * If x is a point, c_0 = centroids[bestIndex], c = centroids[testIndex], then:
        *       (x-c).(x-c) < (x-c_0).(x-c_0)
        *   <=> (c-c_0).(c-c_0) < 2(x-c_0).(c-c_0)
        *
        * The right-hand side is maximized for a vertex of the box where for each
        * dimension, we choose the low or high value based on the sign of x-c_0 in
        * that dimension.
        **/
        private bool prune(VSM center, VSM radius, VSM[] centroids, int bestIndex, int testIndex)
        {
            if (bestIndex == testIndex)
            {
                return false;
            }

            VSM best = centroids[bestIndex];
            VSM test = centroids[testIndex];
            VSM diff = VSM.subtractVSMs(test, best);
            double lhs = 0.0, rhs = 0.0;
            foreach (int key in diff.getWeights().Keys)
            {
                lhs += diff.weightAt(key) * diff.weightAt(key);
                if (diff.weightAt(key) > 0)
                    rhs += (center.weightAt(key) + radius.weightAt(key) - best.weightAt(key)) * diff.weightAt(key);
                else
                    rhs += (center.weightAt(key) - radius.weightAt(key) - best.weightAt(key)) * diff.weightAt(key);
            }

            return (lhs >= 2 * rhs);
        }
    }
}
