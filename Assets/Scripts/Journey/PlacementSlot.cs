using System.Collections.Generic;
using UnityEngine;

namespace Hiking.Journey
{
    public class PlacementSlot : MonoBehaviour
    {
        public string slotId;
        public SpriteRenderer marker;
        public TextMesh label;
        Mesh frameMesh;
        public Station station;
        public int tileIndex;
        public int SeedCount { get; private set; }
        public void CreateMarker(Material material, float width)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            void Quad(float x0, float z0, float x1, float z1, float y, Color color)
            {
                int v = vertices.Count;
                vertices.Add(new Vector3(x0,y,z0)); vertices.Add(new Vector3(x0,y,z1));
                vertices.Add(new Vector3(x1,y,z1)); vertices.Add(new Vector3(x1,y,z0));
                triangles.AddRange(new[] {v,v+1,v+2,v,v+2,v+3});
                for (int i=0;i<4;i++) colors.Add(color);
            }
            float w = width * .5f;
            Quad(-w,-.4f,w,.4f,0,new Color(.16f,.2f,.2f));
            Quad(-w+.025f,-.375f,w-.025f,-.31f,.005f,Color.white);
            Quad(-w+.025f,.31f,w-.025f,.375f,.005f,Color.white);
            Quad(-w+.025f,-.375f,-w+.09f,.375f,.005f,Color.white);
            Quad(w-.09f,-.375f,w-.025f,.375f,.005f,Color.white);
            frameMesh = new Mesh { name = "PlacementFrame" };
            frameMesh.SetVertices(vertices); frameMesh.SetTriangles(triangles,0); frameMesh.SetColors(colors);
            frameMesh.RecalculateNormals(); frameMesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = frameMesh;
            var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            var tint = new MaterialPropertyBlock(); tint.SetColor("_Color", Color.white); renderer.SetPropertyBlock(tint);
            var text = new GameObject("Count"); text.transform.SetParent(transform,false);
            text.transform.localPosition = new Vector3(0,.12f,0);
            label = text.AddComponent<TextMesh>(); label.fontSize = 48; label.characterSize = .025f;
            label.anchor = TextAnchor.MiddleCenter; label.color = Color.white; label.text = "0";
        }
        public void Highlight(bool active, Color color)
        {
            if (frameMesh == null) return;
            var colors = frameMesh.colors;
            for (int i = 4; i < colors.Length; i++) colors[i] = active ? color : Color.white;
            frameMesh.colors = colors;
        }
        public void ShowPlacement(MaterialDefinition material, int quantity)
        {
            if (material.id == "water") station.AddWater(tileIndex, quantity);
            else if (material.id == "seed")
            {
                SeedCount += quantity;
                if (label != null) { label.text = SeedCount.ToString(); label.color = material.color; }
            }
        }
        void OnDestroy() { if (frameMesh != null) Destroy(frameMesh); }
    }
}
