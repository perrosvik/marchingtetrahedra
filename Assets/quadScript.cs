using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;

public class quadScript : MonoBehaviour
{
    private meshScript mScript;
    private static readonly int xdim = 100;
    private static readonly int ydim = 100;
    private static readonly int zdim = 100;

    private float step = (1.0f / 100.0f);

    private float iso;

    private Texture2D texture;

    private int slice;

    private List<Vector3> vertices;
    private List<int> indices;
    private int index;

    private Slice[] _slices;
    private string dicomfilepath;

    private int _numSlices;
    private int _minIntensity;
    private int _maxIntensity;
    private int _sliceNum;
    private float _iso;
    private float _brightness;
    private int _xdim;
    private int _ydim;
    private int _zdim;
    private Vector3 _voxelSize;
    private float min = 0;
    private float max = 0;

    void Start()
    {
        print("void Start was called!");
        //SetSlice(50);
        //MarchingSquares(0.5f);

        print("loading dicoms....");
        Slice.initDicom();
        dicomfilepath = Application.dataPath + @"\..\dicomdata\head";
        _numSlices = _numSlices = Slice.getnumslices(dicomfilepath);
        _slices = new Slice[_numSlices];
        Slice.getSlices(dicomfilepath, _numSlices, out _slices, out min, out max);
        SliceInfo info = _slices[0].sliceInfo;
        _minIntensity = (int)min;
        _maxIntensity = (int)max;
        _iso = (_minIntensity + _maxIntensity) / 2;
        _xdim = info.Rows;
        _ydim = info.Columns;
        _zdim = _numSlices;
        print("finished loading dicoms.");
    }

    void Update(){  }

    void SetSlice(int z)
    {
        this.slice = z;
        texture = new Texture2D(xdim, ydim, TextureFormat.RGB24, false);     // garbage collector will tackle that it is new'ed 

        for (int y = 0; y < ydim; y++)
            for (int x = 0; x < xdim; x++)
            {
                float v = PixelVal(new Vector3(x, y, z));//50.0f;
                texture.SetPixel(x, y, new Color(v, v, v));
            }
        texture.filterMode = FilterMode.Point;  // nearest neigbor interpolation is used.  (alternative is FilterMode.Bilinear)
        texture.Apply();  // Apply all SetPixel calls
        GetComponent<Renderer>().material.mainTexture = texture;
    }
    
    void MarchingSquares(float iso)
    {
        vertices = new List<Vector3>();
        indices = new List<int>();

        index = 0;

        for (int y = 0; y < ydim; y++)
        {
            for (int x = 0; x < xdim; x++)
            {

                //    d---c
                //    |   |
                //    a---b

                float worldX = (x / 100.0f) - 0.5f;
                float worldY = (y / 100.0f) - 0.5f;
              
                Vector3 pa = new Vector3(worldX, worldY, 0.0f);
                Vector3 pb = new Vector3(worldX + step, worldY, 0.0f);
                Vector3 pc = new Vector3(worldX + step, worldY + step, 0.0f);
                Vector3 pd = new Vector3(worldX, worldY + step, 0.0f);

                float va = PixelVal(new Vector3(x, y, slice));
                float vb = PixelVal(new Vector3(x + 1, y, slice));
                float vc = PixelVal(new Vector3(x + 1, y + 1, slice));
                float vd = PixelVal(new Vector3(x, y + 1, slice));

                bool ba = va >= iso;
                bool bb = vb >= iso;
                bool bc = vc >= iso;
                bool bd = vd >= iso;

                string pattern = (bd ? "1" : "0") + (bc ? "1" : "0") + (bb ? "1" : "0") + (ba ? "1" : "0");
                switch (pattern)
                {
                    case "1110":
                    case "0001":
                        vertices.Add(lerp(pa, pb, va, vb, iso));
                        vertices.Add(lerp(pa, pd, va, vd, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "1101":
                    case "0010":
                        vertices.Add(lerp(pa, pb, va, vb, iso));
                        vertices.Add(lerp(pb, pc, vb, vc, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "1011":
                    case "0100":
                        vertices.Add(lerp(pb, pc, vb, vc, iso));
                        vertices.Add(lerp(pc, pd, vc, vd, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "0111":
                    case "1000":
                        vertices.Add(lerp(pa, pd, va, vd, iso));
                        vertices.Add(lerp(pc, pd, vc, vd, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "1100":
                    case "0011":
                        vertices.Add(lerp(pa, pd, va, vd, iso));
                        vertices.Add(lerp(pb, pc, vb, vc, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "1001":
                    case "0110":
                        vertices.Add(lerp(pa, pb, va, vb, iso));
                        vertices.Add(lerp(pc, pd, vc, vd, iso));
                        indices.Add(index + 0);
                        indices.Add(index + 1);
                        break;
                    case "0000":
                    case "1111":
                    // do nothing.
                    default:
                        continue;
                }
                // increment the index counter if we added some new vertices.
                index += 2;
            }
        }
        mScript = GameObject.Find("GameObjectMesh").GetComponent<meshScript>();
        mScript.createMeshGeometry(vertices, indices);
    }

    Vector3 lerp(Vector3 p1, Vector3 p2, float v1, float v2, float iso)
    {
        Vector3 plerp;
        float vlerp;
        if (v1 < v2)
        {
            vlerp = 1.0f - (iso - v1) / (v2 - v1);
            plerp = Vector3.Lerp(p2, p1, vlerp);
        }
        else
        {
            vlerp = 1.0f - (iso - v2) / (v1 - v2);
            plerp = Vector3.Lerp(p1, p2, vlerp);
        }
        return plerp;
    }

    void MarchingTetrahederOnCTScan()
    {
        vertices = new List<Vector3>();
        indices = new List<int>();
        index = 0;
        for (int z = 0; z < _zdim-1; z++)
        {
            ushort[] pixels = _slices[z].getPixels();
            ushort[] nextSlicePixels = _slices[z+1].getPixels();
            float worldZ = (z / 100.0f) - 0.5f;

            for (int y = 0; y < _ydim-1; y++)
            {
                float worldY = (y / 100.0f) - 0.5f;
                for (int x = 0; x < _xdim-1; x++)
                {
                    float worldX = (x / 100.0f) - 0.5f;
                          
                    Vector3 p4 = new Vector3(worldX, worldY, worldZ);
                    Vector3 p5 = new Vector3(worldX + step, worldY, worldZ);
                    Vector3 p7 = new Vector3(worldX + step, worldY + step, worldZ);
                    Vector3 p6 = new Vector3(worldX, worldY + step, worldZ);

                    Vector3 p0 = new Vector3(worldX, worldY, worldZ + step);
                    Vector3 p1 = new Vector3(worldX + step, worldY, worldZ + step);
                    Vector3 p3 = new Vector3(worldX + step, worldY + step, worldZ + step);
                    Vector3 p2 = new Vector3(worldX, worldY + step, worldZ + step);
                    
                    float v4 = pixels[x + y * _xdim];
                    float v5 = pixels[(x + 1) + y * _xdim];
                    float v7 = pixels[(x + 1) + (y + 1) * _xdim];
                    float v6 = pixels[x + (y + 1) * _xdim];

                    float v0 = nextSlicePixels[x + y * _xdim];
                    float v1 = nextSlicePixels[(x + 1) + y * _xdim];
                    float v3 = nextSlicePixels[(x + 1) + (y + 1) * _xdim];
                    float v2 = nextSlicePixels[x + (y + 1) * _xdim];

                    DoTetra(_iso, p4, p6, p0, p7, v4, v6, v0, v7);
                    DoTetra(_iso, p6, p0, p7, p2, v6, v0, v7, v2);
                    DoTetra(_iso, p0, p7, p2, p3, v0, v7, v2, v3);
                    DoTetra(_iso, p4, p5, p7, p0, v4, v5, v7, v0);
                    DoTetra(_iso, p1, p7, p0, p3, v1, v7, v0, v3);
                    DoTetra(_iso, p0, p5, p7, p1, v0, v5, v7, v1);
                }
            }
        }
        mScript = GameObject.Find("GameObjectMesh").GetComponent<meshScript>();
        mScript.createMeshGeometry(vertices, indices);
        print("generate from ct scan completed");
    }

    void MarchingTetraheder(float iso)
    {
        vertices = new List<Vector3>();
        indices = new List<int>();
        index = 0;
        for (float z = 0; z < zdim; z++)
        {
            float worldZ = (z / 100.0f) - 0.5f;
            for (int x = 0; x < xdim; x++)
            {
                float worldX = (x / 100.0f) - 0.5f;
                for (int y = 0; y < ydim; y++)
                {
                    float worldY = (y / 100.0f) - 0.5f;
                    
                    Vector3 p4 = new Vector3(worldX, worldY, worldZ);
                    Vector3 p5 = new Vector3(worldX + step, worldY, worldZ);
                    Vector3 p7 = new Vector3(worldX + step, worldY + step, worldZ);
                    Vector3 p6 = new Vector3(worldX, worldY + step, worldZ);

                    Vector3 p0 = new Vector3(worldX, worldY, worldZ+step);
                    Vector3 p1 = new Vector3(worldX + step, worldY, worldZ+step);
                    Vector3 p3 = new Vector3(worldX + step, worldY + step, worldZ+step);
                    Vector3 p2 = new Vector3(worldX, worldY + step, worldZ+step);

                    float v4 = PixelVal(new Vector3(x, y, z));
                    float v5 = PixelVal(new Vector3(x + 1, y, z));
                    float v7 = PixelVal(new Vector3(x + 1, y + 1, z));
                    float v6 = PixelVal(new Vector3(x, y + 1, z));

                    float v0 = PixelVal(new Vector3(x, y, z+1));
                    float v1 = PixelVal(new Vector3(x + 1, y, z+1));
                    float v3 = PixelVal(new Vector3(x + 1, y + 1, z+1));
                    float v2 = PixelVal(new Vector3(x, y + 1, z+1));

                    DoTetra(iso, p4, p6, p0, p7, v4, v6, v0, v7);
                    DoTetra(iso, p6, p0, p7, p2, v6, v0, v7, v2);
                    DoTetra(iso, p0, p7, p2, p3, v0, v7, v2, v3);
                    DoTetra(iso, p4, p5, p7, p0, v4, v5, v7, v0);
                    DoTetra(iso, p1, p7, p0, p3, v1, v7, v0, v3);
                    DoTetra(iso, p0, p5, p7, p1, v0, v5, v7, v1);
                }
            }
        }
        mScript = GameObject.Find("GameObjectMesh").GetComponent<meshScript>();
        mScript.createMeshGeometry(vertices, indices);
    }

    void DoTetra(float iso, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float v1, float v2, float v3, float v4)
    {
        bool b1 = v1 >= iso;
        bool b2 = v2 >= iso;
        bool b3 = v3 >= iso;
        bool b4 = v4 >= iso;

        Vector3 p12 = lerp(p1, p2, v1, v2, iso);    // (p1 + p2) / 2;
        Vector3 p13 = lerp(p1, p3, v1, v3, iso);    // (p1 + p3) / 2;
        Vector3 p14 = lerp(p1, p4, v1, v4, iso);    // (p1 + p4) / 2;
        Vector3 p23 = lerp(p2, p3, v2, v3, iso);    // (p2 + p3) / 2;
        Vector3 p24 = lerp(p2, p4, v2, v4, iso);    // (p2 + p4) / 2;
        Vector3 p34 = lerp(p3, p4, v3, v4, iso);    // (p3 + p4) / 2;

        string pattern = (b1 ? "1" : "0") + (b2 ? "1" : "0") + (b3 ? "1" : "0") + (b4 ? "1" : "0");

        switch (pattern)
        {
            case "0001":
                MakeTri(p14, p24, p34);
                break;
            case "1110":
                MakeTri(p14, p34, p24);
                break;
            case "0010":
                MakeTri(p13, p34, p23);
                break;
            case "1101":
                MakeTri(p13, p23, p34);
                break;
            case "0100":
                MakeTri(p12, p23, p24);
                break;
            case "1011":
                MakeTri(p12, p24, p23);
                break;
            case "0111":
                MakeTri(p12, p13, p14);
                break;
            case "1000":
                MakeTri(p12, p14, p13);
                break;
            case "0011":
                MakeQuad(p13, p14, p24, p23);
                break;
            case "1100":
                MakeQuad(p13, p23, p24, p14);
                break;
            case "0101":
                MakeQuad(p12, p23, p34, p14);
                break;
            case "1010":
                MakeQuad(p12, p14, p34, p23);
                break;
            case "0110":
                MakeQuad(p12, p13, p34, p24);
                break;
            case "1001":
                MakeQuad(p12, p24, p34, p13);
                break;
            case "0000":
            case "1111":
            // do nothing.
            default:
                break;
        }
    }

    void MakeTri(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        vertices.Add(p1);
        vertices.Add(p2);
        vertices.Add(p3);
        indices.Add(index + 0);
        indices.Add(index + 1);
        indices.Add(index + 2);
        index += 3;
    }

    void MakeQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        MakeTri(p1, p2, p3);
        MakeTri(p1, p3, p4);
    }

    private static Vector3 origo = new Vector3(50.0f, 50.0f, 50.0f);
    float PixelVal(Vector3 point)
    {
        float magnitude = (origo - point).magnitude;
        float intensity = magnitude / 50.0f;
        return intensity;
    }

    public void SlicePosSliderChange(float val)
    {
        SetSlice((int)(val*100));
        print(val);
    }

    public void SliceIsoSliderChange(float val)
    {
        //MarchingSquares(val);
        MarchingTetraheder(val);
        //MarchingTetrahederOnCTScan(val);
        //this.iso = val;
    }

    public void button1Pushed()
    {
        print("button1Pushed");
        MarchingTetrahederOnCTScan();
    }

    public void button2Pushed()
    {
        print("button2Pushed");
        mScript.MeshToFile("savedmesh.obj", vertices, indices);
    }

}