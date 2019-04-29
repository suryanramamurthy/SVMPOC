using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BBDNodeH5
    {
        // The dimensionality of the full Newton output is 5579609
        public int count; // Number of VSMs stored under this node
        public int index; // the smalles point index stored in this node
        public VSM center; // The center/mean of the bounding box
        public VSM radius; // The half-side lengths of the bounding box
        public VSM sum; // The sum of the VSMs stored in this node
        public double cost; // The min cost for putting all data in this node in 1 cluster
        public BBDNodeH5 lower; // The child node of the lower half box
        public BBDNodeH5 upper; // The child node of the upper half box
        
        public BBDNodeH5()
        {
            center = new VSM();
            radius = new VSM();
            sum = new VSM();
        }
    }
}
