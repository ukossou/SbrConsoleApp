using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Timers;

namespace SbrConsoleApp
{

    class MonitoredSerialPort //: IDisposable
    {
        private SerialPort SerialCom;
        private Timer ReleaseTimer;
        //private Timer ReceptionTimer;
        private SerialPortManager Manager;
        private const Double PORT_RELEASE_TIMEOUT = 20000;//20 seconds
        //private const Double RECEPTION_TIMEOUT = 15000;//15 seconds

        public MonitoredSerialPort(ref SerialPort serialPort, SerialPortManager manager)
        {
            SerialCom = serialPort;
            Manager = manager;

            ReleaseTimer = new Timer(PORT_RELEASE_TIMEOUT);
            ReleaseTimer.AutoReset = false;
            ReleaseTimer.Elapsed += new ElapsedEventHandler(ReleaseTimerElapsed);

            //ReceptionTimer = new Timer(RECEPTION_TIMEOUT);
            //ReceptionTimer.AutoReset = false;
            //ReceptionTimer.Elapsed += new ElapsedEventHandler(ReceptionTimeOutHandler);

            SerialCom.DataReceived += new SerialDataReceivedEventHandler(DataReceivedEventHandler);
        }

        public string getPortName()
        {
            return SerialCom.PortName;
        }

        private void ReceptionTimeOutHandler(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(SerialCom.PortName + " reception timeout "+ DateTime.Now.ToLongTimeString());
            //ReceptionTimer.AutoReset = true;

        }

        private Boolean FistTimeDataRecieved = true;

        private Boolean recievingData = false;

        public Boolean dataRecieved()
        {
            return recievingData;
        }

        public ref SerialPort getSerialPort()
        {
            return ref SerialCom;
        }
        private void DataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            lock (this)
            {
                recievingData = true;
                Manager.StartObserveTimer(SerialCom.PortName);
                /*if (FistTimeDataRecieved)
                {
                    Manager.StartReleaseTimers();
                    FistTimeDataRecieved = false;
                    SerialCom.DiscardInBuffer();

                    //ReceptionTimer.Start();
                }
                else
                {
                    //ReceptionTimer.Stop();
                    //ReceptionTimer.AutoReset = false;
                    if (SerialCom.IsOpen)
                    {
                        SerialCom.DiscardInBuffer();
                    }

                    if (ReleaseTimer.Enabled)
                    {
                        ReleaseTimer.Stop();
                    };
                    //ReceptionTimer.Start();
                }*/
            }

        }

        public void StartReleaseTimer()
        {
            ReleaseTimer.Start();
        }

        private void ReleaseTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //Manager.DisposePort(this);
        }

        public void freePort()
        {
            if (SerialCom.IsOpen)
            {
                lock (this)
                {
                    ReleaseTimer.Stop();
                    ReleaseTimer.Dispose();

                    //ReceptionTimer.Stop();
                    //ReceptionTimer.Dispose();

                    Manager = null;

                    String portName = SerialCom.PortName;
                    SerialCom.Close();
                    SerialCom.Dispose();
                    //Console.WriteLine("Port " + portName + "  disposed !!!");
                }
            }
        }
    }
}
