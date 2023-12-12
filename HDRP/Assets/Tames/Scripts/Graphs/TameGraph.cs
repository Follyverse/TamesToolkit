using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Graphs
{
    public class TameGraph
    {
        public Markers.MarkerGraph marker;
        public Material[] material;
        public List<GraphItem> items;
        public GameObject gameObject;
        internal float[,] values;
        internal int row, col;
        public Tames.TameElement element;
        Markers.GraphType type;
        public void ExtractMaterial()
        {
            material = new Material[col];
            MeshRenderer mr;
            Material m;
            switch (type)
            {
                case Markers.GraphType.Bar:
                    for (int i = 0; i < marker.visual.Length; i++)
                        if (marker.visual[i] != null && i < col)
                        {
                            mr = marker.visual[i].GetComponent<MeshRenderer>();
                            if (mr != null)
                                material[i] = mr.sharedMaterial;
                        }
                    break;
                case Markers.GraphType.Pie:
                    for (int i = 0; i < col; i++)
                    {
                        if (i < marker.materials.Length)
                            if (marker.materials[i] != null)
                                material[i] = marker.materials[i];
                            else
                            {
                                if (Utils.HDActive)
                                    m = new Material(Shader.Find("HDRP/Lit"));
                                else
                                    m = new Material(Shader.Find("Standard"));

                                if (m != null)
                                    if (i < marker.colors.Length)
                                        m.SetColor(Utils.ProperyKeywords[Tames.TameMaterial.BaseColor], marker.colors[i]);
                                material[i] = m;
                            }
                    }
                    break;
            }
        }
        public virtual void Update(int from, int to, float p)
        {

        }
        public virtual void Initialize()
        {

        }
    }
    public class BarGraph : TameGraph
    {

    }
    public class PieGraph : TameGraph
    {
        public MeshRenderer renderer;
        public MeshFilter meshFilter;
        public Mesh mesh;
        public static int Count = 360;
        public static int VertexPerPie = 4;
        public static int Back = Count * VertexPerPie;
        public static int Front = Count * VertexPerPie + 1;
        public void CreateCircle()
        {
            int C = Count;
            Vector3[] vs = new Vector3[C * 4 + 2];
            Vector3[] ns = new Vector3[C * 4 + 2];
            Vector2[] uv = new Vector2[C * 4 + 2];
            for (int i = 0; i < C; i++)
            {
                vs[i] = vs[i + C] = new Vector3(Mathf.Cos(2 * i * Mathf.PI / C), Mathf.Cos(2 * i * Mathf.PI / C), 0);
                vs[i + C * 2] = vs[i + C * 3] = new Vector3(Mathf.Cos(2 * i * Mathf.PI / C), Mathf.Cos(2 * i * Mathf.PI / C), marker.thickness);

                ns[i] = -Vector3.forward;
                ns[i + C] = ns[i + C * 2] = vs[i];
                ns[i + C * 3] = Vector3.forward;

                uv[i] = uv[i + C] = uv[i + C * 2] = uv[i + C * 3] = new Vector2((vs[i].x + 1) / 2, (vs[i].y + 1) / 2);
            }
            vs[Back] = Vector3.zero;
            vs[Front] = marker.thickness * Vector3.forward;
            ns[Back] = -Vector3.forward;
            ns[Front] = Vector3.forward;
            uv[Back] = uv[Front] = 0.5f * Vector2.one;

            int Q = VertexPerPie * 3;
            int[] ts = new int[C * Q];
            for (int i = 0; i < C; i++)
            {
                ts[i * Q] = Back;
                ts[i * Q + 1] = i;
                ts[i * Q + 2] = (i + 1) % C;

                ts[i * Q + 3] = i + C;
                ts[i * Q + 4] = (i + 1) % C + C;
                ts[i * Q + 5] = i + C * 2;

                ts[i * Q + 6] = (i + 1) % C + C;
                ts[i * Q + 7] = (i + 1) % C + C * 2;
                ts[i * Q + 8] = i + C * 2;

                ts[i * Q + 9] = Front;
                ts[i * Q + 10] = i + C * 3;
                ts[i * Q + 11] = (i + 1) % C + C * 3;
            }
            meshFilter = gameObject.AddComponent<MeshFilter>();
            mesh = new Mesh();
            mesh.vertices = vs;
            mesh.triangles = ts;
            mesh.normals = ns;
            mesh.uv = uv;
            meshFilter.sharedMesh = mesh;
            renderer = gameObject.AddComponent<MeshRenderer>();

        }
        bool firstUpdate;
        override public void Initialize()
        {
            // CreateMaterials();
            values = marker.GetValues(out col, out row);

            ExtractMaterial();

            items = new List<GraphItem>();
           
            for (int j = 0; j < Count; j++)
            {
                float total = 0;
                for (int i = 0; i < col; i++)
                    total += values[i, j] == float.NaN ? 0 : values[i, j];
                for (int i = 0; i < col; i++)
                    values[i, j] = values[i, j] == float.NaN ? 0 : values[i, j] / total;
            }
            CreateCircle();
            firstUpdate = true;
            Update(0, 1, 0);
        }
        override public void Update(int from, int to, float p)
        {
            float[] toV = new float[col];
            for (int i = 0; i < col; i++)
                toV[i] = values[i, from] + p * (values[i, to] - values[i, from]);
            int[,] index = new int[col, 2];
            index[0, 0] = 0;
            for (int i = 0; i < col; i++)
            {
                if (i > 0) index[i, 0] = index[i - 1, 1];
                index[i, 1] = (int)toV[i] * Count;
                if (index[i, 1] >= Count) index[i, 1] = Count - 1;
            }
            UnityEngine.Rendering.SubMeshDescriptor sm;
            if (firstUpdate) mesh.subMeshCount = col;

            for (int i = 0; i < col; i++)
            {
                sm = firstUpdate ? new UnityEngine.Rendering.SubMeshDescriptor() : mesh.GetSubMesh(i);
                sm.indexStart = index[i, 0] * 12;
                sm.indexCount = (index[i, 1] - index[i, 0]) * 12;
                sm.topology = MeshTopology.Triangles;
                sm.firstVertex = index[i, 0] * 4;
                sm.vertexCount = Count * 4 + 2 - sm.firstVertex;
                mesh.SetSubMesh(i, sm);
            }
            firstUpdate = false;
        }

    }
}
