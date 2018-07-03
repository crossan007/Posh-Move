using System;
using System.IO;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Diagnostics;
using LZ4;

namespace PoshMove
{

    public static class Utils
    {
        public class SendFileInfo
        {
            public string FileName;
            public long length;
        }

        public static void Send(string FilePath, string TargetIP, string TargetPort, string PassPhrase)
        {
            Aes myr = Aes.Create();
            PoshMove.Utils.SetUpSymmetric(myr, PassPhrase);
            ICryptoTransform transform = myr.CreateEncryptor();

            IPAddress remoteEP = IPAddress.Parse(TargetIP);
            IPEndPoint endpoint = new IPEndPoint(remoteEP, int.Parse(TargetPort));
            Socket senderSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Sending metadata to: " + remoteEP.ToString());
            senderSocket.Connect(endpoint);
            NetworkStream ns = new NetworkStream(senderSocket);
            CryptoStream cs = new CryptoStream(ns, transform, CryptoStreamMode.Write);
            FileInfo fi = new FileInfo(FilePath);
            SendFileInfo sfi = new SendFileInfo()
            {
                FileName = fi.Name,
                length = fi.Length
            };
            XmlSerializer serializer = new XmlSerializer(typeof(SendFileInfo));
            serializer.Serialize(cs, sfi);
            cs.FlushFinalBlock();
            senderSocket.Close();

            senderSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSocket.SendBufferSize = 65536;
            Console.WriteLine("SendBuffer Size " + senderSocket.SendBufferSize);
            Console.WriteLine("Sending file to: " + remoteEP.ToString() +" "+fi.Length+" bytes");
            senderSocket.Connect(endpoint);
            ns = new NetworkStream(senderSocket);
            FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
            //CryptoStream csfile = new CryptoStream(ns, transform, CryptoStreamMode.Write);
            //GZipStream gsz = new GZipStream(csfile, CompressionLevel.Optimal);
            //LZ4Stream gsz = new LZ4Stream(csfile, LZ4StreamMode.Compress, LZ4StreamFlags.Default, 1024 * 1024);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            fs.CopyTo(ns,8192);
           
            senderSocket.Close();
            sw.Stop();
            double kbps = (fs.Length / 1000) / (sw.Elapsed.TotalSeconds);
           
            Console.WriteLine("Copy finished in " + sw.ElapsedMilliseconds + " at " + kbps + " kbps.  Total Bytes / Bytes on Wire: "+fs.Length);

        }
        public static void Receive(string TargetPath, string ListenPort, string PassPhrase)
        {

            Aes myr = Aes.Create();
            PoshMove.Utils.SetUpSymmetric(myr, PassPhrase);
            ICryptoTransform transform =  myr.CreateDecryptor();


            IPAddress localhost = IPAddress.Any;
            IPEndPoint localEP = new IPEndPoint(localhost, int.Parse(ListenPort));
            Socket receiveSocket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            receiveSocket.Bind(localEP);
            receiveSocket.Listen(1);

            Socket handler = receiveSocket.Accept();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            NetworkStream ns = new NetworkStream(handler);
            CryptoStream cs = new CryptoStream(ns, transform, CryptoStreamMode.Read);
            XmlSerializer deserializer = new XmlSerializer(typeof(SendFileInfo));
            SendFileInfo sfi = (SendFileInfo)deserializer.Deserialize(cs);
            Console.WriteLine("Receiving: " + sfi.FileName + "@" + sfi.length.ToString() + " bytes");

            handler = receiveSocket.Accept();
            ns = new NetworkStream(handler);
            //CryptoStream csFile = new CryptoStream(ns, transform, CryptoStreamMode.Read);
            //GZipStream gsz = new GZipStream(csFile, CompressionMode.Decompress);
            //LZ4Stream gsz = new LZ4Stream(cs, LZ4StreamMode.Decompress);
            var targetPath = Path.Combine(TargetPath, sfi.FileName);
            FileStream fs = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);

            ns.CopyTo(fs,8192);
            fs.Flush();
            sw.Stop();
            double kbps = (fs.Length / 1000) / (sw.Elapsed.TotalSeconds);
            fs.Close();
            Console.WriteLine("Copy finished in " + sw.Elapsed.TotalSeconds + " seconds at "+kbps+" kbps");

        }

        public static void SetUpSymmetric(SymmetricAlgorithm myr, string passphrase)
        {
            byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(passphrase, SALT);
            myr.GenerateKey();
            myr.GenerateIV();
            myr.Key = pdb.GetBytes(myr.Key.Length);
            myr.IV = pdb.GetBytes(myr.IV.Length);
        }
    }
}