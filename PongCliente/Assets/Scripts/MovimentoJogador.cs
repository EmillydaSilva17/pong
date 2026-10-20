using UnityEngine;
using UnityEngine.InputSystem;

public class MovimentoJogador : MonoBehaviour
{
    public float velocidade = 5f;
    public float limiteSuperior = 4f;
    public float limiteInferior = -4f;

    public int numeroJogador = 1;

    public ClienteUDP clienteUDP;

    private float ultimaPosicaoY;

    void Start()
    {
        ultimaPosicaoY = transform.position.y;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        float movimento = 0f;

        // JOGADOR 1 → W / S
        if (numeroJogador == 1)
        {
            if (Keyboard.current.wKey.isPressed)
                movimento = 1f;

            if (Keyboard.current.sKey.isPressed)
                movimento = -1f;
        }

        // JOGADOR 2 → SETA PARA CIMA / SETA PARA BAIXO
        if (numeroJogador == 2)
        {
            if (Keyboard.current.upArrowKey.isPressed)
                movimento = 1f;

            if (Keyboard.current.downArrowKey.isPressed)
                movimento = -1f;
        }

        // JOGADOR 3 → I / K
        if (numeroJogador == 3)
        {
            if (Keyboard.current.iKey.isPressed)
                movimento = 1f;

            if (Keyboard.current.kKey.isPressed)
                movimento = -1f;
        }

        // JOGADOR 4 → O / L
        if (numeroJogador == 4)
        {
            if (Keyboard.current.oKey.isPressed)
                movimento = 1f;

            if (Keyboard.current.lKey.isPressed)
                movimento = -1f;
        }

        Vector3 posicao = transform.position;

        posicao.y += movimento * velocidade * Time.deltaTime;

        posicao.y = Mathf.Clamp(
            posicao.y,
            limiteInferior,
            limiteSuperior
        );

        transform.position = posicao;

        // Envia a posição somente do jogador local
        if (clienteUDP != null &&
            clienteUDP.numeroJogador == numeroJogador)
        {
            if (Mathf.Abs(
                transform.position.y - ultimaPosicaoY
            ) >= 0.01f)
            {
                clienteUDP.EnviarPosicao(
                    transform.position.y
                );

                ultimaPosicaoY =
                    transform.position.y;
            }
        }
    }
}