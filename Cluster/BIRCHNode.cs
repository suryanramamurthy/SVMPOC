using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class BIRCHNode
    {
        public int n; // The number of observations
        public VSM sum; // The sum of the observations as a VSM
        public int numChildren; // The number of children
        public BIRCHNode[] children; // Children nodes
        BIRCHNode parent; // Parent node

        // Constructor of root node
        public BIRCHNode()
        {
            n = 0;
            sum = new VSM();
            parent = null;
            numChildren = 0;
            children = new BIRCHNode[BIRCHTree.B];
        }

        // Calculates the distance between x and CF center
        double distance(VSM x)
        {
            double dist = 0.0;
            VSM centroid = VSM.divideVSM(this.sum, this.n);
            dist = VSM.squaredDistance(x, centroid);
            return Math.Sqrt(dist);
        }

        /**
         * Calculates the distance between CF centers
         */
        double distance(BIRCHNode node)
        {
            double dist = 0.0;
            VSM thisCentroid = VSM.divideVSM(this.sum, this.n);
            VSM nodeCentroid = VSM.divideVSM(node.sum, node.n);
            dist = VSM.squaredDistance(thisCentroid, nodeCentroid);
            return Math.Sqrt(dist);
        }

        //Returns the leaf node closest to the given data.
        public BIRCHLeaf search(VSM x)
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
                return (BIRCHLeaf)children[index];
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
            for (int i = 1; i < numChildren; i++)
            {
                double dist = children[i].distance(x);
                if (dist < smallest)
                {
                    index = i;
                    smallest = dist;
                }
            }
            // At this point, the right logic should be "try to add" in the else
            // portion
            if (children[index] is BIRCHLeaf)
                if (smallest > BIRCHTree.T)
                    this.add(new BIRCHLeaf(x));
                else
                {
                    // if this BIRCHLeaf has B VSM's, then add new BIRCHLeaf
                    if (children[index].n < BIRCHTree.B)
                        ((BIRCHLeaf)children[index]).add(x);
                    else this.add(new BIRCHLeaf(x));
                }
            else
                children[index].add(x);
        }

        // Add a node as children. Split this node if the number of children
        // reach the Branch Factor.
        public void add(BIRCHNode node)
        {
            if (numChildren < BIRCHTree.B)
            {
                children[numChildren++] = node;
                node.parent = this;
            }
            else
            {
                if (parent == null)
                {
                    parent = new BIRCHNode();
                    parent.add(this);
                    BIRCHTree.root = parent;
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
        BIRCHNode split(BIRCHNode node)
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
            BIRCHNode[] child = children;

            // clean up this node.
            numChildren = 0;
            n = 0;
            sum = new VSM();

            BIRCHNode brother = new BIRCHNode();
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
