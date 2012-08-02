using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using API_TestSuite_GUI.AASreference;
using API_TestSuite_GUI.ADSreference;
using API_TestSuite_GUI.APSreference;
using Tests;

namespace API_TestSuite_GUI
{
    public partial class TestSuite : Form
    {
        #region Global Vars

        public AQAcquisitionServiceClient AASclient;
        public AquariusPublishServiceClient APSclient;
        public AquariusDataServiceClient ADSclient;

        bool gotClients;
        public string connectedHost = "";

        string logPath;
        public StringBuilder writeLog = new StringBuilder();

        CustomAppendWatcher customWatcher;
        delegate void customResultUpdateCallback(int pts);
        delegate void customStatUpdateCallback(int type, string txt);
        delegate void customAppendEndCallback();

        ContinuousAppendWatcher contWatcher;
        delegate void contAppendEndCallback();
        delegate void contResultUpdateCallback(int cycle, int total);

        string defLabel = "e.g. MyFirstTS";
        string defLoc = "e.g. 02AB006";
        string defParam = "e.g. HG";

        bool suiteRunning = false;

        //Pair<bool, standardTestMethod>[] _listAppTests;
        //Pair<bool, standardTestMethod>[] _listPubTests;

        PublishTestMethod[] PublishTestList;
        AcquisitionTestMethod[] AcquisitionTestList;
        TestMethod[] FullTestList;

        string[] AcquisitionTestNames;

        string[] appResBoxLines; //Text spaces in the app result box
        string[] appTestBoxLines; //Text spaces in the app test box
        string[] acqTimeBoxLines;
        int acqLineCount; //Number of app tests selected

        static string[] PublishTestNames;

        public string[] pubResBoxLines;
        public string[] pubTestBoxLines;
        string[] pubTimeBoxLines;
        public int pubLineCount;

        public string host;
        public static string authToken;
        public string user = "dummy.supe";
        public string password = "dummy1";
        public string tsParam = "HG";
        public string tsLabel = "JoshAPItest";
        public string tsName;

        public string dbType;
        public string sqlGetLoc = "select TOP 1 LocationID FROM Location WHERE LocationID > 0";
        public string tsLoc;
        public string tsLocLabel;
        public long tsLocID;
        public float locUTCOff_hrs;
        public float servUTCOff_hrs;

        //public DateTime createTime, halfTime;

        //public long createTS; // A.K.A. created TimeSeries' ID
        //public int append;
        //public AppendResult appendResult;

        //public byte[] ptArray1, ptArray2;

                public string csv1 = @"2010-07-31 01:00:00,4.89599990844727,192,10,1,3
        2010-07-31 01:05:00,4.89499998092651,192,10,1,3
        2010-07-31 01:10:00,4.89400005340576,192,10,1,3
        ";

                public string csv2 = @"2010-07-31 01:15:00,4.88899993896484,192,10,1,3
        2010-07-31 01:20:00,4.89400005340576,192,10,1,3
        2010-07-31 01:25:00,4.89300012588501,192,10,1,3
        ";

        #endregion

        public TestSuite()
        {
            try
            {
                string currDir = AppDomain.CurrentDomain.BaseDirectory;
                logPath = currDir + "\\ResultLog";
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                DateTime today = DateTime.Today;
                logPath += "\\" + today.ToString("yyMMdd") + "_testLog.txt";

                InitializeComponent();
                InitializeTestInfo();
                PopulateTestListUI();
                gotClients = false;
            }
            catch (Exception e)
            {
                string message = "Unable to create ResultLog directory - an Exception was thrown: " + e.Message;
                if (e.InnerException != null)
                {
                    message += "\nInnerException: " + e.InnerException.Message;
                }
                MessageBox.Show(message, "Start-up Error");
                writeLog.AppendLine(message);
                writeToLog();
            }
        }

        #region Helper Methods

        

        public void AppendToAppResBox(string msg)
        {
            appResBoxLines[acqLineCount] = msg;
            this.appResultBox.Lines = appResBoxLines;
            acqLineCount++;
        }

        public void AppendToPubResBox(string msg)
        {
            pubResBoxLines[pubLineCount] = msg;
            this.pubResultBox.Lines = pubResBoxLines;
            pubLineCount++;
        }

        public void AppendToLog(string msg)
        {
            writeLog.AppendLine(msg);
            writeToLog();
        }

        public void AppendToAcqTimeBox(string msg, StringBuilder sb)
        {
            sb.Append(msg + ',');
            string[] split = sb.ToString().Split(',');
            this.acqTimeBox.Lines = split;
        }

        public void AppendToPubTimeBox(string msg, StringBuilder sb)
        {
            sb.Append(msg + ',');
            string[] split = sb.ToString().Split(',');
            this.pubTimeBox.Lines = split;
        }

        public void SumTotalTime(StringBuilder sb)
        {
            string[] spl = sb.Remove(sb.Length-1,1).ToString().Split(',');
            double sum = 0;
            foreach (string s in spl)
            {
                sum += Convert.ToDouble( s.Trim() );
            }
            totalTimeBox.Text = sum.ToString("#0.000");
            writeLog.AppendLine("Total time for all tests:" + sum.ToString("#0.000"));
            writeToLog();
        }

        //public void AppendToAcqTimeBox(string msg)
        //{
        //    acqTimeBoxLines[acqLineCount] = msg;
        //    this.acqTimeBox.Lines = acqTimeBoxLines;
        //}

        //public void AppendToPubTimeBox(string msg)
        //{
        //    pubTimeBoxLines[pubLineCount] = msg;
        //    this.pubTimeBox.Lines = pubTimeBoxLines;
        //}

        public void PopulateTestListUI()
        {
            appTestSelectBox.Items.AddRange(AcquisitionTestNames);
            pubTestSelectBox.Items.AddRange(PublishTestNames);
        }

        /// <summary>
        /// Add new test suite methods to this list.
        /// </summary>
        public void InitializeTestInfo()
        {
            string AsWhen, SinceWhen, FromWhen, ToWhen;
            AsWhen = null;
            SinceWhen = null;
            FromWhen = null;
            ToWhen = null;

            AcquisitionTestList = new AcquisitionTestMethod[]
            {
                new CreateTimeSeriesTest("Create Time Series", this),
                new CreateTimeSeries2Test("Create Time Series 2", this),
                new GetTimeSeriesIDTest("Get Time Series ID", this),
                new GetTimeSeriesID2Test("Get Time Series ID 2", this),
                new AppendTimeSeriesFromBytesTest("Append Time Series From Bytes", this),
                new AppendTimeSeriesFromBytes2Test("Append Time Series From Bytes 2", this),
                new DeleteTimeSeriesPointsByTimeRangeTest("Delete Time Series Points By Time Range", this),
                new AppendAndMergeTest("Append and Merge", this),
                new UndoAppendTest("Undo Append", this),
                new DeleteTimeSeriesTest("Delete Time Series", this),
                new AddUpdateDeleteLocationTest("Add Update Delete Location", this),
                new GetLocationsByFolderIdTest("Get Location By FolderId", this),
                new GetLocationTest("Get Location", this),
            };

            PublishTestList = new PublishTestMethod[]
            {
                new GetParameterListTest("Get Parameter List", this),
                new GetFlagListTest("Get Flag List", this),
                new GetGradeListTest("Get Grade List", this),
                new GetApprovalListTest("Get Approval List", this),
                new GetPublishViewListTest("Get Publish View List", this),
                new GetDataSetsListAllTest("Get All Data Sets", this),
                new GetDataSetsListChangesSinceTimeTest("Get Data Sets List (Changes Since Time)", this),
                new GetTimeSeriesDataAllTest("Get All Time Series Data", this),
                new GetTimeSeriesDataAsAtTimeTest("Get Time Series Data As At Time", this, AsWhen, 0),
                new GetTimeSeriesDataChangesSinceTest("Get Time Series Data (Changes Since Time)", this, SinceWhen),
                new GetTimeSeriesDataFromTimeToTimeTest("Get Time Series Data From Time To Time", this, FromWhen, ToWhen),
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 1", this, 2, csv1+csv2, null, "2010-07-31T01:00:00.000", "2010-07-31T01:05:00.000", "2000-01-01T00:00:00.000", null),
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 2", this, 6, csv1+csv2, null, null, null, null, null),
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 3", this, 6, csv1+csv2, null, null, null, null, "1900-01-01T00:00:00.000"), //This returns all points appended apparently...
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 4", this, 0, csv1+csv2, null, null, "2010-07-31T01:05:00.000", "2010-07-31T01:00:00.000"),
                // Note that Test 5 needs to be run within 30 minutes of starting the APITester
                // and the ChangesSince field imprecisely (TODO) uses the time of the client, but the time zone of the server
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 5", this, 0, csv1+csv2, null, null, "2010-07-31T01:00:00.000", "2010-07-31T01:05:00.000", DateTime.Now.AddMinutes(30).ToString(@"yyyy-MM-ddTHH:mm:ss.fff")),
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 6", this, 3, csv1, csv2, null, null, null, null, null),
                new GetTimeSeriesDataCustomTest("Get Time Series Data Custom Test 7", this, 1, csv1, csv2, null, "2010-07-31T01:10:00.000", null, null, null),
                new GetTimeSeriesRawDataAllTest("Get All Time Series Raw Data", this),
                new GetTimeSeriesDataResampledTest("Get All Time Series Data Resampled", this, SinceWhen),
           };
            int i = 0;
            AcquisitionTestNames = new string[AcquisitionTestList.Length];
            foreach (AcquisitionTestMethod atm in AcquisitionTestList)
            {
                AcquisitionTestNames[i++] = atm.Name;
            }
            i = 0;
            PublishTestNames = new string[PublishTestList.Length];
            foreach (PublishTestMethod ptm in PublishTestList)
            {
                PublishTestNames[i++] = ptm.Name;
            }
            FullTestList = new TestMethod[AcquisitionTestList.Length + PublishTestList.Length];
            AcquisitionTestList.CopyTo(FullTestList, 0);
            PublishTestList.CopyTo(FullTestList, AcquisitionTestList.Length - 1);
        }

        public string timeStringFormat(DateTime dt, float utcHrs)
        {
            char sign;
            if (utcHrs >= 0)
                sign = '+';
            else
                sign = '-';
            string result = dt.ToString("yyyy-MM-dd") + "T" + dt.ToString("HH:mm:ss.fff") + sign + Math.Abs(utcHrs).ToString("#00") + ":00";
            return result;
        }

        /// <summary>
        /// Use this method to make sure your online soap call will have the correct soap header.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="aqClientService"></param>
        /// <returns></returns>
        public static OperationContextScope NewContextScope(IContextChannel channel)
        {
            OperationContextScope contextScope = new OperationContextScope(channel);
            try
            {
                MessageHeader runtimeHeader = MessageHeader.CreateHeader("AQAuthToken", "", authToken, false);
                OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
                return contextScope;
            }
            catch (Exception ex)
            {
                contextScope.Dispose();
                throw ex;
            }
        }

        public void initService()
        {
            if (textBoxHost.Text.Length > 0)
            {
                bool gotDBtype = false;
                try
                {
                    if (gotClients)
                    {
                        AASclient.Close();
                        ADSclient.Close();
                        APSclient.Close();
                        gotClients = false;
                    }

                    host = textBoxHost.Text;
                    string aashost = string.Empty;
                    string adshost = string.Empty;
                    string apshost = string.Empty;

                    if (host.Contains(@"localhost"))
                    {
                        if (host.Contains(":8000"))
                        {
                            aashost = @"http://localhost:8000/AQUARIUS/AQAcquisitionService.svc";
                            adshost = @"http://localhost:8000/AQUARIUS/AquariusDataService.svc";
                            apshost = @"http://localhost:8000/AQUARIUS/AquariusPublishService.svc";
                        }
                        else
                        {
                            aashost = @"http://localhost:6995/AQAcquisitionService.svc";
                            adshost = @"http://localhost:6995/AquariusDataService.svc";
                            apshost = @"http://localhost:2498/AquariusPublishService.svc";
                        }
                    }
                    else
                    {
                        aashost = string.Format(@"http://{0}/AQUARIUS/AQAcquisitionService.svc", host);
                        adshost = string.Format(@"http://{0}/AQUARIUS/AquariusDataService.svc", host);
                        apshost = string.Format(@"http://{0}/AQUARIUS/Publish/AquariusPublishService.svc", host);
                    }

                    AASclient = new AQAcquisitionServiceClient("WSHttpBinding_IAQAcquisitionService", aashost);
                    ADSclient = new AquariusDataServiceClient("BasicHttpBinding_AquariusDataService", adshost);
                    APSclient = new AquariusPublishServiceClient("BasicHttpBinding_IAquariusPublishService", apshost);

                    connectedHost = host;
                    gotClients = true;

                    statusInfo.Text = "Acquiring Authorization Token... ";
                    statusInfo.Refresh();
                    authToken = APSclient.GetAuthToken(user, password);//All 3 services share the same token
                    System.Diagnostics.Trace.WriteLine("AuthToken is: " + authToken);
                    statusInfo.Text += "Done";
                    statusInfo.Refresh();

                    dbType = ADSclient.GetDbType();
                    if (dbType.CompareTo("Oracle") == 0)
                        sqlGetLoc = "select LocationID from Location where rownum =1 AND LocationID > 0";
                    else
                        if (dbType.CompareTo("MySql") == 0)
                            sqlGetLoc = "select LocationID from Location limit 1 where LocationID > 0";
                        else
                            sqlGetLoc = "select TOP 1 LocationID FROM Location WHERE LocationID > 0";
                    gotDBtype = true;

                    //ToDo: we may to improve this head setting logic. Token will expire in an hour.
                    //We can move this authen part to before run test.


                    //Add token to data service:
                    {
                        OperationContextScope context = new OperationContextScope(ADSclient.InnerChannel);
                        MessageHeader runtimeHeader = MessageHeader.CreateHeader("AQAuthToken", "", authToken, false);
                        OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
                    }

                    //Add token to publish service context:
                    {
                        OperationContextScope context = new OperationContextScope(APSclient.InnerChannel);
                        MessageHeader runtimeHeader = MessageHeader.CreateHeader("AQAuthToken", "", authToken, false);
                        OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
                    }

                    //Add acquisition service:
                    {
                        OperationContextScope context = new OperationContextScope(AASclient.InnerChannel);
                        MessageHeader runtimeHeader = MessageHeader.CreateHeader("AQAuthToken", "", authToken, false);
                        OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
                    }

                    servUTCOff_hrs = (ADSclient.GetCurrentServerTime() - ADSclient.GetCurrentServerTime().ToUniversalTime()).Hours;

                    //if (connectedHost == "aiapp1") textboxRootLocationFolder.Text = "WSC";
                    //else if (connectedHost == "chilko") textboxRootLocationFolder.Text = "Third Primary Root";


                }
                catch (Exception ex)
                {
                    string message = "";
                    if (gotClients)
                        if (gotDBtype)
                            message += "PublishClient.GetAuthToken() threw exception: " + ex.Message;
                        else
                            message += "DataServiceClient.GetDbType() threw exception: " + ex.Message;
                    else
                        message += "Client Constructor threw exception: " + ex.Message;
                    MessageBox.Show(message, "Aquarius Server Error");
                    writeLog.AppendLine("@" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " there was an error in initService() method - " + message);
                    writeToLog();
                    throw ex;
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid server name/address", "Invalid Entry");
            }
        }

        //public void initSuite()
        //{
        //    // some tests require time-stamps, other don't. The ones that do use this method, others don't.
        //    createTS = createNewTS();
        //    createTime = ADSclient.GetCurrentServerTime();// DateTime.Now;      // changesSince = full TS
        //    createTime = createTime.AddHours(locUTCOff_hrs - servUTCOff_hrs); // Sets createTime to the location's timezone

        //    append = appendData(ptArray1); 

        //    Thread.Sleep(2000);

        //    halfTime = ADSclient.GetCurrentServerTime(); //DateTime.Now;        // asAt = 1st half TS; changesSince = 2nd half TS
        //    halfTime = halfTime.AddHours(locUTCOff_hrs - servUTCOff_hrs); // Sets halfTime to the location's timezone
        //}

        //public void initSuite2()
        //{
        //    // some tests require time-stamps, other don't. The ones that do use this method, others don't.
        //    createTS = createNewTS();
        //    createTime = ADSclient.GetCurrentServerTime();// DateTime.Now;      // changesSince = full TS
        //    createTime = createTime.AddHours(locUTCOff_hrs - servUTCOff_hrs); // Sets createTime to the location's timezone

        //    appendResult = appendData2(ptArray1);

        //    Thread.Sleep(2000);

        //    halfTime = ADSclient.GetCurrentServerTime(); //DateTime.Now;        // asAt = 1st half TS; changesSince = 2nd half TS
        //    halfTime = halfTime.AddHours(locUTCOff_hrs - servUTCOff_hrs); // Sets halfTime to the location's timezone
        //}

        public void runTests()
        {
            PerformanceTest pt = new PerformanceTest();
            StringBuilder acqTimeResults = new StringBuilder();
            StringBuilder pubTimeResults = new StringBuilder();
            string duration;
            
            acqLineCount = 0;
            pubLineCount = 0;
            int i = 0;
            foreach (AcquisitionTestMethod atm in AcquisitionTestList)
            {
                if (atm.IsChecked)
                {
                    statusInfo.Text = AcquisitionTestNames[i] + "... ";
                    statusInfo.Refresh();
                    pt.Start();
                    atm.RunTest();
                    pt.Stop();
                    statusInfo.Text += "Done";
                    statusInfo.Refresh();
                    progBar.PerformStep();
                    duration = pt.getDuration();
                    AppendToAcqTimeBox(duration, acqTimeResults);
                    AppendToLog(String.Format("Time to complete: {0} \n", duration));

                }
                i++;
            }
            i = 0;
            foreach (PublishTestMethod ptm in PublishTestList)
            {
                if (ptm.IsChecked)
                {
                    statusInfo.Text = PublishTestNames[i] + "... ";
                    statusInfo.Refresh();
                    pt.Start();
                    ptm.RunTest();
                    pt.Stop();
                    statusInfo.Text += "Done";
                    statusInfo.Refresh();
                    progBar.PerformStep();
                    duration = pt.getDuration();
                    AppendToPubTimeBox(duration, pubTimeResults);
                    AppendToLog(String.Format("Time to complete: {0} \n", duration));
                }
                i++;
            }
            statusInfo.Text = "Done";
            if( acqTimeResults.Length > 0 || pubTimeResults.Length > 0 ) 
                SumTotalTime(acqTimeResults.Append(pubTimeResults));
        }

        //public long deleteSuiteTS()
        //{
        //    try
        //    {
        //        string parameter = "HG";
        //        string label = "JoshAPItest";
        //        string sql = string.Format("delete from AQAtom_TimeSeries_ where aqparentid_={0} and parametertype_='{1}' and label_='{2}'", tsLocID, parameter, label);
        //        ADSclient.InvokeAopMethod("Execute", new object[] { sql });

        //        System.Diagnostics.Trace.WriteLine("Deleted: " + tsLocID);
        //        return tsLocID;
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Trace.WriteLine(ex.Message);
        //        MessageBox.Show("An Exception was thrown by deleteTS(). Original message: " + ex.Message, "Aquarius Acquisition Service Error");
        //        writeLog.AppendLine("TestSuite method deleteTS() threw: " + ex.Message);
        //        return -1;
        //    }
        //}

        public void writeToLog()
        {
            using (StreamWriter outLog = new StreamWriter(logPath, true))
            {
                outLog.Write(writeLog.ToString());
                writeLog.Length = 0; //Clear
            }
        }

        #endregion

        #region Windows Forms Methods

        private void label4_Click(object sender, EventArgs e)
        {

        }


        private void customRunButton_Click(object sender, EventArgs e)
        {
            bool valid = true;
            byte[] cPtArray = null;
            object[] values = new object[5];

            progBar.Value = 0;
            progBar.Maximum = 2;
            customResultBox.ResetText();

            string invalidMsg = "Please select a valid Label, Location, Parameter and CSV file";
            string invalidCpt = "Invalid Input";

            string cLabel = customTSlabelBox.Text;
            string cLoc = customTSlocBox.Text;
            string cParam = customTSparamBox.Text;

            bool newTS = createTScheckBox.Checked;

            int labLength = cLabel.Length;
            int locLength = cLoc.Length;
            int paramLength = cParam.Length;

            using (Stream inFile = openFileDialog1.OpenFile())
            {
                if (inFile != null)
                {
                    long allByte = inFile.Length;
                    cPtArray = new Byte[allByte];
                    inFile.Read(cPtArray, 0, (int)allByte);
                }
                else
                    valid = false;
            }

            System.Diagnostics.Trace.WriteLine(locLength);

            if ((cLabel.CompareTo(defLabel) == 0) || (cLoc.CompareTo(defLoc) == 0) || (cParam.CompareTo(defParam) == 0) || (labLength == 0) || (locLength == 0) || (paramLength == 0))
                valid = false;

            if (valid)
            {
                customDelButton.Enabled = false;

                values[0] = cLabel;
                values[1] = cLoc;
                values[2] = cParam;
                values[3] = newTS;
                values[4] = cPtArray;
                customWatcher = new CustomAppendWatcher(this, values);
                customWatcher.svcHost = textBoxHost.Text;
                customWatcher.doAppend();
            }
            else
            {
                MessageBox.Show(this, invalidMsg, invalidCpt);
            }
        }

        private void customDelButton_Click(object sender, EventArgs e)
        {
            progBar.Value = 0;
            progBar.Maximum = 1;
            customWatcher.undoAppend();
            customDelButton.Enabled = false;
            customResultBox.ResetText();
        }

        private void contStartButton_Click(object sender, EventArgs e)
        {
            float[] appVals = new float[11];
            bool[] usedVals = new bool[7];
            bool valid = true;

            contResBox.Enabled = true;
            contTotalBox.Enabled = true;
            progBar.Value = 0;

            string invalidMsg = "Please enter valid Label, Location, Parameter and Append Settings";
            string invalidCpt = "Invalid Input";

            valid = float.TryParse(ptIntBox.Text, out appVals[0]);

            if (ptValRadio.Checked) valid = float.TryParse(ptValBox.Text, out appVals[1]);
            usedVals[0] = ptValRadio.Checked;

            if (contQcheck.Checked) valid = float.TryParse(contQbox.Text, out appVals[2]);
            usedVals[1] = contQcheck.Checked;

            if (contInterpCheck.Checked) valid = float.TryParse(contInterpBox.Text, out appVals[3]);
            usedVals[2] = contInterpCheck.Checked;

            if (contApprovCheck.Checked) valid = float.TryParse(contApprovBox.Text, out appVals[4]);
            usedVals[3] = contApprovCheck.Checked;

            if (contFlagCheck.Checked) valid = float.TryParse(contFlagBox.Text, out appVals[5]);
            usedVals[4] = contFlagCheck.Checked;

            valid = float.TryParse(appNumBox.Text, out appVals[6]);
            valid = float.TryParse(appIntBox.Text, out appVals[7]);

            if (ptIncCheckbox.Checked) valid = float.TryParse(ptIncBox.Text, out appVals[8]);
            usedVals[5] = ptIncCheckbox.Checked;

            if (stopTimeCheckbox.Checked)
            {
                valid = float.TryParse(stopTimeBox.Text, out appVals[9]);
                if (stopParamBox.SelectedIndex == -1) valid = false;
                else appVals[10] = (float)stopParamBox.SelectedIndex;
            }
            usedVals[6] = stopTimeCheckbox.Checked;

            string cLabel = contLabelBox.Text;
            string cLoc = contLocBox.Text;
            string cParam = contParamBox.Text;

            int labLength = cLabel.Length;
            int locLength = cLoc.Length;
            int paramLength = cParam.Length;

            System.Diagnostics.Trace.WriteLine(locLength);

            if ((cLabel.CompareTo(defLabel) == 0) || (cLoc.CompareTo(defLoc) == 0) || (cParam.CompareTo(defParam) == 0) || (labLength == 0) || (locLength == 0) || (paramLength == 0))
                valid = false;

            if (valid)
            {
                contStartButton.Enabled = false;
                contDelButton.Enabled = false;
                contStopButton.Enabled = true;
                contWatcher = new ContinuousAppendWatcher(this, cLabel, cLoc, cParam, appVals, usedVals);
                contWatcher.svcHost = textBoxHost.Text;
                contWatcher.newTS = contTScheckbox.Checked;
                contWatcher.Start();
            }
            else
            {
                MessageBox.Show(this, invalidMsg, invalidCpt);
            }

            writeToLog();
        }

        private void contStopButton_Click(object sender, EventArgs e)
        {
            contResBox.Enabled = false;
            contTotalBox.Enabled = false;
            contWatcher.Stop();
        }

        private void contDelButton_Click(object sender, EventArgs e)
        {
            contWatcher.Clean();
            contDelButton.Enabled = false;
            contResBox.ResetText();
            contTotalBox.ResetText();
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region bak
            int testListInd = 0;
            int selectedItems = ((CheckedListBox)sender).CheckedIndices.Count;
            //if (allTestSelected) selectedItems++;

            if (sender.Equals(appTestSelectBox))
            {
                appTestBoxLines = new string[selectedItems];
                for (int i = 0; i < AcquisitionTestList.Length; i++)
                {
                    //_listAppTests[i].First = false; //selectedAppTests[i] = false;
                    AcquisitionTestList[i].UnCheck();
                    System.Diagnostics.Trace.WriteLine("selectedTest F index: " + i);
                }

                System.Diagnostics.Trace.WriteLine("selected items: " + selectedItems);

                for (int j = 0; j < selectedItems; j++)
                {
                    int k = appTestSelectBox.CheckedIndices[j];

                    AcquisitionTestList[k].Check();
                    appTestBoxLines[testListInd] = AcquisitionTestNames[k];
                    testListInd++;
                }
                appResultBox.ResetText();
                appTestBox.Lines = appTestBoxLines;
            }
            else
            {
                pubTestBoxLines = new string[selectedItems];

                for (int i = 0; i < PublishTestList.Length; i++)
                {
                    PublishTestList[i].UnCheck();
                    System.Diagnostics.Trace.WriteLine("selectedTest F index: " + i);
                }

                System.Diagnostics.Trace.WriteLine("selected items: " + selectedItems);

                for (int j = 0; j < selectedItems; j++)
                {
                    int k = pubTestSelectBox.CheckedIndices[j];

                    PublishTestList[k].Check();
                    pubTestBoxLines[testListInd] = PublishTestNames[k];
                    testListInd++;
                }
                pubResultBox.ResetText();
                pubTestBox.Lines = pubTestBoxLines;
            }
            #endregion
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                int numTests = appTestSelectBox.Items.Count;

                appTestBox.Lines = AcquisitionTestNames;
                pubTestBox.Lines = PublishTestNames;

                for (int i = 0; i < numTests; i++)
                {
                    appTestSelectBox.SetItemChecked(i, true);
                    AcquisitionTestList[i].Check();
                }
                numTests = pubTestSelectBox.Items.Count;
                for (int i = 0; i < numTests; i++)
                {
                    pubTestSelectBox.SetItemChecked(i, true);
                    PublishTestList[i].Check();

                }

            }
            else
            {
                int numTests = appTestSelectBox.Items.Count;

                appTestBox.ResetText();
                pubTestBox.ResetText();

                for (int i = 0; i < numTests; i++)
                {
                    appTestSelectBox.SetItemChecked(i, false);
                    AcquisitionTestList[i].UnCheck();
                }
                numTests = pubTestSelectBox.Items.Count;
                for (int i = 0; i < numTests; i++)
                {
                    pubTestSelectBox.SetItemChecked(i, false);
                    PublishTestList[i].UnCheck();
                }
            }

        }

        private void testTabBox_Deselected(Object sender, TabControlEventArgs e)
        {
            statusInfo.Text = "Idle";
            progBar.Value = 0;
        }

        private void fileBrowseButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (Stream open = openFileDialog1.OpenFile())
                    {
                        if (open != null)
                        {
                            string fileName = openFileDialog1.SafeFileName;
                            int valStart = fileName.LastIndexOf('.');
                            string ext = fileName.Substring(valStart);
                            if ((ext == ".txt") || (ext == ".csv"))
                            {
                                selectedFileBox.Text = openFileDialog1.FileName;
                                if (gotClients) customRunButton.Enabled = true;
                            }
                            else
                            {
                                customRunButton.Enabled = false;
                                MessageBox.Show("Please choose a file with valid extension type", "Invalid File Type");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
            openFileDialog1.Dispose();
        }

        private void openLogButton_Click(object sender, EventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\ResultLog");
        }

        private void ptIncCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            ptIncBox.Enabled = !ptIncBox.Enabled;
        }

        private void stopTimeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            stopTimeBox.Enabled = !stopTimeBox.Enabled;
            stopParamBox.Enabled = !stopParamBox.Enabled;
        }

        private void ptValRadio_CheckedChanged(object sender, EventArgs e)
        {
            ptValBox.Enabled = ptValRadio.Checked;

            if (!ptValRadio.Checked)
                contTScheckbox.Checked = false;
        }

        private void contTScheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (contTScheckbox.Checked)
            {
                ptValRadio.Checked = true;
                ptMeanRadio.Checked = false;
            }
        }

        private void contQcheck_CheckedChanged(object sender, EventArgs e)
        {
            contQbox.Enabled = contQcheck.Checked;
        }

        private void contInterCheck_CheckedChanged(object sender, EventArgs e)
        {
            contInterpBox.Enabled = contInterpCheck.Checked;
        }

        private void contApprovCheck_CheckedChanged(object sender, EventArgs e)
        {
            contApprovBox.Enabled = contApprovCheck.Checked;
        }

        private void contFlagCheck_CheckedChanged(object sender, EventArgs e)
        {
            contFlagBox.Enabled = contFlagCheck.Checked;
        }

        private void srvSet_Click(object sender, EventArgs e)
        {
            PwBox srvLogin = new PwBox();

            if (srvLogin.ShowDialog() == DialogResult.OK)
            {
                user = srvLogin.getUsr;
                password = srvLogin.getPswd;

                try
                {
                    initService();

                    startTestButton.Enabled = true;
                    contStartButton.Enabled = true;
                    lstLocations.Items.Clear();
                    try
                    {
                        if (chkGetAllLocations.Checked )
                        {
                            LocationDTO[] locationList = AASclient.GetAllLocations();
                            for (int j = 0; j < locationList.Length; j++)
                            {
                                string[] items = { locationList[j].LocationId.ToString(), locationList[j].Identifier, locationList[j].LocationName };
                                ListViewItem lvItem = new ListViewItem(items);
                                lstLocations.Items.Add(lvItem);
                            }
                            string allLocationsName = locationList[0].LocationPath;
                            allLocationsName = allLocationsName.Split('.')[0];
                            if (allLocationsName.ToString() != "")
                                textboxRootLocationFolder.Text = allLocationsName;
                        }
                    }
                    catch (Exception ex)
                    { MessageBox.Show("Unable to populate Locations list, so disabling.\n" + 
                        "Exception is: " + ex.Message + "\nIf it's a timeout error, you may try changing the recommended settings in app.config under WSHttpBinding_IAQAcquisitionService"); 
                    
                    }
                    tsLocID = -99;
                    tsLocID = Convert.ToInt64(ADSclient.InvokeAopMethod("ExecuteScalar", new object[] { sqlGetLoc }));
                    if (txtLocationID.Text != "") tsLocID = Int64.Parse(txtLocationID.Text);
                    else
                    {
                        txtLocationID.Text = tsLocID.ToString();
                    }
                    tsLoc = ADSclient.GetLocationIdentifier(tsLocID);
                    txtLocation.Text = tsLoc;

                    LocationDTO currLocDTO = AASclient.GetLocation(tsLocID);
                    locUTCOff_hrs = currLocDTO.UtcOffset;

                    MessageBox.Show("Connected successfuly to " + connectedHost + "!");

                    srvSet.Enabled = false;

                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.InnerException.Message);
                        error += "\nInnerException: " + ex.InnerException.Message;
                    }
                    string cause;
                    if (tsLocID == -99)
                    {
                        cause = "Set Server - InvokeAopMethod(<get top location>) threw Exception: ";
                        writeLog.AppendLine(cause + error);
                    }
                    else
                    {
                        cause = "Set Server - GetLocationIdentifier() threw Exception: ";
                        writeLog.AppendLine(cause + error);
                    }

                    MessageBox.Show(cause + error, "Aquarius Data Service Exception");
                    writeToLog();
                }
            }
            srvLogin.Dispose();
        }

        private void textBoxHost_TextChanged(object sender, EventArgs e)
        {
            if (textBoxHost.Text != connectedHost)
            {
                if (contWatcher != null)
                {
                    if (!contWatcher.isRunning() && !suiteRunning) { srvSet.Enabled = true; }
                }
                else
                    if (!suiteRunning) srvSet.Enabled = true;
            }
            else
                srvSet.Enabled = false;
            txtLocationID.Clear();
        }

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (gotClients)
            {
                AASclient.Close();
                ADSclient.Close();
                APSclient.Close();
                writeToLog();
            }

            contWatcher = null;
            customWatcher = null;
        }

        #endregion

        #region Suite Methods
        
        public LocationDTO CreateLocationDTO(string identifier, string locName)
        {
            //Create a new location instance:

            string locTypeName = "Hydrology Station";
            
            //Uncomment the following for MWS
            //string locTypeName = "Telemetered Hydrology Station";
            //Uncomment the following for USGS
            //string locTypeName = "Land (LA)";

            //ToDo: we can add attributes if the custom tables are implemented:
            Dictionary<string, object> attrDic = null;
            //E.g.:
            //Dictionary<string, object> attrDic = new Dictionary<string, object>();
            //attrDic["LoggerType"] = "Sonic";
            //attrDic["Number2_1Single"] = 2.3;
            //Location_Extension table:
            //attrDic["Contact"] = "Bill Chen";
            //attrDic["LoggerNumber"] = 21;
            //attrDic["INTNOTNULL"] = 1;

            float lat = -19.67F;
            float lon = 55.07F;
            float utcOffset = locUTCOff_hrs;

            string locPath = textboxRootLocationFolder.Text; //Default: "All Locations"

            LocationDTO newLoc = new LocationDTO()
            {
                Identifier = identifier,
                LocationName = locName,
                LocationTypeName = locTypeName,
                ExtendedAttributes = attrDic,
                Latitude = lat,
                Longitude = lon,
                UtcOffset = utcOffset,
                LocationPath = locPath
            };
            return newLoc;
        }


        #endregion

        #region Custom Methods

        public void customResultUpdate(int pts)
        {
            if (customResultBox.InvokeRequired)
            {
                customResultUpdateCallback call = new customResultUpdateCallback(customResultUpdate);
                this.Invoke(call, new object[] { pts });
            }
            else
            {
                customResultBox.Text = "" + pts;
            }
        }

        public void customStatUpdate(int type, string txt)
        {
            if ((statusInfo.InvokeRequired) || (progBar.InvokeRequired))
            {
                customStatUpdateCallback call = new customStatUpdateCallback(customStatUpdate);
                this.Invoke(call, new object[] { type, txt });
            }
            else
            {
                if (type == 0)
                    statusInfo.Text = txt;
                else
                {
                    statusInfo.Text += txt;
                    progBar.PerformStep();
                }

                statusInfo.Refresh();
            }
        }

        public void customAppendEnd()
        {
            if (customDelButton.InvokeRequired)
            {
                customAppendEndCallback call = new customAppendEndCallback(customAppendEnd);
                this.Invoke(call, new object[] { });
            }
            else
            {
                customDelButton.Enabled = true;
            }
        }

        public void contResultUpdate(int cycle, int total)
        {
            if ((contResBox.InvokeRequired) || (contTotalBox.InvokeRequired))
            {
                contResultUpdateCallback call = new contResultUpdateCallback(contResultUpdate);
                this.Invoke(call, new object[] { cycle, total });
            }
            else
            {
                contResBox.Text = "" + cycle;
                contTotalBox.Text = "" + total;
            }
        }

        public void contAppendEnd()
        {
            if ((contStopButton.InvokeRequired) || (contDelButton.InvokeRequired) || (contStartButton.InvokeRequired))
            {
                contAppendEndCallback call = new contAppendEndCallback(contAppendEnd);
                this.Invoke(call, new object[] { });
            }
            else
            {
                contStopButton.Enabled = false;
                if (contWatcher.ptCount > 0)
                    contDelButton.Enabled = true;
                contStartButton.Enabled = true;
            }
        }

        #endregion


        private void txtLocationID_TextChanged(object sender, EventArgs e)
        {
            if (contWatcher != null)
            {
                if (!contWatcher.isRunning() && !suiteRunning) { srvSet.Enabled = true; }
            }
            else
                if (!suiteRunning) srvSet.Enabled = true;
        }

        private void lstLocations_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstLocations.SelectedItems.Count > 0)
            {
                tsLocID = Int64.Parse(lstLocations.SelectedItems[0].Text);
                tsLoc = ADSclient.GetLocationIdentifier(tsLocID);
                txtLocationID.Text = lstLocations.SelectedItems[0].Text;
                ListView.SelectedListViewItemCollection selectedLVItemCollection = lstLocations.SelectedItems;
                txtLocation.Text = selectedLVItemCollection[0].SubItems[1].Text;

                try
                {
                    LocationDTO currLocDTO = AASclient.GetLocation(tsLocID);
                    locUTCOff_hrs = currLocDTO.UtcOffset;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                //srvSet.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
                        
        }

        private void btnSetLocation_Click(object sender, EventArgs e)
        {
            if (txtLocationID.Text != "") tsLocID = Int64.Parse(txtLocationID.Text);
            else
            {
                txtLocationID.Text = tsLocID.ToString();
            }
            if (ADSclient == null)
                this.srvSet_Click(this, e);
           
            tsLoc = ADSclient.GetLocationIdentifier(tsLocID);
            txtLocation.Text = tsLoc;
            LocationDTO currLocDTO;
            try
            {
                currLocDTO = AASclient.GetLocation(tsLocID);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (currLocDTO == null)
            {
                startTestButton.Enabled = false;
                throw new Exception("No location with that ID found");
            }
            locUTCOff_hrs = currLocDTO.UtcOffset;

            srvSet.Enabled = false;
        }

        private void startTestButton_Click(object sender, EventArgs e)
        {
            DateTime runTime = DateTime.Now;
            suiteRunning = true;

            writeLog.AppendLine(string.Format("---| New Test Suite Run on Server {0} |--- @{1}", host, runTime.ToString("dd-MM-yyyy HH:mm:ss")));
            writeLog.AppendLine(string.Format("Using location: {0}", tsLoc));

            //ptArray1 = Encoding.ASCII.GetBytes(csv1);
            //ptArray2 = Encoding.ASCII.GetBytes(csv2);

            try
            {
                int numTestsRun = appTestBox.Lines.Length + pubTestBox.Lines.Length;
                progBar.Maximum = numTestsRun + 1;
                progBar.PerformStep();

                Location myLoc = ADSclient.GetLocationByIdentifier(tsLoc);
                if (myLoc == null)
                    throw new Exception("No location with that ID found");
                tsLocLabel = myLoc.Label;
                tsName = tsParam + " " + tsLabel + " @" + tsLoc;

                appResBoxLines = new string[appTestBox.Lines.Length];
                pubResBoxLines = new string[pubTestBox.Lines.Length];

                runTests();
                suiteTabControl.SelectedTab = testResTab;
                suiteRunning = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                writeLog.AppendLine("Exception Thrown during Test Suite start-up: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Trace.WriteLine(ex.InnerException.Message);
                    writeLog.AppendLine("InnerException: " + ex.InnerException.Message);
                }
                MessageBox.Show(ex.Message, "Aquarius Webservice Error");
            }

            suiteRunning = false;
            writeToLog();

        }

        private void TestSuite_Load(object sender, EventArgs e)
        {
            textBoxHost.Text = Properties.Settings.Default.Server;
            chkGetAllLocations.Checked = Properties.Settings.Default.GetAllLocations;
        }

        private void TestSuite_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Server = textBoxHost.Text;
            Properties.Settings.Default.GetAllLocations = chkGetAllLocations.Checked;
            Properties.Settings.Default.Save();
        }


    }

    public class CustomAppendWatcher
    {
        AQAcquisitionServiceClient AASclient;
        AquariusPublishServiceClient APSclient;
        AquariusDataServiceClient ADSclient;

        string logPath;
        StringBuilder customLog = new StringBuilder();

        TestSuite myParent;
        public string svcHost;
        string cLabel, cLoc, cParam, cName;
        int cResult;
        byte[] cPtArray = null;
        long tsID = 0;
        long locID;
        bool newTS;

        public CustomAppendWatcher(TestSuite parent, object[] tsParam)
        {
            myParent = parent;

            AASclient = myParent.AASclient;
            APSclient = myParent.APSclient;
            ADSclient = myParent.ADSclient;

            logPath = AppDomain.CurrentDomain.BaseDirectory + "\\ResultLog";
            logPath += "\\" + DateTime.Today.ToString("yyMMdd") + "_testLog.txt";

            cLabel = (string)tsParam[0];
            cLoc = (string)tsParam[1];
            cParam = (string)tsParam[2];
            newTS = (bool)tsParam[3];
            cPtArray = (byte[])tsParam[4];

            cName = cParam + " " + cLabel + " @" + cLoc;
        }

        public void doAppend()
        {
            customLog.AppendLine(string.Format("---| New Custom Append Run on Server {0} |--- @{1}", svcHost, DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")));
            try
            {
                locID = ADSclient.GetLocationId(cLoc);

                if (newTS)
                {
                    myParent.customStatUpdate(0, "Creating new Time Series... ");
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        tsID = AASclient.CreateTimeSeries2(locID, cLabel, "Custom Append Test", null, "HG", 0, "m", 0);
                    }
                    myParent.customStatUpdate(1, "Done");
                }
                else
                {
                    myParent.customStatUpdate(0, "Getting Time Series ID... ");
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        tsID = AASclient.GetTimeSeriesID(cName);
                    }
                    myParent.customStatUpdate(1, "Done");
                }

                if (tsID > 0)
                {
                    myParent.customStatUpdate(0, "Appending Points... ");
                    cResult = -1;
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        cResult = AASclient.AppendTimeSeriesFromBytes(tsID, cPtArray, "API Test Suite", "Custom Append");
                    }
                    myParent.customStatUpdate(1, "Done");
                    myParent.customResultUpdate(cResult);
                    customLog.AppendLine("Appended " + cResult + " points to " + cName);
                    myParent.customAppendEnd();
                }
                else
                {
                    if (newTS)
                    {
                        string msg = "AqSvc Error: Time Series was not created.";
                        MessageBox.Show(msg, "Aquarius Acquisition Service Error");
                        customLog.AppendLine(msg);
                    }
                    else
                    {
                        string msg = "AqSvc Error: Time Series was not found. Ensure entered values are valid.";
                        MessageBox.Show(msg, "Aquarius Acquisition Service Error");
                        customLog.AppendLine(msg);
                        customLog.AppendLine(string.Format("Used Label: {0}; Location: {1}; Parameter: {2}", cLabel, cLoc, cParam));
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = "Custom Append Test threw Exception: " + ex.Message;
                MessageBox.Show(msg, "Aquarius Webservice Error");
                customLog.AppendLine(msg);
            }

            using (StreamWriter outLog = new StreamWriter(logPath, true))
            {
                outLog.WriteLine(customLog.ToString());
                customLog.Length = 0;
            }
        }

        public void undoAppend()
        {
            if (newTS)
            {
                myParent.customStatUpdate(0, "Deleting New TS... ");
                string sql = string.Format("delete from AQAtom_TimeSeries_ where aqparentid_={0} and parametertype_='{1}' and label_='{2}'", locID, cParam, cLabel);
                int val = (int)ADSclient.InvokeAopMethod("Execute", new object[] { sql });
                myParent.customStatUpdate(1, "Done");
                customLog.AppendLine("Deleted new TS: " + cName);
            }
            else
            {
                myParent.customStatUpdate(0, "Deleting appended points... ");
                char[] splitChar = { ',', '\n' };
                string stringIn = Encoding.ASCII.GetString(cPtArray);
                string[] inArray = stringIn.Split(splitChar);
                string dateFind = "^\\d{4}-\\d{2}-\\d{2}";
                string startString = "";
                string endString = "";
                DateTime startTime, endTime;
                bool dateFound = false;

                while (!dateFound)
                {
                    for (int i = 0; i < inArray.Length; i++)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(inArray[inArray.Length - 1 - i], dateFind))
                        {
                            endString = inArray[inArray.Length - 1 - i];
                            dateFound = true;
                            break;
                        }
                    }
                }

                startString = inArray[0];
                startTime = DateTime.Parse(startString);
                endTime = DateTime.Parse(endString);

                int del = -1;
                using (TestSuite.NewContextScope(AASclient.InnerChannel))
                {
                    del = AASclient.DeleteTimeSeriesPointsByTimeRange(tsID, startTime, endTime);
                }

                myParent.customStatUpdate(1, "Done");
                customLog.AppendLine("Removed " + del + " points from: " + startTime + " to: " + endTime + " from TS: " + cName);
            }

            using (StreamWriter outLog = new StreamWriter(logPath, true))
            {
                outLog.WriteLine(customLog.ToString());
                customLog.Length = 0;
            }
        }
    }

    public class ContinuousAppendWatcher
    {
        AQAcquisitionServiceClient AASclient;
        AquariusPublishServiceClient APSclient;
        AquariusDataServiceClient ADSclient;

        char[] splitChar = { ',', '\n' };
        string logPath;

        TestSuite myParent;
        StringBuilder contLog = new StringBuilder();

        public bool newTS;
        bool useQ, useInterp, useApprov, useMean, useFlag, incPts, timeToStop, running;
        public string svcHost;
        string tsLabel, tsLoc, tsParam, tsName, tsStart, message, appendPts;

        public int ptCount;
        float appValue, ptValue, ptQ, ptInterp, ptApprov, ptFlag, appNum, appInterv, ptInc, stopVal, stopParam;
        long tsID;

        DateTime tsStartTime, tsAppendTime, runTime, stopTime, nowTime;
        TimeSpan ptInterval, appInterval;

        System.Timers.Timer appendTimer = new System.Timers.Timer();

        public ContinuousAppendWatcher(TestSuite parent, string label, string location, string param, float[] appVals, bool[] usedVals)
        {
            myParent = parent;

            AASclient = myParent.AASclient;
            APSclient = myParent.APSclient;
            ADSclient = myParent.ADSclient;

            running = false;
            ptCount = 0;

            logPath = AppDomain.CurrentDomain.BaseDirectory + "\\ResultLog";
            logPath += "\\" + DateTime.Today.ToString("yyMMdd") + "_testLog.txt";

            tsLabel = label;
            tsLoc = location;
            tsParam = param;
            tsName = tsParam + " " + tsLabel + " @" + tsLoc;

            useMean = !usedVals[0];
            if (!useMean) ptValue = appVals[1];

            useQ = usedVals[1];
            if (usedVals[1]) ptQ = appVals[2];

            useInterp = usedVals[2];
            if (useInterp) ptInterp = appVals[3];

            useApprov = usedVals[3];
            if (useApprov) ptApprov = appVals[4];

            useFlag = usedVals[4];
            if (useFlag) ptFlag = appVals[5];

            appNum = appVals[6];
            appInterv = appVals[7];
            appInterval = TimeSpan.FromMinutes(appInterv);

            incPts = usedVals[5];
            if (incPts) ptInc = appVals[8];

            timeToStop = usedVals[6];
            if (timeToStop) stopVal = appVals[9];

            if (appVals[10] == 0) stopParam = 1;
            else stopParam = 60;

            ptInterval = TimeSpan.FromMinutes(appVals[0]);

            appendTimer.Elapsed += new ElapsedEventHandler(alarmRing);
            appendTimer.AutoReset = true;
        }

        public void Start()
        {
            running = true;
            contLog.AppendLine(string.Format("---| New Continuous Append Run on Server {0} |--- @{1}", svcHost, DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")));

            try
            {
                if (newTS)
                {
                    tsStartTime = DateTime.Parse("1991-07-31 00:00:00");
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        tsID = AASclient.CreateTimeSeries(tsName);
                    }
                }
                else
                {
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        tsID = AASclient.GetTimeSeriesID(tsName);
                    }

                    if (useMean)
                    {
                        string dataSetList = string.Empty;
                        using (TestSuite.NewContextScope(APSclient.InnerChannel))
                        {
                            dataSetList = APSclient.GetDataSetsList(tsLoc, null);
                        }
                        int valStart = dataSetList.LastIndexOf(tsName);

                        string s1 = dataSetList.Substring(valStart + tsName.Length + 1);
                        string s2 = s1.Trim();
                        string[] result = s2.Split(splitChar);
                        float.TryParse(result[4], out ptValue);
                    }

                    getStartDate();
                }
            }
            catch (Exception e)
            {
                string message = "";
                if (newTS)
                    message += "CreateTimeSeries() threw Exception: " + e.Message;
                else
                {
                    message += "GetTimeSeriesID()";
                    if (useMean)
                        message += " or GetDataSetsList()";
                    message += " threw Exception: " + e.Message;
                }

                MessageBox.Show(message, "Aquarius Webservice Exception");
                contLog.AppendLine(message);

                Stop();
            }

            tsAppendTime = tsStartTime;

            if (running)
            {

                if (tsID > 0)
                {
                    appendTimer.Interval = appInterv * 60000;
                    appendTimer.Start();
                    appendPoints();

                    if (timeToStop)
                    {
                        runTime = DateTime.Now;
                        stopTime = runTime.Add(TimeSpan.FromMinutes(stopVal * stopParam));
                    }
                }
                else
                {
                    if (newTS)
                        message = "Unexpected return: Time Series was not created";
                    else
                        message = "Time Series could not be found";
                    MessageBox.Show(message, "Aquarius Webservice Issue");

                    contLog.AppendLine("Aquarius Webservice Issue - " + message);
                    Stop();
                }
            }
        }

        public void Stop()
        {
            running = false;
            appendTimer.Stop();

            using (StreamWriter outLog = new StreamWriter(logPath, true))
            {
                outLog.WriteLine(contLog.ToString());
                contLog.Length = 0;
            }

            myParent.contAppendEnd();
        }

        public void Clean()
        {
            int val = -1;

            if (!running)
            {
                try
                {
                    if (newTS)
                    {
                        long locID = ADSclient.GetLocationId(tsLoc);
                        string sql = string.Format("delete from AQAtom_TimeSeries_ where aqparentid_={0} and parametertype_='{1}' and label_='{2}'", locID, tsParam, tsLabel);
                        val = (int)ADSclient.InvokeAopMethod("Execute", new object[] { sql });
                    }
                    else
                    {
                        int del = -1;
                        using (TestSuite.NewContextScope(AASclient.InnerChannel))
                        {
                            del = AASclient.DeleteTimeSeriesPointsByTimeRange(tsID, tsStartTime.Add(ptInterval), tsAppendTime);
                        }
                    }
                }
                catch (Exception e)
                {
                    string message = "";
                    if (newTS)
                    {
                        if (val > 0)
                            message += "InvokeAopMethod(<sql delete method>) threw Exception: " + e.Message;
                        else
                            message += "GetLocationID() threw Exception: " + e.Message;
                    }
                    else
                        message += "DeleteTimeSeriesPointsByTimeRange() threw Exception: " + e.Message;
                    MessageBox.Show(message, "Aquarius Webservice Exception");
                    contLog.AppendLine(message);

                    Stop();
                }
            }
        }

        public void appendPoints()
        {
            if (running)
            {
                appendPts = "";
                appValue = ptValue;

                for (int i = 0; i < appNum; i++)
                {
                    incPtDate();
                    if (incPts) appValue = ptValue + (ptInc * i);
                    appendPts += tsAppendTime.ToString("yyyy-MM-dd HH:mm:ss") + ",";
                    appendPts += appValue + ",";
                    if (useFlag) appendPts += ptFlag;
                    appendPts += ",";
                    if (useQ) appendPts += ptQ;
                    appendPts += ",";
                    if (useInterp) appendPts += ptInterp;
                    appendPts += ",";
                    if (useApprov) appendPts += ptApprov;
                    appendPts += ",";
                    appendPts += "Appended Automatically by API Tester";
                    appendPts += "\n";
                }

                try
                {
                    byte[] ptArray = Encoding.ASCII.GetBytes(appendPts);
                    int cycle = -1;
                    using (TestSuite.NewContextScope(AASclient.InnerChannel))
                    {
                        cycle = AASclient.AppendTimeSeriesFromBytes(tsID, ptArray, "API Test Suite", "Continuous Append");
                    }
                    ptCount += cycle;
                    myParent.contResultUpdate(cycle, ptCount);

                    contLog.AppendLine(string.Format("Continuous Append Test appended {0} points at {1}, in total {2} points, to TimeSeries {3}", cycle, DateTime.Now, ptCount, tsName));
                }
                catch (Exception e)
                {
                    string message = "AppendTimeSeriesFromBytes() threw Exception: " + e.Message;
                    MessageBox.Show(message, "Aquarius Acquistion Service Exception");
                    contLog.AppendLine(message);

                    Stop();
                }
            }
        }

        public void getStartDate()
        {
            if (running)
            {
                try
                {
                    string dataSetList = string.Empty;
                    using (TestSuite.NewContextScope(APSclient.InnerChannel))
                    {
                        dataSetList = APSclient.GetDataSetsList(tsLoc, null);
                    }
                    int valStart = dataSetList.LastIndexOf(tsName);

                    string s1 = dataSetList.Substring(valStart + tsName.Length + 1);
                    string s2 = s1.Trim();
                    string[] result = s2.Split(splitChar);
                    tsStart = result[7];

                    tsStart = tsStart.Remove(tsStart.IndexOf('.'));
                    tsStartTime = DateTime.Parse(tsStart);
                }
                catch (Exception e)
                {
                    string message = "GetDataSetsList() threw Exception: " + e.Message;
                    MessageBox.Show(message, "Aquarius Publish Service Exception");
                    contLog.AppendLine(message);

                    Stop();
                }
            }
        }

        private void alarmRing(object sender, ElapsedEventArgs e)
        {
            nowTime = DateTime.Now;
            appendPoints();
            if (timeToStop) if (DateTime.Compare(nowTime.Add(appInterval), stopTime) >= 0) Stop();
        }

        public void incPtDate()
        {
            tsAppendTime = tsAppendTime.Add(ptInterval);
        }

        public bool isRunning()
        {
            return running;
        }
    }
    
}