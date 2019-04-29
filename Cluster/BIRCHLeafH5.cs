using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BIRCHLeafH5 : BIRCHNodeH5
    {
        public int y; // The cluster label of the leaf node
        public VSM squaredSum; // The squared sum required for calculating the radius of the leaf

        public BIRCHLeafH5(VSM x) : base()
        {
            this.n = 1;
            this.sum = x;
            this.squaredSum = VSM.getSquaredVSM(x);
        }

        // Calculate the radius for a given values of LS, SS and N
        float radius(VSM LS, VSM SS, int N)
        {
            // sqrt ((SS - LS^2/n)/n)
            double radius = 0.0;
            VSM temp = VSM.subtractVSMs(SS,VSM.divideVSM(VSM.getSquaredVSM(LS), N));
            temp = VSM.divideVSM(temp, N);
            VSM origin = new VSM(); // vsm with all weights set to 0
            radius = Math.Sqrt(VSM.squaredDistance(temp, origin));
            return (float)radius;
        }

        public float radius()
        {
            // sqrt ((SS - LS^2/n)/n)
            double radius = 0.0;
            VSM temp = VSM.subtractVSMs(this.squaredSum, 
                VSM.divideVSM(VSM.getSquaredVSM(this.sum), this.n));
            temp = VSM.divideVSM(temp, this.n);
            VSM origin = new VSM(); // vsm with all weights set to 0
            radius = Math.Sqrt(VSM.squaredDistance(temp, origin));
            return (float)radius;
        }
        // Check for Threshold violation
        public bool checkAndAddVSM(VSM x)
        {
            VSM tempSquaredSum = VSM.addVSMs(this.squaredSum, VSM.getSquaredVSM(x));
            VSM tempSum = VSM.addVSMs(this.sum, x);
            float radius = this.radius(tempSum, tempSquaredSum, this.n + 1);
            if (radius < BIRCHTreeH5.T)
            {
                // Update the leaf with new LS, SS and n and return true
                this.sum = tempSum;
                this.squaredSum = tempSquaredSum;
                this.n++;
                return true;
            }
            else return false;
        }

        public int getNumOfObservations()
        {
            return this.n;
        }
        public override void add(VSM x)
        {
            this.n++;
            this.sum = VSM.addVSMs(this.sum, x);
            this.squaredSum = VSM.addVSMs(this.squaredSum, VSM.getSquaredVSM(x));
        }
    }
}
