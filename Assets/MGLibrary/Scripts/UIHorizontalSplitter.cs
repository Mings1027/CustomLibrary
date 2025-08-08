using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class UIHorizontalSplitter : MonoBehaviour
    {
        [SerializeField] private float[] ratios; // 각 자식 요소의 비율

        private void Reset()
        {
            var horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlHeight = true;
            horizontalLayoutGroup.childForceExpandHeight = true;
            horizontalLayoutGroup.childForceExpandWidth = true;
            ratios = null;
            ratios = new float[transform.childCount];
        }

        private void Start()
        {
            HorizontalSplitter();
        }

        [ContextMenu("Horizontal Splitter")]
        private void HorizontalSplitter()
        {
            if (ratios.Length <= 0) return;
            var totalRatio = ratios.Sum();
            var parentWidth = GetComponent<RectTransform>().rect.width;
            var childRects = new List<RectTransform>();
            for (var i = 0; i < transform.childCount; i++)
            {
                childRects.Add(transform.GetChild(i).GetComponent<RectTransform>());
            }

            for (var i = 0; i < childRects.Count; i++)
            {
                var childWidth = ratios[i] / totalRatio * parentWidth;
                childRects[i].sizeDelta = new Vector2(childWidth, childRects[i].sizeDelta.y);
            }
        }

    }
}