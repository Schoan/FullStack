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
                }
            }
        }

        static void Main(string[] args)
        {
            new AsyncClient();
        }
    }
}
