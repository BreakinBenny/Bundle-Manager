using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using PluginAPI;
using BundleUtilities;
using System.Threading.Tasks;
using LangEditor;
using BundleFormat;

namespace VaultFormat
{
    public delegate void Notify();  // delegate
    public partial class AttribSysVaultForm : Form, IEntryEditor
    {
        public AttribSysVaultForm()
        {
            InitializeComponent();
        }
       
        public event Notify EditEvent;

        private AttribSys _attribSys;
        public AttribSys AttribSys
        {
            get => _attribSys;
            set
            {
                _attribSys = value;
                UpdateDisplay();
            }
        }


        private void UpdateDisplay()
        {
            lstDataChunks.Items.Clear();

            if (AttribSys == null)
                return;

            for (int i = 0; i < AttribSys.DataChunks.Count; i++)
            {
                DataChunk chunk = AttribSys.DataChunks[i];

                string[] value = {
                    chunk.ClassName,
                    chunk.ClassHash.ToString("X8"),
                    chunk.CollectionHash.ToString("X8"),
                };
                lstDataChunks.Items.Add(new ListViewItem(value));
            }

            lstDataChunks.ListViewItemSorter = new AttribSysVaultSorter(0);
            lstDataChunks.Sort();
        }

        private void AttribSysVaultForm_Load(object sender, EventArgs e)
        {

        }
        private class AttribSysVaultSorter : IComparer
        {
            public readonly int Column;
            public bool Direction;

            public AttribSysVaultSorter(int column)
            {
                this.Column = column;
                this.Direction = false;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                if (Column > itemX.SubItems.Count || Column > itemY.SubItems.Count)
                {
                    if (this.Direction)
                        return -1;
                    return 1;
                }

                string iX = itemX.SubItems[Column].Text;
                string iY = itemY.SubItems[Column].Text;

                int iXint;
                int iYint;

                if (int.TryParse(iX, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out iXint))
                {
                    if (int.TryParse(iY, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out iYint))
                    {
                        int val2 = iXint.CompareTo(iYint);
                        if (this.Direction)
                            return val2 * -1;
                        return val2;
                    }
                }

                int val = String.CompareOrdinal(iX, iY);
                if (this.Direction)
                    return val * -1;
                return val;
            }

            public void Swap()
            {
                this.Direction = !this.Direction;
            }
        }

        private void lstVehicles_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void changeCollectionHashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to get the lookup 8.");
            if (value == null)
                return;
            ulong result = Utilities.calcLookup8(value);
            int index = AttribSys.DataChunks.FindIndex(i => i.ClassName == lstDataChunks.SelectedItems[0].Text);
            AttribSys.DataChunks[index].CollectionHash = result;
            MessageBox.Show(this, "The lookup 8 hashed value is: " + result.ToString("X16"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            EditEvent?.Invoke();
            UpdateDisplay();
        }
    }

}
