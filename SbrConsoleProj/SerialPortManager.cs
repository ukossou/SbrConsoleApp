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
        private const Double PORT_RELEASE = 15000;//15 seconds

        private String[] comArray;
        private Dictionary<String, SerialPort> openedPorts;
        private List<MonitoredSerialPort> MonitoredSerialPorts;

        public void InitComPorts()
        {
            comArray = SerialPort.GetPortNames();

            PrintStringArray(comArray);
            openedPorts = new Dictionary<string, SerialPort>();
            MonitoredSerialPorts = new List<MonitoredSerialPort>();

            //init serial ports 
            foreach (var name in comArray)
            {
                SerialPort serialPort = new SerialPort(name,BAUD_RATE);

                try
                {
                    serialPort.Open();
                    openedPorts.Add(name, serialPort);
                    serialPort.DiscardInBuffer();

                    MonitoredSerialPort monitoredSp = new MonitoredSerialPort(ref serialPort, this);
                    monitoredSp.StartReleaseTimer();
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
            }
        }

        public void DisposePort(ref SerialPort sp)
        {
            if (sp.IsOpen)
            {
                sp.Dispose();
            }
        }

        public void FreePorts()
        {
            foreach(var serialPort in openedPorts.Values)
            {
                if (serialPort.IsOpen)
                {
                    String portName = serialPort.PortName;
                    serialPort.Close();
                    serialPort.Dispose();
                    Console.WriteLine("Port " + portName + "  disposed !!!");
                }
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
