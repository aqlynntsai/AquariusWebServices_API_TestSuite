using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ServiceModel;

using API_TestSuite_GUI;
using API_TestSuite_GUI.AASreference;
using API_TestSuite_GUI.ADSreference;

namespace Tests
{
    public delegate void OutLogger(string message);

    public abstract partial class TestMethod
    {
        public TestMethod(string name, TestSuite suite)
        {
            Name = name;
            Suite = suite;
            LoggerStream = Suite.AppendToLog;
            IsChecked = false;
            ptArray1 = Encoding.ASCII.GetBytes(csv1);
            ptArray2 = Encoding.ASCII.GetBytes(csv2);
            createTS = 0;
        }

        ~TestMethod()
        {
            if (createTS > 0)
            {
                deleteSuiteTS();
            }
        }

        public virtual void RunTest() { }

        public string Name { get; set; }

        public void Check() { IsChecked = true; }
        public void UnCheck() { IsChecked = false; }

        protected TestSuite Suite;

        protected static OutLogger LoggerStream;
        protected OutLogger ResultStream;
        protected void initSuite()
        {
            // some tests require time-stamps, other don't. The ones that do use this method, others don't.
            createTS = createNewTS();
            //createTime = createTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets createTime to the location's timezone

            append = appendData(ptArray1);

            Thread.Sleep(2000);

            halfTime = Suite.ADSclient.GetCurrentServerTime();// asAt = 1st half TS; changesSince = 2nd half TS
            //halfTime = halfTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets halfTime to the location's timezone
        }
        protected void initSuite2()
        {
            // some tests require time-stamps, other don't. The ones that do use this method, others don't.
            createTS = createNewTS();
            createTime = Suite.ADSclient.GetCurrentServerTime();// DateTime.Now;      // changesSince = full TS
            //createTime = createTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets createTime to the location's timezone

            appendResult = appendData2(ptArray1);

            Thread.Sleep(2000);

            halfTime = Suite.ADSclient.GetCurrentServerTime(); //DateTime.Now;        // asAt = 1st half TS; changesSince = 2nd half TS
            //halfTime = halfTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets halfTime to the location's timezone
        }
        protected string[] stringParse(string input, int type)
        {
            char[] splitChar = { ',', '\n' };
            string[] splitArray, result = null;
            int inSize, outSize, valStart;
            int cInd = 0;
            int nInd = 0;
            int ind = 0;
            string s1, s2, s3;

            switch (type)
            {
                case 0:         // csv files returned by the APS
                    valStart = input.LastIndexOf('l');
                    s1 = input.Substring(valStart + 1);
                    s2 = s1.Replace('T', ' ');
                    s3 = s2.Trim();

                    splitArray = s3.Split(splitChar);

                    inSize = splitArray.Length;
                    if (inSize < 6)
                        return new string[0];
                    outSize = inSize / 6;
                    outSize = (outSize) * 5;

                    result = new string[outSize];

                    for (ind = 0; ind < splitArray.Length; ind++)
                    {
                        cInd = ind % 6;
                        //if ((cInd != 0) && (cInd != 2))
                        if (cInd != 0)
                        {
                            if (cInd == 1)
                            {
                                string modStr = splitArray[ind];
                                int del = modStr.IndexOf('.');
                                if (del > 0)
                                {
                                    result[nInd] = modStr.Remove(del);
                                    nInd++;
                                }
                            }
                            else
                            {
                                result[nInd] = splitArray[ind].Trim();
                                nInd++;
                            }
                        }
                    }
                    break;

                case 1:         // csv files (in string form) as entered by user
                    s3 = input.Trim();
                    splitArray = s3.Split(splitChar);

                    inSize = splitArray.Length;
                    outSize = inSize / 6;
                    outSize = (outSize) * 5;

                    result = new string[outSize];

                    for (ind = 0; ind < splitArray.Length; ind++)
                    {
                        cInd = ind % 6;
                        if (cInd != 2)
                        {
                            result[nInd] = splitArray[ind].Trim();
                            nInd++;
                        }
                    }
                    break;

                case 2:         // Data Set Lists and Rating Tables
                    //string fullTSname = tsLabel + " @" + tsLoc;
                    //valStart = input.LastIndexOf(/*tsLabel+" @"+tsLoc*/tsName);

                    //s2 = input.Substring(valStart + tsName.Length + 1);
                    //s3 = s2.Trim();
                    //result = s3.Split(splitChar);
                    string locId = Suite.tsParam + "." + Suite.tsLabel + "@" + Suite.tsLoc;
                    valStart = input.LastIndexOf(locId);
                    s2 = input.Substring(valStart + locId.Length + 1);
                    s3 = s2.Trim();
                    result = s3.Split(splitChar);
                    break;

                case 3:
                    valStart = input.IndexOf(Suite.tsLoc);

                    s2 = input.Substring(valStart + Suite.tsLoc.Length + 1);
                    s3 = s2.Trim();
                    result = s3.Split(splitChar);
                    break;

                case 4:
                    splitArray = input.Split('\n');
                    s2 = splitArray[1];
                    s3 = s2.Split(',')[5];
                    result = new string[] { s3 };
                    break;

            }

            /*
            System.Diagnostics.Trace.WriteLine("Date-Val string: " + s3);
            System.Diagnostics.Trace.WriteLine("End Date-Val string");
            
            System.Diagnostics.Trace.WriteLine("inSize: " + inSize + "\toutSize: " + outSize);
            
            System.Diagnostics.Trace.WriteLine("Split array: ");
            for (int tst = 0; tst < inSize; tst++)
            {
                System.Diagnostics.Trace.WriteLine(tst + ": " + splitArray[tst] + ";");
            }
            System.Diagnostics.Trace.WriteLine("cInd: " + cInd);

            System.Diagnostics.Trace.WriteLine("Parsed array: ");
            foreach (string s in result)
            {
                System.Diagnostics.Trace.WriteLine(s);
            }
            */

            return result;
        }
        protected long createNewTS()
        {
            long val = 0;
            try
            {
                if (deleteSuiteTS() >= 0)
                {
                    // create new TS
                    using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                    {
                        val = Suite.AASclient.CreateTimeSeries(Suite.tsName);
                    }
                    createTime = Suite.ADSclient.GetCurrentServerTime();
                    System.Diagnostics.Trace.WriteLine("New TS Created at ServerTime = " + createTime.ToString("yyyy-MM-dd HH:mm:ss.fffzzz"));
                    /* Param: HG
                    *  Label: JoshAPItest
                    *  Location: @02AB006
                    *  AQParentID: 5000 0000 0000 0023
                    *  TZ: -05:00
                    *  NOTE: No longer current selection, now uses random AOP taken from DB
                    */
                }
                return val;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("Unable to create new Time Series, an Exception was thrown. Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("Unable to create new TS - CreateTimeSeries() threw: " + ex.Message);
                return -1;
            }
        }

        public struct TimeSeriesParameters
        {
            public long _parentId;
            public string _label;
            public string _comments;
            public string _description;
            public string _parameter;
            public int _utcOffsetInMin;
            public string _unit;
            public double _maxGaps;

            public TimeSeriesParameters(long parentId, string label, string comments, string description, string parameter, int utcOffsetInMinutes, string unit, double maxGaps)
            {
                _parentId = parentId;
                _label = label;
                _comments = comments;
                _description = description;
                _parameter = parameter;
                _utcOffsetInMin = utcOffsetInMinutes;
                _unit = unit;
                _maxGaps = maxGaps;
            }
        }

        protected TimeSeriesParameters createDefaultTimeSeriesCreationParameters()
        {
            TimeSeriesParameters parameters = new TimeSeriesParameters();
            parameters._parameter = "HG";
            parameters._label = "JoshAPItest";
            parameters._comments = "JoshAPItest -> done via createTS2";
            parameters._description = "JoshAPItest description";
            parameters._utcOffsetInMin = (int)(Suite.locUTCOff_hrs * 60);
            parameters._unit = "m";
            parameters._maxGaps = 0.0;
            parameters._parentId = deleteSuiteTS();

            return parameters;
        }


        protected long createNewTS2()
        {
            TimeSeriesParameters parameters = createDefaultTimeSeriesCreationParameters();
            return createNewTS2(parameters);
        }

        protected long createNewTS2(TimeSeriesParameters parameters)
        {
            long val = 0;
            try
            {
                if (parameters._parentId >= 0)
                {
                    using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                    {
                        val = Suite.AASclient.CreateTimeSeries2(parameters._parentId, parameters._label, parameters._comments, parameters._description, parameters._parameter, parameters._utcOffsetInMin, parameters._unit, parameters._maxGaps);
                    }
                    System.Diagnostics.Trace.WriteLine("New TS Created!");
                }
                return val;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("Unable to create new Time Series, an Exception was thrown. Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("Unable to create new TS - CreateTimeSeries2() threw: " + ex.Message);
                return -1;
            }
        }

        protected int appendData(string appendDataString)
        {
            return appendData(Encoding.ASCII.GetBytes(appendDataString));
        }

        protected int appendData(byte[] data)
        {
            int val = 0;
            try
            {
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    val = Suite.AASclient.AppendTimeSeriesFromBytes(createTS, data, "API Test Suite", null);
                }
                Suite.AppendToLog("AppendTimeSeriesFromBytes complete time: " + Suite.ADSclient.GetCurrentServerTime().ToString("yyyy-MM-dd HH:mm:ss.fffzzz"));
                return val;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("Unable to append data, an Exception was thrown. Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("Unable to append data - AppendTimeSeriesFromBytes() threw: " + ex.Message);
                return -1;
            }
        }
        protected AppendResult appendData2(byte[] data)
        {
            AppendResult result = null;
            try
            {
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    result = Suite.AASclient.AppendTimeSeriesFromBytes2(createTS, data, "API Test Suite");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("Unable to append data, an Exception was thrown.  Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("Unable to append data - AppendTimeSeriesFromBytes2() threw: " + ex.Message);
            }

            return result;
        }
        protected long deleteSuiteTS()
        {
            try
            {
                string parameter = "HG";
                string label = "JoshAPItest";
                string sql = string.Format("delete from AQAtom_TimeSeries_ where aqparentid_={0} and parametertype_='{1}' and label_='{2}'", Suite.tsLocID, parameter, label);
                Suite.ADSclient.InvokeAopMethod("Execute", new object[] { sql });

                System.Diagnostics.Trace.WriteLine("Deleted: " + Suite.tsLocID);

                return Suite.tsLocID;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("An Exception was thrown by deleteTS(). Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("TestSuite method deleteTS() threw: " + ex.Message);
                return -1;
            }
        }

        protected byte[] ptArray1, ptArray2;

        protected static string csv1 = @"2010-07-31 01:00:00,4.89599990844727,192,10,1,3
                                          2010-07-31 01:05:00,4.89499998092651,192,10,1,3
                                          2010-07-31 01:10:00,4.89400005340576,192,10,1,3
                                         ";

        protected static string csv2 = @"2010-07-31 01:15:00,4.88899993896484,192,10,1,3
                                          2010-07-31 01:20:00,4.89400005340576,192,10,1,3
                                          2010-07-31 01:25:00,4.89300012588501,192,10,1,3
                                         ";

        protected long createTS; // A.K.A. created TimeSeries' ID
        protected int append;
        protected AppendResult appendResult;
        protected DateTime createTime, halfTime;

        public bool IsChecked;
    }

    public class LocationList
    {
        public LocationList(TestSuite caller)
        {
            _caller = caller;
        }

        ~LocationList()
        {
            cleanUpCreatedLocations();
        }

        public const string testLocationNamePrefix = "PublishAPI testSuite Location";
        public LocationDTO createLocation()
        {
            string identifier = testLocationNamePrefix + " " + DateTime.Now.ToString();
            string locName = testLocationNamePrefix + " For " + _caller.Name;

            return createLocation(identifier, locName);
        }

        public LocationDTO createLocation(string identifier, string locationName)
        {
            Thread.Sleep(1000);
            LocationDTO existingLocation = _caller.AASclient.GetLocation(_caller.tsLocID);
            existingLocation.Identifier = identifier;
            existingLocation.LocationName = locationName;
            existingLocation.LocationPath = existingLocation.LocationPath.Split('.')[0];

            long locId = -1;
            cleanUpCreatedLocations();

            try
            {
                using (TestSuite.NewContextScope(_caller.AASclient.InnerChannel))
                {
                    locId = _caller.AASclient.CreateLocation(existingLocation);
                }

                if (locId < 0)
                {
                    throw new Exception("CreateLocation returned invalid locationId");
                }

                existingLocation.LocationId = locId;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            _locationIdList.Add(locId);
            return existingLocation;
        }

        protected void cleanUpCreatedLocations()
        {
            if (_locationIdList.Count > 0)
            {
                try
                {
                    _caller.ADSclient.DeleteLocationList(_locationIdList.ToArray(), true, "");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            _locationIdList.Clear();
        }

        protected List<long> _locationIdList = new List<long>();
        protected TestSuite _caller;
    }

    public class csvData
    {
        static string[] rowDeliminaters = new string[] { "\n", "\r" };
        static string[] columnDeliminaters = new string[] { "," };
        
        public csvData(string rawData)
        {
            List<string> rows = getRowsList(rawData);
            if (rows.Count < 1)
            {
                return;
            }

            List<string> headers = getEntriesFromRow(rows[0]);
            for(uint columnIndex=0; columnIndex < headers.Count; ++columnIndex)
            {
                _headers.Add(headers[(int)columnIndex], columnIndex);
            }
            rows.RemoveAt(0);

            rows.ForEach(delegate(string row)
            {
                List<string> entries = getEntriesFromRow(row);
                _tableValues.Add(entries);
            });
        }

        static List<string> getRowsList(string rowsData)
        {
            List<string> rowsList = new List<string>();
            // remove empty row.
            string[] rows = rowsData.Split(rowDeliminaters, StringSplitOptions.RemoveEmptyEntries);
            foreach(string row in rows)
            {
                rowsList.Add(row);
            }
            return rowsList;
        }

        static List<string> getEntriesFromRow(string row)
        {
            List<string> entriesList = new List<string>();
            // do not remove empty entries
            string[] entries = row.Split(columnDeliminaters, StringSplitOptions.None);
            foreach (string entry in entries)
            {
                entriesList.Add(entry.Trim());
            }
            return entriesList;
        }

        public string checkHeaders(string[] expectedHeaders)
        {
            foreach (string headerEntry in expectedHeaders)
            {
                if (!_headers.ContainsKey(headerEntry))
                {
                    return "Unexpected headers found: "
                        + _headers.Keys.ToString()
                        + "\nExpected: "
                        + expectedHeaders;
                }
            }
            return string.Empty;
        }

        public Dictionary<string, uint> _headers = new Dictionary<string, uint>();
        public List<List<string>> _tableValues = new List< List<string> >();
    }



    #region PublishTests
    public abstract class PublishTestMethod : TestMethod
    {
        public PublishTestMethod(string name, TestSuite suite)
            : base(name, suite)
        {
            ResultStream = Suite.AppendToPubResBox;
        }
        public override void RunTest()
        {
            base.RunTest();
        }

        protected void testPublishServiceAPI(publishServiceMethodDelegate method)
        {
            LoggerStream(Name + ":");
            string result = tryInvokePublishServiceMethod(method);
            if (result.Length > 0)
            {
                ResultStream(result);
            }
            else
            {
                ResultStream("Passed. ");
                Suite.AppendToLog("Passed. ");
            }            
        }

        protected string tryInvokePublishServiceMethod(publishServiceMethodDelegate method)
        {
            try
            {
                using (TestSuite.NewContextScope((Suite.APSclient).InnerChannel))
                {
                    return method();
                }
            }
            catch (Exception e)
            {
                LoggerStream("Exception Message: " + e.Message);
                return "Threw Exception";
            }
        }

        protected delegate string publishServiceMethodDelegate();
    }

    public partial class GetParameterListTest : PublishTestMethod
    {
        public GetParameterListTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetParameterList_test();
        }
        private void GetParameterList_test()
        {
            string result = "";
            LoggerStream("GetParameterList: ");
            try
            {
                string paramList = string.Empty;
                using (TestSuite.NewContextScope((Suite.APSclient).InnerChannel))
                {
                    paramList = Suite.APSclient.GetParameterList();
                }
                if (paramList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }



        }
    }
    public partial class GetFlagListTest : PublishTestMethod
    {
        public GetFlagListTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetFlagList_test();
        }
        private void GetFlagList_test()
        {

            string result = "";
            LoggerStream("GetFlagList: ");
            try
            {
                string flagList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    flagList = Suite.APSclient.GetFlagList();
                }
                if (flagList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }
    public partial class GetGradeListTest : PublishTestMethod
    {
        public GetGradeListTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetGradeList_test();
        }
        private void GetGradeList_test()
        {
            string result = "";
            LoggerStream("GetGradeList: ");
            try
            {
                string gradeList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    gradeList = Suite.APSclient.GetGradeList();
                }
                if (gradeList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
      }
    }
    public partial class GetApprovalListTest : PublishTestMethod
    {
        public GetApprovalListTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetApprovalList_test();
        }
        private void GetApprovalList_test()
        {
            string result = "";
            LoggerStream("GetApprovalList: ");
            try
            {
                string approvalList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    approvalList = Suite.APSclient.GetApprovalList();
                }
                if (approvalList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }

            //this.pubResultBox.Lines = pubResBoxLines;
            //pubLineCount++;
        }
    }
    public partial class GetPublishViewListTest : PublishTestMethod
    {
        public GetPublishViewListTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetPublishViewList_test();
        }
        private void GetPublishViewList_test()
        {
            string result = "";
            LoggerStream("GetPublishViewList: ");
            try
            {
                string pubViewList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    pubViewList = Suite.APSclient.GetPublishViewList();
                }
                if (pubViewList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }

    public partial class GetDataSetsListAllTest : PublishTestMethod
    {
        public GetDataSetsListAllTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetDataSetsList_test();
        }
        private void GetDataSetsList_test()
        {
            string result = "";
            LoggerStream("GetDataSetsList: ");
            try
            {
                string dataSetList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    dataSetList = Suite.APSclient.GetDataSetsList(Suite.tsLoc, null);
                }
                if (dataSetList.Length != 0)
                    result = "pass";
                else
                    result = "FAIL";
                LoggerStream(result);
                ResultStream(result);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }
    public partial class GetDataSetsListChangesSinceTimeTest : PublishTestMethod
    {
        // TODO: There is a bug here caused by the following:
        // When appendData is called, the ModifiedDate field on the dataset is 
        // updated, but using the datetime of the *database* server.
        // The test code uses the datetime of the *application* server for testing 
        // whether the appended data had an updated date.
        // This comparison of datetimes from different servers can be problematic: 
        // If the database is significantly (>10 seconds) slower than
        // the application server, the 
        // Suite.APSclient.GetDataSetsList(Suite.tsLoc, changesSince)
        // command will always return nothing, causing 
        // an exception to be thrown in this line of code: 
        // linesplitDataSet[1].Split(',')[12][0];
        public GetDataSetsListChangesSinceTimeTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetDataSetsList_changesSinceTime_test();
        }
        private void GetDataSetsList_changesSinceTime_test()
        {
            LoggerStream("GetDataSetsList(changesSinceTime): ");

            string changesSince = "";

            initSuite();
            changesSince = createTime.ToString("yyyy-MM-dd HH:mm:ss.fffzzz");
            appendData(ptArray2);
            try
            {
                string dataSetList = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    dataSetList = Suite.APSclient.GetDataSetsList(Suite.tsLoc, null);
                }
                string dataSetListSince = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    dataSetListSince = Suite.APSclient.GetDataSetsList(Suite.tsLoc, changesSince);
                }

                string[] delims = { "\r\n" };
                string[] linesplitDataSet = dataSetListSince.Split(delims, StringSplitOptions.RemoveEmptyEntries);

                if (linesplitDataSet.Length < 2)
                {
                    LoggerStream("GetDataSetsList returned no datasets");
                    ResultStream("FAIL");
                    return;
                }

                char numPtsChanged = linesplitDataSet[1].Split(',')[12][0];
                if (numPtsChanged == '6')
                {
                    ResultStream("pass");
                    LoggerStream(string.Format("pass - returned {0} points", numPtsChanged));
                }
                else
                {
                    ResultStream("FAIL");
                    LoggerStream(string.Format("FAIL - returned {0} points, expected 6 points", numPtsChanged));
                }
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }

    public struct GetTimeSeriesDataQuery
    {
        public string _dataId;
        public string _publishView;
        public string _queryFromTime;
        public string _queryToTime;
        public string _changesSinceTime;
        public string _asAtTime;

        public GetTimeSeriesDataQuery(string dataId, string publishView, string queryFromTime, string queryToTime, string changesSinceTime, string asAtTime)
        {
            _dataId = dataId;
            _publishView = publishView;
            _queryFromTime = queryFromTime;
            _queryToTime = queryToTime;
            _changesSinceTime = changesSinceTime;
            _asAtTime = asAtTime;
        }
    }

    public class GetTimeSeriesDataTest : PublishTestMethod
    {
        protected delegate string GetTimeSeriesDataAPI(GetTimeSeriesDataQuery query);
        protected GetTimeSeriesDataAPI _queryAPI;
        protected string _appendData;

        protected GetTimeSeriesDataTest(string name, TestSuite suite, string appendDataString, GetTimeSeriesDataAPI queryAPI)
            : base(name, suite)
        {
            _queryAPI = queryAPI;
            _appendData = appendDataString;
        }

        public GetTimeSeriesDataTest(string name, TestSuite suite, string appendDataString)
            : this(
                name,
                suite,
                appendDataString,
                delegate(GetTimeSeriesDataQuery query)
                {
                    return suite.APSclient.GetTimeSeriesData(query._dataId, query._publishView, query._queryFromTime, query._queryToTime, query._changesSinceTime, query._asAtTime);
                }
            )
        {
        }

        public override void RunTest()
        {
 	        base.RunTest();
            RunGetTimeSeriesDataTest();
        }

        protected virtual void RunGetTimeSeriesDataTest()
        {
            System.Diagnostics.Trace.WriteLine(Name);
            initializeTimeSeries();

            try
            {
                queryGetTimeSeriesDataAPI();
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }

        public virtual void queryGetTimeSeriesDataAPI()
        {
            string input = string.Empty;
            GetTimeSeriesDataQuery query = getQuery(null);
            input = getTimeSeriesData(query);
            evaluateSuccess(stringParse(input, 0), stringParse(_appendData, 1));
        }

        public virtual void initializeTimeSeries(string appendDataString)
        {
            System.Diagnostics.Trace.WriteLine(Name);
            LoggerStream(Name);

            createTS = createNewTS2();
            appendData(Encoding.ASCII.GetBytes(appendDataString));
        }

        protected virtual void initializeTimeSeries()
        {
            initializeTimeSeries(_appendData);
        }

        public string timeStringFormat(DateTime dt, float utcHrs)
        {
            char sign;
            if (utcHrs >= 0)
                sign = '+';
            else
                sign = '-';
            string result = dt.ToString("yyyy-MM-dd") + "T" + dt.ToString("HH:mm:ss.fffzzz") + sign + Math.Abs(utcHrs).ToString("#00") + ":00";
            return result;
        }
        protected string getDate(string date, int type)
        {
            string changesSince = null;
            switch (type)
            {
                case 0:
                    goto default;
                case 1:
                    changesSince = date;
                    System.Diagnostics.Trace.WriteLine("Date: " + changesSince);
                    break;
                default:
                    changesSince = halfTime.ToString("yyyy-MM-dd HH:mm:sszzz");
                    System.Diagnostics.Trace.WriteLine("Date: " + changesSince);
                    break;
            }
            return changesSince ?? halfTime.ToString("yyyy-MM-dd HH:mm:sszzz");
        }

        protected virtual GetTimeSeriesDataQuery getQuery(string asAt)
        {
            string input = string.Empty;
            GetTimeSeriesDataQuery query = new GetTimeSeriesDataQuery(Suite.tsName, "Public", null, null, null, asAt);
            return query;
        }

        protected virtual string getTimeSeriesData(GetTimeSeriesDataQuery getTimeSeriesDataQuery)
        {
            string resultString = String.Empty;
            using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
            {
                resultString += _queryAPI(getTimeSeriesDataQuery);
            }
            System.Diagnostics.Trace.WriteLine("From APS: " + resultString);
            LoggerStream("Method Output: " + resultString);
            
            return resultString;
        }

        protected virtual void evaluateSuccess(string[] actual, string[] expected)
        {
            int passCount = 0;
            bool lengthPass = true;
            bool[] result = new bool[5];
            string allResult = "";

            if (expected.Length != actual.Length)
            {
                ResultStream("FAIL");
                lengthPass = false;
                LoggerStream(string.Format("FAIL - Returned {0} points, expected {1} points", actual.Length, expected.Length));
            }
            else
            {
                int cInd = 0;
                for (int i = 0; i < expected.Length; i++)
                {
                    cInd = i % 5;
                    switch (cInd)
                    {
                        case 0:
                            System.Diagnostics.Trace.WriteLine("Date: " + actual[i]);
                            if (expected[i] == actual[i])
                                result[0] = true;
                            else
                            {
                                result[0] = false;
                                LoggerStream(string.Format("- Date Incorrect. Returned {0}, expected {1}", actual[i], expected[i]));
                            }
                            break;
                        case 1:
                            System.Diagnostics.Trace.WriteLine("Value: " + actual[i]);
                            double e = Convert.ToDouble(expected[i]);
                            double a = Convert.ToDouble(actual[i]);
                            if (e.ToString("#0.000") == a.ToString("#0.000"))
                                result[1] = true;
                            else
                            {
                                result[1] = false;
                                LoggerStream(string.Format("- Value Incorrect. Returned {0}, expected {1}", actual[i], expected[i]));
                            }
                            break;
                        case 2:
                            System.Diagnostics.Trace.WriteLine("Quality: " + actual[i]);
                            if (expected[i] == actual[i])
                                result[2] = true;
                            else
                            {
                                result[2] = false;
                                LoggerStream(string.Format("- Quality Incorrect. Returned {0}, expected {1}", actual[i], expected[i]));
                            }
                            break;
                        case 3:
                            System.Diagnostics.Trace.WriteLine("Interpolation: " + actual[i]);
                            if (expected[i] == actual[i])
                                result[3] = true;
                            else
                            {
                                result[3] = false;
                                LoggerStream(string.Format("- Interpolation Incorrect. Returned {0}, expected {1}", actual[i], expected[i]));
                            }
                            break;
                        case 4:
                            System.Diagnostics.Trace.WriteLine("Approval: " + actual[i]);
                            if (expected[i] == actual[i])
                                result[4] = true;
                            else
                            {
                                result[4] = false;
                                LoggerStream(string.Format("- Approval Incorrect. Returned {0}, expected {1}", actual[i], expected[i]));
                            }
                            break;
                    }
                }
            }
            if (lengthPass)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (result[i])
                    {
                        allResult += "p";
                        passCount++;
                    }
                    else
                    {
                        allResult += "F";
                    }
                }
                if (passCount == 5)
                    LoggerStream("pass - all parameters");
                ResultStream(allResult + " (" + passCount + "/5)");
            }
        }
    }

    public class GetTimeSeriesDataAllTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataAllTest(string name, TestSuite suite) : base(name, suite, csv1 + csv2) { }
    }

    public class GetTimeSeriesDataResampledTest : GetTimeSeriesDataChangesSinceTest
    {
        protected static string csv1WithIntermediatePoints =
                                        @"2010-07-31 01:02:50,4.89599990844727,192,10,1,3
                                          2010-07-31 01:07:50,4.89499998092651,192,10,1,3
                                         ";

        public GetTimeSeriesDataResampledTest(string name, TestSuite suite, string anchorTime)
            : base(name, suite, anchorTime)
        {

            _queryAPI = delegate(GetTimeSeriesDataQuery query)
                {
                    return suite.APSclient.GetTimeSeriesDataResampled(query._dataId, query._publishView, query._queryFromTime, query._queryToTime, 5, query._changesSinceTime);
                };
        }

        protected override void initializeTimeSeries()
        {
            base._appendData = csv1;
            base.initializeTimeSeries();
            appendData(csv1WithIntermediatePoints);
        }
    }

    public class GetTimeSeriesRawDataAllTest : GetTimeSeriesDataAllTest
    {
        public GetTimeSeriesRawDataAllTest(string name, TestSuite suite)
            : base(name, suite)
        {
            _queryAPI = delegate(GetTimeSeriesDataQuery query)
            {
                return suite.APSclient.GetTimeSeriesRawData(query._dataId, query._publishView, query._queryFromTime, query._queryToTime, query._changesSinceTime, query._asAtTime);
            };
        }

        ~GetTimeSeriesRawDataAllTest()
        {
            deleteTimeSeriesFromIdList(_timeSeriesFromAOPIdList);
        }

        protected List<long> _timeSeriesFromAOPIdList = new List<long>();
        protected long createNewTS3(string aopFileFullPath)
        {
            long val = 0;
            try
            {
                if (deleteSuiteTS() >= 0)
                {
                    byte[] aopBytes = System.IO.File.ReadAllBytes(aopFileFullPath);

                    // create new TS
                    using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                    {
                        val = Suite.ADSclient.CreateTimeSeries3(Suite.tsLocID, aopBytes);
                    }
                    System.Diagnostics.Trace.WriteLine("New TS Created!");
                }

                _timeSeriesFromAOPIdList.Add(val);
                return val;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("Unable to create new Time Series, an Exception was thrown. Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("Unable to create new TS - CreateTimeSeries() threw: " + ex.Message);
                return -1;
            }
        }

        protected void deleteTimeSeriesFromIdList(List<long> idList)
        {
            try
            {
                foreach (long id in idList)
                {
                    Suite.AASclient.DeleteTimeSeries(id);
                    System.Diagnostics.Trace.WriteLine("Deleted: " + id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                MessageBox.Show("An Exception was thrown by deleteTS(). Original message: " + ex.Message, "Aquarius Acquisition Service Error");
                LoggerStream("TestSuite method deleteTS() threw: " + ex.Message);
            }
        }
    }

    public class GetTimeSeriesDataFromTimeToTimeTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataFromTimeToTimeTest(string name, TestSuite suite, string FromTime, string ToTime)
            : base(name, suite, csv2)
        {
            fromTime = FromTime;
            toTime = ToTime;
        }

        protected override void initializeTimeSeries()
        {
            initSuite();
            appendData(Encoding.ASCII.GetBytes(_appendData));
        }

        protected override void RunGetTimeSeriesDataTest()
        {
            LocationDTO locationInfo = Suite.AASclient.GetLocation(Suite.tsLocID);
            String locTimezoneOffset = String.Format("{0:+00;-00;+00}:00", locationInfo.UtcOffset);
            try
            {
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    locationInfo = Suite.AASclient.GetLocation(Suite.tsLocID);
                    locTimezoneOffset = String.Format("{0:+00;-00;+00}:00", locationInfo.UtcOffset);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Unable to retrieve location information - test failed. Exception: {0}", e.Message);
                LoggerStream(String.Format("Unable to retrieve location information - test failed. Exception: {0}", e.Message));
                ResultStream("FAIL");
                return;
            }

            if (fromTime == string.Empty)
            {
                fromTime = String.Format("{0}{1}", "2010-07-31T01:15:00.000", locTimezoneOffset);
            }

            if (toTime == string.Empty)
            {
                toTime = String.Format("{0}{1}", "2010-07-31T01:25:00.000", locTimezoneOffset);
            }

            RunGetTimeSeriesDataTest(fromTime, toTime, 0);
        }       
        
        protected void RunGetTimeSeriesDataTest(string fromTime, string toTime, int type)
        {
            System.Diagnostics.Trace.WriteLine("Get TS data fromTime toTime, type {0}: ", type.ToString());
            LoggerStream(String.Format("GetTimeSeriesData(fromTime, toTime), type {0}: ", type.ToString())); //Semantic change

            initializeTimeSeries();

            try
            {
                string input = string.Empty;
                GetTimeSeriesDataQuery query = getQuery(null);
                query._queryFromTime = fromTime;
                input = getTimeSeriesData(query);

                switch (type)
                {
                    case 0:
                        goto default;
                    case 1:
                        #region C1_Details
                        if (input.EndsWith(fromTime + "," + toTime + ",,\r\n"))
                        {
                            LoggerStream("pass");
                            ResultStream("pass");
                        }
                        else
                        {
                            LoggerStream("FAIL");
                            LoggerStream("Method output: \n" + input);
                            ResultStream("FAIL");
                        }
                        #endregion
                        break;
                    default:
                        string[] actual = stringParse(input, 0);
                        string[] expected = stringParse(csv2, 1);
                        evaluateSuccess(actual, expected);

                        break;
                }
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }

        }

        public string fromTime{ get; set; }
        public string toTime{ get; set; }
    }

    public class GetTimeSeriesDataAsAtTimeTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataAsAtTimeTest(string name, TestSuite suite, string AsWhen, int Type)
            : base(name, suite, csv2)
        {
            asWhen = AsWhen;
            type = Type;
        }

        private string asWhen;
        private int type;

        public override void initializeTimeSeries(string appendDataString)
        {
            initSuite();
            appendData(Encoding.ASCII.GetBytes(appendDataString));
        }

        protected override void RunGetTimeSeriesDataTest()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(String.Format("Get TS data AS OF {0}, type {1}", asWhen, type)); //same
                Suite.writeLog.Append(String.Format("GetTimeSeriesData(asAtTime): Time: {0} Type: {1}", asWhen, type));

                initializeTimeSeries();

                string asAt = String.Empty;
                string input = String.Empty;
                GetTimeSeriesDataQuery query;

                switch (type) //Define asAt, get input, evaluate
                {
                    case 0: //Default test, default date
                        goto default;
                    case 1: //Date specified, before all points
                        asAt = asWhen;
                        query = getQuery(asWhen);
                        input = getTimeSeriesData(query);
                        System.Diagnostics.Trace.WriteLine("Date: " + asAt);
                        evaluateSuccess(stringParse(input, 0), stringParse(csv1 + csv2, 1));
                        break;
                    default:
                        asAt = halfTime.ToString("yyyy-MM-dd HH:mm:sszzz");
                        query = getQuery(halfTime.ToString("yyyy-MM-dd HH:mm:sszzz"));
                        input = getTimeSeriesData(query);
                        System.Diagnostics.Trace.WriteLine("Date: " + asAt);
                        evaluateSuccess(stringParse(input, 0), stringParse(csv1, 1));
                        break;
                }

            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }

    public class GetTimeSeriesDataChangesSinceTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataChangesSinceTest(string name, TestSuite suite, string FromWhen)
            : base(name, suite, csv2)
        {
            fromWhen = FromWhen;
        }

        protected override void RunGetTimeSeriesDataTest()
        {
            GetTimeSeriesData_changesSinceTime_test(fromWhen, 0);
        }
        /// <summary>
        /// <para>Performs GetTimeSeriesData with changesSince parameter, based on input:
        /// case 0 (def): Appends two csvs, evaluates changesSince between the appends
        /// case 1: Appends two csvs, evaluates changesSince at the specified date. Date format: "yyyy-MM-dd HH:mm:ss"</para>
        /// </summary>
        /// <param name="date"></param>
        /// <param name="type"></param>
        private void GetTimeSeriesData_changesSinceTime_test(string date, int type)
        {
            System.Diagnostics.Trace.WriteLine("Get TS data SINCE, type " + type.ToString() + ": ");
            LoggerStream(Name + ", type: " + type.ToString() + ": ");
            initializeTimeSeries();

            string changesSince = getDate(date, type);
            LoggerStream(String.Format("Changes since parameter: {0}", changesSince));
            try
            {
                string input = string.Empty;
                GetTimeSeriesDataQuery query = getQuery(null);
                query._changesSinceTime = changesSince;
                input = getTimeSeriesData(query);
                System.Diagnostics.Trace.WriteLine("From APS: " + input);

                string[] actual = stringParse(input, 0);
                string[] expected = stringParse(_appendData,1);

                if (expected.Length != actual.Length)
                {
                    ResultStream("FAIL");
                    LoggerStream(string.Format("FAIL - Returned {0} points, expected {1} points", actual.Length, expected.Length));
                }
                else
                {
                    evaluateSuccess(actual, expected);
                }
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }

        }

        private string fromWhen;
    }

    public class GetTimeSeriesDataCustomTest : GetTimeSeriesDataTest
    {
        /// <summary>
        /// Required params: string name, TestSuite Suite, int NumPtsExpected, string Data1. //// Optional params: string publishView, queryFromTime, queryToTime, changesSinceTime, asAtTime, Data2. //// If Data2 is specified, asAtTime will be ignored and the interim time between appends will be used.
        /// </summary>
        public GetTimeSeriesDataCustomTest(string name, TestSuite suite, int numPointsExpected, string data, string data2, params string[] args)
            : base(name, suite, data)
        {
            NumPtsExpected = numPointsExpected;
            Data = data;
            Data2 = data2;
            if (args.Length > 0) publishView = args[0];
            if (args.Length > 1) queryFromTime = args[1];
            if (args.Length > 2) queryToTime = args[2];
            if (args.Length > 3) changesSinceTime = args[3];
            if (args.Length > 4) asAtTime = args[4];
        }

        /// <summary>
        /// Checks whether dt has already been UTCified
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>0 if not, an integer with an index to the UTC if there</returns>
        private int isDateTimeInUTC(string dt)
        {
            //return (dt.Substring(dt.Length-3,3).Equals(":"));
            string sPattern = "[-\\+]\\d{2}:\\d{2}";
            int j = 0;
            while (System.Text.RegularExpressions.Regex.IsMatch(dt.Substring(j + 1), sPattern) && j < dt.Length)
            {
                j++;
            }
            return j;
        }
        private void convertToUTC(ref string dt)
        {
            if (!String.IsNullOrEmpty(dt) && !dt.EndsWith(addUTCOffset(Suite.locUTCOff_hrs)))
            {
                int UTCPos = isDateTimeInUTC(dt);
                if (UTCPos != 0)
                    dt = dt.Remove(UTCPos);
                dt += addUTCOffset(Suite.locUTCOff_hrs);

            }
        }
        
        protected override void  RunGetTimeSeriesDataTest()
        {
            convertToUTC(ref queryFromTime);
            convertToUTC(ref queryToTime);
            convertToUTC(ref changesSinceTime);
            convertToUTC(ref asAtTime);

            try
            {
                GetTimeSeriesDataCustomTest_test();
            }
            catch (Exception exception)
            {
                ResultStream("Threw exception");
                LoggerStream("Threw exception: " + exception.Message);
            }
        }

        protected string addUTCOffset(float UTC)
        {
            string result = "";
            if (UTC > 0) result += "+";
            else result += "-";
            result += Math.Abs(UTC).ToString("#00") + ":00";
            return result;
        }
        
        protected override GetTimeSeriesDataQuery getQuery(string asAt)
        {
            GetTimeSeriesDataQuery query = base.getQuery(asAt);
            query._publishView = publishView ?? "Public";
            query._queryFromTime = queryFromTime;
            query._queryToTime = queryToTime;
            query._changesSinceTime = changesSinceTime;
            query._asAtTime = asAt;
            
            return query;
        }

        protected GetTimeSeriesDataQuery getQuery()
        {
            return getQuery(asAtTime);
        }

        public void GetTimeSeriesDataCustomTest_test()
        {
            System.Diagnostics.Trace.WriteLine(Name);
            LoggerStream(Name);
            LoggerStream(String.Format(
                "Params: \nPublish View: {0} \nqueryFromTime: {1} \nqueryToTime: {2} \nchangesSinceTime: {3} \nasAtTime: {4} \nData2: {5}",
                publishView ?? "null", queryFromTime ?? "null", queryToTime ?? "null", changesSinceTime ?? "null", asAtTime ?? "null", Data2 ?? "null"));

            string input = "";

            initializeTimeSeries(Data);

            if (!String.IsNullOrEmpty(Data2))
            {
                appendData(Encoding.ASCII.GetBytes(Data2));
                asAtTime = halfTime.ToString(@"yyyy-MM-ddTHH:mm:ss.fff") + addUTCOffset(Suite.locUTCOff_hrs);
            }

            GetTimeSeriesDataQuery query = getQuery();
            input = getTimeSeriesData(query);
            System.Diagnostics.Trace.WriteLine("Input: " + input);

            string[] actual = stringParse(input, 4);
            char cNumPtsReturned = actual[0][0];
            int NumPtsReturned = (int)Char.GetNumericValue(cNumPtsReturned);

            if (NumPtsReturned == NumPtsExpected)
            {
                ResultStream("pass");
                LoggerStream(String.Format("Pass - returned {0} points", NumPtsExpected));
            }
            else
            {
                ResultStream("FAIL");
                LoggerStream(String.Format("FAIL - returned {0} points, expected {1} points", NumPtsReturned, NumPtsExpected));
            }
        }

        public override void initializeTimeSeries(string appendDataString)
        {
            // some tests require time-stamps, other don't. The ones that do use this method, others don't.
            createTS = createNewTS();
            createTime = Suite.ADSclient.GetCurrentServerTime();// DateTime.Now;      // changesSince = full TS
            createTime = createTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets createTime to the location's timezone

            append = appendData(Encoding.ASCII.GetBytes(appendDataString));

            Thread.Sleep(2000);

            halfTime = Suite.ADSclient.GetCurrentServerTime(); //DateTime.Now;        // asAt = 1st half TS; changesSince = 2nd half TS
            halfTime = halfTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets halfTime to the location's timezone
        }

        private int NumPtsExpected;
        private string Data;
        private string Data2;
        private string publishView, queryFromTime, queryToTime, changesSinceTime, asAtTime;
    }

    public class GetLocationsTest : PublishTestMethod
    {
        protected const string ExpectedHeader =
            @"LOCATIONID,LOCATIONFOLDERID,LASTMODIFIED,LOCATIONNAME,DESCRIPTION,IDENTIFIER,LOCATIONTYPEID,LATITUDE,LONGITUDE,SRID,ELEVATIONUNITS,ELEVATION,UTCOFFSET,TIMEZONE";

        public GetLocationsTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetLocations_test();
        }

        protected void RunBaseTest()
        {
            base.RunTest();
        }

        protected void GetLocations_test()
        {
            LocationList testLocations = new LocationList(Suite);
            LocationDTO testLocationDTO = testLocations.createLocation();

            List<string> filterStrings = new List<string>();
            filterStrings.Add("IDENTIFIER=" + testLocationDTO.Identifier);
            string locationData = string.Empty;
            try
            {
                foreach (string filterString in filterStrings)
                {
                    using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                    {
                        locationData = Suite.APSclient.GetLocations(filterString);
                    }
                }
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }

            ResultStream("Pass.");
        }
    }

    public class GetLocationsByFolderIdTest : GetLocationsTest
    {
        protected const uint identifierIndex = 5;

        public GetLocationsByFolderIdTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunBaseTest();
            GetLocationsByFolderId_test();
        }

        private void GetLocationsByFolderId_test()
        {
            string failureMessage = string.Empty;
            LocationList testLocations = new LocationList(Suite);
            LocationDTO testLocationDTO = testLocations.createLocation();

            try
            {
                long locationId = (long)testLocationDTO.LocationId;

                string folderFilter = Suite.ADSclient.GetLocationFolders();
                string[] deliminators = new string[] { " ", "<", ">" };
                string[] seperatedFolderFilters = folderFilter.Split(deliminators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string filter in seperatedFolderFilters)
                {
                    failureMessage += getLocationByIdentifier(filter);
                }

                if (failureMessage.Length > 0)
                {
                    LoggerStream("GetLocationsByFolderId failed: " + failureMessage);
                    ResultStream("FAIL");
                }
                else
                {
                    ResultStream("pass");
                    LoggerStream("Pass. Location created:" + testLocationDTO.LocationName);
                }

            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }

        protected const string key = "LocationFolderID=";
        protected string getLocationByIdentifier(string filter)
        {
            string failureMessage = string.Empty;

            if (filter.StartsWith(key))
            {
                string id = filter.Substring(key.Length);
                char[] trimCharacters = new char[] { '\"', '\\' };
                id = id.Trim(trimCharacters);

                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    string locations = Suite.APSclient.GetLocationsByFolderId(Convert.ToInt64(id), null);
                    csvData unfilteredLocations = new csvData(locations);
                    failureMessage = unfilteredLocations.checkHeaders(ExpectedHeader.Split(','));
                    if (failureMessage != string.Empty)
                    {
                        return failureMessage;
                    }

                    if(unfilteredLocations._tableValues.Count == 0 )
                    {
                        return string.Empty;
                    }

                    List<String> entryData = unfilteredLocations._tableValues[0];
                    uint identifierIndex = unfilteredLocations._headers["IDENTIFIER"];
                    
                    if (entryData.Count <= identifierIndex)
                    {
                        return "Unexpected row data returned: " + entryData.ToString();
                    }

                    string chosenIdentifier = entryData[(int)identifierIndex].Trim();
                    string filterString = "IDENTIFIER=" + chosenIdentifier;
                    locations = Suite.APSclient.GetLocationsByFolderId(Convert.ToInt64(id), filterString);
                    
                    csvData filteredLocations = new csvData(locations);
                    failureMessage = filteredLocations.checkHeaders(ExpectedHeader.Split(','));
                    
                    if (failureMessage != string.Empty)
                    {
                        return failureMessage;
                    }

                    string unexpectedLocations = string.Empty;
                    filteredLocations._tableValues.ForEach(delegate(List<string> location)
                    {
                        if (location[(int)identifierIndex].Trim() != chosenIdentifier)
                        {
                            unexpectedLocations += location.ToString() + "\n";
                        }
                    });

                    if (unexpectedLocations != string.Empty)
                    {
                        failureMessage = "Unexpected location identifier returned by GetLocationsByFolderId with Identifier filter.\nLocation data: ";
                        failureMessage += unexpectedLocations;
                    }
                }
            }
            return failureMessage;
        }
    }

    class GetRatingTableTest : PublishTestMethod
    {
        public GetRatingTableTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }
        
        static string[] expectedHeaders = {"LocationId","InParameter","OutParameter","NumTables","NumPeriods","NumPoints"};
        
        public static string checkRatingTableCsv(csvData ratingTableCsv)
        {
            return ratingTableCsv.checkHeaders(expectedHeaders);
        }

        public override void RunTest()
        {
            base.RunTest();
            testPublishServiceAPI(
                delegate()
                {
                    string ratingTableData = Suite.APSclient.GetRatingTable(Suite.tsLoc, null, null);
                    csvData ratingTableCsv = new csvData(ratingTableData);
                    
                    if(ratingTableCsv._headers.Count == 0)
                    {
                        Suite.AppendToLog("Skipped: No rating table at location");
                        return "Skipped.";
                    }
                    else
                    {
                        return checkRatingTableCsv(ratingTableCsv);
                    }
                }
            );
        }
    }

    class GetRatingTableExtensionTest : PublishTestMethod
    {
        public GetRatingTableExtensionTest(string name, TestSuite suite, string label)
            : base(name, suite)
        {
            _ratingCurveLabel = label;
        }

        protected publishServiceMethodDelegate getRatingTableExtensionDelegate(string label)
        {
            return delegate()
                {
                    string ratingTableData = Suite.APSclient.GetRatingTableExtension(Suite.tsLoc, null, null, label);
                    csvData ratingTableCsv = new csvData(ratingTableData);

                    if (ratingTableCsv._headers.Count == 0)
                    {
                        Suite.AppendToLog("Skipped: No rating table at location");
                        return "Skipped.";
                    }
                    else
                    {
                        return GetRatingTableTest.checkRatingTableCsv(ratingTableCsv);
                    }
                };
        }

        string[] expectedHeaders = { "LocationId", "InParameter", "OutParameter", "NumTables", "NumPeriods", "NumPoints" };
        public override void RunTest()
        {
            base.RunTest();
            string ratingTableData = tryInvokePublishServiceMethod(
                delegate()
                {
                    return Suite.APSclient.GetRatingTable(Suite.tsLoc, null, null);
                });
            
            csvData ratingTableCsv = new csvData(ratingTableData);
            if (ratingTableCsv._headers.Count == 0)
            {
                testPublishServiceAPI(getRatingTableExtensionDelegate(null));
            }
            else
            {
                testPublishServiceAPI(getRatingTableExtensionDelegate(_ratingCurveLabel));
            }
        }

        private string _ratingCurveLabel = string.Empty;
    }
    struct TemplateItem
    {
        public TemplateItem(string reportId, string label, string type)
        {
            _reportId = "";
            _label = "";
            uint typeInt = Convert.ToUInt32(type.Trim());
            
            if (typeInt == 0)
            {
                _type = reportType.Template;
            }
            else if (typeInt == 1)
            {
                _type = reportType.Description;
            }
            else
            {
                throw new Exception("Invalid template type");
            }
            _reportId += reportId;
            _label += label;
        }

        public string _reportId;
        public string _label;
        public enum reportType : uint
        {
            Template = 0,
            Description = 1,
        }
        public reportType _type;
    }

    class TemplateItemList
    {
        public TemplateItemList(string listData)
        {
            if(listData.Length == 0)
            {
                throw new Exception("TemplateItemList: listData empty");
            }
            _templateList = getTemplateList(listData);
        }

        public static List<TemplateItem> getTemplateList(string templateListData)
        {
            List<TemplateItem> list = new List<TemplateItem>();

            csvData templateListCsv = new csvData(templateListData);
            if (templateListCsv._headers.Count == 0)
            {
                return list;
            }
            string headerError = templateListCsv.checkHeaders(expectedHeaders);
            if (headerError != string.Empty)
            {
                throw new Exception(headerError);
            }

            templateListCsv._tableValues.ForEach(delegate(List<string> listEntry)
            {
                list.Add(new TemplateItem(listEntry[0], listEntry[1], listEntry[2]));
            });
            return list;
        }

        static string[] expectedHeaders = { "Id", "Label", "Type (0=Template; 1=Description)" };
        const uint expectedHeaderCount = 3;

        List<TemplateItem> _templateList;
        public List<TemplateItem> getList()
        {
            return _templateList;
        }
    }

    class GetTemplateListTest : PublishTestMethod
    {
        public GetTemplateListTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        string[] expectedHeaders = {"Id","Label","Type"};
        public override void RunTest()
        {
            base.RunTest();
            testPublishServiceAPI(
                delegate()
                {
                    string templateListData = Suite.APSclient.GetTemplateList();
                    try
                    {
                        TemplateItemList templateItemList = new TemplateItemList(templateListData);
                        
                        if (templateItemList.getList().Count == 0)
                        {
                            Suite.AppendToLog("Warning: Template CSV in correct format, but no System templates were found at chosen location");
                        }
                        return string.Empty;
                    }
                    catch (Exception ex)
                    {
                        return ex.Message;
                    }
                }
            );
        }
    }

    class GetReportDataTest : PublishTestMethod
    {
        public GetReportDataTest(string name, TestSuite suite, string outputPath)
            : base(name, suite)
        {
            _outputPath = outputPath;
        }

        public override void RunTest()
        {
            base.RunTest();
            GetReportData_test();
        }

        private void GetReportData_test()
        {
            initSuite();
            string templateList = base.tryInvokePublishServiceMethod(
                delegate()
                {
                    return Suite.APSclient.GetTemplateList();
                }
            );

            TemplateItemList templateItemList = new TemplateItemList(templateList);
            if(templateItemList.getList().Count == 0)
            {
                ResultStream("Skip");
                LoggerStream("Skipped: No templates");
            }

            string reportId = templateItemList.getList()[0]._reportId;
            int reportType = (int)templateItemList.getList()[0]._type;

            base.testPublishServiceAPI(
                delegate()
                {
                    Stream reportStream = Suite.APSclient.GetReportData(Suite.tsName, reportId, reportType, null, _outputPath);
                    if (reportStream != null && reportStream.CanRead)
                    {
                        return string.Empty;
                    }

                    Suite.AppendToLog("Failed: GetReportData returned empty stream");
                    return "Failed.";
                }
            );
        }

        private string _outputPath;
    }

    #endregion

    #region AcquisitionTests
    public abstract class AcquisitionTestMethod : TestMethod
    {
        public AcquisitionTestMethod(string name, TestSuite suite)
            : base(name, suite)
        {
            ResultStream = suite.AppendToAppResBox;
        }
        public override void RunTest()
        {
            base.RunTest();
        }

        protected OperationContextScope getOperationContextScope()
        {
            return TestSuite.NewContextScope(Suite.AASclient.InnerChannel);
        }

        protected string tryInvokeAcquisitionServiceMethod(acquisitionServiceMethodDelegate method)
        {
            try
            {
                using (getOperationContextScope())
                {
                    return method();
                }
            }
            catch (Exception e)
            {
                LoggerStream("Exception Message: " + e.Message);
                return "Threw Exception";
            }
        }
        
        protected delegate string acquisitionServiceMethodDelegate();
    }

    public partial class CreateTimeSeriesTest : AcquisitionTestMethod
    {
        public CreateTimeSeriesTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            CreateTimeSeries_test();
        }
        private void CreateTimeSeries_test()
        {
            System.Diagnostics.Trace.WriteLine("Create Time Series test");
            Suite.writeLog.Append("CreateTimeSeries: ");

            string result = "";

            createTS = createNewTS();
            if (createTS > 0)
                result = "pass";
            else
                result = "FAIL";

            ResultStream(result);
            LoggerStream(result + " - CreateTimeSeries returned ID: " + createTS);
        }
    }
    public partial class CreateTimeSeries2Test : AcquisitionTestMethod
    {
        public CreateTimeSeries2Test(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            CreateTimeSeries2_test();
        }
        private void CreateTimeSeries2_test()
        {
            string result = "";

            System.Diagnostics.Trace.WriteLine("Create Time Series 2 test");
            Suite.writeLog.Append("CreateTimeSeries2: ");

            createTS = createNewTS2();
            if (createTS > 0)
                result = "pass";
            else
                result = "FAIL";

            ResultStream(result);
            LoggerStream(result + " - CreateTimeSeries2 returned ID: " + createTS);
        }
    }
    public partial class GetTimeSeriesIDTest : AcquisitionTestMethod
    {
        public GetTimeSeriesIDTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetTimeSeriesID_test();
        }
        private void GetTimeSeriesID_test()
        {
            System.Diagnostics.Trace.WriteLine("Get TS ID test");
            Suite.writeLog.Append("GetTimeSeriesID: ");

            createTS = createNewTS();

            string result = "";

            try
            {
                long getID = -1;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    getID = Suite.AASclient.GetTimeSeriesID(Suite.tsName);
                }
                System.Diagnostics.Trace.WriteLine("createTS: " + createTS + "\ngetID: " + getID);

                if ((getID == createTS) && (getID > 0))
                    result = "pass";
                else
                    result = "FAIL";

                ResultStream(result);
                LoggerStream(result + " - GetTimeSeriesID returned ID: " + getID);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }
    public partial class GetTimeSeriesID2Test : AcquisitionTestMethod
    {
        public GetTimeSeriesID2Test(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetTimeSeriesID2_test();
        }
        private void GetTimeSeriesID2_test()
        {
            System.Diagnostics.Trace.WriteLine("Get TS ID 2 test");
            Suite.writeLog.Append("GetTimeSeriesID2: ");

            createTS = createNewTS();

            string result = "";

            try
            {
                long getID = -1;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    getID = Suite.AASclient.GetTimeSeriesID2(Suite.tsLocLabel, Suite.tsLabel, @"HG");
                }
                System.Diagnostics.Trace.WriteLine("createTS: " + createTS + "\ngetID: " + getID);

                if ((getID == createTS) && (getID > 0))
                    result = "pass";
                else
                    result = "FAIL";

                ResultStream(result);
                LoggerStream(result + " - GetTimeSeriesID2 returned ID: " + getID);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }
    }
    public partial class AppendTimeSeriesFromBytesTest : AcquisitionTestMethod
    {
        public AppendTimeSeriesFromBytesTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            AppendTimeSeriesFromBytes_test();
        }
        private void AppendTimeSeriesFromBytes_test()
        {
            System.Diagnostics.Trace.WriteLine("Append TS from Bytes test");
            Suite.writeLog.Append("AppendTimeSeriesFromBytes: ");

            initSuite();
            string result = "";

            if (append == 3)
                result = "pass";
            else
                result = "FAIL";

            ResultStream(result);
            LoggerStream(result + " - AppendTimeSeriesFromBytes returned: " + append);
        }
    }
    public partial class AppendTimeSeriesFromBytes2Test : AcquisitionTestMethod
    {
        public AppendTimeSeriesFromBytes2Test(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            AppendTimeSeriesFromBytes2_test();
        }
        private void AppendTimeSeriesFromBytes2_test()
        {
            System.Diagnostics.Trace.WriteLine("Append TS from Bytes 2 test");
            Suite.writeLog.Append("AppendTimeSeriesFromBytes2: ");

            initSuite2();

            string result = "";

            if (appendResult != null && appendResult.NumPointsAppended == 3)
                result = "pass";
            else
                result = "FAIL";

            ResultStream(result);
            LoggerStream(result + " - AppendTimeSeriesFromBytes2 returned: " + appendResult.NumPointsAppended + " appended.");
        }
    }
    public partial class DeleteTimeSeriesPointsByTimeRangeTest : AcquisitionTestMethod
    {
        public DeleteTimeSeriesPointsByTimeRangeTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            DeleteTimeSeriesPointsByTimeRange_test();
        }
        private void DeleteTimeSeriesPointsByTimeRange_test()
        {
            System.Diagnostics.Trace.WriteLine("Delete Points by Time-range test");
            Suite.writeLog.Append("DeleteTimeSeriesPointsByTimeRange: ");

            initSuite();
            appendData(ptArray2);
            string result = "";

            DateTime startTime = DateTime.Parse("2010-07-31 01:00:00");
            DateTime endTime = DateTime.Parse("2010-07-31 01:10:00");

            try
            {
                int del = -1;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    del = Suite.AASclient.DeleteTimeSeriesPointsByTimeRange(createTS, startTime, endTime);
                }
                System.Diagnostics.Trace.WriteLine("deleted: " + del);

                if (del == 3)
                    result = "pass";
                else
                    result = "FAIL";

                ResultStream(result);
                LoggerStream(result + " - DeleteTimeSeriesPointsByTimeRange returned: " + del);
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }

        }
    }
    public partial class AppendAndMergeTest : AcquisitionTestMethod
    {
        public AppendAndMergeTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            AppendAndMerge_test();
        }
        private void AppendAndMerge_test()
        {
            Suite.writeLog.Append("AppendAndMerge: "); //ToDo: fix this
            initSuite();
            MessageBox.Show("Please select a folder on the server in which to store the .csv file being appended", "Append and Merge Test");
            using (System.Windows.Forms.FolderBrowserDialog ChooseFolderBox = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (ChooseFolderBox.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string paraMap = "JoshAAMtest.*=JoshAPItest;";
                        string aamDir = ChooseFolderBox.SelectedPath;
                        string aamFile = aamDir + "\\JoshAAMtest.txt";
                        string result = "";

                        using (StreamWriter aamOut = new StreamWriter(aamFile, true))
                        {
                            aamOut.WriteLine(csv2);
                        }

                        int aamRes = -1;
                        using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                        {
                            aamRes = Suite.AASclient.AppendAndMerge(Suite.tsLocLabel, "Append And Merge test", "", paraMap, aamFile);
                        }
                        if (aamRes == 3)
                            result = "pass";
                        else
                            result = "FAIL";

                        ResultStream(result);
                        LoggerStream(result + " - appended " + aamRes + " points.");
                    }
                    catch (Exception e)
                    {
                        ResultStream("Threw Exception");
                        LoggerStream("Exception Message: " + e.Message);
                    }
                }
            }
        }
    }
    public partial class UndoAppendTest : AcquisitionTestMethod
    {
        public UndoAppendTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            UndoAppend_test();
        }
        private void UndoAppend_test()
        {
            System.Diagnostics.Trace.WriteLine("Undo append test");
            Suite.writeLog.Append("UndoAppend: ");

            initSuite2();
            int numPoints = 0;
            try
            {
                if (appendResult.NumPointsAppended == 3)
                {
                    using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                    {
                        numPoints = Suite.AASclient.UndoAppend(appendResult.TsIdentifier, appendResult.AppendToken);
                    }
                    if (numPoints == appendResult.NumPointsAppended)
                    {
                        ResultStream("pass");
                        LoggerStream("pass");
                    }
                    else
                    {
                        ResultStream("FAIL");
                        LoggerStream("FAIL");
                    }
                }
                else
                {
                    LoggerStream("Append failed: " + appendResult.NumPointsAppended + " appended.");
                    ResultStream("FAIL");
                }
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
            LoggerStream(" - UndoAppend returned: " + numPoints + " unappended");
        }
    }
    public partial class DeleteTimeSeriesTest : AcquisitionTestMethod
    {
        public DeleteTimeSeriesTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            DeleteTimeSeries_test();
        }
        private void DeleteTimeSeries_test()
        {
            System.Diagnostics.Trace.WriteLine("Delete time series test.");
            Suite.writeLog.Append("DeleteTimeSeries: ");
            try
            {
                long tsId = createNewTS();
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    Suite.AASclient.DeleteTimeSeries(tsId);//ToDo: Should we throw if an invalid id is specified?
                }
                ResultStream("pass");
                LoggerStream("Pass. Time series deleted. TsId= " + tsId.ToString());
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }
    }
    public class GetAllLocationsTest : AcquisitionTestMethod
    {
        public GetAllLocationsTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        public override void RunTest()
        {
            base.RunTest();
            GetAllLocationsTest_test();
        }

        public void GetAllLocationsTest_test()
        {
            try
            {
                LocationDTO[] allLocations = new LocationDTO[] { };
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    allLocations = Suite.AASclient.GetAllLocations();
                }

                foreach (LocationDTO location in allLocations)
                {
                    if (location.Identifier == Suite.tsLoc)
                    {
                        ResultStream("Pass");
                        return;
                    }
                }
                
                ResultStream("Failed.");
                LoggerStream("Failed. List of LocationDTO's returned by GetAllLocations did not include current location.");
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }
    }

    public class GetLocationTest : AcquisitionTestMethod
    {
        public GetLocationTest(string name, TestSuite suite) : base(name, suite)
        {
        }

        public override void RunTest()
        {
            base.RunTest();
            GetLocation_test();
        }

        protected void runBaseTest()
        {
            base.RunTest();
        }

        protected void GetLocation_test()
        {
            try
            {
                RunGetLocationTest();
                ResultStream("pass");
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }

        public void RunGetLocationTest()
        {
            LocationList testLocations = new LocationList(Suite);
            try
            {
                LocationDTO createdLocation = testLocations.createLocation();
                LocationDTO returnedLocation = GetLocation(createdLocation);
                AssertLocationsAreSame(createdLocation, returnedLocation);
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }

        protected virtual LocationDTO GetLocation(LocationDTO createdLocation)
        {
            LocationDTO locationReturned = null;
            using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
            {
                locationReturned = Suite.AASclient.GetLocation((long)createdLocation.LocationId);
            }
            if (createdLocation.LocationId != locationReturned.LocationId)
            {
                throw new Exception("GetLocation(long LocationId) returned a LocationDTO with mismatched locationId");
            }
            return locationReturned;
        }

        public void AssertLocationsAreSame(LocationDTO newLoc, LocationDTO locRet)
        {
            if(!LocationsAreSame(newLoc, locRet))
            {
                ResultStream("FAIL");
                LoggerStream("Some values retrieved from the database did not match. Location identifier=: " + newLoc.Identifier);
                Suite.ADSclient.SaveLocation(new Location() 
                    { 
                        AQDataID = newLoc.LocationId.Value, 
                        IsDeleted = true 
                    }
                );
           }
        }

        protected bool LocationsAreSame(LocationDTO newLoc, LocationDTO locRet)
        {
            return (newLoc.Identifier == locRet.Identifier) &&
            (newLoc.Latitude == locRet.Latitude) &&
            (newLoc.Longitude == locRet.Longitude) &&
            (newLoc.LocationTypeName.ToUpper() == locRet.LocationTypeName.ToUpper()) &&
            (newLoc.LocationId == locRet.LocationId) &&
            (newLoc.LocationName == locRet.LocationName) &&
            (newLoc.LocationPath == locRet.LocationPath) &&
            (newLoc.UtcOffset == locRet.UtcOffset) &&
            (newLoc.ExtendedAttributes != null && locRet.ExtendedAttributes != null ?
               newLoc.ExtendedAttributes.Count == locRet.ExtendedAttributes.Count : true);
        }
    }

    public class CreateAndGetLocationIdTest : GetLocationTest
    {
        public CreateAndGetLocationIdTest(string name, TestSuite suite) : base(name, suite) { }

        protected override LocationDTO GetLocation(LocationDTO createdLocation)
        {
            long locationIdReturned = -1;
            using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
            {
                locationIdReturned = Suite.AASclient.GetLocationId(createdLocation.Identifier);
            }
            if (locationIdReturned != createdLocation.LocationId)
            {
                throw new Exception("GetLocationId(string Identifier) returned a mismatched locationId");
            }
            return base.GetLocation(createdLocation);
        }
    }

    public class GetLocationIdTest : GetLocationTest
    {
        public GetLocationIdTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.runBaseTest();
            GetLocationId_test();
        }

        private void GetLocationId_test()
        {
            long locationIdReturned = -1;
            try
            {
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    locationIdReturned = Suite.AASclient.GetLocationId(Suite.tsLoc);
                }

                if (locationIdReturned != Suite.tsLocID)
                {
                    ResultStream("Failed");
                    LoggerStream("Failed: GetLocationId(string Identifier) returned unexpected LocationId");
                }
                else
                {
                    ResultStream("Pass");
                    LoggerStream("Pass");
                }
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }
    }

    public partial class AddUpdateDeleteLocationTest : GetLocationTest
    {
        public AddUpdateDeleteLocationTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.runBaseTest();
            AddUpdateDeleteLocation_test();
        }

        private void AddUpdateDeleteLocation_test()
        {
            System.Diagnostics.Trace.WriteLine("Add/Update/Delete location test");
            Suite.writeLog.Append("AddUpdateDeleteLocation: ");

            string locName = "Acquisition's Location For AddUpdateDeleteLocationTest";
            
            try
            {
                //Create and Get:
                LocationList testLocations = new LocationList(Suite);
                LocationDTO createdLocation = testLocations.createLocation();

                //Modify:
                LocationDTO modLoc = createdLocation;
                modLoc.LocationName = locName;

                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    Suite.AASclient.ModifyLocation(modLoc);
                }

                LocationDTO locRetAfterMod = GetLocation(createdLocation);
                long testLocationId = (long)locRetAfterMod.LocationId;

                if (!LocationsAreSame(modLoc, locRetAfterMod))
                {
                    ResultStream("FAIL");
                    LoggerStream("Some modified values were not saved. Location identifier=: " + createdLocation.Identifier);
                    Suite.ADSclient.SaveLocation(new Location() { AQDataID = testLocationId, IsDeleted = true });
                    return;
                }

                //Delete:We'll use aquariusDataService
                Suite.ADSclient.SaveLocation(new Location() { AQDataID = testLocationId, IsDeleted = true });

                LocationDTO nullLoc = null;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    nullLoc = Suite.AASclient.GetLocation(testLocationId);
                }
                if (nullLoc != null)
                {
                    ResultStream("FAIL");
                    LoggerStream("The location was not deleted from the databse.  Location identifier =: " + createdLocation.Identifier);
                    return;
                }
                ResultStream("pass");
            }
            catch (Exception ex)
            {
                ResultStream("Threw Exception");
                LoggerStream("Threw Exception: " + ex.ToString());
            }
        }
    }

    public class FieldVisitTests : AcquisitionTestMethod
    {
        protected FieldVisitTests(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        ~FieldVisitTests()
        {
            _savedFieldVisit.ForEach(delegate(FieldVisit saved)
            {
                deleteSavedFieldVisit(saved);
            });
        }

        protected FieldVisit saveFieldVisit(FieldVisit visit)
        {
            FieldVisit saved = visit;
            try
            {
                using (getOperationContextScope())
                {
                    // save the visit
                    saved = Suite.AASclient.SaveFieldVisit(visit); // The returned object will contain the assigned IDs for each record
                }
            }
            catch (Exception ex)
            {
                LoggerStream("SaveFieldVisit Exception Message: " + ex.Message);
                return visit;
            }

            if (saved != null)
            {
                _savedFieldVisit.Add(saved);
            }

            return saved;
        }

        protected void deleteSavedFieldVisit(FieldVisit visit)
        {
            if(visit == null)
            {
                return;
            }

            _savedFieldVisit.ForEach(delegate(FieldVisit saved)
            {
                if(saved.FieldVisitID == Math.Abs(visit.FieldVisitID))
                {
                    _savedFieldVisit.Remove(saved);
                }
            });

            if (visit.FieldVisitID > 0)
            {
                visit.FieldVisitID *= (-1);
            }
            if (saveFieldVisit(visit) !=  null)
            {
                Suite.AppendToLog("Unable to delete saved FieldVisit. FieldVisitID: " + visit.FieldVisitID);
            }
        }
        
        public static FieldVisit createNewFieldVisit(long locationId)
        {
            FieldVisit visit = new FieldVisit();
            visit.LocationID = locationId;
            visit.FieldVisitID = 0; // Set to 0 to create a new field visit
            visit.StartDate = new DateTime(2011, 07, 08, 12, 00, 00); // start time is required
            visit.EndDate = new DateTime(2011, 07, 08, 13, 00, 00); // end time is optional
            visit.Party = ""; // optional - supports multiple lines
            visit.Remarks = ""; // optional - supports multiple lines
            // Create a new measurement object to contain observations
            FieldVisitMeasurement measurement = new FieldVisitMeasurement();
            measurement.MeasurementID = 0; // set to 0 to create new measurement
            measurement.FieldVisitID = visit.FieldVisitID; // must match visit id
            // set the "magic string" that tells the generic field visit plug in to display this measurement
            measurement.MeasurementType = "AqFvtPlugin_MeasurementActivity";
            measurement.MeasurementTime = visit.StartDate;
            measurement.MeasurementEndTime = visit.EndDate; // End Time is optional

            // create stage measurement
            FieldVisitResult stage = new FieldVisitResult();
            stage.MeasurementID = measurement.MeasurementID; // must match measurement id
            stage.ResultID = 0; // set to 0 to create new result record
            stage.StartTime = visit.StartDate;
            stage.ParameterID = "HG"; // Must match a ParameterID found on the Parameters tab of AQUARIUS Manager
            stage.UnitID = "m"; // Must match a UnitID found on the Units tab of AQUARIUS Manager
            stage.ObservedResult = 5.0;

            // create discharge measurement
            FieldVisitResult discharge = new FieldVisitResult();
            discharge.MeasurementID = measurement.MeasurementID;
            discharge.ResultID = 0;
            discharge.StartTime = visit.StartDate;
            discharge.ParameterID = "QR";
            discharge.UnitID = "m^3/s";
            discharge.ObservedResult = 35.0;

            // set measurement results
            measurement.Results = new FieldVisitResult[] { stage, discharge };
            visit.Measurements = new FieldVisitMeasurement[] { measurement };

            return visit;
        }

        protected List<FieldVisit> _savedFieldVisit = new List<FieldVisit>();
    }

    public class SaveFieldVisitTest : FieldVisitTests
    {
        public SaveFieldVisitTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        public override void RunTest()
        {
            base.RunTest();
            SaveFieldVisit_test();
        }

        protected void checkSavedFieldVisit(FieldVisit saved)
        {
            if (saved == null)
            {
                ResultStream("FAIL");
                LoggerStream("Null returned. FieldVisit deleted.");
            }
            else if (saved.FieldVisitID <= 0)
            {
                ResultStream("FAIL");
                LoggerStream("Invalid FieldVisitID found returned");
            }
            else
            {
                ResultStream("Pass");
                LoggerStream("Pass");
            }
        }

        private void SaveFieldVisit_test()
        {
            FieldVisit visit = createNewFieldVisit(Suite.tsLocID);
            FieldVisit saved = saveFieldVisit(visit);
            checkSavedFieldVisit(saved);
        }
    }

    public class GetFieldVisitsByLocationTest : FieldVisitTests
    {
        public GetFieldVisitsByLocationTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        public override void RunTest()
        {
            base.RunTest();
            GetFieldVisitsByLocation_test();
        }

        public static string checkRetrievedFieldVisit(FieldVisit[] visits, FieldVisit saved)
        {
            bool found = false;
            foreach (FieldVisit retrieved in visits)
            {
                if (retrieved.LocationID != saved.LocationID)
                {
                    return "GetLocationsByFolderId returned a FieldVisit object from an unexpected Location";
                }

                if (retrieved.FieldVisitID == saved.FieldVisitID)
                {
                    found = true;
                }
            }

            if (!found)
            {
                return "GetFieldVisitsByLocation: unable to retrieve saved location at " + saved.LocationID;
            }
            else
            {
                return string.Empty;
            }
        }

        protected virtual string getSavedFieldVisit(FieldVisit saved)
        {
            return base.tryInvokeAcquisitionServiceMethod(delegate()
            {
                FieldVisit[] visits = Suite.AASclient.GetFieldVisitsByLocation(saved.LocationID);
                return GetFieldVisitsByLocationTest.checkRetrievedFieldVisit(visits, saved);
            });
        }

        private void GetFieldVisitsByLocation_test()
        {
            FieldVisit visit = createNewFieldVisit(Suite.tsLocID);
            FieldVisit saved = saveFieldVisit(visit);

            string failureMessage = getSavedFieldVisit(saved);

            if (failureMessage.Length > 0)
            {
               ResultStream("FAIL");
               LoggerStream("GetFieldVisitsByLocation failed: " + failureMessage);
            }
            else
            {
                ResultStream("Pass");
                LoggerStream("Pass");
            }
        }
    }
    
    public class GetFieldVisitsByLocationAndDateTest : GetFieldVisitsByLocationTest
    {
        public GetFieldVisitsByLocationAndDateTest(string name, TestSuite suite)
            : base(name, suite)
        {
        }

        protected override string getSavedFieldVisit(FieldVisit saved)
        {
            return base.tryInvokeAcquisitionServiceMethod(delegate()
            {
                FieldVisit[] visits = Suite.AASclient.GetFieldVisitsByLocationAndDate(saved.LocationID, saved.StartDate);
                return GetFieldVisitsByLocationTest.checkRetrievedFieldVisit(visits, saved);
            });
        }
    }

    #endregion

    public class PerformanceTest
    {
        public PerformanceTest() { }

        public string getDuration()
        {
            return String.Format(interim.TotalSeconds.ToString("#0.000"));
        }

        public void Start()
        {
            start = DateTime.Now;
        }

        public void Stop()
        {
            stop = DateTime.Now;
            interim = stop - start;
        }

        private DateTime start;
        private DateTime stop;
        private TimeSpan interim;
    }
}