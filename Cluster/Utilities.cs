using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class Utilities
    {
        public static Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> GetDocIDMaps(string docIDMap)
        {
            StreamReader docIDMapReader = new StreamReader(docIDMap);
            Dictionary<int, BinaryReader> docIDStreamMap = new Dictionary<int, BinaryReader>();
            Dictionary<int, long> docIDOffsetMap = new Dictionary<int, long>();
            Dictionary<string, BinaryReader> fileToStreamMap = new Dictionary<string, BinaryReader>();
            string line = docIDMapReader.ReadLine();
            while (line != null)
            {
                // Split the lines to get docID, filename and offset
                string[] tokens = line.Split(',');
                int docID = int.Parse(tokens[0]);
                string vsmFile = tokens[1];
                long offset = long.Parse(tokens[2]);

                // If BinaryReader doesn't exist for the filename, create and add to the fileToStreamMap
                if (!fileToStreamMap.ContainsKey(vsmFile))
                    fileToStreamMap[vsmFile] = new BinaryReader(new FileStream(vsmFile, FileMode.Open));

                // Add the BinaryReader and offset to the dictionaries
                docIDStreamMap[docID] = fileToStreamMap[vsmFile];
                docIDOffsetMap[docID] = offset;

                line = docIDMapReader.ReadLine();
            }
            return Tuple.Create(docIDStreamMap, docIDOffsetMap);
        }

        public static string getVSMString(int docID, Dictionary<int, BinaryReader> docIDStreamMap,
            Dictionary<int, long> docIDOffsetMap)
        {
            docIDStreamMap[docID].BaseStream.Seek(docIDOffsetMap[docID], SeekOrigin.Begin);
            //string fileName = (docIDStreamMap[docID].BaseStream as FileStream).Name;
            VSM vsm = new VSM();
            int currDocID = docIDStreamMap[docID].ReadInt32(); // First read the docID
            while (true) // From the seeked position keep reading until you get -1 and -1
            {
                int index = 0;
                float weight = 0;
                index = docIDStreamMap[docID].ReadInt32();
                weight = docIDStreamMap[docID].ReadSingle();
                if (index == -1 && weight == -1) break; //We have reached the end of VSM
                vsm.addWeight(index, weight);
            }
            return vsm.getIndexedWeights();
        }

        public static VSM getVSMAt(int index, Dictionary<int, BinaryReader> docIDStreamMap,
            Dictionary<int, long> docIDOffsetMap)
        {
            int docID = docIDStreamMap.ElementAt(index).Key;
            return getVSM(docID, docIDStreamMap, docIDOffsetMap);
        }
        public static VSM getVSM(int docID, Dictionary<int, BinaryReader> docIDStreamMap,
            Dictionary<int, long> docIDOffsetMap)
        {
            VSM vsm = new VSM();
            docIDStreamMap[docID].BaseStream.Seek(docIDOffsetMap[docID], SeekOrigin.Begin);
            int currDocID = docIDStreamMap[docID].ReadInt32(); // First read the docID
            while (true) // From the seeked position keep reading until you get -1 and -1
            {
                int index = 0;
                float weight = 0;
                index = docIDStreamMap[docID].ReadInt32();
                weight = docIDStreamMap[docID].ReadSingle();
                if (index == -1 && weight == -1) break; //We have reached the end of VSM
                vsm.addWeight(index, weight);
            }
            return vsm;
        }

        public static double sum(double[] x)
        {
            double sum = 0.0;

            foreach (double n in x)
            {
                sum += n;
            }

            return sum;
        }

        /**
        * To restore the max-heap condition when a node's priority is decreased.
        * We move down the heap, exchanging the node at position k with the larger
        * of that node's two children if necessary and stopping when the node at
        * k is not smaller than either child or the bottom is reached. Note that
        * if n is even and k is n/2, then the node at k has only one child -- this
        * case must be treated properly.
        */
        public static void siftDown(int[] arr, int k, int n)
        {
            while (2 * k <= n)
            {
                int j = 2 * k;
                if (j < n && arr[j] < arr[j + 1])
                {
                    j++;
                }
                if (arr[k] >= arr[j])
                {
                    break;
                }
                swap(arr, k, j);
                k = j;
            }
        }

        // Swap two positions.
        public static void swap(int[] arr, int i, int j)
        {
            int a;
            a = arr[i];
            arr[i] = arr[j];
            arr[j] = a;
        }
    }
}
