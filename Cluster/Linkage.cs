using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cluster
{
    public abstract class Linkage
    {
        /**
        * The proximity matrix to store the pair-wise distance measure as
        * dissimilarity between clusters. To save space, we only need the
        * lower half of matrix. During the clustering, this matrix will be
        * updated to reflect the dissimilarity of merged clusters.
        */
        protected double[][] proximity;

        // Returns the proximity matrix.
        public double[][] getProximity()
        {
            return proximity;
        }

        // Returns the distance/dissimilarity between two clusters/objects, which
        // are indexed by integers.
        double d(int i, int j)
        {
            if (i > j)
                return proximity[i][j];
            else
                return proximity[j][i];
        }

        // Merge two clusters into one and update the proximity matrix.
        // i and j are cluster ids
        public abstract void merge(int i, int j);
    }
}
