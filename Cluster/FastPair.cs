﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/**
* Fast pair algorithm: hybrid of conga line and nearest neighbors.
* This is based on the observation that the conga line data structure, in practice, does better 
* the more subsets you give to it: even though the worst case time for k subsets is O(nk log (n/k)), 
* that worst case seems much harder to reach than the nearest neighbor algorithm. In the limit of 
* arbitrarily many subsets, each new addition or point moved by a deletion will be in a singleton 
* subset, and the algorithm will differ from nearest neighbors in only a couple of ways: (1) when we
* create the initial data structure, we use a conga line rather than all nearest neighbors, to keep 
* the in-degree of each point low, and (2) when we insert a point, we don't bother updating other 
* points' neighbors.
*
* Total space: 20n bytes. (Could be reduced to 4n at some cost in update time.)
* Time per insertion or single distance update: O(n)
* Time per deletion or point update: O(n) expected, O(n^2) worst case
* Time per closest pair: O(n)
*
* References
* David Eppstein.Fast hierarchical clustering and other applications of dynamic closest pairs.
* SODA 1998.
*/

namespace Cluster
{
    class FastPair
    {
        private int[] points;            // points currently in set
        private int[] index;             // indices into points
        private int npoints;             // how much of array is actually used?
        private int[] neighbor;
        private double[] distance;
        private double[][] proximity;

        public FastPair(int[] points, double[][] proximity)
        {
            this.points = points;
            this.proximity = proximity;

            npoints = points.Length;
            neighbor = new int[npoints];
            index = new int[npoints];
            distance = new double[npoints];

            // Find all neighbors. We use a conga line rather than calling getNeighbor.
            for (int i = 0; i < npoints - 1; i++)
            {
                // find neighbor to p[0]
                int nbr = i + 1;
                double nbd = Double.MaxValue;
                for (int j = i + 1; j < npoints; j++)
                {
                    double d = D(points[i], points[j]);
                    if (d < nbd)
                    {
                        nbr = j;
                        nbd = d;
                    }
                }

                // add that edge, move nbr to points[i+1]
                distance[points[i]] = nbd;
                neighbor[points[i]] = points[nbr];
                points[nbr] = points[i + 1];
                points[i + 1] = neighbor[points[i]];
            }

            // No more neighbors, terminate conga line
            neighbor[points[npoints - 1]] = points[npoints - 1];
            distance[points[npoints - 1]] = Double.MaxValue;

            // set where_are...
            for (int i = 0; i < npoints; i++)
            {
                index[points[i]] = i;
            }
        }

        // Returns the distance/dissimilarity between two clusters/objects, which
        // are indexed by integers.
        private double D(int i, int j)
        {
            if (i > j)
                return proximity[i][j];
            else
                return proximity[j][i];
        }

        // Find nearest neighbor of a given point.
        private void findNeighbor(int p)
        {
            // if no neighbors available, set flag for UpdatePoint to find
            if (npoints == 1)
            {
                neighbor[p] = p;
                distance[p] = Double.MaxValue;
                return;
            }

            // find first point unequal to p itself
            int first = 0;
            if (p == points[first])
            {
                first = 1;
            }

            neighbor[p] = points[first];
            distance[p] = D(p, neighbor[p]);

            // now test whether each other point is closer
            for (int i = first + 1; i < npoints; i++)
            {
                int q = points[i];
                if (q != p)
                {
                    double d = D(p, q);
                    if (d < distance[p])
                    {
                        distance[p] = d;
                        neighbor[p] = q;
                    }
                }
            }
        }

        // Add a point and find its nearest neighbor.
        public void add(int p)
        {
            findNeighbor(p);
            points[index[p] = npoints++] = p;
        }

        // Remove a point and update neighbors of points for which it had been nearest
        public void remove(int p)
        {
            npoints--;
            int q = index[p];
            index[points[q] = points[npoints]] = q;

            for (int i = 0; i < npoints; i++)
            {
                if (neighbor[points[i]] == p)
                {
                    findNeighbor(points[i]);
                }
            }
        }

        // Find closest pair by scanning list of nearest neighbors
        public double getNearestPair(int[] pair)
        {
            if (npoints < 2)
            {
                throw new InvalidOperationException("FastPair: not enough points to form pair");
            }

            double d = distance[points[0]];
            int r = 0;
            for (int i = 1; i < npoints; i++)
            {
                if (distance[points[i]] < d)
                {
                    d = distance[points[i]];
                    r = i;
                }
            }

            pair[0] = points[r];
            pair[1] = neighbor[pair[0]];

            if (pair[0] > pair[1])
            {
                int t = pair[0];
                pair[0] = pair[1];
                pair[1] = t;
            }

            return d;
        }

        /**
         * All distances to point have changed, check if our structures are ok. Note that although 
         * we completely recompute the neighbors of p, we don't explicitly call findNeighbor, 
         * since that would double the number of distance computations made by this routine.
         * Also, like deletion, we don't change any other point's neighbor to p.
         */
        public void updatePoint(int p)
        {
            neighbor[p] = p;    // flag for not yet found any
            distance[p] = Double.MaxValue;
            for (int i = 0; i < npoints; i++)
            {
                int q = points[i];
                if (q != p)
                {
                    double d = D(p, q);
                    if (d < distance[p])
                    {
                        distance[p] = d;
                        neighbor[p] = q;
                    }
                    if (neighbor[q] == p)
                    {
                        if (d > distance[q])
                        {
                            findNeighbor(q);
                        }
                        else
                        {
                            distance[q] = d;
                        }
                    }
                }
            }
        }

        // Single distance has changed, check if our structures are ok.
        public void updateDistance(int p, int q)
        {
            double d = D(p, q);

            if (d < distance[p])
            {
                distance[p] = q;
                neighbor[p] = q;
            }
            else if (neighbor[p] == q && d > distance[p])
            {
                findNeighbor(p);
            }

            if (d < distance[q])
            {
                distance[q] = p;
                neighbor[q] = p;
            }
            else if (neighbor[q] == p && d > distance[q])
            {
                findNeighbor(q);
            }
        }
    }
}
