using UnityEngine;

namespace Hiking.Journey
{
    public class PlacementSlot : MonoBehaviour
    {
        public string slotId;
        public SpriteRenderer marker;
        public TextMesh label;
        int count;

        public void Highlight(bool active, Color color)
        {
            if (marker != null) SetTint(marker, active ? color : new Color(.3f, .42f, .4f, .55f));
        }

        public void ShowPlacement(MaterialDefinition material, int quantity)
        {
            count += quantity;
            if (label != null) label.text = count.ToString();
            var token = new GameObject("Placed_" + material.id);
            token.transform.SetParent(transform, false);
            // Compact visible stack; the full record remains in JourneySession.
            token.transform.localPosition = new Vector3((count - 1) % 4 * .23f - .35f, .35f + ((count - 1) / 4 % 3) * .2f, -.1f);
            token.transform.localScale = Vector3.one * .2f;
            var renderer = token.AddComponent<SpriteRenderer>();
            renderer.sprite = marker.sprite;
            renderer.sharedMaterial = marker.sharedMaterial;
            SetTint(renderer, material.color);
            renderer.sortingOrder = 6;
        }
        static void SetTint(SpriteRenderer renderer, Color color)
        {
            renderer.color = color;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name == "Hiking/ArcWindow")
            {
                // Meshes use vertex colors; SpriteRenderer batching gets an explicit per-object tint.
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", color);
                properties.SetFloat("_UseVertexColor", 0);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
