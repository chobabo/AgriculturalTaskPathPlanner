using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.WindowsForms;
using OpenTK;
using IAM.GisLib;
using System.Net.Sockets;
using Algorithm;
using System.Runtime.InteropServices;

namespace PathCreator
{
    public delegate void ChangeCenterPositionDelegate(double _lat, double _lon);

    public partial class FormMain : Form
    {
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);
        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);

        #region variables
        private FormPath form_path = null;
        private FormGmap form_gmap = null;
        private List<string> corners_string = null;

        public int selected_roller_line_num = 0;
        public int selected_line_num = 0;
        public Vector3[] converted_abs_corners = null;
        public double std_orientation_deg = 0.0;
        public double avg_field_length = 0.0;
        public double avg_field_width = 0.0;
        public double field_area = 0.0;
        public double avg_field_orientation = 0.0;

        public List<IAM.GisLib.LatLonPoint[]> map_corners = new List<IAM.GisLib.LatLonPoint[]>();
        public List<string> map_name = new List<string>();
        public ChangeCenterPositionDelegate ChangeCenterPositionCallBack;
        public PointLatLng double_clicked_pt;
        public PointLatLng routes_start_pt = new PointLatLng();
        public PointLatLng routes_end_pt = new PointLatLng();
        #endregion

        #region constructor
        public FormMain()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            this.DesktopLocation = new Point(0, 0);

            AddMapInfo();

            //2019-07-31: GPS 프로젝션 존을 설정할 수 있도록 변경
            numericUpDown_GPS_Projection_Zone.Value = new decimal(Properties.Settings.Default.GPS_Projection_Zone);
            int gps_projection_zone = Convert.ToInt32(numericUpDown_GPS_Projection_Zone.Value);
            InitializationOfGnss(1, gps_projection_zone);

            comboBoxSelectMap.SelectedIndex = 0;    //default selected index: 0
            comboBox_path_type.SelectedIndex = 0;   //default
            comboBox_entrance.SelectedIndex = 0;    //right
            comboBox_map_type.SelectedIndex = 0;    //GoogleChinaSatelliteMap
            comboBox_map_direction.SelectedIndex = 0;
            comboBox_direction_option_avoidhighways.SelectedIndex = 0;
            comboBox_direction_option_avoidTolls.SelectedIndex = 0;
            comboBox_direction_option_metric.SelectedIndex = 0;
            comboBox_direction_option_sensor.SelectedIndex = 0;
            comboBox_direction_option_workingMode.SelectedIndex = 0;

            form_path = new FormPath(this);
            form_path.Owner = this;
        }
        #endregion

        #region methods
        /// <summary>
        /// Latitude and longitude < > Plane Rectangular Coordinate system
        /// http://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/algorithm/bl2xy/bl2xy.htm
        /// </summary>
        /// <param name="_gps_projection_type"></param>
        /// <param name="_gps_projection_zone"></param>
        private void InitializationOfGnss(int _gps_projection_type, int _gps_projection_zone)
        {
            IAM.GisLib.GlobalPoint.TypeOfProjection = (IAM.GisLib.ProjectionTypes)_gps_projection_type;
            IAM.GisLib.GlobalPoint.ProjectionZone = _gps_projection_zone;
        }

        private void InitializeParamOfMap()
        {
            //convert LatLng to EabsNabs
            int c_cnt = map_corners[comboBoxSelectMap.SelectedIndex].Length;
            converted_abs_corners = new Vector3[c_cnt];

            for (int i = 0; i < c_cnt; i++)
            {
                //2018-04-13
                IAM.GisLib.GlobalPoint gp = new GlobalPoint(map_corners[comboBoxSelectMap.SelectedIndex][i]);

                converted_abs_corners[i].X = (float)gp.X;
                converted_abs_corners[i].Y = (float)gp.Y;
                converted_abs_corners[i].Z = 0.0f;
            }

            //debug
            textBox_field_corners_0_eabs.Text = converted_abs_corners[0].X.ToString();
            textBox_field_corners_0_nabs.Text = converted_abs_corners[0].Y.ToString();
            textBox_field_corners_1_eabs.Text = converted_abs_corners[1].X.ToString();
            textBox_field_corners_1_nabs.Text = converted_abs_corners[1].Y.ToString();
            textBox_field_corners_2_eabs.Text = converted_abs_corners[2].X.ToString();
            textBox_field_corners_2_nabs.Text = converted_abs_corners[2].Y.ToString();
            textBox_field_corners_3_eabs.Text = converted_abs_corners[3].X.ToString();
            textBox_field_corners_3_nabs.Text = converted_abs_corners[3].Y.ToString();

            textBox_field_corners_integrated.Text =
                textBox_field_corners_0_eabs.Text + "," + textBox_field_corners_0_nabs.Text + "," + "0.0" + "," +
                textBox_field_corners_1_eabs.Text + "," + textBox_field_corners_1_nabs.Text + "," + "0.0" + "," +
                textBox_field_corners_2_eabs.Text + "," + textBox_field_corners_2_nabs.Text + "," + "0.0" + "," +
                textBox_field_corners_3_eabs.Text + "," + textBox_field_corners_3_nabs.Text + "," + "0.0";

            //avg length
            double l1 = Math.Sqrt(Math.Pow(converted_abs_corners[0].X - converted_abs_corners[1].X, 2.0) + Math.Pow(converted_abs_corners[0].Y - converted_abs_corners[1].Y, 2.0));
            double l2 = Math.Sqrt(Math.Pow(converted_abs_corners[2].X - converted_abs_corners[3].X, 2.0) + Math.Pow(converted_abs_corners[2].Y - converted_abs_corners[3].Y, 2.0));
            avg_field_length = (l1 + l2) / 2.0;
            textBox_debug_field_length.Text = avg_field_length.ToString();

            //standard orientation deg
            if (l1 == l2) { std_orientation_deg = l1; }
            else if(l1 > l2) { std_orientation_deg = l1; }
            else if(l2 > l1) { std_orientation_deg = l2; }

            //avg width
            double w1 = Math.Sqrt(Math.Pow(converted_abs_corners[0].X - converted_abs_corners[3].X, 2.0) + Math.Pow(converted_abs_corners[0].Y - converted_abs_corners[3].Y, 2.0));
            double w2 = Math.Sqrt(Math.Pow(converted_abs_corners[1].X - converted_abs_corners[2].X, 2.0) + Math.Pow(converted_abs_corners[1].Y - converted_abs_corners[2].Y, 2.0));
            avg_field_width = (w1 + w2) / 2.0;
            textBox_debug_field_width.Text = avg_field_width.ToString();

            //area
            field_area = avg_field_length * avg_field_width;
            textBox_debug_field_area.Text = field_area.ToString();

            //orientation degree
            double orientation1 = Math.Atan2(converted_abs_corners[1].Y - converted_abs_corners[0].Y, converted_abs_corners[1].X - converted_abs_corners[0].X) * 180.0 / Math.PI;
            double orientation2 = Math.Atan2(converted_abs_corners[2].Y - converted_abs_corners[3].Y, converted_abs_corners[2].X - converted_abs_corners[3].X) * 180.0 / Math.PI;
            avg_field_orientation = (orientation1 + orientation2) / 2.0;
            textBox_debug_field_orientation.Text = avg_field_orientation.ToString();
        }

        private void AddMapInfo()
        {
            if (ReadMapCornersPoint() == true)
            {
                for (int i = 0; i < corners_string.Count; i++)
                {
                    string[] s_data = corners_string[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    comboBoxSelectMap.Items.Add(s_data[0]);

                    int corners_count = (int)(s_data.Length / 3);
                    //Vector3[] c_pts = new Vector3[corners_count];
                    IAM.GisLib.LatLonPoint[] c_pts = new LatLonPoint[corners_count];

                    map_name.Add(s_data[0]);
                    for (int j = 0; j < corners_count; j++)
                    {
                        //2018-04-13
                        c_pts[j].Lat = Convert.ToDouble(s_data[j * 3 + 1]);  //latitude
                        c_pts[j].Lon = Convert.ToDouble(s_data[j * 3 + 2]);  //longitude
                        c_pts[j].Z = Convert.ToDouble(s_data[j * 3 + 3]);  //altitude    
                    }
                    map_corners.Add(c_pts);
                }
            }
        }

        private bool ReadMapCornersPoint()
        {
            bool is_success = false;

            corners_string = new List<string>();
            corners_string.Clear();

            // get the current absolute path
            Uri startupPath = new Uri(Application.StartupPath);

            // get the absolute path to 'z_fielddata' directory using relative path from the current path
            string root = "../../z_map_corners/corners.txt";
            Uri targetPath = new Uri(startupPath, root);

            string path = targetPath.ToString().Substring(8);

            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    while (sr.Peek() >= 0)
                    {
                        string tmp_corners = sr.ReadLine();
                        string[] s_data = tmp_corners.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (!s_data[0].Contains("#"))
                        {
                            corners_string.Add(tmp_corners);
                        }
                    }

                    is_success = true;
                }
            }
            catch (Exception e)
            {
                string error = "the process failed: " + e.ToString();
                is_success = false;
            }

            return is_success;
        }

        private bool ReadFileData(out List<string> readData)
        {
            bool isSuccess = false;

            readData = new List<string>();
            readData.Clear();

            openFileDialog_readdata.Multiselect = false;
            openFileDialog_readdata.Title = "Please select targetpath(xxx.txt)";

            // get the current absolute path
            Uri startupPath = new Uri(Application.StartupPath);
            Uri targetPath = new Uri(startupPath, "../../");

            string name = targetPath.ToString().Substring(8);
            openFileDialog_readdata.InitialDirectory = name;

            if (openFileDialog_readdata.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(openFileDialog_readdata.FileName))
                    {
                        while (sr.Peek() >= 0)
                        {
                            readData.Add(sr.ReadLine());
                        }

                        isSuccess = true;
                    }
                }
                catch (Exception e)
                {
                    string error = "the process failed: " + e.ToString();
                    isSuccess = false;
                }
            }

            return isSuccess;
        }

        #endregion

        #region event
        private void comboBoxSelectMap_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitializeParamOfMap();

            if (form_gmap != null)
            {
                //2018-03-14
                ChangeCenterPositionCallBack(
                    map_corners[comboBoxSelectMap.SelectedIndex][0].Lat,
                    map_corners[comboBoxSelectMap.SelectedIndex][0].Lon);
            }
        }

        private void button_open_map_Click(object sender, EventArgs e)
        {
            if (button_open_map.BackColor == Color.Yellow)
            {
                InitializeParamOfMap();

                int map_type = comboBox_map_type.SelectedIndex;

                form_gmap = new FormGmap(this, map_type);
                form_gmap.Owner = this;
                form_gmap.Show();

                button_open_map.BackColor = Color.GreenYellow;
                button_open_map.Text = "Close";
            }
            else
            {
                form_gmap.Close();

                button_open_map.BackColor = Color.Yellow;
                button_open_map.Text = "Open";
            }
        }

        private void button_add_route_Click(object sender, EventArgs e)
        {
            form_path.CreatePath();
            //form_gmap.DrawPath(form_path.absolute_makurazi, "makurazi");
            //form_gmap.DrawPath(form_path.absolute_working, "working");
            form_gmap.DrawPath(form_path.total_absolute_path, "total");
        }

        private void button_delete_one_line_Click(object sender, EventArgs e)
        {
            //form_path.RemoveLastTargetPath();
            //form_gmap.DrawPath(form_path.absolute_working, "working");
            form_path.RemoveSelectedPath(selected_line_num);
            
            form_gmap.DrawPath(form_path.total_absolute_path, "total");     //2019-07-31
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            List<string> save_str = new List<string>();
            save_str.Clear();

            //[0]
            save_str.Add("path_name");

            //[1]
            save_str.Add(comboBoxSelectMap.Text);

            //[2]
            save_str.Add("index,relative_x,relative_y,absolute_x,absolute_y");

            //[3]corner0, [4]corner1, [5]corner2, [6]corner3
            for (int i = 0; i < converted_abs_corners.Length; i++)
            {
                float r_x = converted_abs_corners[i].X - converted_abs_corners[0].X;
                float r_y = converted_abs_corners[i].Y - converted_abs_corners[0].Y;

                save_str.Add(
                    i.ToString() + "," +
                    r_x.ToString() + "," +
                    r_y.ToString() + "," +
                    converted_abs_corners[i].X.ToString() + "," +
                    converted_abs_corners[i].Y.ToString()
                    );
            }

            //[7]corner0
            save_str.Add("4" + "," + "0.0" + "," + "0.0" + "," + converted_abs_corners[0].X.ToString() + "," + converted_abs_corners[0].Y.ToString());

            //[8]total path count
            save_str.Add(form_path.total_absolute_path.Count.ToString());

            //[9]
            save_str.Add("index,relative_x,relative_y,absolute_x,absolute_y,none,none,path_type");

            //[10]~[end]
            for (int i = 0; i < form_path.total_absolute_path.Count; i++)
            {
                float r_x = form_path.total_absolute_path[i].X - converted_abs_corners[0].X;
                float r_y = form_path.total_absolute_path[i].Y - converted_abs_corners[0].Y;

                save_str.Add(
                    i.ToString() + "," +
                    form_path.total_absolute_path[i].X.ToString() + "," +
                    form_path.total_absolute_path[i].Y.ToString() + "," +
                    r_x.ToString() + "," +
                    r_y.ToString() + "," +
                    "0.0" + "," + 
                    "0.0" + "," +
                    form_path.total_path_type[i].ToString()
                    );
            }

            // get the current absolute path
            Uri startupPath = new Uri(Application.StartupPath);

            // get the absolute path to 'z_fielddata' directory using relative path from the current path
            Uri targetPath = new Uri(startupPath, "../../z_map_kurita_format/");

            string name = targetPath.ToString().Substring(8) + "path_" + comboBoxSelectMap.Text + ".txt";

            using (StreamWriter sw = new StreamWriter(name))
            {
                for (int i = 0; i < save_str.Count; i++)
                {
                    sw.WriteLine(save_str[i]);
                }
            }
        }

        private void numericUpDown_select_rows_ValueChanged(object sender, EventArgs e)
        {
            selected_line_num = Convert.ToInt32(numericUpDown_select_rows.Value);
            //form_gmap.DrawSelectedRow(form_path.absolute_working, selected_line_num);
            form_gmap.DrawSelectedRow(form_path.total_absolute_path, selected_line_num);

            if (selected_line_num < form_path.total_absolute_path.Count - 1)
            {
                textBox_selected_line_start_x.Text = form_path.total_absolute_path[selected_line_num].X.ToString();
                textBox_selected_line_start_y.Text = form_path.total_absolute_path[selected_line_num].Y.ToString();
                textBox_selected_line_end_x.Text = form_path.total_absolute_path[selected_line_num + 1].X.ToString();
                textBox_selected_line_end_y.Text = form_path.total_absolute_path[selected_line_num + 1].Y.ToString();
            }
        }

        private void numericUpDown_line_extension_ValueChanged(object sender, EventArgs e)
        {
            
        }

        private void button_resize_line_Click(object sender, EventArgs e)
        {
            form_path.LineExtension(selected_line_num, comboBox_revise_start_end.Text, Convert.ToDouble(numericUpDown_line_extension.Value));
            form_gmap.DrawPath(form_path.total_absolute_path, "total");     //2019-07-31
        }

        private void button_update_Click(object sender, EventArgs e)
        {          
            form_gmap.DrawPath(form_path.total_absolute_path, "total");
        }

        private void button_add_line_Click(object sender, EventArgs e)
        {
            Vector3 start_pt = new Vector3(
                (float)(Convert.ToDouble(textBox_start_x.Text)),
                (float)(Convert.ToDouble(textBox_start_y.Text)),
                0.0f
                );

            Vector3 end_pt = new Vector3(
                (float)(Convert.ToDouble(textBox_end_x.Text)),
                (float)(Convert.ToDouble(textBox_end_y.Text)),
                0.0f
                );

            form_path.total_absolute_path.Insert(selected_line_num + 2, end_pt);
            form_path.total_absolute_path.Insert(selected_line_num + 2, start_pt);

            form_path.total_path_type.Insert(selected_line_num + 2, Convert.ToInt32(textBox_path_type.Text));
            form_path.total_path_type.Insert(selected_line_num + 2, Convert.ToInt32(textBox_path_type.Text));
        }

        private void button_create_roller_default_target_Click(object sender, EventArgs e)
        {
            form_path.CreatePath();
            form_gmap.DrawPath(form_path.absolute_working, "roller_working");
        }

        private void numericUpDown_select_roller_path_ValueChanged(object sender, EventArgs e)
        {
            selected_roller_line_num = Convert.ToInt32(numericUpDown_select_roller_path.Value);
            form_gmap.DrawSelectedRow(form_path.absolute_working, selected_roller_line_num);
        }

        private void button_add_roller_line_Click(object sender, EventArgs e)
        {
            form_path.AddRollerPath(selected_roller_line_num);
        }

        private void button_read_Click(object sender, EventArgs e)
        {
            form_path.total_absolute_path.Clear();
            form_path.total_path_type.Clear();

            List<string> readData;

            bool is_read = ReadFileData(out readData);
            if (is_read == true)
            {
                for (int i = 0; i < readData.Count - 10; i++)
                {
                    string[] s_data = readData[i + 10].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Vector3 absolute_pt = new Vector3(
                        (float)(Convert.ToDouble(s_data[1])),
                        (float)(Convert.ToDouble(s_data[2])),
                        0.0f
                        );
                    int p_type = Convert.ToInt32(s_data[7]);

                    form_path.total_absolute_path.Add(absolute_pt);
                    form_path.total_path_type.Add(p_type);
                }

                form_gmap.DrawPath(form_path.total_absolute_path, "total");
            }        
        }

        private void button_insert_route_start_pt_Click(object sender, EventArgs e)
        {
            routes_start_pt.Lat = double_clicked_pt.Lat;
            routes_start_pt.Lng = double_clicked_pt.Lng;

            textBox_route_start_lat.Text = routes_start_pt.Lat.ToString();
            textBox_route_start_lon.Text = routes_start_pt.Lng.ToString();
        }

        private void button_insert_route_end_pt_Click(object sender, EventArgs e)
        {
            routes_end_pt.Lat = double_clicked_pt.Lat;
            routes_end_pt.Lng = double_clicked_pt.Lng;

            textBox_route_end_lat.Text = routes_end_pt.Lat.ToString();
            textBox_route_end_lon.Text = routes_end_pt.Lng.ToString();
        }

        private void button_create_route_Click(object sender, EventArgs e)
        {
            int tail_num = Convert.ToInt32(numericUpDown_route_tail_name.Value);
            bool avoidhighways = false;
            bool avoidTolls = false;
            bool workingMode = false;
            bool sensor = false;
            bool metric = false;

            if(comboBox_direction_option_avoidhighways.SelectedIndex == 1) { avoidhighways = true; }

            if(comboBox_direction_option_avoidTolls.SelectedIndex == 1) { avoidTolls = true; }

            if(comboBox_direction_option_workingMode.SelectedIndex == 1) { workingMode = true; }

            if(comboBox_direction_option_sensor.SelectedIndex == 1) { sensor = true; }

            if(comboBox_direction_option_metric.SelectedIndex == 1) { metric = true; }

            form_gmap.CreateDrivingRoute(routes_start_pt, routes_end_pt, comboBox_map_type.SelectedIndex, tail_num, avoidhighways, avoidTolls, workingMode, sensor, metric);

            numericUpDown_route_tail_name.Value++;
        }
        #endregion

        #region sps855 tcp ip

        private System.Net.Sockets.TcpClient clientSocket;
        private System.Net.Sockets.NetworkStream stream;

        private bool is_connect_sps855 = false;
        private bool is_stream = false;
        private int rev_gps_cnt = 0;

        private string current_nmea_data = null;
        private List<string> sps855_data_list = new List<string>();
        private List<string> sps855_id_list = new List<string>();


        private void InitSps855Socket(string _hostname, int _port)
        {
            this.clientSocket = new System.Net.Sockets.TcpClient();

            try
            {
                if (this.clientSocket.Connected == false)
                {
                    this.clientSocket.Connect(textBox_sps855_ip.Text, Convert.ToInt32(textBox_sps855_port.Text));
                    label_sps855_debug_msg.Text = "ready";
                }

                if ((this.clientSocket.Connected == true) && (this.is_stream == false))
                {
                    this.stream = this.clientSocket.GetStream();
                    this.is_stream = true;
                    label_sps855_debug_msg.Text = "connected";
                }
            }
            catch (SocketException se)
            {
                label_sps855_debug_msg.Text = se.Message;
            }
            catch (Exception ex)
            {
                label_sps855_debug_msg.Text = ex.Message;
            }
        }

        private void CloseSps855()
        {
            this.clientSocket.Close();
            this.is_stream = false;
            label_sps855_debug_msg.Text = "closed";
        }

        private string ReceiveDataSps855()
        {
            byte[] rBuffer = new byte[1024];
            this.stream.Read(rBuffer, 0, rBuffer.Length);
            string result = Encoding.Default.GetString(rBuffer);

            if (result != null)
            {
                label_sps855_debug_msg.Text = "Received data";
            }
            else
            {
                label_sps855_debug_msg.Text = "Cannot receive data";
            }

            return result;
        }

        private string ConvertNMEA(int _id, string _nmea)
        {
            string result = null;

            char[] delimiterChars = { ',' };
            string[] fields = _nmea.Split(delimiterChars);

            //Latitude: fields[4]
            double latitude = this.ConvertMinToSec(fields[4]);

            //longitude: fields[6]
            double longitude = this.ConvertMinToSec(fields[6]);

            IAM.GisLib.GlobalPoint gp = new GlobalPoint(new IAM.GisLib.LatLonPoint(latitude, longitude, 0.0));
            double Nabs = gp.Y;
            double Eabs = gp.X;

            result =
                Convert.ToString(_id) + "," +
                Convert.ToString(latitude) + "," +
                Convert.ToString(longitude) + "," +
                Convert.ToString(Nabs) + "," +
                Convert.ToString(Eabs);

            return result;
        }

        private double ConvertMinToSec(string field)
        {
            double val = 0.0;

            int index = field.IndexOf(".");
            if (index == 4)
            {
                val = Convert.ToDouble(field.Substring(0, 2)) + (Convert.ToDouble(field.Substring(2)) / 60.0);
            }
            else if (index == 5)
            {
                val = Convert.ToDouble(field.Substring(0, 3)) + (Convert.ToDouble(field.Substring(3)) / 60.0);
            }

            return val;
        }

        private void button_sps855_connect_Click(object sender, EventArgs e)
        {
            if (this.is_connect_sps855 == false)
            {
                this.InitSps855Socket(textBox_sps855_ip.Text, Convert.ToInt32(textBox_sps855_port.Text));

                this.is_connect_sps855 = true;
                button_sps855_connect.Text = "Connected";
                button_sps855_connect.BackColor = Color.Green;

                this.timer_sps855.Interval = 100;
                this.timer_sps855.Enabled = true;

                this.textBox_sps855_nmea.Clear();
                rev_gps_cnt = 0;
            }
            else
            {
                this.is_connect_sps855 = false;
                button_sps855_connect.Text = "Disconnect";
                button_sps855_connect.BackColor = Color.Red;

                this.timer_sps855.Enabled = false;
                this.CloseSps855();
            }
        }

        private void timer_sps855_Tick(object sender, EventArgs e)
        {
            rev_gps_cnt++;
            //this.textBox_sps855_nmea.Text += ReceiveDataSps855() + System.Environment.NewLine;
            this.current_nmea_data = ReceiveDataSps855();
            this.textBox_sps855_nmea.Text = Convert.ToString(rev_gps_cnt) + ": " + this.current_nmea_data;
        }

        private void button_sps855_add_data_Click(object sender, EventArgs e)
        {
            string converted_data = this.ConvertNMEA(Convert.ToInt32(numericUpDown_sps855_id.Value), this.current_nmea_data);
            this.sps855_data_list.Add(converted_data);

            int current_id = Convert.ToInt32(numericUpDown_sps855_id.Value);
            int next_id = current_id + 1;
            string converted_id = Convert.ToString(current_id) + "," + Convert.ToString(next_id);
            this.sps855_id_list.Add(converted_id);

            numericUpDown_sps855_id.Value += 1;
            label_sps855_debug_msg.Text = "DATA added";
        }

        private void button_sps855_save_file_Click(object sender, EventArgs e)
        {
            System.IO.File.WriteAllLines("C:\\RoboData\\raw_gps.csv", this.sps855_data_list);
            System.IO.File.WriteAllLines("C:\\RoboData\\list_id.csv", this.sps855_id_list);

            label_sps855_debug_msg.Text = "SAVE SUCCESS";
        }

        #endregion

        #region Global path using waypoint: 2019-04-03
        public GlobalPath g_path { get; set; }
        public SearchEngine SearchEngine { get; set; }

        private void button_globalpath_read_waypoint_Click(object sender, EventArgs e)
        {
            this.g_path = GlobalPath.ReadNodes();
            this.label_globalpath_debug_msg.Text = "Length of waypoint is " + this.g_path.Nodes.Count.ToString();

            this.SearchEngine = new SearchEngine(this.g_path);

            form_gmap.DrawDrivingWayPointMarker(this.g_path);
        }

        private void button_globalpath_view_waypoint_Click(object sender, EventArgs e)
        {
            int node_idx = Convert.ToInt32(numericUpDown_node_id.Value);

            textBox_rcv_node_id.Text = this.g_path.Nodes[node_idx].Id.ToString();
            textBox_rcv_node_lat.Text = this.g_path.Nodes[node_idx].GpsPoint.Latitude.ToString();
            textBox_rcv_node_lon.Text = this.g_path.Nodes[node_idx].GpsPoint.Longitude.ToString();
            textBox_rcv_node_x.Text = this.g_path.Nodes[node_idx].GpsPoint.X.ToString();
            textBox_rcv_node_y.Text = this.g_path.Nodes[node_idx].GpsPoint.Y.ToString();

            int edges_len = this.g_path.Nodes[node_idx].Edges_id.Count;
            string edges_str = null;
            for (int i = 0; i < edges_len; i++)
            {
                //edges_str += this.g_path.Nodes[node_idx].Edges_id[i].ToString() + ",";
                edges_str +=
                    this.g_path.Nodes[node_idx].Connections[i].ConnectedNode.Id.ToString() + ":" +
                    this.g_path.Nodes[node_idx].Connections[i].Cost.ToString();

                if (i < edges_len - 1) { edges_str += "," + Environment.NewLine; }
            }

            textBox_rcv_node_edges.Text = edges_str;
        }

        private void button_globalpath_find_path_astar_Click(object sender, EventArgs e)
        {
            textBox_result_path_astar.Clear();

            long startSW = 0;
            QueryPerformanceCounter(ref startSW);
            long freq = 0;
            QueryPerformanceFrequency(ref freq);

            var map= GlobalPath.ReadNodes();
            this.label_globalpath_debug_msg.Text = "Length of waypoint is " + map.Nodes.Count.ToString();

            var astar = new SearchEngine(map);

            astar.SetStartEnd(
                map.Nodes[Convert.ToInt32(numericUpDown_start_node.Value)],
                map.Nodes[Convert.ToInt32(numericUpDown_end_node.Value)]
                 );

            map.ShortestPath = astar.GetShortestPathAstar();

            long endSW = 0;
            QueryPerformanceCounter(ref endSW);
            double processing_time = (endSW - startSW) * 1.0 / freq;
            label_astar_processing_time.Text = processing_time.ToString("0.000000") + " [s]";

            for (int i = 0; i < map.ShortestPath.Count; i++)
            {
                textBox_result_path_astar.Text += map.ShortestPath[i].Id.ToString();

                if (i < map.ShortestPath.Count - 1) { textBox_result_path_astar.Text += " -> "; }
            }

            form_gmap.DrawDrivingRoadRoute(map);
        }

        #endregion

        #region Temp
        private void button_temp_tangent_Click(object sender, EventArgs e)
        {
            double temp_tan_1 = Math.Tan(Convert.ToDouble(textBox_temp_BCE.Text) * Math.PI / 180.0);
            double BC_1 = (1.6 / temp_tan_1) - 0.8;
            textBox_temp_BC.Text = BC_1.ToString("0.0");

            double BE_1 = temp_tan_1 * BC_1;
            textBox_temp_BE.Text = BE_1.ToString("0.0");
        }
        #endregion

        #region Load Road Boundary: 2019-07-02
        private bool is_load_road = false;
        private List<Road_link> road_Links = new List<Road_link>();                 //2019-04-08
        private List<Road_waypoint> road_Waypoints = new List<Road_waypoint>();     //2019-04-08

        /// <summary>
        /// 2019-07-02
        /// 도로 경계선을 읽어온다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_read_road_boundary_Click(object sender, EventArgs e)
        {
            if (is_load_road == false)
            {
                //get the current absolute path
                Uri startupPath = new Uri(Application.StartupPath);

                // read link
                // get the absolute path to 'z_fielddata' directory using relative path from the current path
                string link_root = "../../z_road/road_link.csv";
                Uri link_targetPath = new Uri(startupPath, link_root);

                string link_path = link_targetPath.ToString().Substring(8);

                try
                {
                    int i = 0;
                    using (StreamReader sr = new StreamReader(link_path))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string line = sr.ReadLine();
                            if (i != 0)
                            {
                                string[] s_line = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                road_Links.Add(new Road_link
                                {
                                    Start_link = Convert.ToInt32(s_line[0]),
                                    End_link = Convert.ToInt32(s_line[1])
                                }
                                );
                            }
                            i++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    string error = "The process failed: " + exception.ToString();
                }

                // read waypoint
                // get the absolute path to 'z_fielddata' directory using relative path from the current path
                string waypoint_root = "../../z_road/road_waypoint.csv";
                Uri waypoint_targetPath = new Uri(startupPath, waypoint_root);

                string waypoint_path = waypoint_targetPath.ToString().Substring(8);

                try
                {
                    int i = 0;
                    using (StreamReader sr = new StreamReader(waypoint_path))
                    {
                        while (sr.Peek() >= 0)
                        {
                            string line = sr.ReadLine();
                            if (i != 0)
                            {
                                string[] s_line = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                road_Waypoints.Add(new Road_waypoint
                                {
                                    Id = Convert.ToInt32(s_line[0]),
                                    Lattitude = Convert.ToDouble(s_line[1]),
                                    Longitute = Convert.ToDouble(s_line[2]),
                                    Eabs = Convert.ToDouble(s_line[4]),
                                    Nabs = Convert.ToDouble(s_line[3])
                                }
                                );
                            }
                            i++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    string error = "The process failed: " + exception.ToString();
                }

                var road_vectors = new List<PointLatLng>();

                for (int i = 0; i < road_Links.Count; i++)
                {
                    int start_id = road_Links[i].Start_link;
                    int end_id = road_Links[i].End_link;

                    road_vectors.Add(new PointLatLng(road_Waypoints[start_id].Lattitude, road_Waypoints[start_id].Longitute));      //start point
                    road_vectors.Add(new PointLatLng(road_Waypoints[end_id].Lattitude, road_Waypoints[end_id].Longitute));      //end point
                }

                form_gmap.DrawRoadBoundary(road_vectors);

                is_load_road = true;
            }
        }
        #endregion

        #region road path marker
        public bool is_add_gps_point = false;
        public PointLatLng current_gps_marker;

        /// <summary>
        /// 2019-07-03
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_gps_point_Click(object sender, EventArgs e)
        {
            if (is_add_gps_point == false)
            {
                button_add_gps_point.BackColor = Color.Aquamarine;
                is_add_gps_point = true;
            }
            else
            {
                button_add_gps_point.BackColor = Color.Red;
                is_add_gps_point = false;
            }
        }

        /// <summary>
        /// 2019-11-01
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_add_save_waypoint_Click(object sender, EventArgs e)
        {
        }
        #endregion

        #region find angle from three points
        private void textBox_result_find_angle_three_points_TextChanged(object sender, EventArgs e)
        {
        }

        private void button_find_angle_three_points_Click(object sender, EventArgs e)
        {
            double p1_x = Convert.ToDouble(textBox_three_points_angle_p1_x.Text);
            double p2_x = Convert.ToDouble(textBox_three_points_angle_p2_x.Text);
            double p3_x = Convert.ToDouble(textBox_three_points_angle_p3_x.Text);

            double p1_y = Convert.ToDouble(textBox_three_points_angle_p1_y.Text);
            double p2_y = Convert.ToDouble(textBox_three_points_angle_p2_y.Text);
            double p3_y = Convert.ToDouble(textBox_three_points_angle_p3_y.Text);

            textBox_result_find_angle_three_points.Text = Convert.ToString(calculateAngle(p1_x, p1_y, p2_x, p2_y, p3_x, p3_y));
        }

        /// <summary>
        /// 2019-07-25: 세점을 이용하여 내각을 구한다.
        /// http://phrogz.net/angle-between-three-points
        /// Center point is p2; angle returned in degrees
        /// </summary>
        /// <param name="P1X"></param>
        /// <param name="P1Y"></param>
        /// <param name="P2X"></param>
        /// <param name="P2Y"></param>
        /// <param name="P3X"></param>
        /// <param name="P3Y"></param>
        /// <returns>degree</returns>
        private double calculateAngle(double P1X, double P1Y, double P2X, double P2Y, double P3X, double P3Y)
        {

            //double numerator = P2Y * (P1X - P3X) + P1Y * (P3X - P2X) + P3Y * (P2X - P1X);
            //double denominator = (P2X - P1X) * (P1X - P3X) + (P2Y - P1Y) * (P1Y - P3Y);
            //double ratio = numerator / denominator;

            //double angleRad = Math.Atan(ratio);
            //double angleDeg = (angleRad * 180) / Math.PI;

            //if (angleDeg < 0)
            //{
            //    angleDeg = 180 + angleDeg;
            //}

            //return Math.Abs(angleDeg);

            double a = Math.Pow(P2X - P1X, 2.0) + Math.Pow(P2Y - P1Y, 2.0);
            double b = Math.Pow(P2X - P3X, 2.0) + Math.Pow(P2Y - P3Y, 2.0);
            double c = Math.Pow(P3X - P1X, 2.0) + Math.Pow(P3Y - P1Y, 2.0);

            double angleRad = Math.Acos((a + b - c) / Math.Sqrt(4 * a * b));
            double angleDeg = (angleRad * 180) / Math.PI;

            return angleDeg;
        }
        #endregion

        #region Find the point of intersection between the lines p1 --> p2 and p3 --> p4.
        private void button_result_intersection_point_Click(object sender, EventArgs e)
        {
            Vector2 p1 = new Vector2(Convert.ToSingle(textBox_intersection_point_p1_x.Text), Convert.ToSingle(textBox_intersection_point_p1_y.Text));
            Vector2 p2 = new Vector2(Convert.ToSingle(textBox_intersection_point_p2_x.Text), Convert.ToSingle(textBox_intersection_point_p2_y.Text));
            Vector2 p3 = new Vector2(Convert.ToSingle(textBox_intersection_point_p3_x.Text), Convert.ToSingle(textBox_intersection_point_p3_y.Text));
            Vector2 p4 = new Vector2(Convert.ToSingle(textBox_intersection_point_p4_x.Text), Convert.ToSingle(textBox_intersection_point_p4_y.Text));

            bool lines_intersect;
            bool segments_intersect;
            Vector2 intersection;
            Vector2 close_p1;
            Vector2 close_p2;

            FindIntersection(p1, p2, p3, p4, out lines_intersect, out segments_intersect, out intersection, out close_p1, out close_p2);

            textBox_result_intersection_point.Text = intersection.X.ToString() + " , " + intersection.Y.ToString();
        }

        /// <summary>
        /// 2019-07-25: Find the point of intersection between the lines p1 --> p2 and p3 --> p4.
        /// http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <param name="lines_intersect"></param>
        /// <param name="segments_intersect"></param>
        /// <param name="intersection"></param>
        /// <param name="close_p1"></param>
        /// <param name="close_p2"></param>
        private void FindIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out bool lines_intersect, out bool segments_intersect, out Vector2 intersection, out Vector2 close_p1, out Vector2 close_p2)
        {
            // Get the segments' parameters.
            float dx12 = p2.X - p1.X;
            float dy12 = p2.Y - p1.Y;
            float dx34 = p4.X - p3.X;
            float dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (float.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Vector2(float.NaN, float.NaN);
                close_p1 = new Vector2(float.NaN, float.NaN);
                close_p2 = new Vector2(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            float t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Vector2(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Vector2(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Vector2(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }
        #endregion

        #region Convert LatLon to XY
        private void button_LatLon_to_XY_Click(object sender, EventArgs e)
        {
            double temp_Lat = Convert.ToDouble(textBox_Convert_LatLon_to_XY_Lat.Text);
            double temp_lon = Convert.ToDouble(textBox_Convert_LatLon_to_XY_Lon.Text);
            LatLonPoint temp_lp = new LatLonPoint(temp_Lat, temp_lon, 0.0);
            IAM.GisLib.GlobalPoint gp = new GlobalPoint(temp_lp);

            textBox_Convert_LatLon_to_XY_X.Text = gp.x.ToString();
            textBox_Convert_LatLon_to_XY_Y.Text = gp.y.ToString();
        }

        private void button_XY_to_LatLon_Click(object sender, EventArgs e)
        {
            double temp_x = Convert.ToDouble(textBox_Convert_LatLon_to_XY_X.Text);
            double temp_y = Convert.ToDouble(textBox_Convert_LatLon_to_XY_Y.Text);
            IAM.GisLib.GlobalPoint gp = new GlobalPoint(temp_x, temp_y);

            textBox_Convert_LatLon_to_XY_Lat.Text = gp.Lat.ToString();
            textBox_Convert_LatLon_to_XY_Lon.Text = gp.Lon.ToString();
        }
        #endregion

        #region Gps Projection Zone setting event
        private void numericUpDown_GPS_Projection_Zone_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GPS_Projection_Zone = Convert.ToInt32(numericUpDown_GPS_Projection_Zone.Value);
        }

        private void button_GPS_Projection_Zone_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Read Trajectory Log
        public List<string> read_trajectory_log = null;

        private bool ReadTrajectoryLog(out List<string> readData)
        {
            int log_read_count = 0;
            bool isSuccess = false;

            readData = new List<string>();
            readData.Clear();

            openFileDialog_readdata.Multiselect = false;
            openFileDialog_readdata.Title = "Please select xxxxxxLogData.log";

            // get the current absolute path
            Uri startupPath = new Uri(Application.StartupPath);
            Uri targetPath = new Uri(startupPath, "../../");

            string name = targetPath.ToString().Substring(8);
            openFileDialog_readdata.InitialDirectory = name;

            if (openFileDialog_readdata.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(openFileDialog_readdata.FileName))
                    {
                        //2018-04-23
                        while (sr.Peek() >= 0)
                        {
                            if (log_read_count == 0)
                            {
                                sr.ReadLine();
                            }
                            else
                            {
                                readData.Add(sr.ReadLine());
                            }

                            log_read_count++;
                        }

                        isSuccess = true;
                    }
                }
                catch (Exception e)
                {
                    string error = "the process failed: " + e.ToString();
                    isSuccess = false;
                }
            }

            return isSuccess;
        }
        #endregion

        private void button_remainder_Click(object sender, EventArgs e)
        {
            int a = Convert.ToInt32(textBox_a.Text);
            int b = Convert.ToInt32(textBox_b.Text);

            int remainder = a % b;

            textBox_remainder.Text = remainder.ToString();
        }

        private void button_read_trajectory_log_Click(object sender, EventArgs e)
        {
            bool is_trajectory_log = ReadTrajectoryLog(out read_trajectory_log);

            if (is_trajectory_log)
            {
                form_gmap.DrawTrajectoryLog(read_trajectory_log);
            }
        }

        /// <summary>
        /// 2019-11-01
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_get_position_message_for_road_boundary_Click(object sender, EventArgs e)
        {
            double tmp_latitude;
            bool is_latitude = double.TryParse(textBox_gps_marker_lat.Text, out tmp_latitude);

            double tmp_longitude;
            bool is_longitude = double.TryParse(textBox_gps_marker_lon.Text, out tmp_longitude);

            double tmp_eabs, tmp_nabs;
            int point_cnt;

            if (is_latitude == true && is_longitude == true)
            {
                IAM.GisLib.GlobalPoint gp = new GlobalPoint(new LatLonPoint(tmp_latitude, tmp_longitude, 0.0));
                tmp_eabs = gp.X;
                tmp_nabs = gp.Y;

                point_cnt = Convert.ToInt32(numericUpDown_waypoint_create_node_id.Value);

                string all_msg =
                    point_cnt.ToString() + "," +
                    tmp_latitude.ToString() + "," +
                    tmp_longitude.ToString() + "," +
                    tmp_nabs.ToString() + "," +
                    tmp_eabs.ToString();

                textBox_get_waypoint_info.Text = all_msg;
            }
        }

        private void button_reeds_shepp_finding_Click(object sender, EventArgs e)
        {
            double start_lat = Convert.ToDouble(textBox_RS_Start_Lat.Text);
            double start_lon = Convert.ToDouble(textBox_RS_Start_Lon.Text);
            double start_heading = Convert.ToDouble(textBox_RS_Start_Heading.Text) * Math.PI / 180.0;

            double goal_lat = Convert.ToDouble(textBox_RS_Goal_Lat.Text);
            double goal_lon = Convert.ToDouble(textBox_RS_Goal_Lon.Text);
            double goal_heading = Convert.ToDouble(textBox_RS_Goal_Heading.Text) * Math.PI / 180.0;

            double turning_radius = Convert.ToDouble(textBox_RS_Turning_Radius.Text);
            double wp_dist = Convert.ToDouble(textBox_RS_Waypoint_Distance.Text);

            IAM.GisLib.GlobalPoint gp_start = new GlobalPoint(new LatLonPoint(start_lat, start_lon, 0.0));
            IAM.GisLib.GlobalPoint gp_goal = new GlobalPoint(new LatLonPoint(goal_lat, goal_lon, 0.0));

            Algorithm.ReedsShepp.Pose start_pose = new Algorithm.ReedsShepp.Pose((float)gp_start.x, (float)gp_start.y, (float)start_heading);
            Algorithm.ReedsShepp.Pose goal_pose = new Algorithm.ReedsShepp.Pose((float)gp_goal.x, (float)gp_goal.y, (float)goal_heading);

            Algorithm.ReedsShepp.ReedsSheppActionSet actionSet = Algorithm.ReedsShepp.ReedsSheppSolver.Solve(
                start_pose,
                goal_pose,
                (float)turning_radius
                );

            List<Algorithm.ReedsShepp.Pose> poses = Algorithm.ReedsShepp.ReedsSheppDriver.Discretize(start_pose, actionSet, (float)turning_radius, (float)wp_dist);

            if (poses.Count != 0)
            {
                List<GMap.NET.PointLatLng> result_pts = new List<PointLatLng>();
                for (int i = 0; i < poses.Count; i++)
                {
                    IAM.GisLib.GlobalPoint gp = new GlobalPoint(poses[i].X, poses[i].Y);
                    result_pts.Add(new PointLatLng(gp.Lat, gp.Lon));
                }

                form_gmap.DrawReedsShepp(result_pts);
            }

        }

        public bool draw_duplicated_path { get; set; }

        private void checkBox_draw_duplicated_path_CheckedChanged(object sender, EventArgs e)
        {
            draw_duplicated_path = checkBox_draw_duplicated_path.Checked;
        }

        private void button_extension_result_Click(object sender, EventArgs e)
        {
            double lat_a = Convert.ToDouble(textBox_extension_lat_a.Text);
            double lon_a = Convert.ToDouble(textBox_extension_lon_a.Text);
            double lat_b = Convert.ToDouble(textBox_extension_lat_b.Text);
            double lon_b = Convert.ToDouble(textBox_extension_lon_b.Text);
            double extension_meter = Convert.ToDouble(textBox_extension_meter.Text);

            //1.convert eabs, nabs
            IAM.GisLib.GlobalPoint a = new GlobalPoint(new LatLonPoint(lat_a, lon_a, 0.0));
            IAM.GisLib.GlobalPoint b = new GlobalPoint(new LatLonPoint(lat_b, lon_b, 0.0));

            //2. tf
            double rad = Math.Atan2(b.y - a.y, b.x - a.x);
            double tf_x = (Math.Cos(rad) * extension_meter) + a.x;
            double tf_y = (Math.Sin(rad) * extension_meter) + a.y;

            //3. convert lat, lon
            IAM.GisLib.GlobalPoint c = new GlobalPoint(tf_x, tf_y);
            textBox_extension_lat_result.Text = c.Lat.ToString();
            textBox_extension_lon_result.Text = c.Lon.ToString();
        }
    }//end class

    /// <summary>
    /// 2019-04-08
    /// </summary>
    public class Road_link
    {
        public int Start_link { get; set; }
        public int End_link { get; set; }
    }//end class

    /// <summary>
    /// 2019-04-08
    /// </summary>
    public class Road_waypoint
    {
        public int Id { get; set; }
        public double Lattitude { get; set; }
        public double Longitute { get; set; }
        public double Eabs { get; set; }
        public double Nabs { get; set; }
    }//end class
}//end namespace
