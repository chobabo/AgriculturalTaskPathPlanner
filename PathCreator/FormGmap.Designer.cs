namespace PathCreator
{
    partial class FormGmap
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gmc = new GMap.NET.WindowsForms.GMapControl();
            this.SuspendLayout();
            // 
            // gmc
            // 
            this.gmc.Bearing = 0F;
            this.gmc.CanDragMap = true;
            this.gmc.EmptyTileColor = System.Drawing.Color.Navy;
            this.gmc.GrayScaleMode = false;
            this.gmc.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gmc.LevelsKeepInMemmory = 5;
            this.gmc.Location = new System.Drawing.Point(12, 12);
            this.gmc.MarkersEnabled = true;
            this.gmc.MaxZoom = 30;
            this.gmc.MinZoom = 2;
            this.gmc.MouseWheelZoomEnabled = true;
            this.gmc.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.gmc.Name = "gmc";
            this.gmc.NegativeMode = false;
            this.gmc.PolygonsEnabled = true;
            this.gmc.RetryLoadTile = 0;
            this.gmc.RoutesEnabled = true;
            this.gmc.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gmc.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gmc.ShowTileGridLines = false;
            this.gmc.Size = new System.Drawing.Size(1210, 937);
            this.gmc.TabIndex = 0;
            this.gmc.Zoom = 18D;
            this.gmc.OnMarkerClick += new GMap.NET.WindowsForms.MarkerClick(this.gmc_OnMarkerClick);
            this.gmc.OnPolygonClick += new GMap.NET.WindowsForms.PolygonClick(this.gmc_OnPolygonClick);
            this.gmc.OnRouteClick += new GMap.NET.WindowsForms.RouteClick(this.gmc_OnRouteClick);
            this.gmc.MouseClick += new System.Windows.Forms.MouseEventHandler(this.gmc_MouseClick);
            this.gmc.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.gmc_MouseDoubleClick);
            // 
            // FormGmap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1234, 961);
            this.Controls.Add(this.gmc);
            this.Name = "FormGmap";
            this.Text = "FormGmap";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormGmap_FormClosing);
            this.Load += new System.EventHandler(this.FormGmap_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormGmap_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private GMap.NET.WindowsForms.GMapControl gmc;
    }
}