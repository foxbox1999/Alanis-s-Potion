using UnityEngine;
using UnityEngine.UI;

public class SpriteCharacterDisplay : MonoBehaviour, ICharacterDisplay
{
    [SerializeField] private Image image;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
    }

    public void Show(Transform parent, CharacterSpriteConfig.CharacterData data, string expression)
    {
        transform.SetParent(parent, false);
        image.sprite = data.GetSprite(expression);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetExpression(string expression, CharacterSpriteConfig.CharacterData data)
    {
        image.sprite = data.GetSprite(expression);
    }
}