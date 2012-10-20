/* Баги:
 *          2 клиента вместе работают ок
 *          при подключении 3-й клиент одна точка не управляем
 *          при подключении 4-го на экране всего 2 точки
 * 
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace SGSclient
{
    //The commands for interaction between the server and the client
 
   

    enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        Draw,
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }


    public partial class SGSClient : Form
    {
        public Socket clientSocket; //The main client socket
        public string strName;      //Name by which the user logs into the room
        public EndPoint epServer;   //The EndPoint of the server
        bool _isMove=false;
                public Color myColor=Color.DarkGray;
        byte []byteData = new byte[1024];
        //Bitmap bmp;
        //Graphics bmpG;
        List<Player> players = new List<Player>();

        public SGSClient(string name, EndPoint elogin)
        {
            InitializeComponent();              // куда подключился с каким именем
            KeyPreview = true;
            epServer = elogin;

          //  bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
          //  bmpG = Graphics.FromImage(bmp);
          //  points.Add(new Point(5, 5));
          //  pointsColor.Add(Color.DarkGray);
          //  Show_Dot();
          players.Add(new Player(name));
          //Invalidate();
          //  label1.Text = players[0].name;
       }

        //Broadcast the message typed by the user to everyone
        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {		
                //Fill the info for the message to be send
                Data msgToSend = new Data();
                
                msgToSend.strName = strName;
                msgToSend.strMessage = txtMessage.Text;
                msgToSend.cmdCommand = Command.Message;

                byte[] byteData = msgToSend.ToByte();

                //Send it to the server
                clientSocket.BeginSendTo (byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(OnSend), null);
                
                txtMessage.Text = null;
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "SGSclientUDP: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }  
        }

        private void butSendXY_Click(object sender, EventArgs e)
        {
            
            try
            {
                //Fill the info for the message to be send
                Data msgToSend = new Data();
                msgToSend.strName = strName;
                msgToSend.strMessage = players[0].position.X.ToString() + " " + players[0].position.Y.ToString() + " " + players[0].color.ToString();
                msgToSend.cmdCommand = Command.Draw;

                byte[] byteData = msgToSend.ToByte();

                //Send it to the server
                clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(OnSend), null);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "SGSclientUDP: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Left_Game(string userName)
        {
            //bmpG.Clear(Color.White);
            //points.RemoveAt(GetUser(userName));
            //for (int i = 1; i < points.Count; i++)
            //{
            //    bmpG.DrawEllipse(new Pen(Color.Blue, 7), points[i].X, points[i].Y, 10, 10);
            //}
            //Invalidate();
        }

        private int GetUser(string userName)
        {
            try
            {
                return lstChatters.FindString(userName, 0);
            }
            catch { MessageBox.Show("Клиента нет на сервере"); return -1; }
        }
        
        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {            
            try
            {                
                clientSocket.EndReceive(ar);

                //Convert the bytes received into an object of type Data
                Data msgReceived = new Data(byteData);

                //Accordingly process the message received
                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        lstChatters.Items.Add(msgReceived.strName);
                        
                        int tmp=  lstChatters.Items.Count;
                        for (int i = players.Count; i < tmp; i++)
                        {
                            players.Add(new Player(lstChatters.Items[i].ToString()));
                        }
                        Invalidate();
                        break;

                    case Command.Logout:
                        lstChatters.Items.Remove(msgReceived.strName);
                        Left_Game(msgReceived.strName);
                        break;

                    case Command.Message:
                        break;

                    case Command.Draw:
                        string[] getPoints = msgReceived.strMessage.Split(' ');

                        //if ((players[0].name+":") != getPoints[0])
                        //{
                        for (int i = 0; i < players.Count;i++ )
                            {                     
                                if (getPoints[0] == (players[i].name + ":"))
                                {
                                    players[i].position = new Point(int.Parse(getPoints[1]), int.Parse(getPoints[2]));
                                    players[i].color = Color.FromName(getPoints[3].Replace("[", "").Replace("]", ""));
                                    break;
                                }
                            }

                            
                       //}
                       Invalidate();
                        break;

                    case Command.List:
                        lstChatters.Items.AddRange(msgReceived.strMessage.Split('*'));
                       
                        lstChatters.Items.RemoveAt(lstChatters.Items.Count - 1);
                        for (int masha = 0; masha < lstChatters.Items.Count; masha++ )
                            players.Add(new Player(lstChatters.Items[masha].ToString()));
                        players.RemoveAt(players.Count - 1);
                        txtChatBox.Text += "<<<" + strName + " has joined the room>>>\r\n";
                        Invalidate();
                        break;
                }

                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                    txtChatBox.Text += msgReceived.strMessage + "\r\n";

                byteData = new byte[1024];                

                //Start listening to receive more data from the user
                clientSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epServer,
                                           new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
	    CheckForIllegalCrossThreadCalls = false;

            this.Text = "SGSclient: " + strName;
            
            //The user has logged into the system so we now request the server to send
            //the names of all users who are in the chat room
            Data msgToSend = new Data ();
            msgToSend.cmdCommand = Command.List;
            msgToSend.strName = strName;
            msgToSend.strMessage = null;

            byteData = msgToSend.ToByte();

            clientSocket.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, 
                new AsyncCallback(OnSend), null);

            byteData = new byte[1024];
            //Start listening to the data asynchronously
            clientSocket.BeginReceiveFrom (byteData,
                                       0, byteData.Length,
                                       SocketFlags.None,
                                       ref epServer,
                                       new AsyncCallback(OnReceive),
                                       null);
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            if (txtMessage.Text.Length == 0)
                btnSend.Enabled = false;
            else
                btnSend.Enabled = true;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            foreach (Player p in players)
            {
                    e.Graphics.FillEllipse(new SolidBrush(p.color), p.position.X - 5, p.position.Y - 5, 10, 10);
                //e.Graphics.Clear(Color.White);
                //e.Graphics.DrawImage(bmp, 0, 0);
            }
        }

        private void SGSClient_FormClosing(object sender, FormClosingEventArgs e)
        {
           /* if (MessageBox.Show("Are you sure you want to leave the chat room?", "SGSclient: " + strName,
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }*/

            try
            {
                //Send a message to logout of the server
                Data msgToSend = new Data ();
                msgToSend.cmdCommand = Command.Logout;
                msgToSend.strName = strName;
                msgToSend.strMessage = null;

                byte[] b = msgToSend.ToByte ();
                clientSocket.SendTo(b, 0, b.Length, SocketFlags.None, epServer);
                clientSocket.Close();
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient: " + strName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                    btnSend_Click(sender, null);
            }
        }
        
        private void butSelectColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if(cd.ShowDialog()==DialogResult.OK)
            {

                myColor=cd.Color;
                players[0].color = myColor;
                butSelectColor.BackColor=cd.Color;
            }
        }

        private void SGSClient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                if (players[0].position.Y >= 10)
                {
                    players[0].position = new Point(players[0].position.X, players[0].position.Y - 5);
                    _isMove = true;
                }
                else _isMove = false;
            }
            else
                if (e.KeyCode == Keys.Down)
                {
                    if (players[0].position.Y <= 200)
                    {
                        players[0].position = new Point(players[0].position.X, players[0].position.Y + 5);
                        _isMove = true;
                    }
                    else _isMove = false;
                }
                else
                    if (e.KeyCode == Keys.Left)
                    {
                        if (players[0].position.X >= 10)
                        {
                            players[0].position = new Point(players[0].position.X - 5, players[0].position.Y);
                            _isMove = true;
                        }
                        else _isMove = false;
                    }
                    else
                        if (e.KeyCode == Keys.Right)
                        {
                            if (players[0].position.X <= 200)
                            {
                                players[0].position = new Point(players[0].position.X + 5, players[0].position.Y);
                                _isMove = true;
                            }
                            else _isMove = false;
                        }
            //Show_Dot();

            //Invalidate();
            butSendXY_Click(null, null);
        }


        private void Show_Dot()
        {

/*          for (int i = 0; i < points.Count; i++)
            {
                bmpG.DrawEllipse(new SolidBrush(pointsColor[i]), points[i].X, points[i].Y, 10, 10);
            }
            Invalidate();*/

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
           // this.strColor = (Color.DarkGray).ToString();
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

        //    this.strColor = (Color.Black).ToString();
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
       // public string strColor;
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }

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
}