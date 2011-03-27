using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WindowPlugins.GUITVSeries;

namespace TraktPlugin
{
    public partial class SeriesSelect : Form
    {

        private int CheckedCount { get; set; }

        public List<DBSeries> CheckedItems
        {
            get
            {
                return _checkedItems;
            }
            set
            {
                _checkedItems = value;
            }
        } private List<DBSeries> _checkedItems = new List<DBSeries>();

        public List<DBSeries> UnCheckedItems
        {
            get
            {
                return _uncheckedItems;
            }
            set
            {
                _uncheckedItems = value;
            }
        } private List<DBSeries> _uncheckedItems = new List<DBSeries>();

        public SeriesSelect()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            // Get list of series in view
            SQLCondition conditions = new SQLCondition();
            conditions.Add(new DBOnlineSeries(), DBOnlineSeries.cTraktIgnore, 1, SQLConditionType.Equal);
            CheckedItems = DBSeries.Get(conditions);

            // Get list of series not in view
            conditions = new SQLCondition();
            conditions.Add(new DBOnlineSeries(), DBOnlineSeries.cTraktIgnore, 1, SQLConditionType.NotEqual);
            UnCheckedItems = DBSeries.Get(conditions);

            // Populate series list, 
            // mark as checked at top of list
            foreach (DBSeries series in CheckedItems)
            {
                checkedListBoxSeries.Items.Add(series, true);
            }

            foreach (DBSeries series in UnCheckedItems)
            {
                checkedListBoxSeries.Items.Add(series, false);
            }

            CheckedCount = CheckedItems.Count;
            labelSeriesSelected.Text = CheckedCount.ToString() + " Series Selected";

            this.checkedListBoxSeries.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBoxSeries_ItemCheck);
            base.OnLoad(e);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            foreach (DBSeries series in CheckedItems)
            {
                // ignore these series
                series[DBOnlineSeries.cTraktIgnore] = 1;
                series.Commit();
            }

            foreach (DBSeries series in UnCheckedItems)
            {
                // unignore these series
                series[DBOnlineSeries.cTraktIgnore] = 0;
                series.Commit();
            }
            
            DialogResult = DialogResult.OK;
            Close();
        }

        private void checkedListBoxSeries_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int index = e.Index;
            DBSeries item = (DBSeries)checkedListBoxSeries.Items[index];

            // Add/Remove items from list
            if (item != null)
            {
                // Item state before item was clicked 
                if (checkedListBoxSeries.GetItemChecked(index))
                {

                    // Store items changes
                    if (!UnCheckedItems.Contains(item))
                    {
                        UnCheckedItems.Add(item);
                    }
                    if (CheckedItems.Contains(item))
                    {
                        CheckedItems.Remove(item);
                    }

                    CheckedCount -= 1;
                    labelSeriesSelected.Text = (CheckedCount).ToString() + " Series Selected";
                }
                else
                {
                    // Store items changes
                    if (!CheckedItems.Contains(item))
                    {
                        CheckedItems.Add(item);
                    }
                    if (UnCheckedItems.Contains(item))
                    {
                        UnCheckedItems.Remove(item);
                    }

                    CheckedCount += 1;
                    labelSeriesSelected.Text = (CheckedCount).ToString() + " Series Selected";
                }
            }

        }

        private void chkBoxToggleAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxSeries.Items.Count; i++)
            {
                checkedListBoxSeries.SetItemChecked(i, chkBoxToggleAll.Checked);
            }
        }

    }
}
