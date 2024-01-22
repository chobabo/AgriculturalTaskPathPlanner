using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;

namespace PathCreator
{
    public partial class FormPath : Form
    {
        private readonly FormMain _fm = null;

        /// <summary>
        /// half of implement width
        /// </summary>
        private double half_implement_width = 0.0;

        private double implement_width = 0.0;
        private double reverse_field_width = 0.0;
        private double outer_bound = 0.0;

        private bool is_roller_path = false;
        private bool reverse_roller_path = false;

        /// <summary>
        /// relative makurazi path: origin point is corners[0]
        /// </summary>
        public List<Vector3> relative_makurazi = new List<Vector3>();

        public List<Vector3> relative_working = new List<Vector3>();

        /// <summary>
        /// absolute makurazi path
        /// </summary>
        public List<Vector3> absolute_makurazi = null;

        public List<Vector3> absolute_working = null;

        /// <summary>
        /// relative corners points: orientation corners[0]
        /// </summary>
        public Vector3[] relative_corners_pt = null;

        /// <summary>
        /// 0: work 1:work turn 3:makurazi 7:entrance
        /// </summary>
        public List<int> path_type_working = new List<int>();

        /// <summary>
        /// 0: work 1:work turn 3:makurazi 7:entrance
        /// </summary>
        public List<int> path_type_makurazi = new List<int>();

        public List<Vector3> total_absolute_path = new List<Vector3>();
        public List<int> total_path_type = new List<int>();

        public FormPath(Form _form)
        {
            _fm = _form as FormMain;

            InitializeComponent();
        }

        private void InitializeParam()
        {
            double heading = -(_fm.std_orientation_deg) * Math.PI / 180.0;   //degree to radian

            int c_cnt = _fm.converted_abs_corners.Length;
            relative_corners_pt = new Vector3[c_cnt];
            for (int i = 0; i < c_cnt; i++)
            {
                //float diff_x = _fm.converted_abs_corners[i].X - _fm.converted_abs_corners[0].X;
                //float diff_y = _fm.converted_abs_corners[i].Y - _fm.converted_abs_corners[0].Y;
                //float diff_z = 0.0f;

                //relative_corners_pt[i].X = (float)(Math.Cos(heading) * diff_x - Math.Sin(heading) * diff_y);
                //relative_corners_pt[i].Y = (float)(Math.Sin(heading) * diff_x + Math.Cos(heading) * diff_y);
                //relative_corners_pt[i].Z = diff_z;

                //2018-04-13
                relative_corners_pt[i].X = _fm.converted_abs_corners[i].X - _fm.converted_abs_corners[0].X;
                relative_corners_pt[i].Y = _fm.converted_abs_corners[i].Y - _fm.converted_abs_corners[0].Y;
                relative_corners_pt[i].Z = 0.0f;
            }

            half_implement_width = Convert.ToDouble(_fm.numericUpDown_implement_width.Value) / 2.0;
            implement_width = Convert.ToDouble(_fm.numericUpDown_implement_width.Value);
            reverse_field_width = -(_fm.avg_field_width);
            outer_bound = Convert.ToDouble(_fm.numericUpDown_outer_bound.Value) + half_implement_width;
        }

        public void CreatePath()
        {
            if (_fm.comboBox_path_type.Text == "Default")
            {
                if (_fm.comboBox_entrance.Text == "Right")
                {
                    DefaultRightPath();
                }
                else if (_fm.comboBox_entrance.Text == "Left")
                {
                    DefaultLeftPath();  //2019-08-01
                }
            }
            else if (_fm.comboBox_path_type.Text == "Cambridge-Roller")
            {
                if (_fm.comboBox_entrance.Text == "Right")
                {
                    if (_fm.comboBox_map_direction.Text == "Length")
                    {
                        RollerRightLengthPath();
                    }
                    else if (_fm.comboBox_map_direction.Text == "Width")
                    {
                        RollerRightWidthPath();
                    }
                }
                else if (_fm.comboBox_entrance.Text == "Left")
                {
                }
            }
            else if (_fm.comboBox_path_type.Text == "Spiral")
            {
                if (_fm.comboBox_entrance.Text == "Right")
                {
                    SpiralRight();
                }
                else if (_fm.comboBox_entrance.Text == "Left")
                {
                }
            }
            // Harvester
            else if (_fm.comboBox_path_type.Text == "Harvester")
            {
                Harvesting();
            }
        }

        private void Harvesting()
        {
            InitializeParam();

            path_type_makurazi.Clear();
            relative_makurazi.Clear();
            path_type_working.Clear();
            relative_working.Clear();

            Vector3 backup_4toNext1 = new Vector3();
            double backup_offset = 0.0;

            int loop_cnt = 0;

            while (true)
            {
                double offset = -outer_bound - (implement_width * loop_cnt) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * loop_cnt);
                double next_offset = -outer_bound - (implement_width * (loop_cnt + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (loop_cnt + 1));

                Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3[] line2 = ParallelLine(offset, relative_corners_pt[2], relative_corners_pt[1]);
                Vector3[] line3 = ParallelLine(offset, relative_corners_pt[1], relative_corners_pt[0]);
                Vector3[] line4 = ParallelLine(offset, relative_corners_pt[0], relative_corners_pt[3]);
                Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);

                Vector3 intersection_1to2 = IntersectionPointOfTwoLines(line1, line2);
                Vector3 intersection_2to3 = IntersectionPointOfTwoLines(line2, line3);
                Vector3 intersection_3to4 = IntersectionPointOfTwoLines(line3, line4);
                Vector3 intersection_4toNext1 = IntersectionPointOfTwoLines(line4, next_line1);

                //path1
                if (loop_cnt == 0)
                {
                    relative_makurazi.Add(line1[0]);
                }
                else
                {
                    relative_makurazi.Add(backup_4toNext1);
                }
                relative_makurazi.Add(intersection_1to2);

                //path2
                relative_makurazi.Add(intersection_1to2);
                relative_makurazi.Add(intersection_2to3);

                //path3
                relative_makurazi.Add(intersection_2to3);
                relative_makurazi.Add(intersection_3to4);

                //path4
                relative_makurazi.Add(intersection_3to4);
                relative_makurazi.Add(intersection_4toNext1);

                for (int j = 0; j < 8; j++)
                {
                    path_type_makurazi.Add(3);
                }

                //backup
                backup_4toNext1 = intersection_4toNext1;
                backup_offset = offset;

                if (DistanceBetweenTwoPoints(intersection_1to2, intersection_2to3) < implement_width)
                //if(loop_cnt > 10)
                {
                    //2019-07-29: 패스는 두점으로 이루어져 있기 때문에 두개를 지워야 한다.
                    relative_makurazi.RemoveAt(relative_makurazi.Count - 1);
                    relative_makurazi.RemoveAt(relative_makurazi.Count - 1);
                    break;
                }
                else
                {
                    loop_cnt++;
                }
            }

            ConvertAbsolutePosition(ref relative_makurazi, out absolute_makurazi); //convert relative pts to absolute pts

            total_absolute_path.Clear();
            total_path_type.Clear();

            for (int i = 0; i < absolute_makurazi.Count; i++)
            {
                total_absolute_path.Add(absolute_makurazi[i]);
                total_path_type.Add(path_type_makurazi[i]);
            }
        }

        private void SpiralRight()
        {
            InitializeParam();

            path_type_makurazi.Clear();
            relative_makurazi.Clear();
            path_type_working.Clear();
            relative_working.Clear();

            Vector3 backup_4toNext1 = new Vector3();
            double backup_offset = 0.0;

            int loop_cnt = 0;

            while (true)
            {
                double offset = -outer_bound - (implement_width * loop_cnt) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * loop_cnt);
                double next_offset = -outer_bound - (implement_width * (loop_cnt + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (loop_cnt + 1));

                Vector3[] line1 = ParallelLine(-offset, relative_corners_pt[3], relative_corners_pt[0]);
                Vector3[] line2 = ParallelLine(-offset, relative_corners_pt[0], relative_corners_pt[1]);
                Vector3[] line3 = ParallelLine(-offset, relative_corners_pt[1], relative_corners_pt[2]);
                Vector3[] line4 = ParallelLine(-offset, relative_corners_pt[2], relative_corners_pt[3]);
                Vector3[] next_line1 = ParallelLine(-next_offset, relative_corners_pt[3], relative_corners_pt[0]);

                Vector3 intersection_1to2 = IntersectionPointOfTwoLines(line1, line2);
                Vector3 intersection_2to3 = IntersectionPointOfTwoLines(line2, line3);
                Vector3 intersection_3to4 = IntersectionPointOfTwoLines(line3, line4);
                Vector3 intersection_4toNext1 = IntersectionPointOfTwoLines(line4, next_line1);

                //path1
                if (loop_cnt == 0)
                {
                    relative_makurazi.Add(line1[0]);
                }
                else
                {
                    relative_makurazi.Add(backup_4toNext1);
                }
                relative_makurazi.Add(intersection_1to2);

                //path2
                relative_makurazi.Add(intersection_1to2);
                relative_makurazi.Add(intersection_2to3);

                //path3
                relative_makurazi.Add(intersection_2to3);
                relative_makurazi.Add(intersection_3to4);

                //path4
                relative_makurazi.Add(intersection_3to4);
                relative_makurazi.Add(intersection_4toNext1);

                for (int j = 0; j < 8; j++)
                {
                    path_type_makurazi.Add(3);
                }

                //backup
                backup_4toNext1 = intersection_4toNext1;
                backup_offset = offset;

                if (DistanceBetweenTwoPoints(intersection_2to3, intersection_3to4) <= implement_width)
                {
                    break;
                }
                else
                {
                    loop_cnt++;
                }
            }

            ConvertAbsolutePosition(ref relative_makurazi, out absolute_makurazi); //convert relative pts to absolute pts

            total_absolute_path.Clear();
            total_path_type.Clear();

            for (int i = 0; i < absolute_makurazi.Count; i++)
            {
                total_absolute_path.Add(absolute_makurazi[i]);
                total_path_type.Add(path_type_makurazi[i]);
            }
        }

        private void RollerRightWidthPath()
        {
            is_roller_path = false;
            reverse_roller_path = false;

            InitializeParam();

            total_absolute_path.Clear();
            total_path_type.Clear();

            path_type_working.Clear();
            relative_working.Clear();

            double backup_offset = -outer_bound - (implement_width * 1) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * 1);
            double lateral_offset = -outer_bound - (implement_width * 3) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * 3);
            Vector3[] lineA = ParallelLine(lateral_offset, relative_corners_pt[1], relative_corners_pt[0]);
            Vector3[] lineB = ParallelLine(lateral_offset, relative_corners_pt[3], relative_corners_pt[2]);

            int wp_index = 1;
            bool is_wp_end = false;
            while (is_wp_end == false)
            {
                double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * wp_index);
                double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (wp_index + 1));
                Vector3[] line1 = ParallelLine(offset, relative_corners_pt[0], relative_corners_pt[3]);
                Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[0], relative_corners_pt[3]);
                Vector3 intersection_1toLineA = new Vector3();
                Vector3 intersection_1toLineB = new Vector3();

                intersection_1toLineA = IntersectionPointOfTwoLines(line1, lineA);
                intersection_1toLineB = IntersectionPointOfTwoLines(line1, lineB);

                relative_working.Add(intersection_1toLineA);
                relative_working.Add(intersection_1toLineB);

                if (Math.Abs(_fm.avg_field_length) > (Math.Abs(next_offset) + Math.Abs(backup_offset)))
                {
                    wp_index++;
                }
                else
                {
                    is_wp_end = true;
                }
            }

            ConvertAbsolutePosition(ref relative_working, out absolute_working); //convert relative pts to absolute pts
            //absolute_working.Reverse();
        }

        private void RollerRightLengthPath()
        {
            is_roller_path = false;
            reverse_roller_path = false;

            InitializeParam();

            total_absolute_path.Clear();
            total_path_type.Clear();

            path_type_working.Clear();
            relative_working.Clear();

            double backup_offset = -outer_bound - (implement_width * 1) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * 1);
            double lateral_offset = -outer_bound - (implement_width * 3) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * 3);
            Vector3[] lineA = ParallelLine(lateral_offset, relative_corners_pt[0], relative_corners_pt[3]);
            Vector3[] lineB = ParallelLine(lateral_offset, relative_corners_pt[2], relative_corners_pt[1]);

            int wp_index = 1;
            bool is_wp_end = false;
            while (is_wp_end == false)
            {
                double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * wp_index);
                double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (wp_index + 1));
                Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3 intersection_1toLineA = new Vector3();
                Vector3 intersection_1toLineB = new Vector3();

                intersection_1toLineA = IntersectionPointOfTwoLines(line1, lineA);
                intersection_1toLineB = IntersectionPointOfTwoLines(line1, lineB);

                relative_working.Add(intersection_1toLineA);
                relative_working.Add(intersection_1toLineB);

                if (Math.Abs(_fm.avg_field_width) > (Math.Abs(next_offset) + Math.Abs(backup_offset)))
                {
                    wp_index++;
                }
                else
                {
                    is_wp_end = true;
                }
            }

            ConvertAbsolutePosition(ref relative_working, out absolute_working); //convert relative pts to absolute pts
            //absolute_working.Reverse();
        }

        /// <summary>
        /// 2019-08-01
        /// </summary>
        private void DefaultLeftPath()
        {
            InitializeParam();

            path_type_makurazi.Clear();
            relative_makurazi.Clear();
            //absolute_makurazi.Clear();
            Vector3 backup_4toNext1 = new Vector3();
            double backup_offset = 0.0;

            int makurazi_count = Convert.ToInt32(_fm.numericUpDown_makurazi.Value);     //2018-04-04

            //2018-04-04
            for (int i = 0; i < makurazi_count; i++)
            {
                double offset = -outer_bound - (implement_width * i) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * i);
                double next_offset = -outer_bound - (implement_width * (i + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (i + 1));

                //Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                //Vector3[] line2 = ParallelLine(offset, relative_corners_pt[2], relative_corners_pt[1]);
                //Vector3[] line3 = ParallelLine(offset, relative_corners_pt[1], relative_corners_pt[0]);
                //Vector3[] line4 = ParallelLine(offset, relative_corners_pt[0], relative_corners_pt[3]);
                //Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);

                Vector3[] line1 = ParallelLine(-offset, relative_corners_pt[0], relative_corners_pt[1]);
                Vector3[] line2 = ParallelLine(-offset, relative_corners_pt[1], relative_corners_pt[2]);
                Vector3[] line3 = ParallelLine(-offset, relative_corners_pt[2], relative_corners_pt[3]);
                Vector3[] line4 = ParallelLine(-offset, relative_corners_pt[3], relative_corners_pt[0]);
                Vector3[] next_line1 = ParallelLine(-next_offset, relative_corners_pt[0], relative_corners_pt[1]);

                Vector3 intersection_1to2 = IntersectionPointOfTwoLines(line1, line2);
                Vector3 intersection_2to3 = IntersectionPointOfTwoLines(line2, line3);
                Vector3 intersection_3to4 = IntersectionPointOfTwoLines(line3, line4);
                Vector3 intersection_4toNext1 = IntersectionPointOfTwoLines(line4, next_line1);

                //path1
                if (i == 0)
                {
                    relative_makurazi.Add(line1[0]);
                }
                else
                {
                    relative_makurazi.Add(backup_4toNext1);
                }
                relative_makurazi.Add(intersection_1to2);

                //path2
                relative_makurazi.Add(intersection_1to2);
                relative_makurazi.Add(intersection_2to3);

                //path3
                relative_makurazi.Add(intersection_2to3);
                relative_makurazi.Add(intersection_3to4);

                //path4
                relative_makurazi.Add(intersection_3to4);
                relative_makurazi.Add(intersection_4toNext1);

                for (int j = 0; j < 8; j++)
                {
                    path_type_makurazi.Add(3);
                }

                //backup
                backup_4toNext1 = intersection_4toNext1;
                backup_offset = offset;
            }

            ConvertAbsolutePosition(ref relative_makurazi, out absolute_makurazi); //convert relative pts to absolute pts

            path_type_working.Clear();
            relative_working.Clear();

            int wp_index = makurazi_count;      //2018-04-04

            double lateral_offset = -outer_bound - (implement_width * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (implement_width / 2.0);       //2018-04-13
            lateral_offset += Convert.ToDouble(_fm.numericUpDown_extension_workpath.Value);         //2018-04-13: extension work path length

            //Vector3[] lineA = ParallelLine(lateral_offset, relative_corners_pt[0], relative_corners_pt[3]);
            //Vector3[] lineB = ParallelLine(lateral_offset, relative_corners_pt[2], relative_corners_pt[1]);
            Vector3[] lineA = ParallelLine(-lateral_offset, relative_corners_pt[3], relative_corners_pt[0]);
            Vector3[] lineB = ParallelLine(-lateral_offset, relative_corners_pt[1], relative_corners_pt[2]);

            bool is_wp_reverse = false;
            bool is_wp_end = false;
            while (is_wp_end == false)
            {
                //2018-04-23
                double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_workpath_overlap.Value) * (wp_index - makurazi_count));
                double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_workpath_overlap.Value) * (wp_index - makurazi_count + 1));
                //double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * wp_index);
                //double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (wp_index + 1));
                //Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                //Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3[] line1 = ParallelLine(-offset, relative_corners_pt[0], relative_corners_pt[1]);
                Vector3[] next_line1 = ParallelLine(-next_offset, relative_corners_pt[0], relative_corners_pt[1]);
                Vector3 intersection_1toLineA = new Vector3();
                Vector3 intersection_1toLineB = new Vector3();

                //2018-04-04
                if (wp_index == makurazi_count)
                {
                    intersection_1toLineA = backup_4toNext1;
                }
                else
                {
                    intersection_1toLineA = IntersectionPointOfTwoLines(line1, lineA);
                }
                intersection_1toLineB = IntersectionPointOfTwoLines(line1, lineB);

                if (is_wp_reverse == false)
                {
                    relative_working.Add(intersection_1toLineA);
                    relative_working.Add(intersection_1toLineB);
                }
                else
                {
                    relative_working.Add(intersection_1toLineB);
                    relative_working.Add(intersection_1toLineA);
                }
                path_type_working.Add(0);
                path_type_working.Add(0);

                if (Math.Abs(_fm.avg_field_width) > (Math.Abs(next_offset) + Math.Abs(backup_offset)))
                {
                    if (is_wp_reverse == false)
                    {
                        relative_working.Add(intersection_1toLineB);
                        Vector3 intersection_next_line1toLineB = IntersectionPointOfTwoLines(next_line1, lineB);
                        relative_working.Add(intersection_next_line1toLineB);
                    }
                    else
                    {
                        relative_working.Add(intersection_1toLineA);
                        Vector3 intersection_next_line1toLineA = IntersectionPointOfTwoLines(next_line1, lineA);
                        relative_working.Add(intersection_next_line1toLineA);
                    }

                    path_type_working.Add(1);
                    path_type_working.Add(1);

                    wp_index++;
                    is_wp_reverse = !is_wp_reverse;
                }
                else
                {
                    is_wp_end = true;
                }
            }

            ConvertAbsolutePosition(ref relative_working, out absolute_working); //convert relative pts to absolute pts

            relative_makurazi.Reverse();
            relative_working.Reverse();
            absolute_makurazi.Reverse();
            absolute_working.Reverse();
            path_type_makurazi.Reverse();
            path_type_working.Reverse();

            total_absolute_path.Clear();
            total_path_type.Clear();

            for (int i = 0; i < absolute_working.Count; i++)
            {
                total_absolute_path.Add(absolute_working[i]);
                total_path_type.Add(path_type_working[i]);
            }

            for (int i = 0; i < absolute_makurazi.Count; i++)
            {
                total_absolute_path.Add(absolute_makurazi[i]);
                total_path_type.Add(path_type_makurazi[i]);
            }

        }//end DefaultLeftPath()

        private void DefaultRightPath()
        {
            InitializeParam();

            path_type_makurazi.Clear();
            relative_makurazi.Clear();
            //absolute_makurazi.Clear();
            Vector3 backup_4toNext1 = new Vector3();
            double backup_offset = 0.0;

            int makurazi_count = Convert.ToInt32(_fm.numericUpDown_makurazi.Value);     //2018-04-04

            //2018-04-04
            for (int i = 0; i < makurazi_count; i++)
            {
                double offset = -outer_bound - (implement_width * i) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * i);
                double next_offset = -outer_bound - (implement_width * (i + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (i + 1));

                Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3[] line2 = ParallelLine(offset, relative_corners_pt[2], relative_corners_pt[1]);
                Vector3[] line3 = ParallelLine(offset, relative_corners_pt[1], relative_corners_pt[0]);
                Vector3[] line4 = ParallelLine(offset, relative_corners_pt[0], relative_corners_pt[3]);
                Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);

                Vector3 intersection_1to2 = IntersectionPointOfTwoLines(line1, line2);
                Vector3 intersection_2to3 = IntersectionPointOfTwoLines(line2, line3);
                Vector3 intersection_3to4 = IntersectionPointOfTwoLines(line3, line4);
                Vector3 intersection_4toNext1 = IntersectionPointOfTwoLines(line4, next_line1);

                //path1
                if (i == 0)
                {
                    relative_makurazi.Add(line1[0]);
                }
                else
                {
                    relative_makurazi.Add(backup_4toNext1);
                }
                relative_makurazi.Add(intersection_1to2);

                //path2
                relative_makurazi.Add(intersection_1to2);
                relative_makurazi.Add(intersection_2to3);

                //path3
                relative_makurazi.Add(intersection_2to3);
                relative_makurazi.Add(intersection_3to4);

                //path4
                relative_makurazi.Add(intersection_3to4);
                relative_makurazi.Add(intersection_4toNext1);

                for (int j = 0; j < 8; j++)
                {
                    path_type_makurazi.Add(3);
                }

                //backup
                backup_4toNext1 = intersection_4toNext1;
                backup_offset = offset;
            }

            ConvertAbsolutePosition(ref relative_makurazi, out absolute_makurazi); //convert relative pts to absolute pts

            path_type_working.Clear();
            relative_working.Clear();

            int wp_index = makurazi_count;      //2018-04-04

            double lateral_offset = -outer_bound - (implement_width * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (implement_width / 2.0);       //2018-04-13
            lateral_offset += Convert.ToDouble(_fm.numericUpDown_extension_workpath.Value);         //2018-04-13: extension work path length

            Vector3[] lineA = ParallelLine(lateral_offset, relative_corners_pt[0], relative_corners_pt[3]);
            Vector3[] lineB = ParallelLine(lateral_offset, relative_corners_pt[2], relative_corners_pt[1]);

            bool is_wp_reverse = false;
            bool is_wp_end = false;
            while (is_wp_end == false)
            {
                //2018-04-23
                double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_workpath_overlap.Value) * (wp_index - makurazi_count));
                double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * makurazi_count) + (Convert.ToDouble(_fm.numericUpDown_workpath_overlap.Value) * (wp_index - makurazi_count + 1));
                //double offset = -outer_bound - (implement_width * wp_index) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * wp_index);
                //double next_offset = -outer_bound - (implement_width * (wp_index + 1)) + (Convert.ToDouble(_fm.numericUpDown_overlap.Value) * (wp_index + 1));
                Vector3[] line1 = ParallelLine(offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3[] next_line1 = ParallelLine(next_offset, relative_corners_pt[3], relative_corners_pt[2]);
                Vector3 intersection_1toLineA = new Vector3();
                Vector3 intersection_1toLineB = new Vector3();

                //2018-04-04
                if (wp_index == makurazi_count)
                {
                    intersection_1toLineA = backup_4toNext1;
                }
                else
                {
                    intersection_1toLineA = IntersectionPointOfTwoLines(line1, lineA);
                }
                intersection_1toLineB = IntersectionPointOfTwoLines(line1, lineB);

                if (is_wp_reverse == false)
                {
                    relative_working.Add(intersection_1toLineA);
                    relative_working.Add(intersection_1toLineB);
                }
                else
                {
                    relative_working.Add(intersection_1toLineB);
                    relative_working.Add(intersection_1toLineA);
                }
                path_type_working.Add(0);
                path_type_working.Add(0);

                if (Math.Abs(_fm.avg_field_width) > (Math.Abs(next_offset) + Math.Abs(backup_offset)))
                {
                    if (is_wp_reverse == false)
                    {
                        relative_working.Add(intersection_1toLineB);
                        Vector3 intersection_next_line1toLineB = IntersectionPointOfTwoLines(next_line1, lineB);
                        relative_working.Add(intersection_next_line1toLineB);
                    }
                    else
                    {
                        relative_working.Add(intersection_1toLineA);
                        Vector3 intersection_next_line1toLineA = IntersectionPointOfTwoLines(next_line1, lineA);
                        relative_working.Add(intersection_next_line1toLineA);
                    }

                    path_type_working.Add(1);
                    path_type_working.Add(1);

                    wp_index++;
                    is_wp_reverse = !is_wp_reverse;
                }
                else
                {
                    is_wp_end = true;
                }
            }

            ConvertAbsolutePosition(ref relative_working, out absolute_working); //convert relative pts to absolute pts

            relative_makurazi.Reverse();
            relative_working.Reverse();
            absolute_makurazi.Reverse();
            absolute_working.Reverse();
            path_type_makurazi.Reverse();
            path_type_working.Reverse();

            total_absolute_path.Clear();
            total_path_type.Clear();

            for (int i = 0; i < absolute_working.Count; i++)
            {
                total_absolute_path.Add(absolute_working[i]);
                total_path_type.Add(path_type_working[i]);
            }

            for (int i = 0; i < absolute_makurazi.Count; i++)
            {
                total_absolute_path.Add(absolute_makurazi[i]);
                total_path_type.Add(path_type_makurazi[i]);
            }

        }//end DefaultRightPath()

        /// <summary>
        /// https://social.msdn.microsoft.com/Forums/vstudio/en-US/4eb3423e-eb81-4977-8ce5-5a568d53fd9b/get-the-intersection-point-of-two-lines?forum=vbgeneral
        /// </summary>
        /// <param name="_line1"></param>
        /// <param name="_line2"></param>
        /// <returns></returns>
        private Vector3 IntersectionPointOfTwoLines(Vector3[] _line1, Vector3[] _line2)
        {
            Vector3 pt = new Vector3();
            float dy1 = _line1[1].Y - _line1[0].Y;
            float dx1 = _line1[1].X - _line1[0].X;
            float dy2 = _line2[1].Y - _line2[0].Y;
            float dx2 = _line2[1].X - _line2[0].X;

            if ((dy1 * dx2) != (dy2 * dx1))
            {
                pt.X = ((_line2[0].Y - _line1[0].Y) * dx1 * dx2 + dy1 * dx2 * _line1[0].X - dy2 * dx1 * _line2[0].X) / (dy1 * dx2 - dy2 * dx1);
                pt.Y = _line1[0].Y + (dy1 / dx1) * (pt.X - _line1[0].X);
                pt.Z = 0.0f;
            }

            return pt;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/2825412/draw-a-parallel-line
        /// </summary>
        /// <param name="_offset"></param>
        /// <param name="_pt1"></param>
        /// <param name="_pt2"></param>
        /// <returns></returns>
        private Vector3[] ParallelLine(double _offset, Vector3 _pt1, Vector3 _pt2)
        {
            Vector3[] pts = new Vector3[2];     //start, end point

            var L = Math.Sqrt(Math.Pow(_pt1.X - _pt2.X, 2.0) + Math.Pow(_pt1.Y - _pt2.Y, 2.0));

            //parallel line
            pts[0].X = (float)(_pt1.X + _offset * (_pt2.Y - _pt1.Y) / L);
            pts[1].X = (float)(_pt2.X + _offset * (_pt2.Y - _pt1.Y) / L);
            pts[0].Y = (float)(_pt1.Y + _offset * (_pt1.X - _pt2.X) / L);
            pts[1].Y = (float)(_pt2.Y + _offset * (_pt1.X - _pt2.X) / L);

            return pts;
        }

        private void ConvertAbsolutePosition(ref List<Vector3> _relative_pts, out List<Vector3> _absolute_pts)
        {
            _absolute_pts = new List<Vector3>();
            Vector3 converted_abs = new Vector3();
            double heading = _fm.std_orientation_deg * Math.PI / 180.0;   //degree to radian

            for (int i = 0; i < _relative_pts.Count; i++)
            {
                //converted_abs.X = (float)(Math.Cos(heading) * _relative_pts[i].X - Math.Sin(heading) * _relative_pts[i].Y + _fm.converted_abs_corners[0].X);
                //converted_abs.Y = (float)(Math.Sin(heading) * _relative_pts[i].X + Math.Cos(heading) * _relative_pts[i].Y + _fm.converted_abs_corners[0].Y);
                //converted_abs.Z = 0.0f;

                //2018-04-13
                converted_abs.X = _fm.converted_abs_corners[0].X + _relative_pts[i].X;
                converted_abs.Y = _fm.converted_abs_corners[0].Y + _relative_pts[i].Y;
                converted_abs.Z = 0.0f;

                _absolute_pts.Add(converted_abs);
            }
        }

        public void RemoveSelectedPath(int _index)
        {
            for (int i = 0; i < 2; i++)
            {
                total_absolute_path.RemoveAt(_index);
                total_path_type.RemoveAt(_index);
            }
        }

        public void LineExtension(int _index, string _direction, double _length)
        {
            float x1 = total_absolute_path[_index].X;
            float x2 = total_absolute_path[_index + 1].X;
            float y1 = total_absolute_path[_index].Y;
            float y2 = total_absolute_path[_index + 1].Y;

            double std_distance = Math.Sqrt(Math.Pow((x2 - x1), 2.0) + Math.Pow((y2 - y1), 2.0));
            float xDiff = 0.0f;
            float yDiff = 0.0f;
            double angle = 0.0f;

            if (_direction == "End")
            {
                xDiff = x2 - x1;
                yDiff = y2 - y1;
                angle = Math.Atan2(yDiff, xDiff);

                double dist = std_distance + _length;
                Vector3 end_pt = new Vector3(
                    (float)(Math.Cos(angle) * dist - Math.Sin(angle) * 0.0 + x1),
                    (float)(Math.Sin(angle) * dist + Math.Cos(angle) * 0.0 + y1),
                    0.0f
                    );

                total_absolute_path[_index + 1] = end_pt;
            }
            else
            {
                //start
                xDiff = x1 - x2;
                yDiff = y1 - y2;
                angle = Math.Atan2(yDiff, xDiff);

                double dist = std_distance + _length;
                Vector3 start_pt = new Vector3(
                    (float)(Math.Cos(angle) * dist - Math.Sin(angle) * 0.0 + x2),
                    (float)(Math.Sin(angle) * dist + Math.Cos(angle) * 0.0 + y2),
                    0.0f
                    );

                total_absolute_path[_index] = start_pt;
            }

            //debug
            _fm.textBox_start_x.Text = total_absolute_path[_index].X.ToString();
            _fm.textBox_start_y.Text = total_absolute_path[_index].Y.ToString();
            _fm.textBox_end_x.Text = total_absolute_path[_index + 1].X.ToString();
            _fm.textBox_end_y.Text = total_absolute_path[_index + 1].Y.ToString();
        }

        public void AddRollerPath(int _index)
        {
            if (is_roller_path == false)
            {
                total_absolute_path.Clear();
                total_path_type.Clear();

                total_absolute_path.Add(absolute_working[_index]);
                total_absolute_path.Add(absolute_working[_index + 1]);
                total_path_type.Add(0);
                total_path_type.Add(0);

                is_roller_path = true;
                reverse_roller_path = true;
            }
            else
            {
                if (reverse_roller_path == true)
                {
                    //before end point
                    total_absolute_path.Add(total_absolute_path[total_absolute_path.Count - 1]);
                    total_absolute_path.Add(absolute_working[_index + 1]);
                    total_path_type.Add(1);
                    total_path_type.Add(1);

                    total_absolute_path.Add(absolute_working[_index + 1]);
                    total_absolute_path.Add(absolute_working[_index]);
                    total_path_type.Add(0);
                    total_path_type.Add(0);

                    reverse_roller_path = false;
                }
                else
                {
                    //before end point
                    total_absolute_path.Add(total_absolute_path[total_absolute_path.Count - 1]);
                    total_absolute_path.Add(absolute_working[_index]);
                    total_path_type.Add(1);
                    total_path_type.Add(1);

                    total_absolute_path.Add(absolute_working[_index]);
                    total_absolute_path.Add(absolute_working[_index + 1]);
                    total_path_type.Add(0);
                    total_path_type.Add(0);

                    reverse_roller_path = true;
                }
            }
        }

        private double DistanceBetweenTwoPoints(Vector3 pt1, Vector3 pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2.0) + Math.Pow(pt1.Y - pt2.Y, 2.0));
        }
    }//end class
}//end namespace
