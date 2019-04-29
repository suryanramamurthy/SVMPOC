using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFDFReader
{
    class TFReader
    {
        BinaryReader[] tfBinaryReader;
        int fileCounter = 0; // Current *.tf file being read
        int termCounter = 0; // Current term being read for a given docID
        bool readHead = true; // Read header before reading termid and tf if true
        int numTerms = 0; // number of terms for the given docID
        int docID = 0;
        int numFiles = 0; // number of files to be read

        // This constructor overload will read all the files with the given "ext" in the given "dir"
        public TFReader(string dir, string ext)
        {
            string[] tfFileNames = Directory.GetFiles(dir, "*." + ext);
            tfBinaryReader = new BinaryReader[tfFileNames.Length];
            this.numFiles = tfFileNames.Length;
            for (int i = 0; i < tfFileNames.Length; i++)
            {
                tfBinaryReader[i] = new BinaryReader(File.OpenRead(tfFileNames[i]));
            }
        }

        // This constructor overload will read all the files with the given "ext" in the given "dir",
        // that start at strIdx (including) up to the endIdx (excluding)
        public TFReader(string dir, string ext, int strIdx, int endIdx)
        {
            string[] tfFileNames = Directory.GetFiles(dir, "*." + ext);
            tfBinaryReader = new BinaryReader[endIdx - strIdx];
            this.numFiles = endIdx - strIdx;
            for (int i = strIdx; i < endIdx; i++)
            {
                tfBinaryReader[i - strIdx] = new BinaryReader(File.OpenRead(tfFileNames[i]));
            }
        }

        // Create a collection of TF by creating a SortedList of docID, filename and filePos
        public Tuple<int, string, long> readNextDocPosition()
        {
            // Read the header, skip all the termID, tf tuples and return the tuple.
            // If EOF reached, open the next file and repeat the same thing.
            try
            {
                readHeader(this.tfBinaryReader[fileCounter]);
                for (int i = 0; i < numTerms; i++)
                {
                    this.tfBinaryReader[fileCounter].ReadUInt64();
                    this.tfBinaryReader[fileCounter].ReadInt32();
                }
                return Tuple.Create(this.docID, (this.tfBinaryReader[fileCounter].BaseStream as FileStream).Name, this.tfBinaryReader[fileCounter].BaseStream.Position);
            }
            // If the EOF is reached, increment fileCounter to read the next *.tf file. 
            // If the fileCounter has reached the total number of files to read then return null
            catch (EndOfStreamException)
            {
                fileCounter++;
                //Console.WriteLine("Reading file {0}", fileCounter);
                if (fileCounter == numFiles) return null;
                else
                {
                    readHeader(tfBinaryReader[fileCounter]);
                    for (int i = 0; i < numTerms; i++)
                    {
                        this.tfBinaryReader[fileCounter].ReadUInt64();
                        this.tfBinaryReader[fileCounter].ReadInt32();
                    }
                    return Tuple.Create(this.docID, (this.tfBinaryReader[fileCounter].BaseStream as FileStream).Name, this.tfBinaryReader[fileCounter].BaseStream.Position);
                }
            }
        }

        // readNext() is a sequential iterator that will read next TF entry and return the
        // docID, termID and tf as a triplet.
        public TF readNext()
        {
            TF tf = new TF();
            try
            {
                // If at beginning of file or new docid/numTerms place, read these two values
                // and reset readHead state.
                if (readHead)
                {
                    readHeader(this.tfBinaryReader[fileCounter]);
                    readHead = false;
                }
                // Read the termid and tf values and update the tf object.
                tf.termID = this.tfBinaryReader[fileCounter].ReadUInt64();
                tf.tf = this.tfBinaryReader[fileCounter].ReadInt32();
                tf.docID = this.docID;
                // Increment termcounter. If the termCounter has reached the number of terms for this docID
                // then set readHead state to True and reset termCounter
                termCounter++;
                if (termCounter == numTerms)
                {
                    readHead = true;
                    termCounter = 0;
                }
                // return the tf object
                return tf;
            }
            // If the EOF is reached, increment fileCounter to read the next *.tf file. Reset termCounter
            // If the fileCounter has reached the total number of files to read then return null indicating
            // that no more files to be read.
            catch (EndOfStreamException)
            {
                fileCounter++;
                Console.WriteLine("Reading file {0}", fileCounter);
                termCounter = 0;
                if (fileCounter == this.tfBinaryReader.Length) return null;
                else
                {
                    // else read the header, reset readHead state
                    readHeader(tfBinaryReader[fileCounter]);
                    readHead = false;
                    // Read the next termid and tf values
                    tf.termID = this.tfBinaryReader[fileCounter].ReadUInt64();
                    tf.tf = this.tfBinaryReader[fileCounter].ReadInt32();
                    // increment termcounter
                    termCounter++;
                    tf.docID = this.docID;
                    // return the tf object. The next readNext call will follow through the try phase
                    // until eof is reached.
                    return tf;
                }
            }
        }

        private void readHeader(BinaryReader reader)
        {
            this.docID = reader.ReadInt32();
            this.numTerms = reader.ReadInt32();
        }
    }
}
