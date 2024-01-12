/*
https://github.com/konbraphat51/UnityPythonConnectionClass
Author: Konbraphat51
License: Boost Software License (BSL1.0)
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connecting to Python
///
/// This will be a client socket to connect to Python server
/// Singleton
/// </summary>
public class PythonConnector : MonoBehaviour
{
    /// <summary>
    /// Returns singleton instance
    /// </summary>
    public static PythonConnector instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PythonConnector>();

                if (_instance == null)
                {
                    Debug.LogError("PythonConnector not found");
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Returns true if currently connected to Python server
    /// </summary>
    public bool connecting { get; private set; }

    /// <summary>
    /// For remember singleton instance
    /// </summary>
    private static PythonConnector _instance;

    [Tooltip("IP Address of Python Server")]
    [SerializeField]
    private string ipAddress = "127.0.0.1";

    [Tooltip("Port of Python Server")]
    [SerializeField]
    private int portPython = 9000;

    [Tooltip("Port of Unity Server")]
    [SerializeField]
    private int portThis = 9001;

    [Tooltip("Buffer size for reading data from Python server. Bytes.")]
    [SerializeField]
    private int bufferSize = 8192;

    [Tooltip("Timeout for receiving data from Python server. Seconds.")]
    [SerializeField]
    private float timeOutReceiving = 10f;

    [Tooltip("Event called when received data from Python server")]
    [SerializeField]
    private UnityEvent onDataReceived = new UnityEvent();

    [Tooltip("Event called when timeout")]
    [SerializeField]
    private UnityEvent onTimeOut = new UnityEvent();

    [Tooltip("If get this string, will finish connection")]
    [SerializeField]
    private string finishString = "!end!";

    private TcpClient client;
    private NetworkStream stream;

    /// <summary>
    /// Connect to the Python server
    /// </summary>
    /// <returns>true if succeeded</returns>
    public bool StartConnection()
    {
        try
        {
            //try connecting
            client = new TcpClient(ipAddress, portThis);
            client.Connect(ipAddress, portPython);
            stream = client.GetStream();

            //set timeout
            // to miliseconds
            client.ReceiveTimeout = (int)(timeOutReceiving * 1000f);

            //start listening thread
            Task.Factory.StartNew(OnProcessListening);

            //connection succeeded
            connecting = true;
            return true;
        }
        catch (SocketException)
        {
            //connection failed
            connecting = false;

            onTimeOut.Invoke();

            return false;
        }
    }

    /// <summary>
    /// Close connection
    /// </summary>
    /// <returns>true if the connection closed successfully. False if not connecting from the beginning</returns>
    public bool StopConnection()
    {
        // if not connecting from the beginning...
        if (!connecting)
        {
            //...show this wasn't closed successfully
            return false;
        }

        //send finish string
        Send(finishString);

        //close connection
        stream.Close();
        client.Close();

        connecting = false;

        //show this was closed successfully
        return true;
    }

    /// <summary>
    /// Send data to Python server by JSON
    /// </summary>
    /// <typeparam name="T">Serializable class</typeparam>
    /// <param name="data">Serializable class instance. This will be converted to JSON</param>
    /// <returns>if the data was sent successfully. False if not connecting from the beginning</returns>
    public bool Send<T>(T data)
    {
        //if not connecting from the beginning...
        if (!connecting)
        {
            //...show this wasn't sent successfully
            return false;
        }

        //encode data
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data.ToString());

        //send data
        stream.Write(bytes, 0, bytes.Length);

        //show this was sent successfully
        return true;
    }

    /// <summary>
    /// Send data to Python server by string
    /// </summary>
    /// <param name="data">string data</param>
    /// <returns>if the data was sent successfully. False if not connecting from the beginning</returns>
    public bool Send(string data)
    {
        //if not connecting from the beginning...
        if (!connecting)
        {
            //...show this wasn't sent successfully
            return false;
        }

        //encode data
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);

        //send data
        stream.Write(bytes, 0, bytes.Length);

        //show this was sent successfully
        return true;
    }

    /// <summary>
    /// Keep listening to the Python server
    ///
    /// this will be running in a separate thread0
    /// </summary>
    private void OnProcessListening()
    {
        try
        {
            while (true)
            {
                //read data from Python server
                byte[] data = new byte[bufferSize];
                int bytes = stream.Read(data, 0, data.Length);
                string message = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                //handle stop code
                if (message == finishString)
                {
                    //stop connection
                    StopConnection();

                    //stop listening
                    break;
                }

                //call registered callback
                OnDataReceived(message);
            }
        }
        catch (SocketException)
        {
            //stop connection
            StopConnection();

            //action when timeout
            onTimeOut.Invoke();
        }
    }

    /// <summary>
    /// Called when received data from Python server.
    ///
    /// This will decode the data and call the registered callback
    /// </summary>
    /// <param name="data"></param>
    private void OnDataReceived(string data) { }
}
