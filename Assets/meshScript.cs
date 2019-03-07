using UnityEngine;
using System.Collections;
using System.Collections.Generic;


using System.IO;
using System.Text;

public class meshScript : MonoBehaviour
{
    void Start()
    {
        // programatically create meshfilter and meshrenderer and add to gameobject this script is attached to.
        GameObject go = gameObject; // GameObject.Find("GameObjectDp");
        MeshFilter meshFilter = (MeshFilter)go.AddComponent(typeof(MeshFilter));
        MeshRenderer renderer = go.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
    }
 
    public void createMeshGeometry(List<Vector3> vertices, List<int> indices)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
     
        // https://docs.unity3d.com/ScriptReference/MeshTopology.html
        //mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);   // MeshTopology.Points  MeshTopology.LineStrip   MeshTopology.Triangles 
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
        mesh.RecalculateBounds();

    }

    public void MeshToFile(string filename, List<Vector3> vertices, List<int> indices)
    {
        StreamWriter stream = new StreamWriter(filename);
        stream.WriteLine("g " + "Mesh");
        System.Globalization.CultureInfo dotasDecimalSeparator = new System.Globalization.CultureInfo("en-US");

        foreach (Vector3 v in vertices)
            stream.WriteLine(string.Format(dotasDecimalSeparator,"v {0} {1} {2}", v.x, v.y, v.z));

        stream.WriteLine();

        for (int i = 0; i < indices.Count; i += 3)
            stream.WriteLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", indices[i] + 1, indices[i + 1] + 1, indices[i + 2] + 1));

        stream.Close();
        print("Mesh saved to file: " + filename);
    }
}