using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFDFReader
{
    // Creates a collection of docID, fileName and position of the docID in the file
    class DocCollection
    {
        SortedList<int, string> fileNameCollection = new SortedList<int, string>();
        SortedList<int, long> positionCollection = new SortedList<int, long>();
        TFReader tfReader = null;

        // Constructor given the dir and ext, will create a collection of document positions
        public DocCollection(string dir, string ext)
        {
            tfReader = new TFReader(dir, ext);
            createDocCollection();
        }

        public DocCollection(string dir, string ext, int strtIdx, int endIdx)
        {
            tfReader = new TFReader(dir, ext, strtIdx, endIdx);
            createDocCollection();
        }

        private void createDocCollection()
        {
            // using the tfReader.readNextDocPosition() create the doc collection
            Tuple<int, string, long> docInfo = null;
            docInfo = tfReader.readNextDocPosition();
            while (docInfo != null)
            {
                this.fileNameCollection[docInfo.Item1] = docInfo.Item2;
                this.positionCollection[docInfo.Item1] = docInfo.Item3;
                docInfo = tfReader.readNextDocPosition();
            }
        }
    }
}
