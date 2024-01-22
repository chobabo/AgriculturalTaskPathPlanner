using System;
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
using Algorithm;

namespace PathCreator
{
    public partial class FormGmap : Form
    {
        private readonly FormMain _fm = null;
        private GMapOverlay field_polygons = null;
        private GMapOverlay target_paths = null;
        private GMapOverlay road_paths = null;
        private GMapOverlay road_boundary = null;
        private GMapOverlay DrivingRoadRoute= null;
        private GMapOverlay DrivingWayPointMarker = null;
        private GMapOverlay RS_path_finding = null;
        private GMapPolygon clicked_polygon_item = null;
        private GMapRoute clicked_route_item = null;
        private GMapMarker clicked_marker_item = null;
        private GMapRoute selected_line = null;
        //private GMapRoute draw_driving_route = null;
        private DirectionsStatusCode driving_route;
        private string clicked_status = String.Empty;
        private string current_work_path = String.Empty;
        private string current_makurazi_path = String.Empty;

        public FormGmap(Form _form, int _map_type)
        {
            _fm = _form as FormMain;
            _fm.ChangeCenterPositionCallBack = new ChangeCenterPositionDelegate(this.ChangeCenterPositionCallBack);

            InitializeComponent();

            //2019-07-31: GPS 프로젝션 존을 설정할 수 있도록 변경
            int gps_projection_zone = Convert.ToInt32(_fm.numericUpDown_GPS_Projection_Zone.Value);
            InitializationOfGnss(1, gps_projection_zone);

            this.StartPosition = FormStartPosition.Manual;
            this.DesktopLocation = new Point(640, 0);

            this.KeyPreview = true;

            //gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaSatelliteMapProvider.Instance;
            //gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaTerrainMapProvider.Instance;

            if (_map_type == 0) { gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaSatelliteMapProvider.Instance; }
            else if (_map_type == 1) { gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaTerrainMapProvider.Instance; }

            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            //gmc.Position = new GMap.NET.PointLatLng(36.0242526350217, 140.098848211558);      //default value(IAM)
            gmc.ShowCenter = false;

            field_polygons = new GMapOverlay("field_polygons");
            List<PointLatLng> c_pts = new List<PointLatLng>();

            for (int i = 0; i < _fm.map_corners.Count; i++)
            {
                c_pts.Clear();
                for (int j = 0; j < _fm.map_corners[i].Length; j++)
                {
                    //2018-03-14
                    c_pts.Add(new PointLatLng(_fm.map_corners[i][j].Lat, _fm.map_corners[i][j].Lon));
                }
                GMapPolygon field = new GMapPolygon(c_pts, _fm.map_name[i]);
                field.Fill = new SolidBrush(Color.FromArgb(50, Color.Black));
                field.Stroke = new Pen(Color.Red, 2);
                field.IsHitTestVisible = false;

                field_polygons.Polygons.Add(field);
            }

            gmc.Overlays.Add(field_polygons);
            //2018-03-14
            gmc.Position = new GMap.NET.PointLatLng(
                _fm.map_corners[_fm.comboBoxSelectMap.SelectedIndex][0].Lat,
                _fm.map_corners[_fm.comboBoxSelectMap.SelectedIndex][0].Lon
                );

            target_paths = new GMapOverlay("target_path");
            gmc.Overlays.Add(target_paths);

            road_paths = new GMapOverlay("road_path");
            gmc.Overlays.Add(road_paths);

            //2019-07-02
            road_boundary = new GMapOverlay("road_boundary");
            gmc.Overlays.Add(road_boundary);

            DrivingWayPointMarker = new GMapOverlay("driving_waypoint_marker");
            gmc.Overlays.Add(DrivingWayPointMarker);

            DrivingRoadRoute = new GMapOverlay("driving_road_route");
            gmc.Overlays.Add(DrivingRoadRoute);

            RS_path_finding = new GMapOverlay("Reeds Shepp");
            gmc.Overlays.Add(RS_path_finding);
        }

        public void CreateDrivingRoute(PointLatLng _start, PointLatLng _end, int _map_type, int _tail_num, bool _avoidhighways, bool _avoidTolls, bool _workingMode, bool _sensor, bool _metric)
        {
            GDirections gd;
            //var route = GMap.NET.MapProviders.GoogleMapProvider.Instance.GetDirections(out gd, _start, _end, false, false, false, false, false);

            if (_map_type == 0)
            {
                driving_route = GMap.NET.MapProviders.GoogleChinaSatelliteMapProvider.Instance.GetDirections(out gd, _start, _end, _avoidhighways, _avoidTolls, _workingMode, _sensor, _metric);
            }
            else if (_map_type == 1)
            {
                driving_route = GMap.NET.MapProviders.GoogleChinaTerrainMapProvider.Instance.GetDirections(out gd, _start, _end, _avoidhighways, _avoidTolls, _workingMode, _sensor, _metric);
            }
            else
            {
                driving_route = GMap.NET.MapProviders.GoogleMapProvider.Instance.GetDirections(out gd, _start, _end, _avoidhighways, _avoidTolls, _workingMode, _sensor, _metric);
            }

            string route_name = "driving_route_" + _tail_num.ToString();
            if (gd != null)
            {
                var r = new GMapRoute(gd.Route, route_name);
                //r.Stroke = new Pen(Color.Yellow, 3);
                r.IsHitTestVisible = true;
                road_paths.Routes.Add(r);

                for (int i = 0; i < gd.Route.Count; i++)
                {
                    GMapMarker marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                        gd.Route[i], GMap.NET.WindowsForms.Markers.GMarkerGoogleType.arrow);
                    marker.IsHitTestVisible = true;
                    marker.ToolTipText = "lat:" + gd.Route[i].Lat.ToString() + " , " + "lon: " + gd.Route[i].Lng.ToString();
                    road_paths.Markers.Add(marker);
                }
            }
        }

        /// <summary>
        /// 2019-07-02
        /// GMap에 도로 경계선을 그린다.
        /// </summary>
        /// <param name="road_vectors"></param>
        public void DrawRoadBoundary(List<PointLatLng> road_vectors)
        {
            var road_lines = new GMapRoute(road_vectors, "road_lines");
            road_lines.SetDrawMode("road");
            road_lines.Stroke = new Pen(Color.Aquamarine, 2);
            road_lines.IsHitTestVisible = true;
            road_boundary.Routes.Add(road_lines);
        }

        public void DrawDrivingWayPointMarker(GlobalPath globalPath)
        {
            for (int i = 0; i < globalPath.Nodes.Count; i++)
            {
                GMapMarker marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                    new PointLatLng(globalPath.Nodes[i].GpsPoint.Latitude, globalPath.Nodes[i].GpsPoint.Longitude),
                    GMap.NET.WindowsForms.Markers.GMarkerGoogleType.orange_dot
                    );
                marker.IsHitTestVisible = true;
                marker.ToolTipText = "ID: " + globalPath.Nodes[i].Id.ToString();
                DrivingWayPointMarker.Markers.Add(marker);
            }
        }

        public void DrawReedsShepp(List<PointLatLng> _pts)
        {
            var route_node_vectors = new GMapRoute(_pts, "Reeds_Shepp");
            route_node_vectors.Stroke = new Pen(System.Drawing.Color.Yellow, 2);
            route_node_vectors.IsHitTestVisible = true;

            RS_path_finding.Clear();
            RS_path_finding.Routes.Add(route_node_vectors);
        }

        public void DrawDrivingRoadRoute(GlobalPath globalPath)
        {
            if (DrivingRoadRoute.Routes.Count != 0)
            {
                DrivingRoadRoute.Routes.Clear();
            }

            var node_vectors = new List<PointLatLng>();

            for (int i = 0; i < globalPath.ShortestPath.Count; i++)
            {
                node_vectors.Add(new PointLatLng(
                    globalPath.ShortestPath[i].GpsPoint.Latitude,
                    globalPath.ShortestPath[i].GpsPoint.Longitude
                    ));
            }

            var route_node_vectors = new GMapRoute(node_vectors, "driving_nodes");
            route_node_vectors.Stroke = new Pen(Color.Yellow, 2);
            route_node_vectors.IsHitTestVisible = true;
            DrivingRoadRoute.Routes.Add(route_node_vectors);
        }

        public void DrawPath(List<Vector3> _path_abs, string _tail_name)
        {
            List<PointLatLng> points = new List<PointLatLng>();
            for (int i = 0; i < _path_abs.Count; i++)
            {
                IAM.GisLib.GlobalPoint gp = new GlobalPoint(_path_abs[i].X, _path_abs[i].Y);
                points.Add(new PointLatLng(gp.Lat, gp.Lon));
            }
            string path_name = _fm.comboBoxSelectMap.Text + "_path_" + _tail_name;
            if (_tail_name == "working") { current_work_path = path_name; }
            else if (_tail_name == "makurazi") { current_makurazi_path = path_name; }

            GMapRoute t_path = new GMapRoute(points, path_name);
            t_path.Stroke = new Pen(Color.White, 2);
            t_path.IsHitTestVisible = true;

            if (_fm.draw_duplicated_path == false)
            {
                target_paths.Clear();   //2019-07-31
            }
            
            target_paths.Routes.Add(t_path);
        }

        public void DrawSelectedRow(List<Vector3> _path_abs, int _index)
        {
            if (selected_line != null)
            {
                target_paths.Routes.Remove(selected_line);
            }

            List<PointLatLng> points = new List<PointLatLng>();
            if (_index < _path_abs.Count - 1)
            {
                for (int i = 0; i < 2; i++)
                {
                    IAM.GisLib.GlobalPoint gp = new GlobalPoint(_path_abs[_index + i].X, _path_abs[_index + i].Y);
                    points.Add(new PointLatLng(gp.Lat, gp.Lon));
                }

                selected_line = new GMapRoute(points, "selected_line");
                selected_line.Stroke = new Pen(Color.Yellow, 3);
                selected_line.IsHitTestVisible = false;
                target_paths.Routes.Add(selected_line);
            }
        }

        /// <summary>
        /// 2019-10-31: Draw Trajectory log from log-data
        /// </summary>
        /// <param name="read_trajectory_log"></param>
        public void DrawTrajectoryLog(List<string> read_trajectory_log)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            for (int i = 0; i < read_trajectory_log.Count; i++)
            {
                string[] log = read_trajectory_log[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                //[5]: Eabs
                double tmp_Eabs;
                bool is_Eabs = double.TryParse(log[5], out tmp_Eabs);
                //if (is_Eabs) { ct.Eabs = tmp_Eabs; }

                //[6]: Nabs
                double tmp_Nabs;
                bool is_Nabs = double.TryParse(log[6], out tmp_Nabs);
                //if (is_Nabs) { ct.Nabs = tmp_Nabs; }

                if (is_Eabs == true && is_Nabs == true)
                {
                    IAM.GisLib.GlobalPoint gp = new GlobalPoint(tmp_Eabs, tmp_Nabs);
                    points.Add(new PointLatLng(gp.Lat, gp.Lon));
                }
            }

            string path_name = "vehicle_trajectory";

            GMapRoute t_path = new GMapRoute(points, path_name);
            t_path.Stroke = new Pen(Color.Yellow, 2);
            t_path.IsHitTestVisible = true;

            //target_paths.Clear();   //2019-07-31
            target_paths.Routes.Add(t_path);
        }

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

        private void FormGmap_Load(object sender, EventArgs e)
        {
            //gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaSatelliteMapProvider.Instance;
            ////gmc.MapProvider = GMap.NET.MapProviders.GoogleChinaTerrainMapProvider.Instance;
            //GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            ////gmc.Position = new GMap.NET.PointLatLng(36.0242526350217, 140.098848211558);      //default value(IAM)
            //gmc.ShowCenter = false;

            //field_polygons = new GMapOverlay("field_polygons");
            //List<PointLatLng> c_pts = new List<PointLatLng>();

            //for (int i = 0; i < _fm.map_corners.Count; i++)
            //{
            //    c_pts.Clear();
            //    for (int j = 0; j < _fm.map_corners[i].Length; j++)
            //    {
            //        //2018-03-14
            //        c_pts.Add(new PointLatLng(_fm.map_corners[i][j].Lat, _fm.map_corners[i][j].Lon));
            //    }
            //    GMapPolygon field = new GMapPolygon(c_pts, _fm.map_name[i]);
            //    field.Fill = new SolidBrush(Color.FromArgb(50, Color.Black));
            //    field.Stroke = new Pen(Color.Red, 2);
            //    field.IsHitTestVisible = false;

            //    field_polygons.Polygons.Add(field);              
            //}

            //gmc.Overlays.Add(field_polygons);
            ////2018-03-14
            //gmc.Position = new GMap.NET.PointLatLng(
            //    _fm.map_corners[_fm.comboBoxSelectMap.SelectedIndex][0].Lat,
            //    _fm.map_corners[_fm.comboBoxSelectMap.SelectedIndex][0].Lon
            //    );

            //target_paths = new GMapOverlay("target_path");
        }

        private void FormGmap_FormClosing(object sender, FormClosingEventArgs e)
        {
            _fm.button_open_map.BackColor = Color.Yellow;
            _fm.button_open_map.Text = "Open";

            gmc.Dispose();
        }

        private void gmc_OnPolygonClick(GMapPolygon item, MouseEventArgs e)
        {
            _fm.toolStripStatusLabel2.Text = String.Format("Polygon {0} was clicked.", item.Name);
            clicked_polygon_item = item;
            clicked_status = "PolygonClick";
        }

        private void FormGmap_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (clicked_status == "PolygonClick")
                {
                    field_polygons.Polygons.Remove(clicked_polygon_item);
                    _fm.toolStripStatusLabel2.Text = clicked_polygon_item.Name + " was removed.";
                }
                else if (clicked_status == "RouteClick")
                {
                    string[] words = clicked_route_item.Name.Split('_');
                    if (words[0] == "driving")
                    {
                        road_paths.Routes.Remove(clicked_route_item);
                    }
                    else
                    {
                        target_paths.Routes.Remove(clicked_route_item);
                    }
                    _fm.toolStripStatusLabel2.Text = clicked_route_item.Name + " was removed.";
                }
                else
                {
                    //clicked_status == "MarkerClick"
                    road_paths.Markers.Remove(clicked_marker_item);
                    _fm.toolStripStatusLabel2.Text = clicked_marker_item.Tag + " was removed.";
                }
            }
        }

        private void ChangeCenterPositionCallBack(double _lat, double _lon)
        {
            gmc.Position = new GMap.NET.PointLatLng(_lat, _lon);
        }

        private void gmc_OnRouteClick(GMapRoute item, MouseEventArgs e)
        {
            _fm.toolStripStatusLabel2.Text = String.Format("Route {0} was clicked.", item.Name);
            clicked_route_item = item;
            clicked_status = "RouteClick";
        }

        private void gmc_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                double lat = gmc.FromLocalToLatLng(e.X, e.Y).Lat;
                double lng = gmc.FromLocalToLatLng(e.X, e.Y).Lng;

                _fm.double_clicked_pt = new PointLatLng(lat, lng);

                _fm.textBox_doublc_clicked_lat.Text = lat.ToString();
                _fm.textBox_doublc_clicked_lon.Text = lng.ToString();

                GMapMarker marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                    new PointLatLng(lat, lng), GMap.NET.WindowsForms.Markers.GMarkerGoogleType.blue_pushpin);
                marker.IsHitTestVisible = true;
                marker.ToolTipText = "lat:" + lat.ToString() + " , " + "lon: " + lng.ToString();
                road_paths.Markers.Add(marker);
            }
        }

        private void gmc_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            _fm.toolStripStatusLabel2.Text = String.Format("Marker {0} was clicked.", item.Tag);
            clicked_marker_item = item;
            clicked_status = "MarkerClick";
        }

        /// <summary>
        /// 2019-07-03
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gmc_MouseClick(object sender, MouseEventArgs e)
        {
            if (_fm.is_add_gps_point == true && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                double lat = gmc.FromLocalToLatLng(e.X, e.Y).Lat;
                double lng = gmc.FromLocalToLatLng(e.X, e.Y).Lng;

                _fm.current_gps_marker = new PointLatLng(lat, lng);

                _fm.textBox_gps_marker_lat.Text = lat.ToString();
                _fm.textBox_gps_marker_lon.Text = lng.ToString();

                GMapMarker marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                    new PointLatLng(lat, lng), GMap.NET.WindowsForms.Markers.GMarkerGoogleType.lightblue_dot);
                marker.IsHitTestVisible = true;
                marker.ToolTipText = "lat:" + lat.ToString() + " , " + "lon: " + lng.ToString();
                road_paths.Markers.Add(marker);
            }
        }
    }//end class
}//end namespace
