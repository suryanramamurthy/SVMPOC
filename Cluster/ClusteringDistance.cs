using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cluster
{
    public enum ClusteringDistance
    {
        EUCLIDEAN, // Squared Euclidean distance for K-Means
        EUCLIDEAN_MISSING_VALUES, // Squared Euclidean distance with missing value handling for K-Means
        JENSEN_SHANNON_DIVERGENCE // Jensen-Shannon divergence for SIB
    }
}
