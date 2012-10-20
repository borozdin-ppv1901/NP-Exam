/* ����: ��� ������ ������ ������������ ����� ������
 *       ��� ������ ������ ������ ����� �� ����� ������������ �� ������ 
 *       �������� ��������� ����������
 * 
*/

using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;


namespace Server
{
    //The commands for interaction between the server and the client
    class Player
    {
        public Point position;
        public Color color;
        public string name;
        public Player(Point pos, Color col, string n)
        {
            position = pos;
            color = col;
            name = n;
        }
        public Player()
        {
            position = new Point(10, 10);
            color = Color.DarkGray;
        }
        public Player(string n)
        {
            position = new Point(10, 10);
            color = Color.DarkGray;
            name = n;
        }
    }
    
    enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        Draw,
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }


    public partial class SGSserverForm : Form
    {
        //The ClientInfo structure holds the required information about every
        //client connected to the server
        struct ClientInfo
        {
            public EndPoint endpoint;   //Socket of the client
            public string strName;      //Name by which the user logged into the chat room
        }

        //The collection of all clients logged into the room (an array of type ClientInfo)
        ArrayList clientList;

        //The main socket on which the server listens to the clients
        Socket serverSocket;

        byte[] byteData = new byte[1024];
        //Bitmap bmp;
        //Graphics bmpG;

        List<Player> players = new List<Player>();

        public SGSserverForm()
        {
            clientList = new ArrayList();
            InitializeComponent();
            //bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //bmpG = Graphics.FromImage(bmp);
            //bmpG.Clear(Color.White);  
        }

    private void Form1_Load(object sender, EventArgs e)
    {            
        try
        {
	    CheckForIllegalCrossThreadCalls = false;

            //We are using UDP sockets
            serverSocket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Dgram, ProtocolType.Udp);

            //Assign the any IP of the machine and listen on port number 1000
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

            //Bind this address to the server
            serverSocket.Bind(ipEndPoint);
            
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            //The epSender identifies the incoming clients
            EndPoint epSender = (EndPoint) ipeSender;

            //Start receiving data
            serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, 
                SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);                
        }
        catch (Exception ex) 
        { 
            MessageBox.Show(ex.Message, "SGSServerUDP", 
                MessageBoxButtons.OK, MessageBoxIcon.Error); 
        }            
    }

    private void Form1_Paint(object sender, PaintEventArgs e)
    {
        //e.Graphics.Clear(Color.White);
        //e.Graphics.DrawImage(bmp, 0, 0);
        foreach (Player p in players)
        {
            e.Graphics.FillEllipse(new SolidBrush(p.color), p.position.X - 5, p.position.Y - 5, 10, 10);
            //e.Graphics.Clear(Color.White);
            //e.Graphics.DrawImage(bmp, 0, 0);
        }
    }

    private int GetUser(string userName)
    {
        int pos = 0;
        try
        {
            foreach (ClientInfo cl in clientList)
            {
                if (cl.strName == userName)
                    return pos;
                pos++;
            }
        }
        catch { MessageBox.Show("������� ��� �� �������"); }
        return -1;
    }

    private void Left_Game(string userName)
    {
//        bmpG.Clear(Color.White);
//        points.RemoveAt(GetUser(userName));
//        for (int i = 0; i < points.Count; i++)
// /       {
//            bmpG.DrawEllipse(new Pen(Color.Blue, 7), points[i].X, points[i].Y, 10, 10);
//        }
//        Invalidate();
    }

    private void Show_Dot()
    {
                
  //      bmpG.Clear(Color.White);
  //      for(int i=0; i<points.Count;i++)
  //      {
 //           bmpG.DrawEllipse(new Pen(pointsColor[i],7), points[i].X, points[i].Y, 10, 10);          
 //       }
 //       Invalidate();

    }
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.EndReceiveFrom (ar, ref epSender);
                
                //Transform the array of bytes received from the user into an
                //intelligent form of object Data
                Data msgReceived = new Data(byteData);
                
                
                //We will send this object in response the users request
                Data msgToSend = new Data();

                byte [] message;
                
                //If the message is to login, logout, or simple text message
                //then when send to others the type of the message remains the same
                msgToSend.cmdCommand = msgReceived.cmdCommand;
                msgToSend.strName = msgReceived.strName;

                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        
                        //When a user logs in to the server then we add her to our
                        //list of clients

                        ClientInfo clientInfo = new ClientInfo();
                        clientInfo.endpoint = epSender;      
                        clientInfo.strName = msgReceived.strName;                        

                        clientList.Add(clientInfo);
                        
                        //Set the text of the message that we will broadcast to all users
                        msgToSend.strMessage = "<<<" + msgReceived.strName + " has joined the room>>>";
                        players.Add(new Player(new Point(10, 10), Color.DarkGray, msgReceived.strName));
                        //Show_Dot();
                        //Invalidate();
                        break;

                    case Command.Logout:                    
                        
                        //When a user wants to log out of the server then we search for her 
                        //in the list of clients and close the corresponding connection

                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.endpoint == epSender)
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }
                            ++nIndex;
                        }                                               
                        
                        msgToSend.strMessage = "<<<" + msgReceived.strName + " has left the room>>>";
                        //Left_Game(msgReceived.strName);                                                   // ����� �� ��� �������??
                        break;

                    case Command.Message:

                        //Set the text of the message that we will broadcast to all users

                            msgToSend.strMessage = msgReceived.strName + ": " + msgReceived.strMessage;
                        break;

                    case Command.Draw:

                        // Draw Player Coordinat on the screen
                        string[] getPoints = msgReceived.strMessage.Split(' ');
                        int userPosition = GetUser(msgReceived.strName);

                            players[userPosition].position=new Point(int.Parse(getPoints[0]), int.Parse(getPoints[1]));
                            players[userPosition].color = Color.FromName(getPoints[3].Replace("[", "").Replace("]", ""));
          
                        //Show_Dot();
                        msgToSend.strMessage = msgReceived.strName + ": " + getPoints[0].ToString() +" "+ getPoints[1].ToString()+" "+getPoints[3] +" �����";
                        Invalidate();
                        break;

                    case Command.List:

                        //Send the names of all users in the chat room to the new user
                        msgToSend.cmdCommand = Command.List;
                        msgToSend.strName = null;
                        msgToSend.strMessage = null;

                        //Collect the names of the user in the chat room
                        foreach (ClientInfo client in clientList)
                        {
                            //To keep things simple we use asterisk as the marker to separate the user names
                            msgToSend.strMessage += client.strName + "*";   
                        }                        

                        message = msgToSend.ToByte();

                        //Send the name of the users in the chat room
                        serverSocket.BeginSendTo (message, 0, message.Length, SocketFlags.None, epSender, 
                                new AsyncCallback(OnSend), epSender);                        
                        break;
                }

                if (msgToSend.cmdCommand != Command.List)   //List messages are not broadcasted
                {
                    message = msgToSend.ToByte();

                    foreach (ClientInfo clientInfo in clientList)
                    {
                        if (clientInfo.endpoint != epSender ||
                            msgToSend.cmdCommand != Command.Login)
                        {
                            //Send the message to all users
                            serverSocket.BeginSendTo (message, 0, message.Length, SocketFlags.None, clientInfo.endpoint, 
                                new AsyncCallback(OnSend), clientInfo.endpoint);                           
                        }
                    }

                    txtLog.Text += msgToSend.strMessage + "\r\n";
                }

                //If the user is logging out then we need not listen from her
                if (msgReceived.cmdCommand != Command.Logout)
                {
                    //Start listening to the message send by the user
                    serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, SocketFlags.None, ref epSender, 
                        new AsyncCallback(OnReceive), epSender);
                }
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {                
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Color strColor;
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}