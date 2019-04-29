using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BIRCHNodeH5
    {
        static readonly object _object = new object();
        public int n; // The number of observations
        public VSM sum; // The sum of the observations as a VSM
        public int numChildren; // The number of children
        public BIRCHNodeH5[] children; // Children nodes
        BIRCHNodeH5 parent; // Parent node

        // Constructor of root node
        public BIRCHNodeH5()
        {
            n = 0;
            sum = new VSM();
            parent = null;
            numChildren = 0;
            children = new BIRCHNodeH5[BIRCHTreeH5.B];
        }



        // Calculates the distance between x and CF center
        double distance(VSM x)
        {
            double dist = 0.0;
            VSM centroid = VSM.divideVSM(this.sum, this.n);
            dist = VSM.ParallelSquaredDistance(x, centroid);
            return Math.Sqrt(dist);
        }

        /**
         * Calculates the distance between CF centers
         */
        double distance(BIRCHNodeH5 node)
        {
            double dist = 0.0;
            VSM thisCentroid = VSM.divideVSM(this.sum, this.n);
            VSM nodeCentroid = VSM.divideVSM(node.sum, node.n);
            dist = VSM.ParallelSquaredDistance(thisCentroid, nodeCentroid);
            return Math.Sqrt(dist);
        }

        //Returns the leaf node closest to the given data.
        public BIRCHLeafH5 search(VSM x)
        {
            int index = 0;
            double smallest = this.children[0].distance(x);

            // find the closest child node to this data point
            for (int i = 1; i < numChildren; i++)
            {
                double dist = children[i].distance(x);
                if (dist < smallest)
                {
                    index = i;
                    smallest = dist;
                }
            }

            if (children[index].numChildren == 0)
                return (BIRCHLeafH5)children[index];
            else
                return children[index].search(x);
        }

        // Increment the number of VSMs under this node and update the node's sum.
        public void update(VSM x)
        {
            n++;
            this.sum = VSM.addVSMs(this.sum, x);
        }

        // Adds data to the node based on the Threshold (T) value provided.
        public virtual void add(VSM x)
        {
            update(x);
            int index = 0;
            double smallest = children[0].distance(x);

            // find the closest child node to this data point
            /*
            for (int i = 1; i < numChildren; i++)
            {
                double dist = children[i].distance(x);
                if (dist < smallest)
                {
                    index = i;
                    smallest = dist;
                }
            }
            */

            Parallel.For(1, numChildren, i =>
                {
                    double dist = children[i].distance(x);
                    lock (_object)
                    {
                        if (dist < smallest)
                        {
                            index = i;
                            smallest = dist;
                        }
                    }
                });
            // At this point, the right logic should be "try to add" in the else
            // portion
            // If the closest node is a Birch Leaf
            // 1. Check if the Birch Leaf has space to take this leaf.
            // 2. If space is there, then check if the radius is within T if the vsm is added to this leaf
            // 3. If 1 fails or 1 passes but 2 fails then create a new Birch Leaf with this vsm
            // 3. and try to add it as a new node to the parent.
            if (children[index] is BIRCHLeafH5)
            {
                bool added = false;
                if (BIRCHTreeH5.L != -1) // If it is -1, then the leaves can any amount of observations
                {
                    if (((BIRCHLeafH5)children[index]).getNumOfObservations() < BIRCHTreeH5.L)
                        added = ((BIRCHLeafH5)children[index]).checkAndAddVSM(x);
                }
                else added = ((BIRCHLeafH5)children[index]).checkAndAddVSM(x);
                if (!added) // the Birch Leaf could not accomodate the VSM x
                    this.add(new BIRCHLeafH5(x));
            }
            else
                children[index].add(x);
        }

        // Add a node as children. Split this node if the number of children
        // reach the Branch Factor.
        public void add(BIRCHNodeH5 node)
        {
            if (numChildren < BIRCHTreeH5.B)
            {
                children[numChildren] = node; // Add the node to the first available slot
                numChildren++; // inrement numChildren to update for the addition
                node.parent = this;
            }
            else
            {
                if (parent == null)
                {
                    parent = new BIRCHNodeH5();
                    parent.add(this);
                    BIRCHTreeH5.root = parent;
                }
                else
                {
                    parent.n = 0;
                    parent.sum = new VSM(); // Reset the number of children and sum for parent
                }

                parent.add(split(node));

                for (int i = 0; i < parent.numChildren; i++)
                {
                    parent.n += parent.children[i].n;
                    parent.sum = VSM.addVSMs(parent.sum, parent.children[i].sum);
                }
            }
        }

        // Split the node and return a new node to add into the parent
        BIRCHNodeH5 split(BIRCHNodeH5 node)
        {
            double farthest = 0.0;
            int c1 = 0, c2 = 0;
            double[,] dist = new double[numChildren + 1, numChildren + 1];
            for (int i = 0; i < numChildren; i++)
            {
                for (int j = i + 1; j < numChildren; j++)
                {
                    dist[i, j] = children[i].distance(children[j]);
                    dist[j, i] = dist[i, j];
                    if (farthest < dist[i, j])
                    {
                        c1 = i;
                        c2 = j;
                        farthest = dist[i, j];
                    }
                }

                dist[i, numChildren] = children[i].distance(node);
                dist[numChildren, i] = dist[i, numChildren];
                if (farthest < dist[i, numChildren])
                {
                    c1 = i;
                    c2 = numChildren;
                    farthest = dist[i, numChildren];
                }
            }

            int nc = numChildren;
            BIRCHNodeH5[] child = children;

            // clean up this node.
            numChildren = 0;
            n = 0;
            sum = new VSM();

            BIRCHNodeH5 brother = new BIRCHNodeH5();
            for (int i = 0; i < nc; i++)
            {
                if (dist[i, c1] < dist[i, c2])
                {
                    add(child[i]);
                }
                else
                {
                    brother.add(child[i]);
                }
            }

            if (dist[nc, c1] < dist[nc, c2])
            {
                add(node);
            }
            else
            {
                brother.add(node);
            }

            for (int i = 0; i < numChildren; i++)
            {
                n += children[i].n;
                sum = VSM.addVSMs(sum, children[i].sum);
            }

            for (int i = 0; i < brother.numChildren; i++)
            {
                brother.n += brother.children[i].n;
                brother.sum = VSM.addVSMs(brother.sum, brother.children[i].sum);
            }

            return brother;
        }
    }
}
