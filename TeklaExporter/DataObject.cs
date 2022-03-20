using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TeklaExporter
{

    [DataContract]
    public class LBContainer
    {

        public class LBMaterial
        {
            [DataMember]
            public string uuid { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string type { get; set; } // MeshPhongMaterial
            [DataMember]
            public string color { get; set; } // 16777215
            [DataMember]
            public string ambient { get; set; } //16777215
            [DataMember]
            public int emissive { get; set; } // 1
            [DataMember]
            public string specular { get; set; } //1118481
            [DataMember]
            public int shininess { get; set; } // 30
            [DataMember]
            public double opacity { get; set; } // 1
            [DataMember]
            public bool transparent { get; set; } // false
            [DataMember]
            public bool wireframe { get; set; } // false
            [DataMember]
            public string map { get; set; }
        }
        public class LBTexture
        {
            [DataMember]
            public string uuid { get; set; }
            [DataMember]
            public string image { get; set; }
            [DataMember]
            public List<string> wrap { get; set; }
            [DataMember]
            public List<int> repeat { get; set; }
        }
        public class LBImage
        {
            [DataMember]
            public string uuid { get; set; }
            [DataMember]
            public string url { get; set; }
        }

        [DataContract]
        public class LBGeometryData
        {

            [DataMember]
            public Attributes attributes { get; set; } 
            [DataMember]
            public Index index { get; set; }
            [DataMember]
            public double scale { get; set; }
            [DataMember]
            public bool visible { get; set; }
            [DataMember]
            public bool castShadow { get; set; }
            [DataMember]
            public bool receiveShadow { get; set; }
            [DataMember]
            public bool doubleSided { get; set; }
        }

        #region  threejs >=v125
        [DataContract] //
        public class Index
        {
            [DataMember]
            public int itemSize { get; set; }
            [DataMember]
            public string type { get; set; } // "Uint16Array"
            [DataMember]
            public List<int> array { get; set; }
        }
        [DataContract] //
        public class Attributes
        {
            [DataMember]
            public Position position { get; set; }
            [DataMember]
            public Normal normal { get; set; }
            [DataMember]
            public UV uv { get; set; }
        }

        [DataContract]
        public class Position
        {
            [DataMember]
            public int itemSize { get; set; }
            [DataMember]
            public string type { get; set; } // "Float32Array"
            [DataMember]
            public List<double> array { get; set; }
        }
        [DataContract]
        public class Normal
        {
            [DataMember]
            public int itemSize { get; set; }
            [DataMember]
            public string type { get; set; } // "Float32Array"
            [DataMember]
            public List<double> array { get; set; }
        }
        [DataContract]
        public class UV
        {
            [DataMember]
            public int itemSize { get; set; }
            [DataMember]
            public string type { get; set; } // "Float32Array"
            [DataMember]
            public List<double> array { get; set; }
        }
        #endregion


        [DataContract]
        public class LBGeometry
        {
            [DataMember]
            public string uuid { get; set; }
            [DataMember]
            public string type { get; set; } // "BufferGeometry"
            [DataMember]
            public LBGeometryData data { get; set; }
            //[DataMember] public double scale { get; set; }
            [DataMember]
            public List<LBMaterial> materials { get; set; }
        }

        [DataContract]
        public class LBObject
        {
            [DataMember]
            public string uuid { get; set; }
            [DataMember]
            public string name { get; set; } // BIM <document name>
            [DataMember]
            public string type { get; set; } // Object3D
            [DataMember]
            public double[] matrix { get; set; } // [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]
            [DataMember]
            public List<LBObject> children { get; set; }


            [DataMember]
            public string geometry { get; set; }
            [DataMember]
            public string material { get; set; }

            [DataMember]
            public Dictionary<string, string> userData { get; set; }
        }

    

        public class Metadata
        {
            [DataMember]
            public string type { get; set; } 
            [DataMember]
            public double version { get; set; } 
            [DataMember]
            public string generator { get; set; } 
        }

        [DataMember]
        public Metadata metadata { get; set; }
        [DataMember(Name = "object")]
        public LBObject obj { get; set; }
        [DataMember]
        public List<LBGeometry> geometries;
        [DataMember]
        public List<LBMaterial> materials;
        [DataMember]
        public List<LBTexture> textures;
        [DataMember]
        public List<LBImage> images;
    }
}
