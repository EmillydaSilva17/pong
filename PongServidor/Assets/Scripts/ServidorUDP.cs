using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ServidorUDP : MonoBehaviour
{
    private UdpClient servidor;
    private Thread threadRecepcao;

    private readonly ConcurrentQueue<MensagemCliente> mensagensRecebidas =
        new ConcurrentQueue<MensagemCliente>();

    private readonly Dictionary<string, int> jogadores =
        new Dictionary<string, int>();

    private readonly Dictionary<int, IPEndPoint> enderecosJogadores =
        new Dictionary<int, IPEndPoint>();

    private int proximoJogador = 1;

    public int porta = 5000;
    public int maxJogadores = 4;

    private readonly float[] posicoesJogadores = new float[4];

    [Header("Bola")]
    private Vector2 posicaoBola = Vector2.zero;
    private Vector2 direcaoBola;

    public float velocidadeBola = 5f;
    public float limiteVertical = 4.75f;
    public float limiteHorizontal = 8f;

    private bool bolaEmPausa = false;
    private float tempoPausa = 0f;

    private float intervaloEstado = 0.05f;
    private float cronometroEstado = 0f;

    [Header("Placar")]
    private int pontosTimeEsquerdo = 0;
    private int pontosTimeDireito = 0;

    void Start()
    {
        try
        {
            servidor = new UdpClient(porta);

            ReiniciarBola();

            Debug.Log("=================================");
            Debug.Log("SERVIDOR UDP INICIADO");
            Debug.Log("Porta: " + porta);
            Debug.Log("Servidor controla a bola.");
            Debug.Log("Esperando jogadores...");
            Debug.Log("=================================");

            threadRecepcao = new Thread(ReceberMensagens);
            threadRecepcao.IsBackground = true;
            threadRecepcao.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao iniciar servidor: " + e.Message);
        }
    }

    void Update()
    {
        while (mensagensRecebidas.TryDequeue(out MensagemCliente mensagem))
        {
            ProcessarMensagem(mensagem);
        }

        AtualizarBola();

        cronometroEstado += Time.deltaTime;

        if (cronometroEstado >= intervaloEstado)
        {
            EnviarEstado();
            cronometroEstado = 0f;
        }
    }

    void ProcessarMensagem(MensagemCliente mensagem)
    {
        string identificador = mensagem.endPoint.ToString();

        Debug.Log("Mensagem recebida de " + identificador +
                  ": " + mensagem.texto);

        if (mensagem.texto == "HELLO")
        {
            EntrarJogador(mensagem.endPoint);
            return;
        }

        if (!jogadores.ContainsKey(identificador))
            return;

        if (mensagem.texto.StartsWith("POSITION|"))
        {
            AtualizarPosicao(identificador, mensagem.texto);
        }
    }

    void EntrarJogador(IPEndPoint endereco)
    {
        string identificador = endereco.ToString();

        if (jogadores.TryGetValue(identificador, out int existente))
        {
            EnviarMensagem(endereco, "WELCOME|" + existente);
            EnviarPlacarPara(endereco);
            return;
        }

        if (proximoJogador > maxJogadores)
        {
            EnviarMensagem(endereco, "FULL");
            Debug.LogWarning("Servidor cheio. Jogador recusado.");
            return;
        }

        int numero = proximoJogador++;

        jogadores.Add(identificador, numero);
        enderecosJogadores.Add(numero, endereco);

        Debug.Log("Jogador " + numero + " entrou!");

        EnviarMensagem(endereco, "WELCOME|" + numero);
        EnviarPlacarPara(endereco);
    }

    void AtualizarPosicao(string identificador, string mensagem)
    {
        string[] partes = mensagem.Split('|');

        if (partes.Length < 3)
            return;

        if (!int.TryParse(partes[1], out int numeroJogador))
            return;

        if (!float.TryParse(
            partes[2],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float posicaoY))
            return;

        if (!jogadores.TryGetValue(identificador, out int jogadorRegistrado))
            return;

        if (numeroJogador != jogadorRegistrado)
            return;

        if (numeroJogador < 1 || numeroJogador > 4)
            return;

        posicoesJogadores[numeroJogador - 1] =
            Mathf.Clamp(posicaoY, -4f, 4f);
    }

    void AtualizarBola()
    {
        if (bolaEmPausa)
        {
            tempoPausa -= Time.deltaTime;

            if (tempoPausa <= 0f)
                ReiniciarBola();

            return;
        }

        posicaoBola += direcaoBola * velocidadeBola * Time.deltaTime;

        // Rebater nas paredes superior e inferior.
        if (posicaoBola.y >= limiteVertical)
        {
            posicaoBola.y = limiteVertical;
            direcaoBola.y = -Mathf.Abs(direcaoBola.y);
        }
        else if (posicaoBola.y <= -limiteVertical)
        {
            posicaoBola.y = -limiteVertical;
            direcaoBola.y = Mathf.Abs(direcaoBola.y);
        }

        // Gol no lado esquerdo: ponto para o time direito.
        if (posicaoBola.x <= -limiteHorizontal)
        {
            RegistrarGol("LADO ESQUERDO");
            return;
        }

        // Gol no lado direito: ponto para o time esquerdo.
        if (posicaoBola.x >= limiteHorizontal)
        {
            RegistrarGol("LADO DIREITO");
        }
    }

    void RegistrarGol(string lado)
    {
        // Impede registrar vários pontos no mesmo gol.
        if (bolaEmPausa)
            return;

        bolaEmPausa = true;
        tempoPausa = 1f;

        if (lado == "LADO ESQUERDO")
        {
            pontosTimeDireito++;
        }
        else
        {
            pontosTimeEsquerdo++;
        }

        posicaoBola = Vector2.zero;
        direcaoBola = Vector2.zero;

        Debug.Log("GOL NO " + lado + "!");

        Debug.Log("PLACAR ATUALIZADO: " +
                  pontosTimeEsquerdo + " x " +
                  pontosTimeDireito);

        // Envia o novo placar a todos os clientes conectados.
        EnviarPlacar();
    }

    void ReiniciarBola()
    {
        posicaoBola = Vector2.zero;

        float direcaoY = UnityEngine.Random.Range(-0.5f, 0.5f);
        float direcaoX = UnityEngine.Random.value < 0.5f ? -1f : 1f;

        direcaoBola = new Vector2(direcaoX, direcaoY).normalized;
        bolaEmPausa = false;

        Debug.Log("Bola iniciada no servidor.");
    }

    void EnviarEstado()
    {
        foreach (var jogador in enderecosJogadores)
        {
            IPEndPoint endereco = jogador.Value;

            for (int i = 0; i < 4; i++)
            {
                EnviarMensagem(
                    endereco,
                    "STATE|" + (i + 1) + "|" +
                    posicoesJogadores[i].ToString(
                        CultureInfo.InvariantCulture)
                );
            }

            EnviarMensagem(
                endereco,
                "BALL|" +
                posicaoBola.x.ToString(CultureInfo.InvariantCulture) + "|" +
                posicaoBola.y.ToString(CultureInfo.InvariantCulture)
            );
        }
    }

    
    void EnviarPlacar()
    {
        string mensagem = "SCORE|" +
                          pontosTimeEsquerdo + "|" +
                          pontosTimeDireito;

        foreach (var jogador in enderecosJogadores)
        {
            Debug.Log("ENVIANDO PLACAR PARA " +
                      jogador.Value + ": " + mensagem);

            EnviarMensagem(jogador.Value, mensagem);
        }
    }


    void EnviarPlacarPara(IPEndPoint endereco)
    {
        string mensagem = "SCORE|" +
                          pontosTimeEsquerdo + "|" +
                          pontosTimeDireito;

        EnviarMensagem(endereco, mensagem);
    }

    void EnviarMensagem(IPEndPoint endereco, string mensagem)
    {
        if (servidor == null)
            return;

        try
        {
            byte[] dados = Encoding.UTF8.GetBytes(mensagem);
            servidor.Send(dados, dados.Length, endereco);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Falha ao enviar UDP: " + e.Message);
        }
    }

    void ReceberMensagens()
    {
        IPEndPoint remetente = new IPEndPoint(IPAddress.Any, 0);

        while (servidor != null)
        {
            try
            {
                byte[] dados = servidor.Receive(ref remetente);
                string mensagem = Encoding.UTF8.GetString(dados);

                mensagensRecebidas.Enqueue(
                    new MensagemCliente(
                        new IPEndPoint(remetente.Address, remetente.Port),
                        mensagem
                    )
                );
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Erro ao receber mensagem: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (servidor != null)
        {
            servidor.Close();
            servidor = null;
        }

        if (threadRecepcao != null && threadRecepcao.IsAlive)
        {
            threadRecepcao.Join(200);
        }
    }

    private class MensagemCliente
    {
        public IPEndPoint endPoint;
        public string texto;

        public MensagemCliente(IPEndPoint endPoint, string texto)
        {
            this.endPoint = endPoint;
            this.texto = texto;
        }
    }
}