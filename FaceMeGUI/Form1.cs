using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SuperMap.Data;
using SuperMap.Realspace;
using SuperMap.UI;

namespace FaceMeGUI
{
    public partial class Form1 : Form
    {
        Workspace ws;
        SceneControl scon;
        public Form1()
        {
            InitializeComponent();
            init();

        }

        void init()
        {
            scon = new SceneControl();
            scon.Dock = DockStyle.Fill;
            this.Controls.Add(scon);
            string wsPath = @"D:\模型找正面\ws.smwu";
            ws = new Workspace();
            WorkspaceConnectionInfo wsCon = new WorkspaceConnectionInfo()
            {
                Server = wsPath,
                Type = WorkspaceType.SMWU
            };

            ws.Open(wsCon);

            scon.Scene.Workspace = ws;
            scon.Scene.Open(ws.Scenes[0]);

            scon.Scene.EnsureVisible(scon.Scene.Layers[0]);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Datasource datasource in ws.Datasources)
            {
                foreach (Dataset dataset in datasource.Datasets)
                {
                    switch (dataset.Type)
                    {
                        case DatasetType.CAD:
                            //case DatasetType.Model:
                            Console.WriteLine(dataset.Name);
                            Phineas p = new Phineas()
                            {
                                dv = dataset as DatasetVector,
                                scene = scon.Scene
                            };
                            p.run();
                            break;
                    }
                }
            }
        }
    }

    class Phineas
    {
        public DatasetVector dv;
        public Scene scene;
        Random R = new Random();

        public void run()
        {
            Recordset rc = dv.GetRecordset(false, CursorType.Dynamic);

            Dictionary<int, Feature> feas = rc.GetAllFeatures();

            foreach (KeyValuePair<int, Feature> item in feas)
            {
                GeoModel gm = item.Value.GetGeometry() as GeoModel;
                Console.WriteLine("==" + gm.Position + "==");

                GeoModel model = new GeoModel();
                model.Position = gm.Position;
                foreach (Mesh m in gm.Meshes)
                {
                    if (m.Material.TextureFile.Length > 1)
                    {
                        //Console.WriteLine(m.Material.TextureFile.ToString());
                        Point3Ds p3ds = new Point3Ds();

                        for (int i = 0; i < m.Vertices.Length; i += 3)
                        {
                            bool repition = false;
                            foreach (Point3D p in p3ds)
                            {
                                if (p.X == m.Vertices[i] && p.Y == m.Vertices[i + 1] && p.Z == m.Vertices[i + 2])
                                {
                                    repition = true;
                                }
                            }
                            if (!repition)
                            {
                                p3ds.Add(new Point3D(m.Vertices[i], m.Vertices[i + 1], m.Vertices[i + 2]));

                            }
                        }

                        foreach (Point3D p3d in p3ds)
                        {
                            Console.WriteLine(string.Format(" {0},{1},{2}", p3d.X, p3d.Y, p3d.Z));
                            scene.TrackingLayer.Add(new GeoPoint3D(p3d.X, p3d.Y, p3d.Z), "");
                        }
                        //model.Meshes.Add(MakeMeshPot(p3ds));
                        Mesh mesh = new Mesh(m);
                        mesh.Material.TextureFile = @".\78310a55b319ebc41f7810198326cffc1e171629.png";
                        model.Meshes.Add(mesh);


                        #region 写属性表
                        Dictionary<string, double> fields = new Dictionary<string, double>();
                        fields.Add("FaceMeshCenterX", model.Position.X);
                        fields.Add("FaceMeshCenterY", model.Position.Y);
                        fields.Add("FaceMeshCenterZ", model.Position.Z);
                        fields.Add("FaceMeshLx", p3ds.leftbottom().X);
                        fields.Add("FaceMeshLy", p3ds.leftbottom().Y);
                        fields.Add("FaceMeshLz", p3ds.leftbottom().Z);
                        fields.Add("FaceMeshUx", p3ds.rightup().X);
                        fields.Add("FaceMeshUy", p3ds.rightup().Y);
                        fields.Add("FaceMeshUz", p3ds.rightup().Z);


                        foreach (KeyValuePair<string, double> field in fields)
                        {
                            if (dv.FieldInfos.IndexOf(field.Key) < 0)
                            {
                                FieldInfo fieldInf = new FieldInfo(field.Key, FieldType.Double);
                                dv.FieldInfos.Add(fieldInf);
                            }

                            string fieldName = field.Key;
                            double fieldValue = field.Value;
                            try
                            {
                                rc.SeekID(item.Value.GetID());
                                rc.Edit();
                                rc.SetFieldValue(fieldName, fieldValue);
                                rc.Update();
                            }
                            catch
                            {
                                Console.WriteLine("error!");
                            }
                            //Console.WriteLine(string.Format("{0},{1},{2}", item.GetID(), fieldName, fieldValue));
                        }
                        #endregion
                    }
                }
                Console.WriteLine("");

                model.ComputeBoundingBox();
                scene.TrackingLayer.Add(model, gm.Position.ToString());
                scene.Refresh();
            }
        }

        Mesh MakeMeshPot(Point3Ds ps)
        { 
            Mesh mesh = new Mesh();
            Double[] vertexes = new Double[12];
            Double[] normals = new Double[12];
            Int32[] indexes = new Int32[6];
            Double[] textureCoords = new Double[8];

            //设置顶点坐标，每三个一组，对应坐标系的X,Y,Z数值
            // -0.661115958904828,-0.239472665266096,1646.15246582031
            double offsetX = 0;
            double offsetY = 0;
            double offsetZ = 0;

            vertexes[0] = ps.leftbottom().X + (ps.rightbottom().X - ps.leftbottom().X) / 10 + offsetX;
            vertexes[1] = ps.leftbottom().Y + (ps.rightbottom().Y - ps.leftbottom().Y) / 10 + offsetY;
            vertexes[2] = ps.leftbottom().Z + (ps.leftup().Z - ps.leftbottom().Z) / 10 * 8 + offsetZ;

            vertexes[3] = ps.leftup().X + (ps.rightup().X - ps.leftup().X) / 10 + offsetX;
            vertexes[4] = ps.leftup().Y + (ps.rightup().Y - ps.leftup().Y) / 10 + offsetY;
            vertexes[5] = ps.leftup().Z - (ps.leftup().Z - ps.leftbottom().Z) / 10 + offsetZ;

            vertexes[6] = ps.rightup().X - (ps.rightup().X - ps.leftup().X) / 10 + offsetX;
            vertexes[7] = ps.rightup().Y - (ps.rightup().Y - ps.leftup().Y) / 10 + offsetY;
            vertexes[8] = ps.rightup().Z - (ps.rightup().Z - ps.rightbottom().Z) / 10 + offsetZ;

            vertexes[9] = ps.rightbottom().X - (ps.rightbottom().X - ps.leftbottom().X) / 10 + offsetX;
            vertexes[10] = ps.rightbottom().Y - (ps.rightbottom().Y - ps.leftbottom().Y) / 10 + offsetY;
            vertexes[11] = ps.rightbottom().Z + (ps.rightup().Z - ps.rightbottom().Z) / 10 * 8 + offsetZ;

            //设置顶点索引，用于绘制图形时应用
            indexes[0] = 0; indexes[1] = 1; indexes[2] = 2;
            indexes[3] = 0; indexes[4] = 2; indexes[5] = 3;

            //设置顶点的法向量方向，设置法向量可以突出Mesh的阴影效果，更加逼真。
            for (int i = 0; i < 12; i += 3)
            {
                normals[i] = 0; normals[i + 1] = 1; normals[i + 2] = 0;
            }

            mesh.Vertices = vertexes;
            mesh.Indexes = indexes;

            return mesh;
        }


    }

    public static class Ext
    {
        public static Point3D leftbottom(this Point3Ds p3ds)
        {
            Point3D p = p3ds[0];

            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X > p3ds[i].X)
                {
                    p = p3ds[i];
                }
            }
            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X == p3ds[i].X && p.Y > p3ds[i].Y)
                {
                    p = p3ds[i];
                }
            }
            p.Z = Math.Min(Math.Min(p3ds[0].Z, p3ds[1].Z), p3ds[2].Z);

            return p;
        }

        public static Point3D leftup(this Point3Ds p3ds)
        {
            Point3D p = p3ds[0];

            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X > p3ds[i].X)
                {
                    p = p3ds[i];
                }
            }
            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X == p3ds[i].X && p.Y > p3ds[i].Y)
                {
                    p = p3ds[i];
                }
            }
            p.Z = Math.Max(Math.Max(p3ds[0].Z, p3ds[1].Z), p3ds[2].Z);

            return p;
        }

        public static Point3D rightup(this Point3Ds p3ds)
        {
            Point3D p = p3ds[0];

            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X < p3ds[i].X)
                {
                    p = p3ds[i];
                }
            }
            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X == p3ds[i].X && p.Y < p3ds[i].Y)
                {
                    p = p3ds[i];
                }
            }
            p.Z = Math.Max(Math.Max(p3ds[0].Z, p3ds[1].Z), p3ds[2].Z);

            return p;
        }

        public static Point3D rightbottom(this Point3Ds p3ds)
        {
            Point3D p = p3ds[0];

            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X < p3ds[i].X)
                {
                    p = p3ds[i];
                }
            }
            for (int i = 1; i < p3ds.Count; i++)
            {
                if (p.X == p3ds[i].X && p.Y < p3ds[i].Y)
                {
                    p = p3ds[i];
                }
            }
            p.Z = Math.Min(Math.Min(p3ds[0].Z, p3ds[1].Z), p3ds[2].Z);

            return p;
        }
    }
}