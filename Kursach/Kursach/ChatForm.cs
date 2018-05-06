using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kursach
{
    public partial class ChatForm : Form
    {
        delegate void TextDelegate(string text);
        delegate void ComboBoxClearer();
        Listener listener;
        string TempNum = String.Empty;
        public ChatForm()
        {
            InitializeComponent();
            listener = Listener.getInstance();
            Thread ListenThread = new Thread(Listen);
            ListenThread.Start();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            listener.client.Client.Send(Encoding.ASCII.GetBytes($"Command:SendMessage\r\nMessage:{textBoxMessage.Text}\r\nGroupId:{comboBoxGroupIds.SelectedItem.ToString()}"));
        }
        public void Listen()
        {
            listener.client.Client.Send(Encoding.ASCII.GetBytes($"Command:GetGroups"));
            while (true)
            {
                byte[] rawdata = new byte[1024];
                string headerStr = String.Empty;
                listener.client.Client.Receive(rawdata);
                headerStr = Encoding.ASCII.GetString(rawdata, 0, rawdata.Length);
                string[] splitted = headerStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                Dictionary<string, string> headers = new Dictionary<string, string>();
                foreach (string s in splitted)
                {
                    if (s.Contains(":"))
                    {
                        headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                    }
                }
                if (headers.ContainsKey("Command"))
                {
                    string Command = headers["Command"].Trim('\0');
                    if (Command.Equals("BroadCasting"))
                    {
                        TextBoxText(headers["Message"].Trim('\0'));
                    }
                    if (Command.Equals("Connected"))
                    {
                        TextBoxText("Вы были подключены к чату\r\n");
                    }
                    if (Command.Equals("ConnectedToGroup"))
                    {
                        var grid = headers["GroupId"].Trim('\0');
                        MessageBox.Show($"Вы подключились к группе {grid}", $"Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    if (Command.Equals("Groups"))
                    {
                        ComboBoxClear();
                        var st = headers["List"].Trim('\0');
                        var it = st.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var i in it)
                        {
                            ComboBoxAdd(i);
                        }
                    }
                    if (Command.Equals("Refresh"))
                    {
                        listener.client.Client.Send(Encoding.ASCII.GetBytes($"Command:GetGroups"));
                    }
                }
            }
        }
        private void TextBoxText(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new TextDelegate(TextBoxText), new object[] { text });
                return;
            }
            else
            {
                textBoxChat.Text+=text;
            }
        }
        private void ComboBoxAdd(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new TextDelegate(ComboBoxAdd), new object[] { text });
                return;
            }
            else
            {
                comboBoxGroupIds.Items.Add(text);
            }
        }
        private void ComboBoxClear()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new ComboBoxClearer(ComboBoxClear));
                return;
            }
            else
            {
                comboBoxGroupIds.Items.Clear();
            }
        }
        private void ChatForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void buttonNewGroup_Click(object sender, EventArgs e)
        {
            listener.client.Client.Send(Encoding.ASCII.GetBytes($"Command:CreateGroup\r\nGroupId:{TempNum}"));
        }

        private void textBoxID_TextChanged(object sender, EventArgs e)
        {
            TempNum = textBoxID.Text;
        }

        private void comboBoxGroupIds_SelectedIndexChanged(object sender, EventArgs e)
        {
            listener.client.Client.Send(Encoding.ASCII.GetBytes($"Command:JoinGroup\r\nGroupId:{comboBoxGroupIds.SelectedItem.ToString()}"));
        }
    }
}
