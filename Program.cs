using System;
using System.IO;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;

namespace PoshMove
{

    public static class Utils
    {
        public class SendFileInfo
        {
            public string FileName;
            public long length;
        }

        public static void Send(string FilePath, string IPAddress)
        {
            IPHostEntry address = Dns.GetHostEntry(IPAddress);
            IPAddress remoteEP = address.AddressList[0];
            IPEndPoint endpoint = new IPEndPoint(remoteEP, 11000);
            Socket senderSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            senderSocket.Connect(endpoint);
            NetworkStream ns = new NetworkStream(senderSocket);
            FileInfo fi = new FileInfo(FilePath);
            SendFileInfo sfi = new SendFileInfo()
            {
                FileName = fi.Name,
                length = fi.Length
            };
            XmlSerializer serializer = new XmlSerializer(typeof(SendFileInfo));
            serializer.Serialize(ns, sfi);


            senderSocket.Close();
            senderSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSocket.Connect(endpoint);
            ns = new NetworkStream(senderSocket);
            FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
            GZipStream gsz = new GZipStream(ns, CompressionMode.Compress);
            fs.CopyTo(gsz);
            gsz.Close();
            ns.Flush();
            senderSocket.Close();

        }
        public static void Receive(string TargetPath)
        {
            IPHostEntry address = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localhost = address.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(localhost, 11000);
            Socket receiveSocket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            receiveSocket.Bind(localEP);
            receiveSocket.Listen(1);

            Socket handler = receiveSocket.Accept();
            NetworkStream ns = new NetworkStream(handler);
            XmlSerializer deserializer = new XmlSerializer(typeof(SendFileInfo));
            SendFileInfo sfi = (SendFileInfo)deserializer.Deserialize(ns);
            Console.Write("Receiving: " + sfi.FileName + "@" + sfi.length.ToString() + " bytes");

            handler = receiveSocket.Accept();
            ns = new NetworkStream(handler);
            var targetPath = Path.Combine(TargetPath, sfi.FileName);
            FileStream fs = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);
            GZipStream gsz = new GZipStream(ns, CompressionMode.Decompress);
            gsz.CopyTo(fs);
            fs.Flush();
            fs.Close();

        }
    }
}