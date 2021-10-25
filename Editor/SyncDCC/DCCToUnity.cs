
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using Unity.EditorCoroutines.Editor;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Linq;

namespace LookDev.Editor
{
    public class DCCToUnity
    {
        const int bufferSize = 4096;

        static EditorCoroutine ec;

        static Socket server = null;

        static Thread thread;

        static Queue<string> Msg = new Queue<string>();


        //[InitializeOnLoadMethod]
        static void RunServer()
        {
            DCCToUnity dToU = new DCCToUnity();
            dToU.StartServer();
        }

        [MenuItem("DCC Sync/Start Server", false, 20)]
        static void RunServerForManualCall()
        {
            RunServer();
        }


        [MenuItem("DCC Sync/Send a testing MSG to SERVER", false, 21)]
        static void SendMsgAsTest()
        {
            Socket sock = null;

            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                sock.Connect("localhost", 6009);

                byte[] buf = System.Text.Encoding.Default.GetBytes("This is a testing Message!!");

                sock.Send(buf, 0, buf.Length, SocketFlags.None);

                sock.Close();

            }
            catch
            {
                sock.Close();
            }
        }


        [MenuItem("DCC Sync/Stop Server", false, 22)]
        static void StopServer()
        {

            if (ec != null)
            {
                EditorCoroutineUtility.StopCoroutine(ec);
                ec = null;
            }

            if (thread != null)
            {
                if (thread.ThreadState == ThreadState.Running)
                    thread.Abort();
            }

        }


        public void StartServer()
        {
            try
            {
                if (server == null)
                {
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    if (!server.IsBound)
                        server.Bind(new IPEndPoint(IPAddress.Any, 6009));

                    server.Listen(5);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("Failed Socket : {0}", ex));
                server.Close();
            }

            if (thread != null)
            {
                if (thread.ThreadState == ThreadState.Running)
                    thread.Abort();
            }

            thread = new Thread(new ThreadStart(PollingServer));

            thread.Start();
            Debug.Log("START SERVER");


            if (ec != null)
            {
                EditorCoroutineUtility.StopCoroutine(ec);
                ec = null;
            }

            if (ec == null)
                ec = EditorCoroutineUtility.StartCoroutine(MsgQueueListen(), this);
        }

        public void CloseServer()
        {
            if (ec != null)
            {
                Debug.Log("STOP SERVER");
                EditorCoroutineUtility.StopCoroutine(ec);
            }
        }

        IEnumerator MsgQueueListen()
        {
            while (true)
            {
                if (Msg.Count != 0)
                {
                    string msg = Msg.Dequeue();

                    ParsingMessage(msg);
                    Debug.Log(msg);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        void ParsingMessage(string msg)
        {
            string[] tokens = msg.Split(' ');

            string methodName = tokens[0].Trim();
            string[] parameters = tokens[1].Split(',');

            System.Reflection.MethodInfo mInfo = typeof(DCCToUnity).GetMethod(methodName);

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = new string(parameters[i].Where(c => !char.IsControl(c)).ToArray());
                }
            }

            if (mInfo != null)
            {
                mInfo.Invoke(this, new System.Object[] { parameters });
            }
        }


        void PollingServer()
        {
            Socket client = null;

            byte[] buff = new byte[bufferSize];

            while (true)
            {
                if (server.IsBound)
                {
                    client = server.Accept();

                    client.Receive(buff, 0, buff.Length, SocketFlags.None);

                    string currentMsg = System.Text.Encoding.Default.GetString(buff);

                    if (currentMsg != string.Empty)
                        Msg.Enqueue(currentMsg);

                    buff.Initialize();

                    client.Close();
                }
            }

        }

        [MenuItem("DCC Sync/Create a testing Material", false, 40)]
        public static void CreateDefaultMaterial()
        {
            CreateMaterial("MyFirstMat");
        }

        public static void CreateMaterial(params string[] matName)
        {


            Material newMaterial = new Material(Shader.Find("HDRP/Autodesk Interactive/AutodeskInteractive"));

            string matPath = string.Format("Assets/LookDev/Materials/{0}.mat", matName[0].Trim());

            matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);

            AssetDatabase.CreateAsset(newMaterial, matPath);

        }


    }
}

#endif

