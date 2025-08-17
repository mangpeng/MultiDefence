using System.Collections;
using TMPro;
using UnityEngine;

public class TextAni : MonoBehaviour
{
    [SerializeField] private float floatSpeed;
    [SerializeField] private float riseDuration;
    [SerializeField] private float fadeDuration;
    [SerializeField] private Vector3 offset = new Vector3();


    public TextMeshPro damageText;
    private Color textColor;

    public void Initialize(int damage)
    {
        damageText.text = damage.ToString();
        textColor = damageText.color;
        StartCoroutine(CMoveAndFade());
    }

    IEnumerator CMoveAndFade()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + offset;

        float elapsedTime = 0;

        // up
        while(elapsedTime < riseDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / riseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // fade-out
        elapsedTime = 0;
        while (elapsedTime < fadeDuration)
        {
            textColor.a = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            damageText.color = textColor;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(this.gameObject);
    }
}
