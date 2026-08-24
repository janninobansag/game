using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private MenuManager menuManager;

    void Start()
    {
        menuManager = FindObjectOfType<MenuManager>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menuManager != null)
        {
            menuManager.PlayHoverSound();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Play click sound for ANY button click
        if (menuManager != null)
        {
            menuManager.PlayClickSound();
        }
    }
}