using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace DummyClient
{
    public struct Telegram
    {
        private int m_DataLength;
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        public void SetLength(byte[] Data)
        {
            if (Data.Length < 4)
            {
                return;
            }
            m_DataLength = BitConverter.ToInt32(Data, 0);
        }

        public int DataLength
        {
            get { return m_DataLength; }
        }

        public void InitData()
        {
            m_Data = new byte[m_DataLength];
        }

        public String GetData()
        {
            return Encoding.Unicode.GetString(m_Data);
        }

        public byte[] GetBuffer()
        {
            return new byte[4];
        }

        public void SetData(String Data)
        {
            m_Data = Encoding.Unicode.GetBytes(Data);
            m_DataLength = m_Data.Length;
        }
    }

    enum ChatType { Send, Receive, System }

    class AsyncClient
    {
        private Socket m_Client = null;
        private List<StringBuilder> m_Display = null;
        private int m_Line;

        public void DataInput()
        {
            String sData;
            Telegram _telegram = new Telegram();
            SendDisplay("Chatting Program Client Start", ChatType.System);
            while(true)
            {
                sData = Console.ReadLine();
                if(sData.CompareTo("exit") == 0)
                {
                    break;
                }
                else
                {
                    if(m_Client != null)
                    {
                        if(!m_Client.Connected)
                        {
                            m_Client = null;
                            SendDisplay("Connection Failed!", ChatType.System);
                            SendDisplay("Press any key... ", ChatType.System);
                        }
                        else
                        {
                            _telegram.SetData(sData);
                            SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
                            _sendArgs.SetBuffer(BitConverter.GetBytes(_telegram.DataLength), 0, 4);
                            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Send_Completed);
                            _sendArgs.UserToken = _telegram;
                            m_Client.SendAsync(_sendArgs);
                        }                      
                    }
                    else { break; }
                }
            }
        }

        public void SendDisplay(String nMessage, ChatType nType)
        {
            StringBuilder buffer = new StringBuilder();
            switch(nType)
            {
                case ChatType.Send:
                    buffer.Append("SendMessage : ");
                    break;
                case ChatType.Receive:
                    buffer.Append("RecieveMessage : ");
                    break;
                case ChatType.System:
                    buffer.Append("SystemMessage : ");
                    break;
            }

            buffer.Append(nMessage);
            if(m_Line < 20)
            {
                m_Display.Add(buffer);
            }
            else
            {
                m_Display.RemoveAt(0);
                m_Display.Add(buffer);
            }
            m_Line++;
            Console.Clear();
            for(int i=0; i<20; i++)
            {
                if(i<m_Display.Count)
                {
                    Console.WriteLine(m_Display[i].ToString());
                }
                else
                {
                    Console.WriteLine();
                }
            }
            Console.Write("Input Msg (exit - 종료): ");
        }

        public AsyncClient()
        {
            m_Display = new List<StringBuilder>();
            m_Line = 0;

            Socket _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint _ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);

            SocketAsyncEventArgs _args = new SocketAsyncEventArgs();
            _args.RemoteEndPoint = _ipep;
            _args.Completed += new EventHandler<SocketAsyncEventArgs>(Connect_Completed);

            _client.ConnectAsync(_args);
            DataInput();
        }

        private void Connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            m_Client = (Socket)sender;

            if(m_Client.Connected)
            {
                Telegram _telegram = new Telegram();
                SocketAsyncEventArgs _receiveArgs = new SocketAsyncEventArgs();
                _receiveArgs.UserToken = _telegram;
                _receiveArgs.SetBuffer(_telegram.GetBuffer(), 0, 4);
                _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Receive_Completed);

                m_Client.ReceiveAsync(_receiveArgs);
                SendDisplay("Server connection Success", ChatType.System);
            }
            else
            {
                m_Client = null;
                SendDisplay("Connection Failed!", ChatType.System);
                SendDisplay("Press any key... ", ChatType.System);
            }
        }

        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket _client = (Socket)sender;
            Telegram _telegram = (Telegram)e.UserToken;
            _client.Send(_telegram.Data);
            SendDisplay(_telegram.GetData(), ChatType.Send);
        }

        private void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            Socket _client = (Socket)sender;
            Telegram _telegram = (Telegram)e.UserToken;
            _telegram.SetLength(e.Buffer);
            _telegram.InitData();

            if(_client.Connected)
            {
                _client.Receive(_telegram.Data, _telegram.DataLength, SocketFlags.None);
                SendDisplay(_telegram.GetData(), ChatType.Receive);
                _client.ReceiveAsync(e);
            }
            else
            {
                SendDisplay("Connection Failed!", ChatType.System);
                m_Client = null;
            }
        }

        static void Main(string[] args)
        {
            new AsyncClient();
        }
    }
}
