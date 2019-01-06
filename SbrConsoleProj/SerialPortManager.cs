using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO.Ports;
//using System.Threading;
using System.Globalization;
using System.Timers;

namespace SbrConsoleApp
{
    class SerialPortManager
    {
        private const Int32  BAUD_RATE    = 38400;

        private List<String> comList;
        //private Dictionary<String, SerialPort> openedPorts;
        private BlockingCollection<MonitoredSerialPort> MonitoredSerialPorts;
        List<MonitoredSerialPort> PortsToFree;

        private Timer ObserveTimer;
        private Boolean ObserveTimerElapsed = false;
        private const Double OBSERVE_TIMEOUT = 30000;//30 seconds

        public void InitComPorts()
        {
            comList = new List<String>(SerialPort.GetPortNames());

            MonitoredSerialPorts = new BlockingCollection<MonitoredSerialPort>();
            PortsToFree = new List<MonitoredSerialPort>();


            ObserveTimer = new Timer(OBSERVE_TIMEOUT);
            ObserveTimer.AutoReset = false;
            ObserveTimer.Elapsed += new ElapsedEventHandler(ObserveTimerTimerElapsed);

            //init serial ports 
            foreach (var name in comList)
            {
                SerialPort serialPort = new SerialPort(name,BAUD_RATE);

                try
                {
                    serialPort.Open();
                    serialPort.DiscardInBuffer();

                    MonitoredSerialPort monitoredSp = new MonitoredSerialPort(ref serialPort, this);

                    MonitoredSerialPorts.Add(monitoredSp);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }
        }

        private void ObserveTimerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ObserveTimerElapsed = true;
        }

        Boolean ReleaseTimeElapsed = false;

        

        public List<SerialPort> getRecievingPorts()
        {
            List<SerialPort> recievingPorts = new List<SerialPort>();
            while (!ObserveTimerElapsed) ;

            foreach(var monitored in MonitoredSerialPorts)
            {             
                if (monitored.dataRecieved())
                {
                    recievingPorts.Add(monitored.getSerialPort());
                }
                else
                {
                    PortsToFree.Add(monitored);
                }
            }

            FreePorts();
            return recievingPorts;
        }

        Boolean releaseTimersStarted = false;
       
        public void StartObserveTimer(String comName)
        {
            if (!ObserveTimer.Enabled)
            {
                ObserveTimer.Start();
                //Console.WriteLine("Manager ... starting ObserveTimer ..." + comName);
            };
        }

        public void StartReleaseTimers()
        {
            if(!releaseTimersStarted)
            {
                foreach(var monPort in MonitoredSerialPorts)
                {
                    monPort.StartReleaseTimer();
                }
                releaseTimersStarted = true;
                Console.WriteLine("Manager ... starting releaseTimer");
            }
        }

        public void DisposePort(MonitoredSerialPort monitored)
        {
            if (!ReleaseTimeElapsed) ReleaseTimeElapsed = true;        
            monitored.freePort();
            monitored = null;
        }

        public void FreePorts()
        {
            foreach(var monitored in PortsToFree)
            {
                monitored.freePort();
            }
        }

        public void PrintStringList(List<String> collection)
        {
            String toPrint = "";

            if (collection.Count == 0) toPrint += "No Port detected";

            foreach (var content in collection)
            {
                toPrint += content + " ";
            }
        }

    }

}
