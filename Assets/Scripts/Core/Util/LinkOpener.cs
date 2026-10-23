using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Makes the links in a TextMeshPro text clickable. Put it on the same object as the text and write links as
/// <link="https://example.com">words</link>; clicking the words opens that address.
/// The text must have Raycast Target on, or it never receives the click.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LinkOpener : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text text;

    private void Awake() => text = GetComponent<TMP_Text>();

    public void OnPointerClick(PointerEventData eventData)
    {
        // A Screen Space - Overlay canvas has no camera, and TMP wants null for it.
        Canvas canvas = text.canvas;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        // -1 means the click landed on text that isn't a link.
        int index = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, camera);
        if (index < 0) return;

        // The link's ID is whatever was written after link= in the tag.
        string url = text.textInfo.linkInfo[index].GetLinkID();

        // Only web addresses, so a typo in the credits can't open anything else.
        if (!url.StartsWith("https://") && !url.StartsWith("http://")) return;

        if (SoundsManager.Instance != null) SoundsManager.Instance.PlayClick();
        Application.OpenURL(url);
    }
}