using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BIRCHLeaf : BIRCHNode
    {
        public int y; // The cluster label of the leaf node

        public BIRCHLeaf(VSM x) : base()
        {
            this.n = 1;
            this.sum = x;
        }

        public override void add(VSM x)
        {
            this.n++;
            this.sum = VSM.addVSMs(this.sum, x);
        }
    }
}
