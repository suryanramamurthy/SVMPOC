using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace Cluster
{
    class Program
    {
        static void Main(string[] args)
        {
            //VSM[] vsms = buildBIRTCHTreeTestData();
            Console.WriteLine("Getting dictionaries ready");
            Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> tuple = createDocMaps();
            Dictionary<int, BinaryReader> docReader = tuple.Item1;
            Dictionary<int, long> docMap = tuple.Item2;

            Console.WriteLine("Starting Birch Tree construction");
            BIRCHTreeH5 tree = new BIRCHTreeH5(200, -1, 0.5);
            int count = 0;
            foreach (int key in docMap.Keys)
            {
                tree.add(Utilities.getVSM(key, docReader, docMap));
                count++;
                if (count % 1000 == 0)
                    Console.WriteLine("{0} {1}", count, DateTime.Now.TimeOfDay);
            }
            //foreach (VSM vsm in vsms) tree.add(vsm);
            //tree.printTreeDetails();
        }

        private static Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> createDocMaps()
        {
            Console.WriteLine("Creating the mapping Dictionary");
            Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> tuple =
                Utilities.GetDocIDMaps(@"C:\Users\sramamurthy\Documents\NewtonOutput\DocID.map");
            return tuple;
        }

        private static void runKMeans(Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> tuple)
        {
            Console.WriteLine("Initializing the Kmeans object");
            KMeans kmeans = new KMeans();
            Console.WriteLine("Starting the KMeans build");
            kmeans.buildKMeans(tuple.Item1, tuple.Item2, 1000, 100);
            Console.WriteLine(kmeans.ToString());
        }

        private static VSM[] buildBIRTCHTreeTestData()
        {
            VSM[] vsms = new VSM[7];
            for (int i = 0; i < vsms.Length; i++) vsms[i] = new VSM();
            vsms[0].addWeight(1, (float)0.5);
            vsms[1].addWeight(1, (float)0.25);
            vsms[2].addWeight(1, (float)0);
            vsms[3].addWeight(1, (float)0.65);
            vsms[4].addWeight(1, (float)1);
            vsms[5].addWeight(1, (float)1.4);
            vsms[6].addWeight(1, (float)1.1);
            return vsms;
        }
    }
}
