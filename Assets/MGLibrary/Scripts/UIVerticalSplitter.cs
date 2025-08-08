using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class UIVerticalSplitter : MonoBehaviour
    {
        public float[] ratios;

        private void Reset()
        {
            var verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childForceExpandHeight = true;
            verticalLayoutGroup.childForceExpandWidth = true;
            ratios = null;
            ratios = new float[transform.childCount];
        }

        private void Start()
        {
            VerticalSplitter();
        }

        [ContextMenu("Vertical Splitter")]
        private void VerticalSplitter()
        {
            if (ratios.Length <= 0) return;
            var totalRatio = ratios.Sum();
            var parentRect = GetComponent<RectTransform>();
            var parentHeight = parentRect.rect.height;
            var childRects = new List<RectTransform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                childRects.Add(transform.GetChild(i).GetComponent<RectTransform>());
            }

            for (var i = 0; i < childRects.Count; i++)
            {
                var childHeight = ratios[i] / totalRatio * parentHeight;
                childRects[i].sizeDelta = new Vector2(parentRect.rect.width, childHeight);
            }
        }
    }
}