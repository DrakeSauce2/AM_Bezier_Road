using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter))]
public class DrawCurves : MonoBehaviour
{
    [SerializeField] Mesh2D shape2D;

    [Range(2, 32)]
    [SerializeField] float edgeRingCount = 8;


    [Range(0, 1)]
    [SerializeField] float tTest = 0;
    
    [SerializeField] Transform[] controlPoints = new Transform[4];
    Vector3 GetPos(int i) => controlPoints[i].position;

    public Slider slider;


    Mesh mesh;



    void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Segment";

        slider.value = edgeRingCount;

        GetComponent<MeshFilter>().sharedMesh = mesh;

        Cursor.visible = true;
    }

    void Update()
    {
        GenerateMesh();

        edgeRingCount = slider.value;

        Mathf.Clamp(slider.value, 2, 32);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            Debug.Log("Quitting");
        }
    }

    
    void GenerateMesh()
    {
        mesh.Clear();

        // Vertices
        float uSpan = shape2D.CalcUspan();

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        for (int ring = 0; ring < edgeRingCount; ring++)
        {
            float t = ring / (edgeRingCount - 1f);

            OrientedPoint op = GetBezierOP(t);

            for (int i = 0; i < shape2D.vertices.Length; i++)
            {
                verts.Add(op.LocalToWorldPos(shape2D.vertices[i].point));
                normals.Add(op.LocalToWorldVect(shape2D.vertices[i].normal));
                uvs.Add(new Vector2(shape2D.vertices[i].u, t / GetApproxLength() / uSpan));
            }
        }

        //Triangles
        List<int> triIndices = new List<int>();
        for (int ring = 0; ring < edgeRingCount-1; ring++)
        {
            int rootIndex = ring * shape2D.VertexCount;
            int rootIndexNext = (ring + 1) * shape2D.VertexCount;

            for (int line = 0; line < shape2D.LineCount; line += 2)
            {
                int lineIndexA = shape2D.lineIndices[line];
                int lineIndexB = shape2D.lineIndices[line+1];

                int currentA = rootIndex + lineIndexA;
                int currentB = rootIndex + lineIndexB;

                int nextA = rootIndexNext + lineIndexA;
                int nextB = rootIndexNext + lineIndexB;


                triIndices.Add(currentA);
                triIndices.Add(nextA);
                triIndices.Add(nextB);

                triIndices.Add(currentA);
                triIndices.Add(nextB);
                triIndices.Add(currentB);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triIndices, 0);


    }

    float GetApproxLength(int precision = 8)
    {
        Vector3[] points = new Vector3[precision];
        for (int i = 0; i < precision; i++)
        {
            float t = i / (precision-1);
            points[i] = GetBezierOP(i).pos;
        }

        float dist = 0;
        for (int i = 0; i < precision-1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            dist += Vector3.Distance(a, b);
        }

        return dist;
    }


    public void OnDrawGizmos()
    {

        for (int i = 0; i < controlPoints.Length; i++)
        {
            Gizmos.DrawSphere(GetPos(i), 0.5f);
        }
        Handles.color = Color.black;
        Handles.DrawLine(GetPos(0), GetPos(1));
        Handles.DrawLine(GetPos(3), GetPos(2));
        Handles.DrawBezier(GetPos(0), GetPos(3), GetPos(1), GetPos(2), Color.green, null, 4f);
       
        

        Gizmos.color = Color.red;

        OrientedPoint testPoint = GetBezierOP(tTest);
        Handles.PositionHandle(testPoint.pos, testPoint.rot);

        Vector3[] verts =  shape2D.vertices.Select(v => testPoint.LocalToWorldPos(v.point)).ToArray();

        for (int i = 0; i < shape2D.lineIndices.Length; i+=2)
        {
            Vector3 a = verts[shape2D.lineIndices[i]];
            Vector3 b = verts[shape2D.lineIndices[i + 1]];

            Gizmos.DrawLine(a, b);
        }
    }

    OrientedPoint GetBezierOP(float t)
    {
        Vector3 p0 = GetPos(0);
        Vector3 p1 = GetPos(1);
        Vector3 p2 = GetPos(2);
        Vector3 p3 = GetPos(3);

        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);
        Vector3 c = Vector3.Lerp(p2, p3, t);

        Vector3 d = Vector3.Lerp(a, b, t);
        Vector3 e = Vector3.Lerp(b, c, t);

        Vector3 pos = Vector3.Lerp(d, e, t);
        Vector3 tangent = (e - d).normalized;

        return new OrientedPoint(pos, tangent);
    }
}
