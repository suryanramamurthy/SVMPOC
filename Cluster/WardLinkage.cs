using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cluster
{
    class WardLinkage : Linkage
    {
        private int[] n; // The number of samples in each cluster

        // proximity -->  the proximity matrix to store the distance measure of
        // dissimilarity. To save space, we only need the lower half of matrix.
        public WardLinkage(double[][] proximity)
        {
            this.proximity = proximity;
            n = new int[proximity.Length];
            for (int i = 0; i < n.Length; i++)
            {
                n[i] = 1;
                for (int j = 0; j < i; j++)
                    proximity[i][j] *= proximity[i][j];
            }
        }

        public override string ToString()
        {
            return "Ward's linkage";
        }

        public override void merge(int i, int j)
        {
            double nij = n[i] + n[j];

            for (int k = 0; k < i; k++)
            {
                proximity[i][k] = (proximity[i][k] * (n[i] + n[k]) + proximity[j][k] * (n[j] + n[k]) - proximity[j][i] * n[k]) / (nij + n[k]);
            }

            for (int k = i + 1; k < j; k++)
            {
                proximity[k][i] = (proximity[k][i] * (n[i] + n[k]) + proximity[j][k] * (n[j] + n[k]) - proximity[j][i] * n[k]) / (nij + n[k]);
            }

            for (int k = j + 1; k < proximity.Length; k++)
            {
                proximity[k][i] = (proximity[k][i] * (n[i] + n[k]) + proximity[k][j] * (n[j] + n[k]) - proximity[j][i] * n[k]) / (nij + n[k]);
            }

            n[i] += n[j];
        }
    }
}
