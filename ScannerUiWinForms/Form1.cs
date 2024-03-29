﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;
using ScannerCore;

namespace ScannerUiWinForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            RegistryKey reg = null;
            try
            {
                reg = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Cursors");
                _cursorSize = reg !=null ? (int)reg.GetValue("CursorBaseSize") : 48;
            }
            catch
            {
                _cursorSize = 48;
            }
            finally
            {
                reg?.Dispose();
            }
            
        }

        private DriveScanner _scanner;
        private long _filterThreshold;
        private readonly int _cursorSize;

        private void Form1_Load(object sender, EventArgs e)
        {
            var staticItems = toolStrip1.Items.Count;
            foreach (var driveInfo in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                toolStrip1.Items.Insert(toolStrip1.Items.Count - staticItems,
                                        new ToolStripButton(driveInfo.Name, null,
                                                            (o, ea) => LoadDrive((ToolStripItem) o)));
            }
            toolStripComboBox1.SelectedIndex = 1;
            toolStripComboBox2.SelectedIndex = 4;
            splitContainer1.SplitterDistance = splitContainer1.Width - LogicalToDeviceUnits(splitContainer1.Width - splitContainer1.SplitterDistance);
        }

        

        private async void LoadDrive(ToolStripItem sender)
        {
            var target = sender.Text.Substring(0, 2);
            _scanner = new DriveScanner();

            toolStrip1.Enabled = false;
            timer1.Start();

            var root = await Task.Run(() => _scanner.ScanDrive(target));

            listBox1.Items.Clear();
            listBox1.Items.AddRange(_scanner.Inaccessible.Cast<object>().ToArray());

            _totals.Clear();

            chart1.BeginInit();
            chart1.ChartAreas.Clear();
            chart1.Series.Clear();

            label2.Text = Humanize.Size(root.Items[1].Size);

            var percent = 0.0025f*toolStripComboBox2.SelectedIndex;
            if (toolStripComboBox1.SelectedIndex == 1)
            {
                root.Items.RemoveRange(0, 2);
                _filterThreshold = _scanner.GetDisplayThreshold(percent, false);
            }
            else
            {
                _filterThreshold = _scanner.GetDisplayThreshold(percent, true);
            }
            LoadChartDataCollection(0, root, 0);
            AlignDoughnuts();
            chart1.EndInit();

            timer1.Stop();
            toolStripProgressBar1.Value = 0;
            toolStripLabel2.Text = string.Empty;
            toolStrip1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripProgressBar1.Value = Math.Min((int) (_scanner.Progress*10), toolStripProgressBar1.Maximum);
            toolStripLabel2.Text = _scanner.CurrentScanned;
        }

        private readonly Dictionary<Series, long> _totals = new Dictionary<Series, long>();

        private void LoadChartDataCollection(int dataLevel, FsItem dataPoint, long precedingObjectSize)
        {
            Series ser;
            if (!TryGetDataSeries(dataLevel, dataPoint, out ser)) return;

            if (precedingObjectSize > 0)
            {
                var delta = precedingObjectSize - _totals[ser];
                if (delta > 0)
                {
                    AddOrExtendPlaceHolder(delta, ser);
                }
            }

            foreach (var point in dataPoint.Items)
            {
                if (point.Size > _filterThreshold)
                {
                    AddPoint(ser, point.Size, point);
                }
                else
                {
                    AddOrExtendPlaceHolder(point.Size, ser);
                }
                if (point.Items != null && point.Items.Count > 0)
                {
                    LoadChartDataCollection(dataLevel + 1, point, precedingObjectSize);
                }
                precedingObjectSize += point.Size;
            }
            LoadChartDataCollection(dataLevel + 1, Empty, precedingObjectSize);
        }

        private void AddOrExtendPlaceHolder(long size, Series series)
        {
            if (series.Points.Count > 0 && series.Points[series.Points.Count - 1].Tag.Equals(PlaceholderTag))
            {
                series.Points[series.Points.Count - 1].YValues[0] += size;
                _totals[series] += size;
            }
            else
            {
                var point = AddPoint(series, size, PlaceholderTag);
                point.Color = Color.FloralWhite;
            }
        }

        private DataPoint AddPoint(Series series, long size, object tag)
        {
            var point = new DataPoint
            {
                YValues = new[] {(double) size},
                Tag = tag
            };
            series.Points.Add(point);
            _totals[series] += size;
            return point;
        }

        private bool TryGetDataSeries(int dataLevel, FsItem dataPoint, out Series ser)
        {
            if (chart1.ChartAreas.Count == dataLevel)
            {
                if (dataPoint == Empty)
                {
                    ser = null;
                    return false;
                }

                //create chart area and series
                var ca = new ChartArea("chartAreaLevel" + dataLevel)
                {
                    Position =
                    {
                        Auto = false,
                        X = 0,
                        Y = 0,
                        Height = 100,
                        Width = 100
                    }
                };
                if (dataLevel > 0)
                {
                    ca.BackColor = Color.Transparent;
                }
                chart1.ChartAreas.Add(ca);

                ser = new Series("seriesLevel" + dataLevel)
                {
                    ChartArea = ca.Name,
                    ChartType = SeriesChartType.Doughnut,
                    IsXValueIndexed = true
                };
                chart1.Series.Add(ser);
                _totals.Add(ser, 0);
            }
            else
            {
                ser = chart1.Series[dataLevel];
            }
            return true;
        }

        private static readonly FsItem Empty = new FsItem(null, 0, false) {Items = new List<FsItem>()};
        private const string PlaceholderTag = "Placeholder";

        private void AlignDoughnuts()
        {
            for (int i = chart1.Series.Count - 1; i >= 0; i--)
            {
                var totalVisible = chart1.Series[i].Points.Sum(p => p.Tag.Equals(PlaceholderTag) ? 0 : p.YValues[0]);
                if (totalVisible <= _filterThreshold)
                {
                    chart1.Series.RemoveAt(i);
                }
            }
            var singleWidth = 85.0/chart1.Series.Count;
            for (int i = 0; i < chart1.Series.Count; i++)
            {
                chart1.Series[i].CustomProperties = "PieStartAngle=270, DoughnutRadius=" + (int) (85 - singleWidth*i);
            }
        }

        private Point _last;
        private HitTestResult[] _lastObjects;
        private string _lastTip;

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_last == e.Location) return;
            _last = e.Location;

            contextMenuStrip1.Hide();
            var objectUnder = chart1.HitTest(e.X, e.Y, true, ChartElementType.DataPoint);
            if (objectUnder.Length > 0)
            {
                if (!CompareCollections(objectUnder))
                {
                    _lastObjects = objectUnder;
                    BuildToolTipText();
                }
                var offset = LogicalToDeviceUnits(_cursorSize/2);
                toolTip1.Show(_lastTip, chart1, (int)(e.X + offset*0.75), e.Y + offset);
            }
        }

        private void BuildToolTipText()
        {
            var fsItems = GetFsItemsArray();
            var list = new List<string>();
            var builder = new StringBuilder();
            for (int j = 0; j < fsItems.Length; j++)
            {
                for (int i = j; i < fsItems.Length - 1; i++)
                {
                    builder.Append(">");
                }
                var fsItem = fsItems[j];
                builder.AppendFormat("{0}: {1}", fsItem.Name, Humanize.FsItem(fsItem));
                list.Add(builder.ToString());
                builder.Clear();
            }
            _lastTip = string.Join("\r\n", list);
        }

        private FsItem[] GetFsItemsArray()
        {
            return _lastObjects.Where(o => o.Object != null && o.ChartElementType == ChartElementType.DataPoint)
                               .Select(o => ((DataPoint) o.Object).Tag as FsItem)
                               .Where(t => t != null)
                               .ToArray();
        }

        private bool CompareCollections(HitTestResult[] result)
        {
            if ((_lastObjects == null ^ result == null) || result == null)
                return false;
            if (_lastObjects.Length != result.Length)
                return false;
            for (int i = 0; i < result.Length; i++)
            {
                if (_lastObjects[i].Object != result[i].Object)
                    return false;
            }
            return true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed ^= true;
        }

        private void contextMenuStrip1_Opened(object sender, EventArgs e)
        {
            var cached = GetFsItemsArray();
            if (cached.Length == 0)
            {
                contextMenuStrip1.Enabled = false;
                return;
            }

            contextMenuStrip1.Enabled = true;
            var builder = new StringBuilder(_scanner.CurrentTarget);
            for (int i = cached.Length - 1; i >= 0; i--)
            {
                builder.Append(Path.DirectorySeparatorChar);
                builder.Append(cached[i].Name);
            }
            contextMenuStrip1.Tag = builder.ToString();
            builder.AppendFormat("{0}{1}; {2}{0}(hover this tooltip to return back to search mode)",
                                 Environment.NewLine,
                                 cached[0].IsDir ? "Folder" : "File",
                                 Humanize.FsItem(cached[0]));
            toolTip1.Show(builder.ToString(),
                          chart1,
                          chart1.PointToClient(new Point(contextMenuStrip1.Left, contextMenuStrip1.Top - LogicalToDeviceUnits(52 + (int)Math.Ceiling(DeviceDpi/96.0)))));
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartExplorerSelect((string)contextMenuStrip1.Tag);
        }

        public static void StartExplorerSelect(string objectToSelect)
        {
            StartExplorer("/select,\"" + objectToSelect + "\"");
        }

        public static void StartExplorer(string command = null)
        {
            const string explorerString = "explorer.exe";
            Process.Start(explorerString, command);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var path = (string) contextMenuStrip1.Tag;
            int i = 0;
            if (File.Exists(path))
            {
                i = 1;
            }
            else if (Directory.Exists(path))
            {
                i = 2;
            }
            if (i == 0)
            {
                MessageBox.Show("Object is already unavailable.");
                return;
            }
            if (
                MessageBox.Show("Are you sure you want to delete " + path, "Confirm operation", MessageBoxButtons.YesNo) ==
                DialogResult.No)
            {
                return;
            }
            try
            {
                if (i == 1)
                {
                    File.Delete(path);
                }
                else
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred: " + ex);
            }
        }
    }
}
