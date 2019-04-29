using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSMProject;

namespace TFDFReader
{
    class TFUtilities
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

                // Add the binaryreader and offset to the dictionaries
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
            string fileName = (docIDStreamMap[docID].BaseStream as FileStream).Name;
            VSM vsm = new VSM();
            int currDocID = docIDStreamMap[docID].ReadInt32();
            // First read the docID
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
    }
}
