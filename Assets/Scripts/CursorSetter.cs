using UnityEngine;

// Put on any object in a scene to choose that scene's mouse cursor.
// The cursor image must have Texture Type = Cursor in its import settings.
// Leave the texture empty to go back to the default system cursor.
public class CursorSetter : MonoBehaviour
{
    [SerializeField] private Texture2D cursorTexture;
    [Tooltip("ON = the click point is the middle of the image (crosshair). OFF = top-left corner (arrow).")]
    [SerializeField] private bool hotspotAtCenter = true;

    private void Start()
    {
        if (cursorTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Vector2 hotspot = hotspotAtCenter
            ? new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f)
            : Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}
