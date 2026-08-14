using UnityEngine;
using UnityEngine.UI;

namespace QuartzDistribution
{
    public sealed class QuartzLegendItem : MonoBehaviour
    {
        [Header("直接在场景中编辑")]
        [SerializeField] private string resourceTypeId;
        [SerializeField] private string displayName;
        [TextArea(2, 4)] [SerializeField] private string description;
        [SerializeField] private Color resourceColor = Color.white;
        [Header("节点引用")]
        [SerializeField] private Toggle toggle;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Graphic colorSwatch;
        [SerializeField] private GameObject selectedRail;
        [SerializeField] private GameObject selectedFill;
        [SerializeField] private Outline outline;

        public string ResourceTypeId { get { return resourceTypeId; } }
        public Toggle Toggle { get { return toggle; } }

        public void Initialize()
        {
            titleText.text = displayName;
            descriptionText.text = description;
            colorSwatch.color = resourceColor;
            Image railImage = selectedRail.GetComponent<Image>();
            if (railImage != null) railImage.color = resourceColor;
            SetSelected(toggle.isOn);
        }

        public void SetSelected(bool selected)
        {
            if (selectedRail != null) selectedRail.SetActive(selected);
            if (selectedFill != null) selectedFill.SetActive(selected);
            if (outline != null) outline.effectColor = selected ? new Color(0f, .74f, .95f, 1f) : new Color(0f, .75f, 1f, .35f);
        }

#if UNITY_EDITOR
        public void EditorConfigure(string id, string title, string details, Color color, Toggle itemToggle,
            Text titleLabel, Text detailsLabel, Graphic swatch, GameObject rail, GameObject fill, Outline itemOutline)
        {
            resourceTypeId = id;
            displayName = title;
            description = details;
            resourceColor = color;
            toggle = itemToggle;
            titleText = titleLabel;
            descriptionText = detailsLabel;
            colorSwatch = swatch;
            selectedRail = rail;
            selectedFill = fill;
            outline = itemOutline;
            Initialize();
        }
#endif
    }
}
