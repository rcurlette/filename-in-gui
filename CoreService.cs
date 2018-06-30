using System.ServiceModel;
using System.Xml;
using Tridion.ContentManager.CoreService.Client;
using System.Net;

namespace FilenameInGui
{
    public class CoreService
    { //acct used to access CoreService, needs to be a member of Editor group in Tridion
      private string username = "cms@blocks.org";
      private string password = "Tridion";
        public CoreServiceClient GetClient()
        {
            var binding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas()
                {
                    MaxStringContentLength = 2147483647,
                    MaxArrayLength = 2147483647
                }
            };
            var endpoint = new EndpointAddress("net.tcp://localhost:2660/CoreService/2011/netTcp");

            var client = new CoreServiceClient(binding, endpoint);
            client.ChannelFactory.Credentials.Windows.ClientCredential = 
                new NetworkCredential(username, password);
            return client;
        }
    }
}
