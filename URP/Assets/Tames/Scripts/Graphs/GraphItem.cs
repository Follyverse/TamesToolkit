using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Graphs
{
    public class GraphItem
    {
        public float[] values;
        public int index;
        public GameObject gameObject, label;
        public Vector3 initialPosition;
        public Markers.MarkerGraph marker;
        public Transform transform;
        public virtual void Scale(float s, float total = 0)
        {

        }
    }
    public class BarGraphItem : GraphItem
    {
        public Vector3 basePoint, topPoint, labelInitialPosition;
        public int axis, side;
        public float initialScale;
        public void Initialize(Markers.InputSetting.Axis yAx)
        {
            transform = gameObject.transform;
            initialPosition = transform.localPosition;
            axis = yAx switch
            {
                Markers.InputSetting.Axis.X => 1,
                Markers.InputSetting.Axis.Y => 2,
                Markers.InputSetting.Axis.Z => 3,
                Markers.InputSetting.Axis.NegX => -1,
                Markers.InputSetting.Axis.NegY => -2,
                Markers.InputSetting.Axis.NegZ => -3,
                _ => 2
            };
            side = axis < 0 ? -1 : 1;
            axis = axis < 0 ? -axis - 1 : axis - 1;
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Mesh mesh = mf.sharedMesh;
                Vector3[] vs = mesh.vertices;
                Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
                for (int i = 0; i < vs.Length; i++)
                {
                    min = Vector3.Min(min, vs[i]);
                    max = Vector3.Max(max, vs[i]);
                }

                Vector3 ax = axis == 0 ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);
                Vector3 top = side == 1 ? Vector3.Scale(ax, max) - Vector3.Scale(ax, transform.localPosition) : Vector3.Scale(ax, min) - Vector3.Scale(ax, transform.localPosition);
                Vector3 bot = side == 1 ? Vector3.Scale(ax, min) - Vector3.Scale(ax, transform.localPosition) : Vector3.Scale(ax, max) - Vector3.Scale(ax, transform.localPosition);
                top += transform.localPosition;
                bot += transform.localPosition;
                labelInitialPosition = label.transform.position;
                basePoint = transform.TransformPoint(bot);
                topPoint = transform.TransformPoint(top);
            }
        }
        override public void Scale(float s, float total = 0)
        {
            Vector3 scale = transform.localScale;
            scale[axis] = initialScale * s;
            transform.localScale = scale;
            transform.localPosition = initialPosition + (1 - s) * (basePoint - initialPosition);
            Vector3 p = initialPosition + s * (topPoint - initialPosition);
            label.transform.position = p + labelInitialPosition - topPoint;
        }
    }
    public class PieGraphItem : GraphItem
    {
        MeshFilter mf;
        float lastFrom, lastTo =1;
        public PieGraphItem(Material m)
        {
            gameObject = new GameObject();
            transform = gameObject.transform;
            transform.parent = marker.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;  

            mf = gameObject.AddComponent<MeshFilter>();
            MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;

        }
        public override void Scale(float s, float end = 0)
        {
            if (s == lastFrom && end == lastTo) return;
            lastFrom = s;
            lastTo = end;
            float from = s * 360;
            float to = end * 360;
            int c = (int)((to - from) / 5);
            int c1 = c + 1;
            float d = (to - from) / (c + 1);
            Vector3[] vs = new Vector3[(c + 1) * 4 + 2];
            float sin, cos, a;
            for (int i = 0; i < c; i++)
            {
                a = Mathf.Deg2Rad * (from + i * d);
                sin = Mathf.Sin(a);
                cos = Mathf.Cos(a);
                vs[i] = vs[i + c1] = new Vector3(cos, sin, 0);
                vs[i + 2 * c1] = vs[i + 3 * c1] = new Vector3(cos, sin, 0.2f);
            }
            a = Mathf.Deg2Rad * to;
            sin = Mathf.Sin(a);
            cos = Mathf.Cos(a);
            vs[c] = vs[2 * c1 - 1] = new Vector3(cos, sin, 0);
            vs[3 * c1 - 1] = vs[4 * c1 - 1 - 1] = new Vector3(cos, sin, 0.2f);
            vs[4 * c1] = Vector3.zero;
            vs[^1] = new Vector3(0, 0, 0.2f);

            Vector3[] ns = new Vector3[(c + 1) * 4 + 2];
            Vector2[] uvs = new Vector2[ns.Length];
            for (int i = 0; i < c1; i++)
            {
                ns[i] = -(ns[i + 3 * c1] = -Vector3.forward);
                ns[i + c1] = ns[i + 2 * c1] = vs[i];
                uvs[i] = uvs[i + c1] = uvs[i + 2 * c1] = uvs[i + 3 * c1] = new Vector2(vs[i].x / 2 + 0.5f, vs[i].y / 2 + 0.5f);
            }
            ns[4 * c1] = -(ns[4 * c1 + 1] = -Vector3.forward);

            int[] ts = new int[c1 * 12];
            int k, tk;
            for (int i = 0; i < c1; i++)
            {
                //front
                k = i * 3;
                ts[k] = i;
                ts[k + 1] = i + 1;
                ts[k + 2] = vs.Length - 2;
                // back
                tk = c1 * 9;
                ts[k + tk] = i + tk;
                ts[k + tk + 1] = i + tk + 1;
                ts[k + tk + 2] = vs.Length - 1;
                tk = c1 * 3;
                ts[k + tk] = i + c1;
                ts[k + tk + 1] = i + c1 + 1;
                ts[k + tk + 2] = i + c1 * 2;
                ts[k + tk * 2] = i + tk + 1;
                ts[k + tk * 2 + 1] = i + tk * 2 + 1;
                ts[k + tk * 2 + 2] = i + tk * 2;
            }
            Mesh m = mf.sharedMesh;
            m.vertices = vs;
            m.triangles = ts;
            m.normals = ns;
            mf.sharedMesh = m;
        }
    }

}