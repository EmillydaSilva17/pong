using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;

public class ClienteUDP : MonoBehaviour
{
    private UdpClient cliente;

    public string enderecoServidor = "127.0.0.1";
    public int portaServidor = 5000;

    public int numeroJogador = 0;

    public GameObject jogador1;
    public GameObject jogador2;
    public GameObject jogador3;
    public GameObject jogador4;
    public GameObject bola;

    [Header("Placar")]
    public TMP_Text textoPlacar;
    public Color corEquipeAzul = new Color(0.25f, 0.65f, 1f);
    public Color corEquipeVermelha = new Color(1f, 0.3f, 0.3f);

    private Vector2 ultimaPosicaoBola;
    private bool recebeuPosicaoBola = false;

    private int pontosAzul = 0;
    private int pontosVermelho = 0;

    void Start()
    {
        AtualizarTextoPlacar();

        if (textoPlacar == null)
        {
            Debug.LogError(
                "PLACAR: TextoPlacar não está atribuído no Inspector!"
            );
        }

        try
        {
            cliente = new UdpClient();
            cliente.Connect(enderecoServidor, portaServidor);

            Debug.Log(
                "CLIENTE UDP INICIADO. Conectando a " +
                enderecoServidor + ":" + portaServidor
            );

            if (bola == null)
                Debug.LogError("CLIENTE: a referência da Bola está vazia.");

            EnviarMensagem("HELLO");
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao iniciar cliente UDP: " + e.Message);
        }
    }

    void Update()
    {
        if (cliente == null)
            return;

        try
        {
            while (cliente.Available > 0)
            {
                IPEndPoint servidor =
                    new IPEndPoint(IPAddress.Any, 0);

                byte[] dados = cliente.Receive(ref servidor);
                string mensagem = Encoding.UTF8.GetString(dados);

                Debug.Log("CLIENTE RECEBEU: " + mensagem);

                ProcessarMensagem(mensagem);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao receber mensagem: " + e.Message);
        }

        if (bola != null && recebeuPosicaoBola)
        {
            Vector3 posicao = bola.transform.position;
            posicao.x = ultimaPosicaoBola.x;
            posicao.y = ultimaPosicaoBola.y;
            bola.transform.position = posicao;
        }
    }

    public void EnviarMensagem(string mensagem)
    {
        if (cliente == null)
        {
            Debug.LogError("Cliente UDP ainda não foi iniciado!");
            return;
        }

        try
        {
            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            cliente.Send(dados, dados.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao enviar mensagem: " + e.Message);
        }
    }

    void ProcessarMensagem(string mensagem)
    {
        string[] partes = mensagem.Trim().Split('|');

        if (partes.Length == 0)
            return;

        if (partes[0] == "WELCOME" && partes.Length >= 2)
        {
            if (int.TryParse(partes[1], out int numero))
            {
                numeroJogador = numero;
                Debug.Log("VOCÊ É O JOGADOR " + numeroJogador);
                ConfigurarJogadorLocal();
            }
        }
        else if (partes[0] == "STATE")
        {
            AtualizarJogador(partes);
        }
        else if (partes[0] == "BALL")
        {
            AtualizarBola(partes);
        }
        else if (partes[0] == "SCORE" && partes.Length >= 3)
        {
            if (int.TryParse(partes[1], out int azul) &&
                int.TryParse(partes[2], out int vermelho))
            {
                pontosAzul = azul;
                pontosVermelho = vermelho;

                AtualizarTextoPlacar();
                
                Debug.Log(
                    "PLACAR ATUALIZADO NA TELA: " +
                    pontosAzul + " x " + pontosVermelho
                );
            }
            else
            {
                Debug.LogError(
                    "Formato de SCORE inválido: " + mensagem
                );
            }
        }
        else if (partes[0] == "FULL")
        {
            Debug.LogError("SERVIDOR CHEIO!");
        }
    }

    void AtualizarTextoPlacar()
    {
        if (textoPlacar == null)
            return;

        string azul = ColorUtility.ToHtmlStringRGB(corEquipeAzul);
        string vermelho = ColorUtility.ToHtmlStringRGB(corEquipeVermelha);

        textoPlacar.text =
            "<color=#" + azul + ">AZUL " + pontosAzul + "</color>" +
            "  <color=#FFFFFF>×</color>  " +
            "<color=#" + vermelho + "> " + pontosVermelho +
            " VERMELHA</color>";

        textoPlacar.ForceMeshUpdate();
        Debug.Log("TEXTO VISUAL DO PLACAR: " + textoPlacar.text);
    }

    void ConfigurarJogadorLocal()
    {
        DesativarControle(jogador1);
        DesativarControle(jogador2);
        DesativarControle(jogador3);
        DesativarControle(jogador4);

        GameObject jogadorLocal = null;

        if (numeroJogador == 1) jogadorLocal = jogador1;
        if (numeroJogador == 2) jogadorLocal = jogador2;
        if (numeroJogador == 3) jogadorLocal = jogador3;
        if (numeroJogador == 4) jogadorLocal = jogador4;

        if (jogadorLocal == null)
        {
            Debug.LogError(
                "Jogador local não encontrado! Confira as referências."
            );
            return;
        }

        MovimentoJogador movimento =
            jogadorLocal.GetComponent<MovimentoJogador>();

        if (movimento != null)
        {
            movimento.numeroJogador = numeroJogador;
            movimento.enabled = true;
        }
        else
        {
            Debug.LogError(
                "O jogador " + numeroJogador +
                " não possui o componente MovimentoJogador."
            );
        }
    }

    void DesativarControle(GameObject jogador)
    {
        if (jogador == null)
            return;

        MovimentoJogador movimento =
            jogador.GetComponent<MovimentoJogador>();

        if (movimento != null)
            movimento.enabled = false;
    }

    void AtualizarJogador(string[] partes)
    {
        if (partes.Length < 3)
            return;

        if (!int.TryParse(partes[1], out int numero))
            return;

        if (!float.TryParse(
            partes[2],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float posicaoY))
            return;

        GameObject jogador = null;

        if (numero == 1) jogador = jogador1;
        if (numero == 2) jogador = jogador2;
        if (numero == 3) jogador = jogador3;
        if (numero == 4) jogador = jogador4;

        if (jogador == null || numero == numeroJogador)
            return;

        Vector3 posicao = jogador.transform.position;
        posicao.y = posicaoY;
        jogador.transform.position = posicao;
    }

    void AtualizarBola(string[] partes)
    {
        if (partes.Length < 3)
            return;

        if (!float.TryParse(
            partes[1],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float x))
            return;

        if (!float.TryParse(
            partes[2],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float y))
            return;

        ultimaPosicaoBola = new Vector2(x, y);
        recebeuPosicaoBola = true;
    }

    public void EnviarPosicao(float posicaoY)
    {
        string mensagem =
            "POSITION|" +
            numeroJogador + "|" +
            posicaoY.ToString(CultureInfo.InvariantCulture);

        EnviarMensagem(mensagem);
    }

    void OnApplicationQuit()
    {
        if (cliente != null)
        {
            cliente.Close();
            cliente = null;
        }
    }
}
