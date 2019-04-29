using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFDFReader
{
    class DFCollection
    {
        // Using SortedList to store the DF Collection. Based on performance, the storage method
        // can be changed. 
        // Since DF is based on terms, it would be better that it be kept in memory as long as it is
        // required.
        private SortedDictionary<ulong, int> dfCollection = new SortedDictionary<ulong, int>();

        public DFCollection(string fileName)
        {
            DFReader dfReader = new DFReader(fileName);
            DF df = dfReader.readNext();
            while (df != null)
            {
                // If there are multiple df entries for the same termID then the latest df value
                // for that termID will be retained
                dfCollection[df.termID] = df.df;
                df = dfReader.readNext();
            }
        }

        public SortedDictionary<ulong, int> getDFCollection()
        {
            return this.dfCollection;
        }

        public int getDF(ulong termID)
        {
            return this.dfCollection[termID];
        }
    }
}
