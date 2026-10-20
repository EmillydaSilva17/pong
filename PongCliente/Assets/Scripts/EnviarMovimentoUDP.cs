using UnityEngine;

public class EnviarMovimentoUDP : MonoBehaviour
{
    public ClienteUDP clienteUDP;

    public float diferencaMinima = 0.01f;

    private float ultimaPosicaoY;

    void Start()
    {
        ultimaPosicaoY = transform.position.y;
    }

    void Update()
    {
        if (clienteUDP == null)
            return;

        if (clienteUDP.numeroJogador <= 0)
            return;

        float posicaoY = transform.position.y;

        if (Mathf.Abs(posicaoY - ultimaPosicaoY) >= diferencaMinima)
        {
            clienteUDP.EnviarPosicao(posicaoY);

            ultimaPosicaoY = posicaoY;
        }
    }
}