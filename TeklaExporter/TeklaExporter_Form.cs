using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tekla.Structures.Model;
using TSD = Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Solid;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Tekla.Structures.Forming;
using EarClipperLib;

namespace TeklaExporter
{
    public partial class TeklaExporter_Form : Form
    {
        public Model myModel;
        public TeklaExporter_Form()
        {
            InitializeComponent();
        }

        private void TeklaExporter_Form_Load(object sender, EventArgs e)
        {
            myModel = new Model();
            if (!myModel.GetConnectionStatus())
            {
                MessageBox.Show("Tekla Structures not connected");
                return;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {

            string folder_path = "";
            string filename = myModel.GetInfo().ModelPath;

            if (0 == filename.Length)
            {
                filename = myModel.GetInfo().ModelName;
            }
            filename = Path.GetFileNameWithoutExtension(filename) + ".js";
            bool check = SelectFile(ref folder_path, ref filename);
            if (check)
            {
                filename = Path.Combine(folder_path, filename);
            }
            Start();


            int count = 0;
            var allObjects = myModel.GetModelObjectSelector().GetAllObjects();
            Dictionary<string, Dictionary<string, ArrayList>> dictionary = new Dictionary<string, Dictionary<string, ArrayList>>();
            while (allObjects.MoveNext())
            {

                ModelObject objectTekla = allObjects.Current as ModelObject;
                Part part = objectTekla as Part;
                if (part != null)
                {
                    string type = objectTekla.GetType().Name;
                    if (!dictionary.ContainsKey(type))
                    {
                        dictionary.Add(type, new Dictionary<string, ArrayList>());
             
                    }
                    if (!dictionary[type].ContainsKey(part.Profile.ProfileString))
                    {
                        dictionary[type].Add(part.Profile.ProfileString, new ArrayList());
                    }
                    dictionary[type][part.Profile.ProfileString].Add(part);

                    if (!_materials.ContainsKey(part.Class))
                        createMaterial(part.Class);

                    count++;
                }


            }
            int n = count;
            string tilte = "{0} of " + n.ToString() + " terminals processed...";
            string caption = "Export Tekla Data Progress ....";
            using (Progress_Form progress = new Progress_Form(caption, tilte, n))
            {
                foreach (var key1 in dictionary.Keys)
                {
                    var item1 = dictionary[key1];
                    var currentElement1 = new LBContainer.LBObject();
                    _objects.Add(key1, currentElement1);

                    currentElement1.name = key1;
                    currentElement1.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
                    currentElement1.type = "TekalElement";
                    currentElement1.uuid = key1;
                    var childrenType = new List<LBContainer.LBObject>(item1.Count);
                    currentElement1.children = childrenType;
                    foreach (var key2 in item1.Keys)
                    {
                        var item2 = item1[key2];
                        var currentElement2 = new LBContainer.LBObject();
                        currentElement1.children.Add(currentElement2);

                        currentElement2.name = key2;
                        currentElement2.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
                        currentElement1.type = "TekalElement";
                        currentElement2.uuid = key2;
                        var childrenProfile = new List<LBContainer.LBObject>(item2.Count);
                        currentElement2.children = childrenProfile;


                        foreach (var obj in item2)
                        {
                            Part part = obj as Part;
                            var currentElement3 = new LBContainer.LBObject();
                            currentElement2.children.Add(currentElement3);

                            currentElement3.name = part.Name;
                            currentElement3.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
                            currentElement3.type = "Mesh";
                            currentElement3.uuid = part.Identifier.GUID.ToString();
                            currentElement3.geometry = part.Identifier.GUID.ToString();
                            currentElement3.material = part.Class;

                            Solid solid = part.GetSolid() as Solid;

                            List<double> listVertices = new List<double>();
                            FaceEnumerator myFaceEnum = solid.GetFaceEnumerator();
                            while (myFaceEnum.MoveNext())
                            {


                                Face face = myFaceEnum.Current as Face;
                                if (face != null)
                                {
                                    List<Vector3m> points = new List<Vector3m>();
                                    List<List<Vector3m>> holes = new List<List<Vector3m>>();
                                    bool isPolygon = true;
                                    LoopEnumerator myLoopEnum = face.GetLoopEnumerator();

                                    while (myLoopEnum.MoveNext())
                                    {
                                        Loop myLoop = myLoopEnum.Current as Loop;
                                        List<Vector3m> hole = new List<Vector3m>();

                                        if (myLoop != null)
                                        {
                                            VertexEnumerator vertexEnum = myLoop.GetVertexEnumerator() as VertexEnumerator;
                                            List<TSD.Point> listPoint = new List<TSD.Point>();

                                            while (vertexEnum.MoveNext())
                                            {
                                                TSD.Point vertex = vertexEnum.Current as TSD.Point;
                                                if (vertex != null)
                                                {
                                                    if (isPolygon)
                                                        points.Add(new Vector3m(vertex.X, vertex.Y, vertex.Z));
                                                    else
                                                        hole.Add(new Vector3m(vertex.X, vertex.Y, vertex.Z));
                                                }
                                            }
                                            isPolygon = false;
                                            if (hole.Count != 0)
                                                holes.Add(hole.ToList());
                                        }
                                    }
                                    try
                                    {
                                        EarClipping earClipping = new EarClipping();
                                        earClipping = new EarClipping();
                                        earClipping.SetPoints(points, holes);
                                        earClipping.Triangulate();
                                        var res = earClipping.Result;
                                        foreach (var i in res)
                                        {
                                            var aP = adjustPoint(i);
                                            addPoint(listVertices, aP);
                                        }
                                    }
                                    catch { }

                                }


                            }

                            if (listVertices.Count != 0)
                            {
                                var geometry = new LBContainer.LBGeometry();
                                geometry.uuid = part.Identifier.GUID.ToString();
                                geometry.type = "BufferGeometry";
                                geometry.data = new LBContainer.LBGeometryData();
                                var attributes = new LBContainer.Attributes();
                                var position = new LBContainer.Position();
                                position.itemSize = 3;
                                position.type = "Float32Array";
                                position.array = listVertices;
                                var normal = new LBContainer.Normal();
                                normal.itemSize = 3;
                                normal.type = "Float32Array";
                                normal.array = new List<double>();
                                var uv = new LBContainer.UV();
                                uv.itemSize = 2;
                                uv.type = "Float32Array";
                                uv.array = new List<double>();
                                var index = new LBContainer.Index();
                                index.itemSize = 1;
                                index.type = "Uint16Array";
                                index.array = new List<int>();

                                attributes.position = position;
                                attributes.normal = normal;
                                attributes.uv = uv;
                                geometry.data.attributes = attributes;
                                geometry.data.visible = true;
                                geometry.data.castShadow = true;
                                geometry.data.receiveShadow = false;
                                geometry.data.doubleSided = true;
                                geometry.data.scale = 1.0;
                                _geometries.Add(part.Identifier.GUID.ToString(), geometry);
                            }

                            progress.Increment();
                        }


                    }
              
                }
            }
         

            Finish(filename);

        }
        public void addPoint(List<double> list, TSD.Point point)
        {
            list.Add(point.X);
            list.Add(point.Y);
            list.Add(point.Z);
        }
        public TSD.Point adjustPoint(Vector3m point)
        {

            double X = Math.Round((double)point.X / 1000, 6);
            double Y = Math.Round((double)point.Y / 1000, 6);
            double Z = Math.Round((double)point.Z / 1000, 6);

            X = -X;
            double tmp = Y;
            Y = Z;
            Z = tmp;
            var p = new TSD.Point(X, Y, Z);
            return p;
        }
        public List<string> colors = new List<string>()
        {
            "0x000000","0x808080","0xE06666" ,"0x6AA84F","0x0B5394","0x0ABACA","0xF1C232","0x674EA7","0x985004","0xA64D79","0x93C47D","0x3D85C6","0xB4A7D6","0xBF9000","0x073763"
        };
        public void createMaterial(string type)
        {
            LBContainer.LBMaterial m = new LBContainer.LBMaterial();
            var index = int.Parse(type);
            var color = "0x808080";
            if (index<14)
             color=  colors[index];
            m.uuid = type;
            m.name = type;
            m.type = "MeshBasicMaterial";
            m.color = color ;
            m.ambient = color;
            m.emissive = 0;
            m.specular = color;
            m.shininess = 1; // todo: does this need scaling to e.g. [0,100]?
            m.opacity = 1;
            m.transparent = false;
            m.wireframe = false;
            _materials.Add(type, m);
        }
        public static bool SelectFile(ref string folder_path, ref string filename)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "Select JSON Output File";
            dlg.Filter = "JSON files|*.js";

            if (null != folder_path
              && 0 < folder_path.Length)
            {
                dlg.InitialDirectory = folder_path;
            }

            dlg.FileName = filename;

            bool rc = DialogResult.OK == dlg.ShowDialog();

            if (rc)
            {
                filename = Path.Combine(dlg.InitialDirectory,
                  dlg.FileName);

                folder_path = Path.GetDirectoryName(
                  filename);
            }
            return rc;
        }
        LBContainer _container;
        Dictionary<string, LBContainer.LBMaterial> _materials;
        Dictionary<string, LBContainer.LBObject> _objects;
        Dictionary<string, LBContainer.LBGeometry> _geometries;
        Dictionary<string, LBContainer.LBTexture> _textures;
        Dictionary<string, LBContainer.LBImage> _images;

        double _scale_bim = 1.0;

        public void Start()
        {
            _materials = new Dictionary<string, LBContainer.LBMaterial>();
            _geometries = new Dictionary<string, LBContainer.LBGeometry>();
            _objects = new Dictionary<string, LBContainer.LBObject>();
            _textures = new Dictionary<string, LBContainer.LBTexture>();
            _images = new Dictionary<string, LBContainer.LBImage>();

            _container = new LBContainer();

            _container.metadata = new LBContainer.Metadata();
            _container.metadata.type = "Object";
            _container.metadata.version = 1.0;
            _container.metadata.generator = "Tekla LB exporter";
            _container.geometries = new List<LBContainer.LBGeometry>();

            var project = myModel.GetProjectInfo();
            var info = myModel.GetInfo();
            string name = project.Name;
            if (name == "")
                name = Path.GetFileNameWithoutExtension(info.ModelPath);
            _container.obj = new LBContainer.LBObject();
            _container.obj.uuid = project.GUID;
            _container.obj.name = "BIM " + name;
            _container.obj.type = "Scene";

            _container.obj.matrix = new double[] {
                _scale_bim, 0, 0, 0,
                0, _scale_bim, 0, 0,
                0, 0, _scale_bim, 0,
                0, 0, 0, _scale_bim };
        }


        public void ExtractObject()
        {
            //_currentElement = new LBContainer.Va3cObject();

            //_currentElement.name = Util.ElementDescription(e);
            //_currentElement.material = _currentMaterialUid;
            //_currentElement.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            //_currentElement.type = "RevitElement";
            //_currentElement.uuid = uid;
        }

        public void Finish(string filename)
        {
            _container.materials = _materials.Values.ToList();

            _container.geometries = _geometries.Values.ToList();

            _container.obj.children = _objects.Values.ToList();

            _container.textures = _textures.Values.ToList();

            _container.images = _images.Values.ToList();

            JsonSerializerSettings settings
           = new JsonSerializerSettings();

            settings.NullValueHandling = NullValueHandling.Ignore;

            Formatting formatting = Formatting.Indented;

            var myjs = JsonConvert.SerializeObject(
             _container, formatting, settings);

            File.WriteAllText(filename, myjs);
        }
    }
}
