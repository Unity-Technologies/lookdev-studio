
#if UNITY_EDITOR

using UnityEngine;
using System.Net.Sockets;

namespace LookDev.Editor
{
    public class BlenderCommands : ICommands
    {

        private string hostName = "localhost";
        private int port = 6002;
        private int byteBufferSize = 4096;

        public BlenderCommands(string inHostName, int inPort, int inBufferSize)
        {
            hostName = inHostName;
            port = inPort;
            byteBufferSize = inBufferSize;
        }

        public void SendPredesinedMessageToBlender(CommandType commandType, string[] args)
        {
            string cmd = string.Empty;

            if (args == null)
            {
                Debug.LogError("Command argument is null");
                return;
            }

            switch (commandType)
            {
                case CommandType.IMPORT_FBX:
                    cmd = string.Format("exec(\"bpy.ops.import_scene.fbx(filepath = \\\"{0}\\\")\")", args[0]);

                    // exec("bpy.ops.import_scene.fbx(filepath = \"d:/Bag_01_Snaps014.fbx\")")
                    break;

                case CommandType.EXPORT_FBX:
                    cmd = string.Format("bpy.ops.mesh.primitive_monkey_add()", args[0]);
                    // exportFile "d:/GGGGG.fbx" #noPrompt
                    break;

                case CommandType.CUSTOM_COMMAND:
                    cmd = args[0];
                    break;
            }

            SendMessageToBlender(cmd);
        }


        public string SendMessageToBlender(string strCmd)
        {
            string responseData = string.Empty;

            Debug.Log("Msg : " + strCmd);

            Socket sock = null;

            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                sock.Connect(hostName, port);

                byte[] buf = System.Text.Encoding.Default.GetBytes(strCmd);

                sock.Send(buf, 0, buf.Length, SocketFlags.None);

                sock.Close();

            }
            catch
            {
                sock.Close();
            }

            /*
            TcpClient tc = null;
            NetworkStream ns = null;

            try
            {
                tc = new TcpClient(hostName, port);

                byte[] buf = System.Text.Encoding.Default.GetBytes(strCmd);

                ns = tc.GetStream();
                ns.Write(buf, 0, buf.Length);

                byte[] data = new byte[byteBufferSize];

                int bytes = ns.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                ns.Flush();

                ns.Close();
                tc.Close();

                data.Initialize();
            }
            catch(System.Exception ex)
            {
                Debug.LogError(string.Format("Failed To send the Message : {0}", ex.Message));
            }
            finally
            {
                ns.Close();
                tc.Close();
            }
            */
            return responseData;
        }

        public bool IsConnectedBlender()
        {
            TcpClient tc = null;

            try
            {
                tc = new TcpClient(hostName, port);

                if (tc.Connected)
                {
                    tc.Close();
                    return true;
                }
                else
                {
                    tc.Close();
                    return false;
                }
            }
            catch
            {
                if (tc != null)
                    tc.Close();

                return false;
            }
        }

        public bool IsBlenderRunning()
        {
            System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcesses();

            for (int i = 0; i < procs.Length; i++)
            {
                if (procs[i].ProcessName.ToLower() == "blender")
                    return true;
            }

            return false;
        }

        public void KillBlenderProcess()
        {
            System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcesses();

            for (int i = 0; i < procs.Length; i++)
            {
                if (procs[i].ProcessName.ToLower() == "blender")
                {
                    procs[i].Kill();
                }
            }
        }


    }
}
#endif

