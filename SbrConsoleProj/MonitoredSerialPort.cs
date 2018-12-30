using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Timers;

namespace SbrConsoleApp
{
   
    class MonitoredSerialPort
    {
        private SerialPort SerPort;
        private Timer ReleaseTimer;
        private SerialPortManager Manager;
        private const Double PORT_RELEASE_DURATION = 30000;//30 seconds

        public MonitoredSerialPort(ref SerialPort serialPort,  SerialPortManager manager)
        {
            SerPort = serialPort;
            Manager = manager;

            ReleaseTimer = new Timer(PORT_RELEASE_DURATION);
            ReleaseTimer.Elapsed += new ElapsedEventHandler(ReleaseTimerElapsed);

            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedEventHandler);
        }


        private Boolean FistTimeDataRecieved = true;

        private void DataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            if (FistTimeDataRecieved)
            {
                Manager.StartReleaseTimers();
                FistTimeDataRecieved = false;
                SerPort.DiscardInBuffer();

                Console.WriteLine(SerPort.PortName + " starting Release timer");
            }
            else
            {
                if (SerPort.IsOpen)
                {
                    SerPort.DiscardInBuffer();
                }
                
                if (ReleaseTimer.Enabled) ReleaseTimer.Stop();
            }
            
        }

        public void StartReleaseTimer()
        {
            ReleaseTimer.Start();
        }

        private void ReleaseTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(SerPort.PortName + " to be released");

            Manager.DisposePort(ref SerPort);

            Console.WriteLine(SerPort.PortName + " released !!!");
        }
    }
}
