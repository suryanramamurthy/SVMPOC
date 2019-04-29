using libsvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVMPOC

{
    class Program
    {
        private static TextReader settingsFile = new StreamReader(@"Settings.txt");
        private static SortedList<string, string> settings = new SortedList<string, string>();
        private static TFIDF tfidf;
        private static RelevancyDataSet relDataSet;
        static void Main(string[] args)
        {
            // Read the settings file
            string line;
            while ((line = Program.settingsFile.ReadLine()) != null) {
                if (!line.StartsWith("#") && line.Length > 0)
                {
                    string[] values = line.Split('=');
                    settings[values[0].Trim()] = values[1].Trim();
                }
            }

            if (settings["COMPUTEDOCCOUNT"].Equals("YES"))
            {
                int numDocs = computeDocCount();
                Console.WriteLine("The total number of documents in the corpus is {0}", numDocs);
            }


            if (settings["COMPUTEIDF"].Equals("YES")) computeIDF();

            if (settings["SVM"].Equals("YES"))
            {
                // Read the relevancy data set
                Console.WriteLine("starting to read the relevant and non-releavant data sets");
                relDataSet = new RelevancyDataSet(settings["RELFILE"]);

                // Get the list of docIDs from the relevancy data set for creating a small
                // TD Matrix just for this data
                IList<int> docIDList = relDataSet.getDocIDs();
                Console.WriteLine("Finished reading the relevancy data set");
                
                // Create the TDMatrix
                Console.WriteLine("Creating the TFIDF object with the given settings");
                tfidf = new TFIDF(settings);
                Console.WriteLine("Finished creating the object");

                Console.WriteLine("Starting the creation of the tdMatrix");
                tfidf.createTDMatrixForGivenDocList(settings["TFIDFFormat"], docIDList);
                Console.WriteLine("Finished creating the tdMatrix");

                System.Console.WriteLine("Writing the SVM relevant and non-relevant data set files");
                string trainFileName = settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_SVMData.train";
                string testFileName = settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_SVMData.test";
               
                System.IO.StreamWriter SVMTrainFile = new System.IO.StreamWriter(trainFileName);
                System.IO.StreamWriter SVMTestFile = new StreamWriter(testFileName);
                int relTrainCount = 0, relTestCount = 0, nonRelTrainCount = 0, nonRelTestCount = 0;
                int relDocCount = relDataSet.relDocList.Count;
                int nonRelDocCount = relDataSet.nonRelDocList.Count;
                Random randm = new Random();
                Console.WriteLine("Relevant Document Count is {0}", relDocCount);
                Console.WriteLine("Non Relevant Document Count is {0}", nonRelDocCount);
                Console.WriteLine("Total Document Count is {0}", docIDList.Count);

                // for each category, 20% of it should go in train file and 80% should go in test file
                foreach (int docId in docIDList)
                {
                    try
                    {
                        string SVMline = tfidf.getVSMforDocID(docId).getIndexedWeights();
                        if (relDataSet.isRelevant(docId))
                        {
                            // Randomly choose if the string has to go to training set or testing set
                            if (randm.Next(0, 2) == 0)
                            {
                                // Write to the training file. If the relTrainCount > 20% of relDocCount 
                                // then write to test file
                                if ((float) relTrainCount/relDocCount <= 1)
                                {
                                    SVMTrainFile.WriteLine("+1 " + SVMline);
                                    relTrainCount++;
                                }
                                else
                                {
                                    SVMTrainFile.WriteLine("+1 " + SVMline);
                                    relTrainCount++;
                                }
                            }
                            else
                            {
                                // Write to the testing file. If the relTestcount > 80% of relDocCount
                                // then write to training file
                                if ((float)relTestCount/relDocCount <= 1)
                                {
                                    SVMTrainFile.WriteLine("+1 " + SVMline);
                                    relTrainCount++;
                                }
                                else
                                {
                                    SVMTrainFile.WriteLine("+1 " + SVMline);
                                    relTrainCount++;
                                }
                            }
                        }
                        else
                        {
                            // Randomly choose if the string has to go to training set or testing set
                            if (randm.Next(0, 2) == 0)
                            {
                                // Write to the training file. If the nonRelTrainCount > 20% of nonRelDocCount 
                                // then write to test file
                                if ((float)nonRelTrainCount / nonRelDocCount <= 1)
                                {
                                    SVMTrainFile.WriteLine("-1 " + SVMline);
                                    nonRelTrainCount++;
                                }
                                else
                                {
                                    SVMTrainFile.WriteLine("-1 " + SVMline);
                                    nonRelTrainCount++;
                                }
                            }
                            else
                            {
                                // Write to the testing file. If the nonRelTestcount > 80% of nonRelDocCount
                                // then write to training file
                                if ((float)nonRelTestCount / nonRelDocCount <= 1)
                                {
                                    SVMTrainFile.WriteLine("-1 " + SVMline);
                                    nonRelTrainCount++;
                                }
                                else
                                {
                                    SVMTrainFile.WriteLine("-1 " + SVMline);
                                    nonRelTrainCount++;
                                }
                            }
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        // Ignore
                    }
                    Console.Write("Writing VSM number {0}", relTrainCount + relTestCount + 
                        nonRelTestCount + nonRelTrainCount);
                    Console.SetCursorPosition(0, Console.CursorTop);

                }
                SVMTestFile.Flush();
                SVMTrainFile.Flush();
                SVMTestFile.Close();
                SVMTrainFile.Close();
                Console.WriteLine("Total size of training file is --> {0}", relTrainCount + nonRelTrainCount);
                Console.WriteLine("Finished writing the SVM relevant data set file");
            }

            if (settings["SVMTRAIN"].Equals("YES"))
            {
                // Train the SVM Model from the train file settings
                string trainFileName = settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_SVMData.train";

                Console.WriteLine("Start time is --> {0}", DateTime.Now.ToString("h:mm:ss tt"));
                Console.WriteLine("Reading and scaling the training data");
                var data_set = libsvm.ProblemHelper.ReadAndScaleProblem(trainFileName);
                Console.WriteLine("Finished reading and scaling the training data");
                Console.WriteLine("Instantiating and training the SVM model");
                var svm = new C_SVC(data_set, KernelHelper.LinearKernel(), 0.5);
                Console.WriteLine("Exporting the SVM model to a file");
                svm.Export(settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_model.svm");
                Console.WriteLine("End time is --> {0}", DateTime.Now.ToString("h:mm:ss tt"));
            }

            if (settings["SVMPREDICT"].Equals("YES"))
            {
                // Load the prediction data from the location based on settings
                string testFileName = settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_SVMData.test";
                //string testFileName = settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_SVMData.train";

                Console.WriteLine("Loading and scaling the testing data");
                var data_set = libsvm.ProblemHelper.ReadAndScaleProblem(testFileName);
                Console.WriteLine("Importing the SVM model to use for prediction");
                var svm = new C_SVC(settings["SVMFILELOC"] + @"\" + settings["SVMBASEFILENAME"] + "_model.svm");
                Console.WriteLine("Starting the prediction");
                Console.WriteLine("Number of data to predict is {0}", data_set.l);
                int count = 0, correct = 0, incorrect = 0;
                for (int i = 0; i < data_set.l; i++)
                {
                    var x = data_set.x[i];
                    var y = data_set.y[i];
                    var predict = svm.Predict(x);
                    if (predict > 0) predict = 1;
                    else predict = -1;
                    count++;
                    if (y == predict) correct++;
                    else incorrect++;

                    Console.Write("Predicting document number {0}", count);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
                Console.WriteLine("Total data predicted is {0}", count);
                Console.WriteLine("Correct prediction is {0}, Incorrect prediction is {1}", correct, incorrect);
                double accuracy = ((double)correct / count) * 100;
                Console.WriteLine("Accuracy is {0}%", accuracy);
            }

            if (settings["COMPUTETDMATRIX"].Equals("YES"))
            {
                // Read the relevancy data set
                Console.WriteLine("starting to read the relevant and non-releavant data sets");
                relDataSet = new RelevancyDataSet(settings["RELFILE"]);

                // Get the list of docIDs from the relevancy data set for creating a small
                // TD Matrix just for this data
                IList<int> docIDList = relDataSet.getDocIDs();
                Console.WriteLine("Finished reading the relevancy data set");


                // Create the TDMatrix
                Console.WriteLine("Creating the TFIDF object with the given settings");
                tfidf = new TFIDF(settings);
                Console.WriteLine("Finished creating the object");

                Console.WriteLine("Starting the creation of the tdMatrix");
                tfidf.createTDMatrixForGivenDocList(settings["TFIDFFormat"], docIDList);
                Console.WriteLine("Finished creating the tdMatrix");

                if (docIDList.Count != tfidf.getDocCount())
                {
                    Console.WriteLine("The counts in the relevancy set and td matrix doesn't match");
                    Console.WriteLine("The relevancy doc Count is {0}", docIDList.Count);
                    Console.WriteLine("The td matrix doc Count is {0}", tfidf.getDocCount());
                }
            }

            // The below code will run the IDFRANGE 
            if (settings["RUNIDFRANGE"].Equals("YES"))
            {
                Console.WriteLine("Starting IDFRANGE simulation");
                simulateIDFRange(float.Parse(settings["DFSTART"]), 
                    float.Parse(settings["DFEND"]), 
                    float.Parse(settings["DFDECREMENT"]));
            }

            double similarity;
            // If Relevant and Non-Relevant similarity has to be calculated then do the following
            if (settings["CALCRELNONRELSIMILARITY"].Equals("YES"))
            {
                // Read the relevancy data set
                Console.WriteLine("starting to read the relevant and non-releavant data sets");
                relDataSet = new RelevancyDataSet(settings["RELFILE"]);

                Console.WriteLine("Starting the relevant vs non-relevant similarity calculation");
                similarity = computeRelNonRelSimilarity();
                Console.WriteLine("The similrity between relevant and non-relevant centroids are {0}", similarity);
            }

            if (settings["ROCCHIO"].Equals("YES")) executeRocchio(float.Parse(settings["DFVALUE"]), settings["MEASURE"], 10, 1000);
        }

        private static void executeRocchio(float dfValue, string measureType, int iterations, int numDocsPerIteration )
        {
            Console.WriteLine("Starting Rocchio simulation for terms with DF < {0}", dfValue);
            if (measureType.Equals("EUCLID"))
            {
                Console.WriteLine("EUCLID not yet supported. Exiting....");
                System.Environment.Exit(1);
            }
            // Create the relevancy Dataset
            Console.WriteLine("Creating the relevancy Dataset and docIDs");
            relDataSet = new RelevancyDataSet(settings["RELFILE"]);
            // Create the relevancy docID list
            IList<int> docIDList = relDataSet.getDocIDs();
            Console.WriteLine("Creating the td matrix");
            // Create TDMatrix for the given docIDList
            tfidf = new TFIDF(settings);
            tfidf.createTDMatrixForGivenDocList(settings["TFIDFFormat"], docIDList);
            // Create termList that DF <= dfValue (dfValue should range from 0 to 1)
            Console.WriteLine("Creating the filter termlist for the given dfValue {0}", dfValue);
            int[] termList = filteredTermsOnDF(dfValue);

            // Iterate for the Rocchio algorithm and print the relevant details
            // Create a Rochhio instance that will used for the below iterations
            Console.WriteLine("Staring rocchio iteration");
            Rocchio rocchio = new Rocchio(relDataSet, tfidf);
            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine("Rocchio iteration {0}", i);
                int[] selectedDocList;

                Console.WriteLine("Getting document list to be classified for this iteration");
                // For the first iteration select numDocPerIterations from the relevancy data set randomly
                if (i == 0) selectedDocList = rocchio.getRandomDocList(numDocsPerIteration);

                // For subsequent iterations select 80% of numDocsPerIteration from relevancy set and 20%
                // of numDocsPerIteration from non relevancy set. These documents will be the highest ranked
                // documents in the relevancy set.
                else selectedDocList = rocchio.getDocListToBeClassified(numDocsPerIteration);

                Console.WriteLine("Getting relevancy feedback from the lawyer for selected document list");
                // For the selectedDocList get the relevancy feedback from the lawyer
                int[] relevancyFeedback = relDataSet.getRelevancyFeedback(selectedDocList);

                Console.WriteLine("Adding the lawyer provided classification to the rocchio algorithm");
                // Add lawyer classified documents to the rocchio instance
                rocchio.addLatestLawyerClassifiedDocIDs(selectedDocList, relevancyFeedback);

                Console.WriteLine("Calculate relevant precision for this iteration");
                // Calculate precision on the up to date lawyer classified relevant list
                float relPrecision = rocchio.calculatePrecision("REL");
                Console.WriteLine("The relevant precision is {0}", relPrecision);

                Console.WriteLine("Calculating the non-relevant precision for this iteration");
                // Calculate precision on the up to date lawyer classified non-relevant list
                float nonRelPrecision = rocchio.calculatePrecision("NONREL");
                Console.WriteLine("The non-relevant precision is {0}", nonRelPrecision);

                Console.WriteLine("Calculating the relevant recall for this iteration");
                // Calculate the recall on the relevancy set based on the rocchio classification 
                float relRecall = relDataSet.recallPercentage("REL", rocchio.getRocchioClassifedDocIDs("REL", -1), false);
                Console.WriteLine("The relevant recall is {0}", relRecall);

                Console.WriteLine("Calculating the non-relevant recall for this iteration");
                float nonRelRecall = relDataSet.recallPercentage("NONREL", rocchio.getRocchioClassifedDocIDs("NONREL", -1), false);
                Console.WriteLine("The non-relevant recall is {0}", nonRelRecall);

                for (int topN = 1; topN <= 5; topN++)
                {
                    Console.WriteLine("Calculating the relevant recall for this iteration for top {0} rankings", topN*1000);
                    //Calculate the recall on the relevancy set based on the rocchio classification 
                    float topNrelRecall = relDataSet.recallPercentage("REL", rocchio.getRocchioClassifedDocIDs("REL", topN*1000), true);
                    Console.WriteLine("The top {0} relevant recall is {1}", topN*1000, topNrelRecall);

                    Console.WriteLine("Calculating the non-relevant recall for this iteration for top {0} rankings", topN*1000);
                    float topNnonRelRecall = relDataSet.recallPercentage("NONREL", rocchio.getRocchioClassifedDocIDs("NONREL", topN*1000), true);
                    Console.WriteLine("The top {0} non-relevant recall is {1}", topN*1000, topNnonRelRecall);
                }
                Console.WriteLine("Creating the new lawyer classified relevant and non-relevant centroids");
                // Create new relevant and non-relevant centroids and store it in the rocchio instance
                // based on the terms that have DF less than the given value for this simulation
                rocchio.calculateCentroids(termList);

                Console.WriteLine("Classifying the master document list for the next iteration");
                // Classify the master data in rocchio instance in relevant and nonrelevant set for the
                // next iteration based on the terms that have DF less than the given value for this simulation
                rocchio.classify(termList);
            }
            Console.WriteLine("Calculate relevant precision for this iteration");
            // Calculate precision on the up to date lawyer classified relevant list
            float relPrecision1 = rocchio.calculatePrecision("REL");
            Console.WriteLine("The relevant precision is {0}", relPrecision1);

            Console.WriteLine("Calculating the non-relevant precision for this iteration");
            // Calculate precision on the up to date lawyer classified non-relevant list
            float nonRelPrecision1 = rocchio.calculatePrecision("NONREL");
            Console.WriteLine("The non-relevant precision is {0}", nonRelPrecision1);

            Console.WriteLine("Calculating the relevant recall for this iteration");
            // Calculate the recall on the relevancy set based on the rocchio classification 
            float relRecall1 = relDataSet.recallPercentage("REL", rocchio.getRocchioClassifedDocIDs("REL", -1), false);
            Console.WriteLine("The relevant recall is {0}", relRecall1);

            Console.WriteLine("Calculating the non-relevant recall for this iteration");
            float nonRelRecall1 = relDataSet.recallPercentage("NONREL", rocchio.getRocchioClassifedDocIDs("NONREL", -1), false);
            Console.WriteLine("The non-relevant recall is {0}", nonRelRecall1);

            for (int topN = 1; topN <= 5; topN++)
            {
                Console.WriteLine("Calculating the relevant recall for this iteration for top {0} rankings", topN * 1000);
                //Calculate the recall on the relevancy set based on the rocchio classification 
                float topNrelRecall = relDataSet.recallPercentage("REL", rocchio.getRocchioClassifedDocIDs("REL", topN * 1000), true);
                Console.WriteLine("The top {0} relevant recall is {1}", topN * 1000, topNrelRecall);

                Console.WriteLine("Calculating the non-relevant recall for this iteration for top {0} rankings", topN * 1000);
                float topNnonRelRecall = relDataSet.recallPercentage("NONREL", rocchio.getRocchioClassifedDocIDs("NONREL", topN * 1000), true);
                Console.WriteLine("The top {0} non-relevant recall is {1}", topN * 1000, topNnonRelRecall);
            }
        }
        public static void simulateIDFRange(float dfStart, float dfEnd, float dfDecrement)
        {
            Console.WriteLine("starting the simulations on IDF");
            relDataSet = new RelevancyDataSet(settings["RELFILE"]);
            Console.WriteLine("Finished creating the relevant data set");

            for (float i = dfStart; i >= dfEnd; i -= dfDecrement)
            {
                Console.WriteLine("Starting the similarity computation for DF % <= {0}", i);
                // Identify a list of terms that df % <= i
                int[] termList = filteredTermsOnDF(i);
                Array.Sort<int>(termList);
                // Read the relevancy data set
                
                Console.WriteLine("Starting the relevant vs non-relevant similarity calculation");
                double similarity = computeRelNonRelSimilarity(termList);
                Console.WriteLine("The similarity between relevant and non-relevant centroids are {0} for DF % <= {1}", similarity, i);
            }
        }

        public static int[] filteredTermsOnDF(float dfPercent)
        {
            float IDF = (float)Math.Log(1.0 / dfPercent);
        
            List<int> filteredList = new List<int>();
            foreach (int key in tfidf.indexToIDF.Keys)
                if (tfidf.indexToIDF[(uint)key] >= IDF)
                    filteredList.Add(key);
            
            return filteredList.ToArray<int>();
        }
        public static int computeDocCount()
        {
            Console.WriteLine("Creating the TFIDF object with the given settings");
            tfidf = new TFIDF(settings);
            Console.WriteLine("Finished creating the object");

            Console.WriteLine("Computing the document count in the corpus");
            return tfidf.getCorpusDocCount();
        }
        public static double computeRelNonRelSimilarity()
        {
            
            // Create and populate the relevant and non-relevant VSM sets
            List<VSM> relVSMs = new List<VSM>();
            List<VSM> nonrelVSMs = new List<VSM>();
                        
            foreach (int key in relDataSet.relevancy.Keys)
            {
                if (relDataSet.relevancy[key] == 0 && tfidf.contains(key))
                    nonrelVSMs.Add(tfidf.getVSMforDocID(key));
                if (relDataSet.relevancy[key] == 1 && tfidf.contains(key))
                    relVSMs.Add(tfidf.getVSMforDocID(key));
            }

            // Calculate the centroids
            Console.WriteLine("Number of relevant documents are {0}", relVSMs.Count);
            Console.WriteLine("Number of non relevant documents are {0}", nonrelVSMs.Count);
            Console.WriteLine("Finding centroids of relevant VSMs");
            
            VSM relCentroid = VSM.findCentroid(relVSMs);
            Console.WriteLine("Finding centroids of non-relevant VSMs");
            
            VSM nonRelCentroid = VSM.findCentroid(nonrelVSMs);

            // Calculate cosine similarity and return
            Console.WriteLine("calculating cosine similarit between relevant and non-relevant centroids");
            return relCentroid.cosine(nonRelCentroid);
        }

        public static double computeRelNonRelSimilarity(int[] filteredDocList)
        {

            // Create and populate the relevant and non-relevant VSM sets
            List<VSM> relVSMs = new List<VSM>();
            List<VSM> nonrelVSMs = new List<VSM>();

            foreach (int key in relDataSet.relevancy.Keys)
            {
                if (relDataSet.relevancy[key] == 0 && tfidf.contains(key))
                    nonrelVSMs.Add(tfidf.getVSMforDocID(key));
                if (relDataSet.relevancy[key] == 1 && tfidf.contains(key))
                    relVSMs.Add(tfidf.getVSMforDocID(key));
            }

            Console.WriteLine("Finding centroids of relevant VSMs");

            VSM relCentroid = VSM.findCentroid(relVSMs, filteredDocList);
            Console.WriteLine("Finding centroids of non-relevant VSMs");

            VSM nonRelCentroid = VSM.findCentroid(nonrelVSMs, filteredDocList);

            // Calculate cosine similarity and return
            Console.WriteLine("calculating cosine similarit between relevant and non-relevant centroids");
            return relCentroid.cosine(nonRelCentroid, filteredDocList);
        }

        public static void computeIDF()
        {
            if (settings["IDFFORMAT"].Equals("IDF"))
            {
                IDF idf = new IDF(settings["DFFILE"], int.Parse(settings["CORPUSDOCCOUNT"]));
                if (settings["WRITEIDF"].Equals("YES")) idf.writeIDFToFile(settings["IDFSTOREBASENAME"]);
                if (settings["PRINTIDFSTATS"].Equals("YES")) idf.printStats();
            }
        }
    }
}
