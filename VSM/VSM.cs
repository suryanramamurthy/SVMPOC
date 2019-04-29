using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Generic.Dictionary<int, float>;

namespace VSMProject
{
    public class VSM
    {
        // The key to the weights is an index starting from 1. The index is mapped
        // in another dictionary to a termID. That mapping can be used to find termID
        // for the given index in the VSM
        //SortedList<int, float> weights;
        Dictionary<int, float> weights;
        public static object _object1 = new object();
        public static object _object2 = new object();

        public VSM()
        {
            this.weights = new Dictionary<int, float>();
        }

        public int length()
        {
            return this.weights.Count;
        }

        public void addWeight(int index, float weight)
        {
            if (this.weights.ContainsKey(index)) this.weights.Remove(index);
            this.weights.Add(index, weight);
        }

        public void trim()
        {
            List<int> keys = this.weights.Keys.ToList();
            foreach (int key in keys)
                if (this.weights[key] == 0) this.weights.Remove(key);
        }

        public void normalizeTF()
        {
            float sumWeights = 0;
            // Sum all the weights in the VSM
            foreach (int key in this.weights.Keys)
                sumWeights += this.weights[key];
            // Normalize each weight by dividing by the sum of the weights
            KeyCollection keyList = this.weights.Keys;
            Dictionary<int, float> newWeights = new Dictionary<int, float>();
            foreach (int key in keyList)
                newWeights[key] = this.weights[key] / sumWeights;
            this.weights = newWeights;
        }

        public void multiplyIDF(Dictionary<uint, double> indexToIDF)
        {
            KeyCollection keyList = this.weights.Keys;
            Dictionary<int, float> newWeights = new Dictionary<int, float>();
            foreach (int key in keyList)
                newWeights[key] = this.weights[key] * (float)indexToIDF[(uint)key];
            this.weights = newWeights;
        }

        public double cosine(VSM vsm)
        {
            double cosineSimilarity = 0;
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        // termList is a list of integers from the termIDIndex (ranges from 1 to number of terms)
        public double cosine(VSM vsm, List<int> termList)
        {
            double cosineSimilarity = 0;
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms if the termID is part of the docList given
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (termList.Contains(key) && vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                if (termList.Contains(key))
                    magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                if (termList.Contains(key))
                    magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        public double cosine(VSM vsm, int[] termList)
        {
            double cosineSimilarity = 0;
            //Array.Sort<int>(docList);
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms if the termID is part of the docList given
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0 && vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0)
                    magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0)
                    magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        public void setWeights(Dictionary<int, float> weights)
        {
            this.weights = weights;
        }
        public static VSM addVSMs(VSM vsm1, VSM vsm2)
        {
            VSM sum = new VSM();
            foreach (int key in vsm1.weights.Keys)
            {
                if (vsm2.weights.ContainsKey(key)) sum.weights.Add(key, vsm1.weights[key] + vsm2.weights[key]);
                else sum.weights.Add(key, vsm1.weights[key]);
            }
            foreach (int key in vsm2.weights.Keys)
                if (!vsm1.weights.ContainsKey(key)) sum.weights.Add(key, vsm2.weights[key]);

            return sum;
        }

        public static VSM subtractVSMs(VSM vsm1, VSM vsm2)
        {
            VSM sum = new VSM();
            foreach (int key in vsm1.weights.Keys)
            {
                if (vsm2.weights.ContainsKey(key)) sum.weights.Add(key, vsm1.weights[key] - vsm2.weights[key]);
                else sum.weights.Add(key, vsm1.weights[key]);
            }
            foreach (int key in vsm2.weights.Keys)
                if (!vsm1.weights.ContainsKey(key)) sum.weights.Add(key, -1 * vsm2.weights[key]);

            return sum;
        }

        // Given 2 VSMs, create a VSM that has the lower weights for the term Index
        // Since 0 is the minimum weight and VSMs are sparse lists. If a term Index 
        // is not present then the weight is 0. Therefore, take only term Index that
        // is present in both VSM and populate a new VSM with the minimum of the 2
        // weights for the given term Index. Return this VSM
        public static VSM min(VSM vsm1, VSM vsm2)
        {
            VSM vsm = new VSM();
            foreach (int key in vsm1.weights.Keys)
                if (vsm2.weights.ContainsKey(key))
                    vsm.weights.Add(key, Math.Min(vsm1.weights[key], vsm2.weights[key]));
            return vsm;
        }

        // Given 2 VSMs, create a VSM that the higher weights for the term Index
        // Since 0 is the minimum weight and VSMs are sparse lists. If a term Index
        // is not present then the weight is 0. Therefore, iterate on vsm1 keys. If
        // vsm2 contains a weight for that key, take the maximum value else take the 
        // value in vsm1. Then iterate on vsm2 keys. Since, we have already taken care
        // of keys that are common to vsm1 and vsm2, only check for keys in vsm2 that
        // are not present in vsm1 and add those weights to the vsm to be returned.
        public static VSM max(VSM vsm1, VSM vsm2)
        {
            VSM vsm = new VSM();
            foreach (int key in vsm1.weights.Keys)
                if (vsm2.weights.ContainsKey(key))
                    vsm.weights.Add(key, Math.Max(vsm1.weights[key], vsm2.weights[key]));
                else vsm.weights.Add(key, vsm1.weights[key]);
            foreach (int key in vsm2.weights.Keys)
                if (!vsm1.weights.ContainsKey(key))
                    vsm.weights.Add(key, vsm2.weights[key]);
            return vsm;
        }
        public static VSM divideVSM(VSM vsm, float number)
        {
            VSM returnVSM = new VSM();
            foreach (int key in vsm.weights.Keys)
                returnVSM.weights.Add(key, vsm.weights[key] / number);
            return returnVSM;
        }

        public static VSM multiplyVSM(VSM vsm, float number)
        {
            VSM returnVSM = new VSM();
            foreach (int key in vsm.weights.Keys)
                returnVSM.weights.Add(key, vsm.weights[key] * number);
            return returnVSM;
        }

        public void removeWeightAt(int key)
        {
            if (this.weights.ContainsKey(key)) this.weights.Remove(key);
        }

        // Finds the centroid of a list of VSMs based on the termID index given in the docList. All
        // other terms are ignored
        public static VSM findCentroid(List<VSM> listOfVSMs, List<int> termList)
        {
            VSM centroid = new VSM();

            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged

            // Along with the above condition, add the key and find avg iff the key is part
            // of the docList
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (termList.Contains(key))
                        if (!centroid.weights.ContainsKey(key))
                            centroid.weights[key] = vsm.weights[key];
                        else
                            centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key] / listOfVSMs.Count;
            return newCentroid;
        }

        public static VSM findCentroid(List<VSM> listOfVSMs, int[] termList)
        {
            VSM centroid = new VSM();

            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged

            // Along with the above condition, add the key and find avg iff the key is part
            // of the docList
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (Array.BinarySearch<int>(termList, key) >= 0)
                        if (!centroid.weights.ContainsKey(key))
                            centroid.weights[key] = vsm.weights[key];
                        else
                            centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key] / listOfVSMs.Count;
            return newCentroid;
        }
        public static VSM findCentroid(List<VSM> listOfVSMs)
        {
            VSM centroid = new VSM();

            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (!centroid.weights.ContainsKey(key))
                        centroid.weights[key] = vsm.weights[key];
                    else
                        centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key] / listOfVSMs.Count;
            return newCentroid;
        }

        public string getIndexedWeights()
        {
            StringBuilder indexedWeights = new StringBuilder(); ;
            //string indexedWeights = null;
            foreach (int key in this.weights.Keys)
                indexedWeights.Append(key + ":" + this.weights[key] + " ");
            //indexedWeights += key + ":" + this.weights[key] + " ";
            return indexedWeights.ToString();
        }

        public float weightAt(int index)
        {
            if (this.weights.ContainsKey(index)) return weights[index];
            else return 0;
        }


        public Dictionary<int, float> getWeights()
        {
            return this.weights;
        }

        public void clear()
        {
            this.weights.Clear();
        }

        public static double ParallelSquaredDistance(VSM x, VSM y)
        {
            double distance = 0;
            object _lock = new object();

            var keys = x.weights.Keys.ToList();
            var partitions = Partitioner.Create(0, keys.Count);
            Parallel.ForEach(partitions, range =>
                {
                    double subtotal = 0;
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        float possibleValue = 0;
                        y.weights.TryGetValue(keys[i], out possibleValue);
                        float currentValue = x.weights[keys[i]] - possibleValue;
                        subtotal += currentValue * currentValue;
                    }
                    if (subtotal != 0)
                    {
                        lock (_lock)
                        {
                            distance += subtotal;
                        }
                    }
                });

            keys = y.weights.Keys.ToList();
            partitions = Partitioner.Create(0, keys.Count);
            Parallel.ForEach(partitions, range =>
            {
                double subTotal = 0;
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    if (!x.weights.ContainsKey(keys[i]))
                    {
                        subTotal += y.weights[keys[i]] * y.weights[keys[i]];
                    }
                }
                if (subTotal != 0)
                {
                    lock (_lock)
                    {
                        distance += subTotal;
                    }
                }
            });

            return distance;
        }

        /**
        * The squared Euclidean distance.
        */
        public static double squaredDistance(VSM x, VSM y)
        {
            double sum = 0.0;

            // Use 2 different sums and run foreach in parallel threads and update sums 
            // at the end
            // First iterate through x and add squared distance in each dimension to sum
            foreach (int key in x.weights.Keys)
            {
                if (y.weights.ContainsKey(key)) sum += (x.weights[key] - y.weights[key]) *
                        (x.weights[key] - y.weights[key]);
                else sum += x.weights[key] * x.weights[key];
            }
            // Next iterate through y and add squared y weights if that key does not exist in x
            foreach (int key in y.weights.Keys)
            {
                if (!x.weights.ContainsKey(key)) sum += y.weights[key] * y.weights[key];
            }

            return sum;
        }

        public static double JensenShannonDivergence(VSM x, VSM y)
        {
            VSM m = new VSM();

            // Iterate on x. If y has the index then (x+y)/2. Otherwise, x/2
            foreach (int key in x.weights.Keys)
            {
                if (y.weights.ContainsKey(key)) m.addWeight(key, (x.weightAt(key) + y.weightAt(key)) / 2);
                else m.addWeight(key, x.weightAt(key) / 2);
            }
            // Iterate on y. If x doesn't have the index then y/2
            foreach (int key in y.weights.Keys)
            {
                if (!x.weights.ContainsKey(key)) m.addWeight(key, y.weightAt(key));
            }

            return (KullbackLeiblerDivergence(x, m) + KullbackLeiblerDivergence(y, m)) / 2;
        }

        public static double KullbackLeiblerDivergence(VSM x, VSM y)
        {
            bool intersection = false;
            double kl = 0;

            foreach (int key in x.weights.Keys)
            {
                if (y.weights.ContainsKey(key))
                {
                    intersection = true;
                    kl += x.weightAt(key) * Math.Log(x.weightAt(key) / y.weightAt(key));
                }
            }

            if (intersection)
            {
                return kl;
            }
            else
            {
                return Double.PositiveInfinity;
            }
        }

        public static VSM getSquaredVSM(VSM x)
        {
            VSM ret = new VSM();
            foreach (int key in x.weights.Keys)
                ret.weights.Add(key, (float)Math.Pow(x.weights[key], 2));
            return ret;
        }
    }
}
