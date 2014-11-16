using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace EDC_Client
{
    class Connection
    {

        //Event-Delegaten
        public delegate void _D_getMessage(string message, string alias);
        public delegate void _D_blockedConnection(string alias);


        #region Settings
            //Events
            public event _D_getMessage                  _getMessage;
            public event _D_blockedConnection           _closedConnection;


            //Crypt-Class
            private crypto                              _cryptClass;


            //Network Vars
            private System.Net.Sockets.TcpClient        _client;
            private System.Net.Sockets.NetworkStream    _mainStream;
            private IPEndPoint                          _ip;
            private string                              _alias;
            private byte[]                              _heartBeatPackage = { 5, 255, 70 };


        #endregion



        //Constructor
        public Connection(crypto cryptClass, IPEndPoint ip, string alias)
        {
            _cryptClass = cryptClass;
            _alias      = alias;
            _ip         = ip;
        }


        #region Interface

            public void setup()
            {
                _client = new System.Net.Sockets.TcpClient();

                _client.Connect(_ip);
                _mainStream = _client.GetStream();

                writeString("handshake");
                if (readnWaitStream() == "ok")
                    writeString(_alias);
                else
                    _mainStream.Close();

                System.Threading.Thread t = new System.Threading.Thread(listener);
                t.Start();

            }

            public void closeConnection()
            {
                _mainStream.Close();
                _client.Close();
            }

            public void sendMessage(string message)
            {
                writeString(message);
            }

        #endregion



        #region Interna



            private void listener()
            {
                //StopWatch für das rechtzeitige Senden eines Hearbeats
                System.Diagnostics.Stopwatch hbSendTimer = new System.Diagnostics.Stopwatch();
                System.Diagnostics.Stopwatch hbRecvTimer = new System.Diagnostics.Stopwatch();
                hbSendTimer.Start();
                hbRecvTimer.Start();

                //Solange Verbunden, auf neue Nachrichten warten und Hearbeat verarbeiten
                while (_client.Connected)
                {
                    //Auf neue Nachrichten prüfen
                    if (_mainStream.DataAvailable)
                    {
                        string buffer = readStream();

                        if (buffer != "_heartbeat_")
                            raiseGetMessage(buffer, "host");

                        hbRecvTimer.Restart();
                    }

                    //Hearbeat senden (alle 5 Sekunden)
                    if (hbSendTimer.ElapsedMilliseconds >= 5000)
                    {
                        sendHeartBeat();
                        hbSendTimer.Restart();
                    }

                    //Kein Heartbeat
                    if (hbRecvTimer.ElapsedMilliseconds >= 7000)
                    {
                        _mainStream.Close();
                        _client.Close();
                        throw new Exception("Timeout des Hosts.");
                    }
                }

                //Verbindung Schließen
                _mainStream.Close();
                raiseClosedConnection("host");

            }



            private int pingAtGoogle()
            {
                Ping pingSender = new Ping();
                PingOptions opt = new PingOptions();

                opt.DontFragment = true;

                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send("8.8.8.8", timeout, buffer, opt);

                if (reply.Status == IPStatus.Success)
                    return Convert.ToInt32(reply.RoundtripTime);
                else
                    return -1;
            }

            //Help-Functions
            private string getHexString(byte[] text)
            {
                String hexCode = "";
                for (int i = 0; i < text.Length; i++)
                    hexCode += String.Format("{0:X}", Convert.ToInt32(text[i]));
                return hexCode;
            }
            private string getHexString(byte text)
            {
                String hexCode = "";
                hexCode += String.Format("{0:X}", Convert.ToInt32(text));
                return hexCode;
            }


            //Read'n'Write-Functions
            private void writeString(string text)
            {
                //Nachricht(string) verschlüsseln 
                byte[] buffer = _cryptClass.encyrptMessage(text);

                //Nachricht(byte[]) in Netzwerkstream schreiben und abschicken
                _mainStream.Write(buffer, 0, buffer.Length);
                _mainStream.Flush();
            }
            private string readnWaitStream()
            {
                //List of Byte zum Lesen/Speichern der Nachricht
                List<byte> buffer = new List<byte>();

                //Warten bis eine Nachricht verfügbar ist
                do { } while (!_mainStream.DataAvailable);

                //Lesen bis Nachrichten-Ende
                while (_mainStream.DataAvailable)
                    buffer.Add(Convert.ToByte(_mainStream.ReadByte()));

                //Nachricht(byte[]) entschlüsseln und zurückgeben
                string output = _cryptClass.decryptMessage(buffer.ToArray());
                return output;
            }
            private string readStream()
            {
                //List of Byte zum Lesen/Speichern der Nachricht
                List<byte> buffer = new List<byte>();

                //Lesen bist Nachrichten-Ende
                while (_mainStream.DataAvailable)
                    buffer.Add(Convert.ToByte(_mainStream.ReadByte()));

                //Nachricht(byte[]) entschlüsseln und zurückgeben
                string output = _cryptClass.decryptMessage(buffer.ToArray());
                return output;
            }

            //Send Heartbeat-Package
            private void sendHeartBeat()
            {
                //HearbeatPackage in den Netzwerkstream schreiben und abschicken
                _mainStream.Write(_heartBeatPackage, 0, _heartBeatPackage.Length);
                _mainStream.Flush();

            }

        #endregion



        #region Eventhandler

            private void raiseGetMessage(string message, string alias)
            {
                if (_getMessage != null)
                    _getMessage(message, alias);
            }

            private void raiseClosedConnection(string alias)
            {
                if (_closedConnection != null)
                    _closedConnection(alias);
            }

         #endregion



    }
}
