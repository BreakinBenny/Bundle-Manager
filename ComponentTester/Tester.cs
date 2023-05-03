using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComponentTester
{
    public partial class Tester : Form
    {
        public Tester()
        {
            InitializeComponent();
        }

        private void New()
        {

        }

        private void Open()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Files|*.*";
            ofd.FileOk += Ofd_FileOk;
            ofd.ShowDialog(this);
        }

        private void Ofd_FileOk(object sender, CancelEventArgs e)
        {
            OpenFileDialog ofd = (OpenFileDialog)sender;

            if (ofd.FileName == null || ofd.FileName.Length <= 0 || !File.Exists(ofd.FileName))
                return;

            Stream s = ofd.OpenFile();
            BinaryReader br = new BinaryReader(s);
            List<byte> bytes = new List<byte>();
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                bytes.Add(br.ReadByte());
            }
            br.Close();

            hexView1.HexData = bytes.ToArray();
        }

        private void Save()
        {
            // Temp
            SaveAs();
        }

        private void SaveAs()
        {

        }

        private void Exit()
        {
            Application.Exit();
        }

        /*private void Tester_Load(object sender, EventArgs e)
        {
            if (!File.Exists("test.bin"))
            {
                //byte[] msg = Encoding.ASCII.GetBytes("Missing Data!");
                return;
            }*/
            /*Stream s = File.Open("test.bin", FileMode.Open);
            BinaryReader br = new BinaryReader(s);
            byte[] data = br.ReadToEnd();
            br.Close();

            hexView1.HexData = data;
        }*/

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }
    }
}
