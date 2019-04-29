using System;
using System.Collections.Generic;
using System.IO;
using VSMProject;

namespace TFDFReader
{
    class TFCollection
    {
        DFCollection dfCollection = null;
        TFReader tfReader = null;
        double docCount = 0;
        public TFCollection(string dfFileName, string tfDir, string tfExt)
        {
            // Construct a DF Collection
            dfCollection = new DFCollection(dfFileName);

            // Construct a TF Reader to identify the number of documents in the corpus
            tfReader = new TFReader(tfDir, tfExt);

            // First read the next doc positions
            Tuple<int, string, long> tuple = tfReader.readNextDocPosition();
            while (tuple != null)
            {
                docCount++;
                tuple = tfReader.readNextDocPosition();
            }
            Console.WriteLine("Total documents in corpus is {0}", docCount);

            // Construct a new TF Reader for the actual processing of the VSMs
            tfReader = new TFReader(tfDir, tfExt);
        }

        // Index mapping the original termID (value) to a sequential termID starting from 1 (key)
        // Process all VSM from the *.tf files and write to vsm files in the VSM format.
        Dictionary<ulong, int> termIDIdx = new Dictionary<ulong, int>();
        int termIdx = 1;
        string mappingDir = @"C:\Users\sramamurthy\Documents\NewtonOutput";
        string idxHashMappingFile = "IndexToHash.map";
        string docIDMappingFile = "DocID.map";
        int termNotFound = 0;
        public void processTFIDFVSM()
        {
            // As a temporary fix, I am fixing the docCount to the total corpus as I am taking a small
            // subset for testing purpose.
            docCount = 1072877;
            // for each doc ID, read each TF class, convert termID to a sequential index, create tfidf and
            // write to the VSM. If the next docID pops up, create an index of filename and offset for each
            // VSM and write the current VSM to a file and continue processing until null is returned.
            bool newVSM = true;
            VSM vsm = null;
            int currDocID = 0;
            TF tf = this.tfReader.readNext();
            while (tf != null)
            {
                // If a new VSM has to be created, create one, reset newVSM to false, and set currDocID
                // to the new DocID from the tf record
                if (newVSM)
                {
                    if (vsm != null) writeVSMToFile(currDocID, vsm);
                    vsm = new VSM();
                    newVSM = false;
                    currDocID = tf.docID;
                }

                // put the try here for catching df not available. Also, add an if condition here
                // that will do the below logic only if the df > 0.2
                // Use the termIdx to add weights. If termID has not been indexed, add it to the 
                // dictionary and increment the termIdx for the next new TermID.
                if (!termIDIdx.ContainsKey(tf.termID))
                {
                    termIDIdx.Add(tf.termID, termIdx);
                    termIdx++;
                }
                // Need to add a filter to remove terms that are less than a certain df value. This should
                // be a overload method.
                try
                {
                    double weight = tf.tf * System.Math.Log(docCount / (double)dfCollection.getDF(tf.termID));
                    vsm.addWeight(termIDIdx[tf.termID], (float)weight);
                }
                catch (KeyNotFoundException)
                {
                    termNotFound++;
                }
                // Read the next tf record
                tf = tfReader.readNext();
                if (tf != null)
                    if (tf.docID != currDocID) newVSM = true;
            }

            // Once all the VSMs have been written to the files, copy the termID to index mapping
            // and docID to filename and file Offset mapping to text files. The termID to index mapping
            // sould be reversed to index to termID mapping and stored.

            // Remove any existing files and create new map files 
            if (File.Exists(mappingDir + "\\" + idxHashMappingFile))
                File.Delete(mappingDir + "\\" + idxHashMappingFile);
            if (File.Exists(mappingDir + "\\" + docIDMappingFile))
                File.Create(mappingDir + "\\" + docIDMappingFile);
            StreamWriter idxHashMapWriter = new StreamWriter(mappingDir + "\\" + idxHashMappingFile);
            StreamWriter docIDMapWriter = new StreamWriter(mappingDir + "\\" + docIDMappingFile);

            // write the dictionaries
            foreach (ulong key in termIDIdx.Keys)
                idxHashMapWriter.WriteLine(termIDIdx[key] + "," + key);
            foreach (int docKey in docIDFileNameDict.Keys)
                docIDMapWriter.WriteLine(docKey + "," + docIDFileNameDict[docKey] + "," + docIDFileOffsetDict[docKey]);
            Console.WriteLine("Number of times term not found is {0}", termNotFound);
            idxHashMapWriter.Close();
            docIDMapWriter.Close();
        }

        public void processTFIDFVSM(float dfCutoff)
        {
            // As a temporary fix, I am fixing the docCount to the total corpus as I am taking a small
            // subset for testing purpose.
            docCount = 1072877;
            // for each doc ID, read each TF class, convert termID to a sequential index, create tfidf and
            // write to the VSM. If the next docID pops up, create an index of filename and offset for each
            // VSM and write the current VSM to a file and continue processing until null is returned.
            bool newVSM = true;
            VSM vsm = null;
            int currDocID = 0;
            TF tf = this.tfReader.readNext();
            while (tf != null)
            {
                // If a new VSM has to be created, create one, reset newVSM to false, and set currDocID
                // to the new DocID from the tf record
                if (newVSM)
                {
                    if (vsm != null) writeVSMToFile(currDocID, vsm);
                    vsm = new VSM();
                    newVSM = false;
                    currDocID = tf.docID;
                }

                // put the try here for catching df not available. Also, add an if condition here
                // that will do the below logic only if the df > 0.2
                // Use the termIdx to add weights. If termID has not been indexed, add it to the 
                // dictionary and increment the termIdx for the next new TermID.
                try
                {
                    if ((double)dfCollection.getDF(tf.termID) / docCount > dfCutoff)
                    {
                        if (!termIDIdx.ContainsKey(tf.termID))
                        {
                            termIDIdx.Add(tf.termID, termIdx);
                            termIdx++;
                        }
                        // Need to add a filter to remove terms that are less than a certain df value. This should
                        // be a overload method.

                        double weight = tf.tf * System.Math.Log(docCount / (double)dfCollection.getDF(tf.termID));
                        vsm.addWeight(termIDIdx[tf.termID], (float)weight);
                    }
                }
                catch (KeyNotFoundException)
                {
                    termNotFound++;
                }
                // Read the next tf record
                tf = tfReader.readNext();
                if (tf != null)
                    if (tf.docID != currDocID) newVSM = true;
            }
            // Once all the VSMs have been written to the files, copy the termID to index mapping
            // and docID to filename and file Offset mapping to text files. The termID to index mapping
            // sould be reversed to index to termID mapping and stored.

            // Remove any existing files and create new map files 
            if (File.Exists(mappingDir + "\\" + idxHashMappingFile))
                File.Delete(mappingDir + "\\" + idxHashMappingFile);
            if (File.Exists(mappingDir + "\\" + docIDMappingFile))
                File.Create(mappingDir + "\\" + docIDMappingFile);
            StreamWriter idxHashMapWriter = new StreamWriter(mappingDir + "\\" + idxHashMappingFile);
            StreamWriter docIDMapWriter = new StreamWriter(mappingDir + "\\" + docIDMappingFile);

            // write the dictionaries
            foreach (ulong key in termIDIdx.Keys)
                idxHashMapWriter.WriteLine(termIDIdx[key] + "," + key);
            foreach (int docKey in docIDFileNameDict.Keys)
                docIDMapWriter.WriteLine(docKey + "," + docIDFileNameDict[docKey] + "," + docIDFileOffsetDict[docKey]);
            Console.WriteLine("Number of times term not found is {0}", termNotFound);
            idxHashMapWriter.Close();
            docIDMapWriter.Close();
        }

        int vsmFileIDx = 1;
        int maxVSMsPerFile = 20000;
        int currNumOfVSMsInFile = 0;
        string vsmDir = @"C:\Users\sramamurthy\Documents\NewtonOutput";
        string vsmFilePrefix = "TFIDF_";
        string vsmExt = ".vsm";
        bool firstWrite = true;
        BinaryWriter currFile = null;
        string currFileName = null;
        Dictionary<int, string> docIDFileNameDict = new Dictionary<int, string>();
        Dictionary<int, long> docIDFileOffsetDict = new Dictionary<int, long>();
        private void writeVSMToFile(int docID, VSM vsm)
        {
            if (firstWrite)
            {
                // reset firstWrite to false
                firstWrite = false;

                // open the first file to write to and increment the fileIdx
                currFileName = vsmDir + "\\" + vsmFilePrefix + vsmFileIDx + vsmExt;
                if (File.Exists(currFileName)) File.Delete(currFileName);
                currFile = new BinaryWriter(new FileStream(currFileName, FileMode.Create));
                vsmFileIDx++;
            }
            if (currNumOfVSMsInFile == maxVSMsPerFile)
            {
                // close the current file and reset currNumOfVSMsInFile to 0
                currFile.Close();
                currNumOfVSMsInFile = 0;

                // open a new file to write vsms, and increment the fileIdx 
                currFileName = vsmDir + "\\" + vsmFilePrefix + vsmFileIDx + vsmExt;
                if (File.Exists(currFileName)) File.Delete(currFileName);
                currFile = new BinaryWriter(new FileStream(currFileName, FileMode.Create));
                vsmFileIDx++;
            }
            // flush the current vsm file, store the filename and offset for lookup and write the current vsm
            // Increment current number of VSMs in file by 1
            currFile.Flush(); // Otherwise, the offset written will not be the latest
            docIDFileNameDict.Add(docID, currFileName);
            docIDFileOffsetDict.Add(docID, currFile.BaseStream.Position);
            currFile.Write(docID); // Write the docID as int
            Dictionary<int, float> weights = vsm.getWeights();
            // Write each pair of index and weight
            foreach (int key in weights.Keys)
            {
                currFile.Write(key);
                currFile.Write(weights[key]);
            }
            // At the end write, -1, -1 that shows the VSM has ended.
            currFile.Write((int)-1);
            currFile.Write((float)-1);
            currFile.Flush();
            currNumOfVSMsInFile++;
        }
    }
}

