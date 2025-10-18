using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LabelSelectorController : MonoBehaviour
{
    public TextMeshProUGUI labelText;

    private List<string> labels = new List<string> {
        "bathtub", "bed", "bookshelf", "cabinet", "chair",
        "counter", "curtain", "desk", "door", "floor",
        "otherfurniture", "picture", "refridgerator", "shower curtain", "sink",
        "sofa", "table", "toilet", "wall", "window"
    };

    private int currentIndex = 0;

    void Start()
    {
        if (labelText == null)
        {
            Debug.LogWarning("labelText 未指定");
            return;
        }

        UpdateLabelText();
    }

    void Update()
    {
        labelText.text = labels[currentIndex];
    }

    public void SetLabelText(string currentLabelText)
    {
        labelText.text = currentLabelText;
        // update currentIndex based on the currentLabelText
        currentIndex = labels.IndexOf(currentLabelText);
        UpdateLabelText();
    }

    public void NextLabel()
    {
        currentIndex = (currentIndex + 1) % labels.Count;
        UpdateLabelText();
    }

    public void PrevLabel()
    {
        currentIndex = (currentIndex - 1 + labels.Count) % labels.Count;
        UpdateLabelText();
    }

    public string GetCurrentLabel()
    {
        return labels[currentIndex];
    }

    private void UpdateLabelText()
    {
        labelText.text = labels[currentIndex];
    }
}
