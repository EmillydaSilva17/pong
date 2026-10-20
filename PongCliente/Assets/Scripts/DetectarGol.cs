using System.Collections;
using UnityEngine;

public class DetectarGol : MonoBehaviour
{
    public ClienteUDP clienteUDP;
    public BolaMovimento bolaMovimento;

    private bool golDetectado = false;

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (golDetectado)
            return;

        if (!outro.CompareTag("Bola"))
            return;

        golDetectado = true;

        if (gameObject.name == "GolEsquerdo")
        {
            Debug.Log("Gol no lado esquerdo!");

            if (clienteUDP != null)
                clienteUDP.EnviarMensagem("GOAL|LEFT");
        }
        else if (gameObject.name == "GolDireito")
        {
            Debug.Log("Gol no lado direito!");

            if (clienteUDP != null)
                clienteUDP.EnviarMensagem("GOAL|RIGHT");
        }

        if (bolaMovimento != null)
            bolaMovimento.ReiniciarBola();

        StartCoroutine(LiberarDetector());
    }

    IEnumerator LiberarDetector()
    {
        yield return new WaitForSeconds(1.5f);
        golDetectado = false;
    }
}