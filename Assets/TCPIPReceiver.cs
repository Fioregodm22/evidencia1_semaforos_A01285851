
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class TCPCIPServer : MonoBehaviour
{
    // referencias a objetos en la escena 
    public GameObject carPrefab;     
    public GameObject trafficLight;    

    // materiales para el semaforo
    public Material redMat;
    public Material yellowMat;
    public Material greenMat;

    // sockets para servidor y cliente
    private Socket serverSocket;
    private Socket clientSocket;

    // buffer para recibir datos
    private byte[] buffer = new byte[4096];

    // ultimo mensaje recibidoe
    private string lastMessage = "";

    void Start()
    {
        // crea el socket con direccion ipv4, tipo stream y protocolo tcp
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // vincula el socket a la ip local y puerto 1107
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1107));

        
        serverSocket.Listen(1);
        Debug.Log("esperando cliente...");

        // inicia la aceptacion asincronica del cliente
        serverSocket.BeginAccept(AcceptCallback, null);
    }

    // se ejecuta cuando un cliente se conecta
    void AcceptCallback(IAsyncResult ar)
    {
        // finaliza la aceptacion y obtiene el socket del cliente
        clientSocket = serverSocket.EndAccept(ar);
        Debug.Log("cliente conectado!");

        // limpia el buffer y comienza a recibir datos asincronicamente
        Array.Clear(buffer, 0, buffer.Length);
        clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
    }

    // se ejecuta cuando llegan datos del cliente
    void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // obtiene la cantidad de bytes recibidos
            int received = clientSocket.EndReceive(ar);
            if (received > 0)
            {
                // convierte los bytes a string usando codificacion ascii
                string str = Encoding.ASCII.GetString(buffer, 0, received);
                Debug.Log("servidor recibe: " + str);

                // guarda el mensaje
                lastMessage = str;
            }

            // limpia el buffer y vuelve a esperar datos
            Array.Clear(buffer, 0, buffer.Length);
            clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
        }
        catch (Exception e)
        {
            Debug.Log("error en ReceiveCallback: " + e.Message);
        }
    }

    // procesa el mensaje si hay uno nuevo
    void Update()
    {
        if (!string.IsNullOrEmpty(lastMessage))
        {
            ProcessMessage(lastMessage);
            lastMessage = "";
        }
    }

    // interpreta el mensaje recibido y actualiza los objetos en la escena
    void ProcessMessage(string msg)
    {
        // separa el mensaje por ';' "CAR,12.5;LIGHT,rojo;"
        string[] items = msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] parts = item.Split(',');
            if (parts.Length < 2) continue;

            // posicioon deel carro
            if (parts[0] == "CAR")
            {
                float carPos = float.Parse(parts[1]);
                if (carPrefab != null)
                    carPrefab.transform.position = new Vector3(carPos, 0, 0);
            }
            // estado del semaforo
            else if (parts[0] == "LIGHT")
            {
                string lightState = parts[1];
                if (trafficLight != null)
                {
                    Renderer r = trafficLight.GetComponent<Renderer>();
                    if (lightState == "verde") r.material = greenMat;
                    else if (lightState == "amarillo") r.material = yellowMat;
                    else r.material = redMat;
                }
            }
        }
    }

    // cierra los sockets
    void OnApplicationQuit()
    {
        if (clientSocket != null) clientSocket.Close();
        if (serverSocket != null) serverSocket.Close();
    }
}
