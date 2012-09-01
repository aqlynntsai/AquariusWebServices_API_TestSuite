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
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Linq.Expressions;
using API_TestSuite_GUI.AASreference;
using API_TestSuite_GUI.ADSreference;
using API_TestSuite_GUI.APSreference;
using Tests;

namespace API_TestSuite_GUI
{
    public class apiParamArgGrid : DataGridView
    {
        public apiParamArgGrid()
        {
            AllowUserToAddRows = false;
            ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            paramName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            argument = new System.Windows.Forms.DataGridViewTextBoxColumn();

            paramName.HeaderText = "Parameter";
            paramName.Name = "paramName";
            paramName.ReadOnly = true;
            // 
            // argument
            // 
            argument.HeaderText = "Argument";
            argument.Name = "argument";
            argument.ReadOnly = false;
            // 

            Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.paramName,
            this.argument});
            Location = new System.Drawing.Point(14, 153);
            Name = "apiParamsDataGridView";
            RowHeadersVisible = false;
        }

        public void reset()
        {
            Rows.Clear();
        }

        public void addParameters(ParameterInfo paramInfo)
        {
            foreach (DataGridViewRow row in Rows)
            {
                if((string)row.Cells["paramName"].Value == paramInfo.Name.ToString())
                {
                    return;
                }
            }

            DataGridViewRow paramInfoRow = new DataGridViewRow();
            int rowNumber = Rows.Add(paramInfoRow);
            Rows[rowNumber].Cells["paramName"].Value = paramInfo.Name.ToString();
        }

        protected bool validateArgs(DataGridViewRow row)
        {
            if(string.IsNullOrEmpty((string)row.Cells["Argument"].Value))
            {
                return false;
            }

            // TODO: Check against param type. required or not.            
            return true;
        }

        public object[] getUserInputArguments()
        {
            List<object> arguments = new List<object>();
            foreach (DataGridViewRow row in Rows)
            {
                if (!validateArgs(row))
                {
                    MessageBox.Show("Empty user input Field. Passing null to API");
                    arguments.Add(null);
                }
                else
                {
                    arguments.Add(row.Cells["Argument"].Value);
                }
            }
            return arguments.ToArray();
        }

        private System.Windows.Forms.DataGridViewTextBoxColumn paramName;
        private System.Windows.Forms.DataGridViewTextBoxColumn argument;
    }

    public partial class TestSuite : Form
    {
        Delegate getSelectedMethodDelegate()
        {
            if (treeView1.SelectedNode.Parent == null)
            {
                return null;
            }

            ManualEntryTests chosenTest;
            if (testDictionary.TryGetValue(treeView1.SelectedNode.Parent, out chosenTest))
            {
                return chosenTest.getNodeDelegate(treeView1.SelectedNode);
            }
            return null;
        }

        OperationContextScope getCurrentContext()
        {
            ManualEntryTests chosenTest;
            if (treeView1.SelectedNode == null)
            {
                return null;
            }
            if (testDictionary.TryGetValue(treeView1.SelectedNode.Parent, out chosenTest))
            {
                return chosenTest.getContext();
            }
            return null;
        }

        void treeView1_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            if (currentSelected == e.Node)
            {
                return;
            }
            
            currentSelected = e.Node;
            Delegate selected = getSelectedMethodDelegate();
            if (selected == null)
            {
                return;
            }

            showParameterInfo(selected.Method);
            invokeAPIButton.Enabled = true;
        }
        
        private void invokeAPIButton_Click(object sender, EventArgs e)
        {
            Delegate d = getSelectedMethodDelegate();
            if (d == null)
            {
                return;
            }

            object[] args = ((apiParamArgGrid)apiParamsDataGridView).getUserInputArguments();
            List<Type> argTypes = ManualEntryTests.getParameterTypes(d.Method);

            List<object> argList = new List<object>();
            if(args.Length != argTypes.Count)
            {
                throw new Exception("API parameter mismatch");
            }

            int index = 0;
            foreach (object arg in args)
            {
                try
                {
                    object c = Convert.ChangeType(arg, argTypes[index]);
                    argList.Add(c);
                }
                catch
                {
                    argList.Add(arg);
                }
           }
            
            try
            {
                using (getCurrentContext())
                {
                    object returnedObj = d.DynamicInvoke(argList.ToArray());
                    if (returnedObj == null)
                    {
                        if (d.Method.ReturnType != typeof(void))
                        {
                            textBoxApiReturn.Text = "Exception: API returned null";
                        }
                        else
                        {
                            textBoxApiReturn.Text = "method call completed successfully.";
                        }
                    }
                    else
                    {
                        textBoxApiReturn.Text = returnedObj.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                string exceptionMessage = String.Format("Exception: {0}. \n", ex.Message);
                if (ex.InnerException != null)
                {
                    exceptionMessage += String.Format("Web Service API Inner Exception: {0}. \n", ex.InnerException.Message);
                }
                
                textBoxApiReturn.Text = exceptionMessage;
            }
        }

        void initializeTestTree()
        {
            TreeNode root = new TreeNode();
            root.Name = "root";
            root.Text = "Aquarius Data Services";
            this.treeView1.Nodes.Add(root);
        }

        void populateManualTestMethods()
        {
            int acquisitionRootId = this.treeView1.Nodes["root"].Nodes.Add(new TreeNode("AQAcquisitionServiceClient"));
            int publishRootId = this.treeView1.Nodes["root"].Nodes.Add(new TreeNode("AQPublishServiceClient"));
            
            TreeNode acquisitionRootNode = treeView1.Nodes["root"].Nodes[acquisitionRootId];
            TreeNode publishRootNode = treeView1.Nodes["root"].Nodes[publishRootId];

            ManualEntryTests acquisitionApiTests = new ManualEntryTests(acquisitionRootNode, AASclient);
            acquisitionApiTests.populateMethods();
            testDictionary.Add(acquisitionRootNode, acquisitionApiTests);

            ManualEntryTests publishApiTests = new ManualEntryTests(publishRootNode, APSclient);
            publishApiTests.populateMethods();
            testDictionary.Add(publishRootNode, publishApiTests);

            this.treeView1.ExpandAll();
        }

        void resetMethodInfoDisplay()
        {
            apiParamsDataGridView.reset();
            labelApiName.Text = "Method: ";
            textBoxApiReturn.Text = "";
        }

        void showParameterInfo(MethodInfo method)
        {
            resetMethodInfoDisplay();
            
            if (method == null)
            {
                return;
            }

            StringBuilder apiSignature = new StringBuilder();
            apiSignature.Append(string.Format("{0} ", method.ReturnType.Name));
            apiSignature.Append(method.Name);
            apiSignature.Append("(");

            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo paramInfo in parameters)
            {
                apiSignature.Append(string.Format("{0} {1} ", paramInfo.ParameterType, paramInfo.Name));
                if (paramInfo.ParameterType == typeof(System.Runtime.Serialization.DataContractAttribute))
                {
                    MemberInfo[] members = paramInfo.ParameterType.GetMembers();
                    foreach (MemberInfo member in members)
                    {
                        object[] os = member.GetCustomAttributes(true);
                        if (os.Length > 0)
                        {
                        }
                    }
                }
                else
                {
                    ((apiParamArgGrid)apiParamsDataGridView).addParameters(paramInfo);
                }
            }
            
            apiSignature.Append(")");
            labelApiName.Text += apiSignature.ToString();

            object[] cas = method.GetCustomAttributes(true);
            {
                foreach (object attribute in cas)
                {
                    labelApiName.Text += string.Format("{0}\n", attribute);
                }
            }
        }

        protected Dictionary<TreeNode, ManualEntryTests> testDictionary = new Dictionary<TreeNode, ManualEntryTests>();
        TreeNode currentSelected = null;
    }

    public class ManualEntryTests
    {
        public ManualEntryTests(TreeNode rootNode, Object service)
        {
            _service = service;
            _rootNode = rootNode;
        }

        public void populateMethods()
        {
            _methodList.Clear();
            if (_service == null)
            {
                return;
            }

            MethodInfo[] methods = _service.GetType().GetMethods(
                BindingFlags.DeclaredOnly |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public);

            foreach (MethodInfo method in methods)
            {
                _methodList.Add(makeMethodDelegate(method, _service));
                TreeNode apiNode = new TreeNode();
                apiNode.Name = method.Name;
                apiNode.Text = method.Name;

                _rootNode.Nodes.Add(apiNode);
            }

            object[] attributes = _service.GetType().GetCustomAttributes(typeof(System.Runtime.Serialization.DataContractAttribute), true);
            foreach (object attribute in attributes)
            {
                ConstructorInfo constructor = attribute.GetType().GetConstructor(Type.EmptyTypes);
            }
        }

        public OperationContextScope getContext()
        {
            IClientChannel channel = null;
            PropertyInfo[] allProperties = _service.GetType().GetProperties();
            foreach (PropertyInfo property in allProperties)
            {
                if (property.Name == "ChannelFactory")
                {
                    foreach (MethodInfo m in property.GetAccessors())
                    {
                        try
                        {
                            object o = m.Invoke(_service, null);
                            var factory = m.Invoke(_service, null);

                            if (factory != null)
                            {
                                MethodInfo creator = (factory.GetType()).GetMethod("CreateFactory");
                                if (creator != null)
                                {
                                    return TestSuite.NewContextScope((IClientChannel)creator.Invoke(factory, null));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex.Message);
                        }
                    }
                }
                
                if (property.Name == "InnerChannel")
                {
                    MethodInfo[] accessors = property.GetAccessors();
                    foreach (MethodInfo m in accessors)
                    {
                        try
                        {
                            object o = m.Invoke(_service, null);
                            channel = (IClientChannel)m.Invoke(_service, null);
                            OperationContextScope nextScope = TestSuite.NewContextScope(channel);
                            return nextScope;

                        }
                        catch(Exception ex)
                        {
                            Debug.Print(ex.InnerException.Message);
                        }
                    }
                }
            }

            MethodInfo method = _service.GetType().GetMethod("CreateChannel");
            if (method != null)
            {
                channel = (IClientChannel)method.Invoke(_service, null);
                OperationContextScope nextScope = TestSuite.NewContextScope(channel);
                return nextScope;
            }
            return null;
        }

        public Delegate getNodeDelegate(TreeNode node)
        {
            return _methodList.Find(
                x => x.Method.Name == node.Name);
        }

        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public delegate TResult Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);

        // .Net 3.5 and below only support generic functions with up to 4 parameters. 
        // Using custom declarations above (up to 10 parameters) until upgrading to .Net 4, which supports up to 17
        static Type makeGenericFunc(Type[] args)
        {
            int numParams = args.Length;

            if (numParams == 6)
            {
                return typeof(Func<,,,,,>).MakeGenericType(args);
            }
            if (numParams == 7)
            {
                return typeof(Func<,,,,,,>).MakeGenericType(args);
            }
            if (numParams == 8)
            {
                return typeof(Func<,,,,,,,>).MakeGenericType(args);
            }
            if (numParams == 9)
            {
                return typeof(Func<,,,,,,,,>).MakeGenericType(args);
            }
            if (numParams == 10)
            {
                return typeof(Func<,,,,,,,,,>).MakeGenericType(args);
            }
            if (numParams == 11)
            {
                return typeof(Func<,,,,,,,,,,>).MakeGenericType(args);
            }

            return null;
        }

        public static List<Type> getParameterTypes(MethodInfo method)
        {
            return new List<Type>(
                method.GetParameters().Select(p => p.ParameterType));
        }

        static Delegate makeMethodDelegate(MethodInfo method, Object target)
        {
            List<Type> args = getParameterTypes(method);
            Type delegateType = null;
            if (method.ReturnType == typeof(void))
            {
                delegateType = Expression.GetActionType(args.ToArray());
            }
            else
            {
                args.Add(method.ReturnType);
                if (args.Count < 6)
                {
                    delegateType = Expression.GetFuncType(args.ToArray());
                }
                else if (args.Count > 10)
                {
                    throw new Exception("Too many parameters in this API");
                }
                else
                {
                    delegateType = makeGenericFunc(args.ToArray());
                }

            }
            Delegate d = Delegate.CreateDelegate(delegateType, target, method);

            return d;
        }

        protected Object _service;
        protected TreeNode _rootNode;
        protected List<Delegate> _methodList = new List<Delegate>();
    }
}
