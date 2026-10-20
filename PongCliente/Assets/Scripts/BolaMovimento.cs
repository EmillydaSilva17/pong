using System.Collections;
using UnityEngine;

public class BolaMovimento : MonoBehaviour
{
    public float velocidade = 5f;
    public float tempoParaReiniciar = 1f;

    private Rigidbody2D rb;
    private bool reiniciando = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        IniciarBola();
    }

    void IniciarBola()
    {
        if (rb == null)
        {
            Debug.LogError("A bola precisa de um Rigidbody2D!");
            return;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 direcao = new Vector2(1f, 0.5f).normalized;
        rb.linearVelocity = direcao * velocidade;

        reiniciando = false;

        Debug.Log("Bola iniciada!");
    }

    public void ReiniciarBola()
    {
        if (reiniciando)
            return;

        StartCoroutine(ReiniciarBolaCoroutine());
    }

    IEnumerator ReiniciarBolaCoroutine()
    {
        reiniciando = true;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        transform.position = Vector3.zero;

        yield return new WaitForSeconds(tempoParaReiniciar);

        IniciarBola();
    }
}