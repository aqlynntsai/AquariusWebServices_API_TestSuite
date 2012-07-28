using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;

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
            createTime = Suite.ADSclient.GetCurrentServerTime();// changesSince = full TS
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
                    System.Diagnostics.Trace.WriteLine("New TS Created!");
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
        protected long createNewTS2()
        {
            long val = 0;
            string parameter = "HG";
            string label = "JoshAPItest";
            string comments = "JoshAPItest -> done via createTS2";
            string description = "JoshAPItest description";
            int utcOffsetInMin = (int)(Suite.locUTCOff_hrs * 60);
            string unit = "m";
            double maxGaps = 0.0;
            long parentID = deleteSuiteTS();

            try
            {
                if (parentID >= 0)
                {
                    using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                    {
                        val = Suite.AASclient.CreateTimeSeries2(parentID, label, comments, description, parameter, utcOffsetInMin, unit, maxGaps);
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
        protected int appendData(byte[] data)
        {
            int val = 0;
            try
            {
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    val = Suite.AASclient.AppendTimeSeriesFromBytes(createTS, data, "API Test Suite", null);
                }
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

                string[] delims = {"\r\n"};
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

        protected GetTimeSeriesDataTest(string name, TestSuite suite, GetTimeSeriesDataAPI queryAPI)
            : base(name, suite)
        {
            _queryAPI = queryAPI;
        }

        public GetTimeSeriesDataTest(string name, TestSuite suite)
            : this(
                name,
                suite,
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
        protected string[] getExpected(int type)
        {
            string[] result = null;
            switch (type)
            {
                case 0:
                    goto default;
                case 1:
                    result = stringParse(csv1 + csv2, 1); //Both data sets
                    break;
                default:
                    result = stringParse(csv2, 1); //The second data set
                    break;
            }
            return result;
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
        public GetTimeSeriesDataAllTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            GetTimeSeriesData_test();
        }

        private void GetTimeSeriesData_test()
        {
            System.Diagnostics.Trace.WriteLine("Get TS data ALL");
            LoggerStream("GetTimeSeriesData(All): ");

            createTS = createNewTS2();
           // string createdTSName;
            appendData(Encoding.ASCII.GetBytes(csv1 + csv2));

            try
            {
                string input = string.Empty;
                GetTimeSeriesDataQuery query = getQuery(null);
                input = getTimeSeriesData(query);
                evaluateSuccess(stringParse(input, 0), stringParse(csv1 + csv2, 1));  
            }
            catch (Exception e)
            {
                ResultStream("Threw Exception");
                LoggerStream("Exception Message: " + e.Message);
            }
        }

        protected override void evaluateSuccess(string[] actual, string[] expected)
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

    public class GetTimeSeriesDataFromTimeToTimeTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataFromTimeToTimeTest(string name, TestSuite suite, string FromTime, string ToTime)
            : base(name, suite)
        {
            //fromTime = FromTime;
            //toTime = ToTime;

        }
        public override void RunTest()
        {
            base.RunTest();
            GetTimeSeriesData_FromTimeToTime_test(fromTime, toTime, 0); //ToDo: fix this method again to be suitable for polymorphism
        }
        private void GetTimeSeriesData_FromTimeToTime_test(string fromTime, string toTime, int type)
        {
            System.Diagnostics.Trace.WriteLine("Get TS data fromTime toTime, type {0}: ", type.ToString());
            LoggerStream(String.Format("GetTimeSeriesData(fromTime, toTime), type {0}: ", type.ToString())); //Semantic change

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
            catch(Exception e)
            {
                System.Diagnostics.Trace.WriteLine("Unable to retrieve location information - test failed. Exception: {0}", e.Message);
                LoggerStream(String.Format("Unable to retrieve location information - test failed. Exception: {0}", e.Message));
                ResultStream("FAIL");
                return;
            }

            fromTime = String.Format("{0}{1}", "2010-07-31T01:15:00.000", locTimezoneOffset);
            toTime = String.Format("{0}{1}", "2010-07-31T01:25:00.000", locTimezoneOffset);
            
            initSuite();
            appendData(ptArray2); //Possibly could change

            try
            {
                string input = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    input = Suite.APSclient.GetTimeSeriesData(@"HG " + Suite.tsLabel + "@" + Suite.tsLoc, "Public", fromTime, toTime, null, null); //Stays the same
                    LoggerStream("Method Output: " + input);
                }
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

        protected override void evaluateSuccess(string[] actual, string[] expected)
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

        private string fromTime;
        private string toTime;
    }

    public class GetTimeSeriesDataAsAtTimeTest : GetTimeSeriesDataTest
    {
        public GetTimeSeriesDataAsAtTimeTest(string name, TestSuite suite, string AsWhen, int Type)
            : base(name, suite)
        {
            asWhen = AsWhen;
            type = Type;
        }
        public override void RunTest()
        {
            base.RunTest();
            GetTimeSeriesData_asAtTime_test();
        }
        private string asWhen;
        private int type;
        protected void GetTimeSeriesData_asAtTime_test()
        {
            try
            {
                System.Diagnostics.Trace.WriteLine(String.Format("Get TS data AS OF {0}, type {1}", asWhen, type)); //same
                Suite.writeLog.Append(String.Format("GetTimeSeriesData(asAtTime): Time: {0} Type: {1}", asWhen, type));

                initSuite();
                appendData(ptArray2); //may change in future

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
            : base(name, suite)
        {
            fromWhen = FromWhen;
        }
        public override void RunTest()
        {
            base.RunTest();
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
            try
            {
                System.Diagnostics.Trace.WriteLine("Get TS data SINCE, type " + type.ToString() + ": ");
                LoggerStream("GetTimeSeriesData(changesSinceTime), type: " + type.ToString() + ": ");

                initSuite();
                appendData(ptArray2);

                string changesSince = getDate(date, type);

                LoggerStream(String.Format("Changes since parameter: {0}", changesSince));

                string input = string.Empty;
                using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
                {
                    input = Suite.APSclient.GetTimeSeriesData(@"HG " + Suite.tsLabel + "@" + Suite.tsLoc, "Public", null, null, changesSince, null);
                }
                System.Diagnostics.Trace.WriteLine("From APS: " + input);

                string[] actual = stringParse(input, 0);
                string[] expected = getExpected(type);

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
        protected override void evaluateSuccess(string[] actual, string[] expected)
        {
            int passCount = 0;
            bool[] result = new bool[5];
            string allResult = "";

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

        private string fromWhen;
    }


    public class GetTimeSeriesDataCustomTest : GetTimeSeriesDataTest
    {
        /// <summary>
        /// Required params: string name, TestSuite Suite, int NumPtsExpected, string Data1. //// Optional params: string publishView, queryFromTime, queryToTime, changesSinceTime, asAtTime, Data2. //// If Data2 is specified, asAtTime will be ignored and the interim time between appends will be used.
        /// </summary>
        public GetTimeSeriesDataCustomTest(string name, TestSuite suite, int numPointsExpected, string data, params string[] args)
            : base(name, suite)
        {
            NumPtsExpected = numPointsExpected;
            Data = data;
            Data2 = null;
            if (args.Length > 0) publishView = args[0];
            if (args.Length > 1) queryFromTime = args[1];
            if (args.Length > 2) queryToTime = args[2];
            if (args.Length > 3) changesSinceTime = args[3];
            if (args.Length > 4) asAtTime = args[4];
            if (args.Length > 5) Data2 = args[5];
        }

        /// <summary>
        /// Checks whether dt has already been UTCified
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>0 if not, an integer with an index to the UTC if there</returns>
        private int isDateTimeInUTC (string dt)
        {
            //return (dt.Substring(dt.Length-3,3).Equals(":"));
            string sPattern = "[-\\+]\\d{2}:\\d{2}";
            int j=0;
            while (System.Text.RegularExpressions.Regex.IsMatch(dt.Substring(j+1), sPattern) && j< dt.Length){
                j++;
            }
            return j;
        }
        private void convertToUTC(ref string dt)
		{    if (!String.IsNullOrEmpty(dt) && !dt.EndsWith(addUTCOffset(Suite.locUTCOff_hrs)))
			{
                int UTCPos = isDateTimeInUTC(dt);
				if (UTCPos != 0)
					dt = dt.Remove(UTCPos);
                dt += addUTCOffset(Suite.locUTCOff_hrs);

			}
		}
        public override void RunTest()
        {
            base.RunTest();

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

        public void GetTimeSeriesDataCustomTest_test()
        {
            System.Diagnostics.Trace.WriteLine(Name);
            LoggerStream(Name);
            LoggerStream(String.Format(
                "Params: \nPublish View: {0} \nqueryFromTime: {1} \nqueryToTime: {2} \nchangesSinceTime: {3} \nasAtTime: {4} \nData2: {5}",
                publishView ?? "null", queryFromTime ?? "null", queryToTime ?? "null", changesSinceTime ?? "null", asAtTime ?? "null", Data2 ?? "null"));

            string input = "";

            initSuite(Data);

            if (!String.IsNullOrEmpty(Data2))
            {
                appendData(Encoding.ASCII.GetBytes(Data2));
                asAtTime = halfTime.ToString(@"yyyy-MM-ddTHH:mm:ss.fff") + addUTCOffset(Suite.locUTCOff_hrs);
            }

            using (TestSuite.NewContextScope(Suite.APSclient.InnerChannel))
            {
                input = Suite.APSclient.GetTimeSeriesData(@"HG " + Suite.tsLabel + "@" + Suite.tsLoc, publishView ?? "Public", queryFromTime, queryToTime, changesSinceTime, asAtTime);
            }
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

        protected void initSuite(string data)
        {
            // some tests require time-stamps, other don't. The ones that do use this method, others don't.
            createTS = createNewTS();
            createTime = Suite.ADSclient.GetCurrentServerTime();// DateTime.Now;      // changesSince = full TS
            createTime = createTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets createTime to the location's timezone



            append = appendData(Encoding.ASCII.GetBytes(data));

            Thread.Sleep(2000);

            halfTime = Suite.ADSclient.GetCurrentServerTime(); //DateTime.Now;        // asAt = 1st half TS; changesSince = 2nd half TS
            halfTime = halfTime.AddHours(Suite.locUTCOff_hrs - Suite.servUTCOff_hrs); // Sets halfTime to the location's timezone
        }

        private int NumPtsExpected;
        private string Data;
        private string Data2;
        private string publishView, queryFromTime, queryToTime, changesSinceTime, asAtTime;
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
    public partial class AddUpdateDeleteLocationTest : AcquisitionTestMethod
    {
        public AddUpdateDeleteLocationTest(string name, TestSuite suite) : base(name, suite) { }
        public override void RunTest()
        {
            base.RunTest();
            AddUpdateDeleteLocation_test();
        }
        private void AddUpdateDeleteLocation_test()
        {
            System.Diagnostics.Trace.WriteLine("Add/Update/Delete location test");
            Suite.writeLog.Append("AddUpdateDeleteLocation: ");

            //Create:
            string identifier = "Test Suite Location " + DateTime.Now.ToString();
            string locName = "Acquisition's Location For CreateLocation_Test";
            try
            {
                LocationDTO newLoc = Suite.CreateLocationDTO(identifier, locName);
                long locId = -1;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    locId = Suite.AASclient.CreateLocation(newLoc);
                }

                //Get:
                LocationDTO locRet = null;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    locRet = Suite.AASclient.GetLocation(locId);
                }
                if (!AssertLocationsAreSame(newLoc, locId, locRet))
                {
                    ResultStream("FAIL");
                    LoggerStream("Some values retrieved from the database did not match. Location identifier=: " + identifier);
                    Suite.ADSclient.SaveLocation(new Location() { AQDataID = locId, IsDeleted = true });
                    return;
                }

                //Modify:
                string newLocName = "Modified:" + locName;
                LocationDTO modLoc = locRet;
                modLoc.LocationName = newLocName;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    Suite.AASclient.ModifyLocation(modLoc);
                }
                LocationDTO locRetAfterMod = null;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    locRetAfterMod = Suite.AASclient.GetLocation(modLoc.LocationId.Value);
                }
                if (!AssertLocationsAreSame(modLoc, locId, locRetAfterMod))
                {
                    ResultStream("FAIL");
                    LoggerStream("Some modified values were not saved. Location identifier=: " + identifier);
                    Suite.ADSclient.SaveLocation(new Location() { AQDataID = locId, IsDeleted = true });
                    return;
                }

                //Delete:We'll use aquariusDataService
                Suite.ADSclient.SaveLocation(new Location() { AQDataID = locId, IsDeleted = true });

                LocationDTO nullLoc = null;
                using (TestSuite.NewContextScope(Suite.AASclient.InnerChannel))
                {
                    nullLoc = Suite.AASclient.GetLocation(locId);
                }
                if (nullLoc != null)
                {
                    //ResultStream = "FAIL";
                    //LoggerStream("The location was not deleted from the database. Location identifier=: " + identifier);
                    ResultStream("FAIL");
                    LoggerStream("The location was not deleted from the databse.  Location identifier =: " + identifier);
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

        private static bool AssertLocationsAreSame(LocationDTO newLoc, long locId, LocationDTO locRet)
        {
            return (newLoc.Identifier == locRet.Identifier) &&
              (newLoc.Latitude == locRet.Latitude) &&
              (newLoc.Longitude == locRet.Longitude) &&
            (newLoc.LocationTypeName.ToUpper() == locRet.LocationTypeName.ToUpper()) &&
             (locId == locRet.LocationId) &&
            (newLoc.LocationName == locRet.LocationName) &&
            (newLoc.LocationPath == locRet.LocationPath) &&
            (newLoc.UtcOffset == locRet.UtcOffset) &&
            (newLoc.ExtendedAttributes != null && locRet.ExtendedAttributes != null ?
               newLoc.ExtendedAttributes.Count == locRet.ExtendedAttributes.Count : true);
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