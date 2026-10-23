using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Makes links in a TextMeshPro text clickable and opens them in the browser.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LinkOpener : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text text;

    private void Awake() => text = GetComponent<TMP_Text>();

    public void OnPointerClick(PointerEventData eventData)
    {
        // Overlay canvases have no camera
        Canvas canvas = text.canvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        // -1 means the click was not on a link
        int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, camera);
        if (index < 0) return;

        string url = text.textInfo.linkInfo[index].GetLinkID();

        // Only open web addresses
        if (!url.StartsWith("https://") && !url.StartsWith("http://")) return;

        if (SoundsManager.Instance != null) SoundsManager.Instance.PlayClick();
        Application.OpenURL(url);
    }
}