using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFDFReader
{
    class Program
    {
        static void Main(string[] args)
        {
            createVSMFilesAndMaps();
            //Tuple<Dictionary<int, BinaryReader>, Dictionary<int, long>> tuple = TFUtilities.GetDocIDMaps(@"C:\Users\sramamurthy\Documents\NewtonOutput\DocID.map");
            //string vsm = TFUtilities.getVSMString(791, tuple.Item1, tuple.Item2);
            //Console.WriteLine(vsm);
        }

        private static void createVSMFilesAndMaps()
        {
            Console.WriteLine("Instantiating TFCollection object");
            TFCollection tfCollection = new TFCollection(@"C:\Users\sramamurthy\Documents\NewtonOutput\summary.df",
                @"C:\Users\sramamurthy\Documents\NewtonOutput", "tf");
            Console.WriteLine("Starting VSM processing");
            tfCollection.processTFIDFVSM((float)0.2);
        }
    }
}
