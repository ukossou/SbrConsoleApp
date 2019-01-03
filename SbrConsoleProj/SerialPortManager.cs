using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Globalization;
using System.Timers;

namespace SbrConsoleApp
{
    class SerialPortManager
    {
        private const Int32  BAUD_RATE    = 9600;

        private String[] comArray;
        //private Dictionary<String, SerialPort> openedPorts;
        private List<MonitoredSerialPort> MonitoredSerialPorts;

        public void InitComPorts()
        {
            comArray = SerialPort.GetPortNames();

            PrintStringArray(comArray);
            MonitoredSerialPorts = new List<MonitoredSerialPort>();

            //init serial ports 
            foreach (var name in comArray)
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

            //start ports monitoring
        }

        Boolean releaseTimersStarted = false;
       
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
            MonitoredSerialPorts.Remove(monitored);          
            monitored.freePort();
            monitored = null;
        }

        public void FreePorts()
        {
            foreach(var monitored in MonitoredSerialPorts)
            {
                monitored.freePort();
            }
        }

        public void PrintStringArray(String[] collection)
        {
            String toPrint = "";

            if (collection.Length == 0) toPrint += "No Port detected";

            foreach (var content in collection)
            {
                toPrint += content + " ";
            }

            Console.WriteLine(toPrint);
        }

    }

}
