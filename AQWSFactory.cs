using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using AquariusDataService;
using Aquarius.Basic;
using Aquarius.Util;

namespace Aquarius.Webclient
{
    public static class AQWSFactory
    {
        private readonly static log4net.ILog log = log4net.LogManager.GetLogger(typeof(AQWSFactory));

        #region New EndpointAddress
        enum AQWSFactoryType
        {
            AQAcquisitionService,
            AquariusDataService,
            IAquariusPublishService
        }
        private static EndpointAddress NewAddress(AQWSFactoryType type, string host)
        {
            EndpointAddress address = null;
            string debugUri = "";
            string releaseUri = "";
            string standardUri = "";
            switch(type)
            {
                case AQWSFactoryType.AQAcquisitionService:
                    debugUri = "/AQAcquisitionService.svc";
                    releaseUri = "/AQUARIUS/AQAcquisitionService.svc";
                    standardUri = "/AQUARIUS/service/AQAcquisitionService.svc";
                    break;
                case AQWSFactoryType.AquariusDataService:
                    debugUri = "/AquariusDataService.svc";
                    releaseUri = "/AQUARIUS/AquariusDataService.svc";
                    standardUri = "/AQUARIUS/service/AquariusDataService.svc";
                    break;
                case AQWSFactoryType.IAquariusPublishService:
                    debugUri = "/AquariusPublishService.svc";
                    releaseUri = "/AQUARIUS/Publish/AquariusPublishService.svc";
                    standardUri = "/AQUARIUS/service/AquariusPublishService.svc";
                    break;
            }
            if (host.StartsWith("http://"))
            {
                address = new EndpointAddress(host);
            }
            else
            {
                if (host.IndexOf(":") > 0)//localhost:6995 
                {
                    if(host.IndexOf(":8000")>0) //localhost:8000
                    {
                        address = new EndpointAddress("http://" + host + standardUri);
                    }
                    else
                    {
                        address = new EndpointAddress("http://" + host + debugUri);
                    }
                }
                else
                {
                    address = new EndpointAddress("http://" + host + releaseUri);
                }
            }
            return address;
        }

        public static EndpointAddress NewAddress_IAQAcquisitionService(string host)
        {
            return NewAddress(AQWSFactoryType.AQAcquisitionService, host);
        }

        public static EndpointAddress NewAddress_AquariusDataService(string host)
        {
            return NewAddress(AQWSFactoryType.AquariusDataService, host);
        }

        public static EndpointAddress NewAddress_IAquariusPublishService(string host)
        {
            return NewAddress(AQWSFactoryType.IAquariusPublishService, host);
        }
        #endregion

        #region New Binding
        public static Binding NewBinding_IAQAcquisitionService()
        {
            WSHttpBinding bind = new WSHttpBinding();
            bind.Name = "WSHttpBinding_IAQAcquisitionService";
            bind.CloseTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_CLOSE_TIMEOUT);
            bind.OpenTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_OPEN_TIMEOUT);
            bind.ReceiveTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_RECV_TIMEOUT);
            bind.SendTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_SEND_TIMEOUT);
            bind.AllowCookies = false;
            bind.BypassProxyOnLocal = false;
            bind.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            bind.MaxBufferPoolSize = 524288;
            bind.MaxReceivedMessageSize = 2147483647;
            bind.MessageEncoding = WSMessageEncoding.Text;
            bind.TextEncoding = Encoding.UTF8;
            bind.UseDefaultWebProxy = true;
            bind.ReaderQuotas.MaxDepth = 32;
            bind.ReaderQuotas.MaxStringContentLength = 2147483647;
            bind.ReaderQuotas.MaxArrayLength = 2147483647;
            bind.ReaderQuotas.MaxBytesPerRead = 4096;
            bind.ReaderQuotas.MaxNameTableCharCount = 16384;
            bind.Security.Mode = SecurityMode.None;
            bind.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            bind.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            bind.Security.Transport.Realm = "";
            bind.Security.Message.ClientCredentialType = MessageCredentialType.Windows;
            bind.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
            return bind;
        }

        public static Binding NewBinding_AquariusDataService()
        {
            BasicHttpBinding bind = new BasicHttpBinding();
            bind.Name = "BasicHttpBinding_AquariusDataService";
            bind.CloseTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_CLOSE_TIMEOUT);
            bind.OpenTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_OPEN_TIMEOUT);
            bind.ReceiveTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_RECV_TIMEOUT);
            bind.SendTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_SEND_TIMEOUT);
            bind.AllowCookies = false;
            bind.BypassProxyOnLocal = false;
            bind.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            bind.MaxBufferSize = 2147483647;
            bind.MaxBufferPoolSize = 524288;
            bind.MaxReceivedMessageSize = 2147483647;
            bind.MessageEncoding = WSMessageEncoding.Text;
            bind.TextEncoding = Encoding.UTF8;
            bind.TransferMode = TransferMode.Buffered;
            bind.UseDefaultWebProxy = true;
            bind.ReaderQuotas.MaxDepth = 32;
            bind.ReaderQuotas.MaxStringContentLength = 2147483647;
            bind.ReaderQuotas.MaxArrayLength = 2147483647;
            bind.ReaderQuotas.MaxBytesPerRead = 4096;
            bind.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            bind.Security.Mode = BasicHttpSecurityMode.None;
            bind.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            bind.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            bind.Security.Transport.Realm = "";
            bind.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            bind.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
            return bind;
        }

        //<binding name="aqBasicHttpBinding" maxReceivedMessageSize="2147483647">
        //  <readerQuotas maxArrayLength="2147483647" maxStringContentLength="2147483647"/>
        //</binding>
        public static Binding NewBinding_IAquariusPublishService()
        {
            BasicHttpBinding bind = new BasicHttpBinding();
            bind.Name = "BasicHttpBinding_IAquariusPublishService";
            bind.CloseTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_CLOSE_TIMEOUT);
            bind.OpenTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_OPEN_TIMEOUT);
            bind.ReceiveTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_RECV_TIMEOUT);
            bind.SendTimeout = TimeSpan.FromSeconds(AQGlobalTokens.WCF_SEND_TIMEOUT);
            bind.AllowCookies = false;
            bind.BypassProxyOnLocal = false;
            bind.HostNameComparisonMode = HostNameComparisonMode.StrongWildcard;
            bind.MaxBufferSize = 2147483647;
            bind.MaxBufferPoolSize = 524288;
            bind.MaxReceivedMessageSize = 2147483647;
            bind.MessageEncoding = WSMessageEncoding.Text;
            bind.TextEncoding = Encoding.UTF8;
            bind.TransferMode = TransferMode.Buffered;
            bind.UseDefaultWebProxy = true;
            bind.ReaderQuotas.MaxDepth = 32;
            bind.ReaderQuotas.MaxStringContentLength = 2147483647;
            bind.ReaderQuotas.MaxArrayLength = 2147483647;
            bind.ReaderQuotas.MaxBytesPerRead = 8192;
            bind.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            bind.Security.Mode = BasicHttpSecurityMode.None;
            bind.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            bind.Security.Transport.ProxyCredentialType = HttpProxyCredentialType.None;
            bind.Security.Transport.Realm = "";
            bind.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            bind.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
            return bind;
        }
        #endregion

        #region New Web Service Client
        public static Aquarius.Webclient.RefAAS.AQAcquisitionServiceClient NewAASClient()
        {
            return NewAASClient(AQAcquisitionService_Url);
        }

        public static Aquarius.Webclient.RefAAS.AQAcquisitionServiceClient NewAASClient(string host)
        {
            if (string.IsNullOrEmpty(AquariusDataService_UserName) || string.IsNullOrEmpty(AquariusDataService_Password))
            {
                return NewAASClient(host, null, null);
            }
            return NewAASClient(host, AquariusDataService_UserName, AquariusDataService_Password);
        }

        public static Aquarius.Webclient.RefAAS.AQAcquisitionServiceClient NewAASClient(string host, string user, string passwd)
        {
            EndpointAddress address = NewAddress_IAQAcquisitionService(host);
            Binding bind = NewBinding_IAQAcquisitionService();
            Aquarius.Webclient.RefAAS.AQAcquisitionServiceClient aasClient = new
                    Aquarius.Webclient.RefAAS.AQAcquisitionServiceClient(
                    bind, address);
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(passwd))
            {
                string authToken = aasClient.GetAuthToken(user, passwd);
                OperationContextScope context = new OperationContextScope(aasClient.InnerChannel);
                //Use token set it to soap header
                MessageHeader runtimeHeader = MessageHeader.CreateHeader(AQGlobalTokens.AQAuthToken_Key, "", authToken, false);
                OperationContext current = OperationContext.Current;
                OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
            }
            return aasClient;
        }

        public static Aquarius.Webclient.RefADS.AquariusDataServiceClient NewADSClient()
        {
            if (string.IsNullOrEmpty(AquariusDataService_UserName) || string.IsNullOrEmpty(AquariusDataService_Password))
            {
                return NewADSClient(AquariusDataService_Url, null, null, false);
            }
            return NewADSClient(AquariusDataService_Url, AquariusDataService_UserName, AquariusDataService_Password, true);
        }

        public static Aquarius.Webclient.RefADS.AquariusDataServiceClient NewADSClient(string host)
        {
            if (string.IsNullOrEmpty(AquariusDataService_UserName) || string.IsNullOrEmpty(AquariusDataService_Password))
            {
                return NewADSClient(AquariusDataService_Url, null, null, false);
            }
            return NewADSClient(host, AquariusDataService_UserName, AquariusDataService_Password, true);
        }

        public static Aquarius.Webclient.RefADS.AquariusDataServiceClient NewADSClient(string host, 
            string user, string passwd, bool login)
        {
            EndpointAddress address = NewAddress_AquariusDataService(host);
            Binding bind = NewBinding_AquariusDataService();
            Aquarius.Webclient.RefADS.AquariusDataServiceClient adsClient = new 
                Aquarius.Webclient.RefADS.AquariusDataServiceClient(bind, address);
            
            if(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(passwd))
            {
                AquariusDataService.UserCredentials cred = new AquariusDataService.UserCredentials();
                cred.UserName = user;
                cred.Password = passwd;

                OperationContextScope scope = new OperationContextScope(adsClient.InnerChannel);
                MessageHeader header
                  = MessageHeader.CreateHeader(
                  typeof(AquariusDataService.UserCredentials).Name,
                  UserCredentials.WS_NAMESPACE,
                  cred,
                  false
                  );
                OperationContext.Current.OutgoingMessageHeaders.Add(header);

                if (login)
                {
                    if (!adsClient.Login(user, passwd))
                    {
                        throw new Exception(string.Format("AquariusDataService.Login('{0}', ...) failed.", user));
                    }
                }
            }
            return adsClient;
        }

        public static Aquarius.Webclient.RefADS.AquariusDataServiceClient NewADSClient(string host,
            string user, string passwd)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(passwd))
            {
                return NewADSClient(AquariusDataService_Url, user, passwd, false);
            }
            return NewADSClient(host, user, passwd, true);
        }

        public static Aquarius.Webclient.RefAPS.AquariusPublishServiceClient NewAPSClient()
        {
            return NewAPSClient(AquariusPublishService_Url);
        }

        public static Aquarius.Webclient.RefAPS.AquariusPublishServiceClient NewAPSClient(string host)
        {
            if (string.IsNullOrEmpty(AquariusDataService_UserName) || string.IsNullOrEmpty(AquariusDataService_Password))
            {
                return NewAPSClient(host, null, null);
            }
            return NewAPSClient(host, AquariusDataService_UserName, AquariusDataService_Password);
        }

        public static Aquarius.Webclient.RefAPS.AquariusPublishServiceClient NewAPSClient(string host,
            string user, string passwd)
        {
            EndpointAddress address = NewAddress_IAquariusPublishService(host);
            Binding bind = NewBinding_IAquariusPublishService();
            Aquarius.Webclient.RefAPS.AquariusPublishServiceClient apsClient = new
                    Aquarius.Webclient.RefAPS.AquariusPublishServiceClient(
                    bind, address);
            if(!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(passwd))
            {
                string authToken = apsClient.GetAuthToken(user, passwd);
                OperationContextScope context = new OperationContextScope(apsClient.InnerChannel);
                //Use token set it to soap header
                MessageHeader runtimeHeader = MessageHeader.CreateHeader(AQGlobalTokens.AQAuthToken_Key, "", authToken, false);
                OperationContext current = OperationContext.Current;
                OperationContext.Current.OutgoingMessageHeaders.Add(runtimeHeader);
            }               
            return apsClient;
        }

#if !NOCLIENTSERVICE
        public static ClientServices.AquariusDataService.svc.LocalClientServiceClient NewLCSClient()
        {
            return NewLCSClient(@"http://localhost:8000/AQUARIUS/Service/AQClientService");
        }
        public static ClientServices.AquariusDataService.svc.LocalClientServiceClient NewLCSClient(string host)
        {
            EndpointAddress address = new EndpointAddress(host);
            System.ServiceModel.Channels.Binding binding = Aquarius.Channels.AQBinding.CreateBinding(Aquarius.Channels.WCFBindType.BasicHttpBinding, true);
            ClientServices.AquariusDataService.svc.LocalClientServiceClient _client = new ClientServices.AquariusDataService.svc.LocalClientServiceClient(binding, address);
            return _client;
        }
#endif
        #endregion

        #region http utillity
        /// <summary>
        /// Deafult timeout is 600000
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public static string HttpGet(string url, string authToken)
        {
            return HttpGet(url, authToken, -1, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authToken"></param>
        /// <param name="timeout">Milliseconds</param>
        /// <returns></returns>
        public static string HttpGet(string url, string authToken, int timeout)
        {
            return HttpGet(url, authToken, timeout, System.Text.Encoding.UTF8);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authToken"></param>
        /// <param name="timeout">Milliseconds</param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpGet(string url, string authToken, int timeout, System.Text.Encoding encoding)
        {
            System.Net.HttpWebRequest request = null;
            System.Net.WebResponse response = null;
            System.IO.Stream responses = null;
            System.IO.StreamReader sr = null;
            try
            {
                request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
				if(timeout>0)
				{
                	request.Timeout = timeout;
                	request.ReadWriteTimeout = timeout;
				}
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers.Add("AQAuthToken", authToken);
                }
                response = request.GetResponse();
                responses = response.GetResponseStream();
                sr = new System.IO.StreamReader(responses, encoding);
                string ret = sr.ReadToEnd();
                return ret;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (sr != null) { try { sr.Close(); } catch { } }
                if (responses != null) { try { responses.Close(); } catch { } }
            }
        }
        #endregion

        public const string AquariusDataService = "AquariusDataService";
        public const string AQAcquisitionService = "AQAcquisitionService";
        public const string AquariusPublishService = "AquariusPublishService";
        public const string AquariusPublishRestService = "AquariusPublishRestService";

        #region All below fields are loaded by InitialFromAppSetting(Settings) methods.
        public const string AQAcquisitionService_Uid = AQAcquisitionService;
        public static string AQAcquisitionService_Url = "http://localhost/AQUARIUS/AQAcquisitionService.svc";
        public static string AQAcquisitionService_Args = " ";

        public const string AquariusPublishService_Uid = AquariusPublishService;
        public static string AquariusPublishService_Url = "http://localhost/AQUARIUS/Publish/AquariusPublishService.svc";
        public static string AquariusPublishService_Args = " ";

        public const string AquariusPublishRestService_Uid = AquariusPublishRestService;
        public static string AquariusPublishRestService_Url = "http://localhost/AQUARIUS/Publish/AquariusPublishRestService.svc";
        public static string AquariusPublishRestService_Args = " ";

        public const string AquariusDataService_Uid = AquariusDataService;
        public static string AquariusDataService_Url = "http://localhost/AQUARIUS/AquariusDataService.svc";
        public static string AquariusDataService_Args = " ";

        public static string AquariusDataService_UserName = "";
        public static string AquariusDataService_Password = "";
        public static string AquariusDataService_Host = "localhost";
        #endregion

        public static void InitialFromAppSetting(IDictionary<string, string> setting)
        {
            if (setting == null) return;
            if (ParametersUtil.ContainsKey("AquariusDataService_Url", setting))
            {
                AquariusDataService_Url = ParametersUtil.GetParam("AquariusDataService_Url", setting, AquariusDataService_Url);
                AquariusDataService_Args = ParametersUtil.GetParam("AquariusDataService_Args", setting, AquariusDataService_Args);
                InitialAquariusDataService(AquariusDataService_Args);
            }
            if (ParametersUtil.ContainsKey("AQAcquisitionService_Url", setting))
            {
                AQAcquisitionService_Url = ParametersUtil.GetParam("AQAcquisitionService_Url", setting, AQAcquisitionService_Url);
                AQAcquisitionService_Args = ParametersUtil.GetParam("AQAcquisitionService_Args", setting, AQAcquisitionService_Args);
            }
            if (ParametersUtil.ContainsKey("AquariusPublishService_Url", setting))
            {
                AquariusPublishService_Url = ParametersUtil.GetParam("AquariusPublishService_Url", setting, AquariusPublishService_Url);
                AquariusPublishService_Args = ParametersUtil.GetParam("AquariusPublishService_Args", setting, AquariusPublishService_Args);
            }
            if (ParametersUtil.ContainsKey("AquariusPublishRestService_Url", setting))
            {
                AquariusPublishRestService_Url = ParametersUtil.GetParam("AquariusPublishRestService_Url", setting, AquariusPublishRestService_Url);
                AquariusPublishRestService_Args = ParametersUtil.GetParam("AquariusPublishRestService_Args", setting, AquariusPublishRestService_Args);
            }
        }

        public static void InitialAquariusDataService(string args)
        {
            IDictionary<string, string> keyValues = new Dictionary<string, string>();
            ArgumentsUtil.RegexParser(args, keyValues);
            AQWSFactory.AquariusDataService_Host = ParametersUtil.GetParam("host", keyValues, AQWSFactory.AquariusDataService_Host);
            if (ParametersUtil.ContainsKey("user", keyValues))
            {
                AQWSFactory.AquariusDataService_UserName = ParametersUtil.GetParam("user", keyValues, AQWSFactory.AquariusDataService_UserName);
                AQWSFactory.AquariusDataService_Password = ParametersUtil.GetParam("pwd", keyValues, AQWSFactory.AquariusDataService_Password);
            }
            else if (ParametersUtil.ContainsKey("svcuser", keyValues))
            {
                AQWSFactory.AquariusDataService_UserName = ParametersUtil.GetParam("svcuser", keyValues, AQWSFactory.AquariusDataService_UserName);
                AQWSFactory.AquariusDataService_Password = ParametersUtil.GetParam("svcpwd", keyValues, AQWSFactory.AquariusDataService_Password);
            }
        }

        public static string Configuration
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("AQWSFactory configuration:");
                sb.AppendFormat("\t{0} : {1}", AquariusPublishService_Uid, AQAcquisitionService_Url);
                sb.AppendLine();
                sb.AppendFormat("\t{0} : {1}", AQAcquisitionService_Uid, AQAcquisitionService_Url);
                sb.AppendLine();
                sb.AppendFormat("\t{0} : {1}", AquariusPublishService_Uid, AquariusPublishService_Url);
                return sb.ToString();
            }

        }
    }
}
