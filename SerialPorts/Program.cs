using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Timers;
using System.Text;

namespace SerialPorts
{
    class Program
    {
        static void Main(string[] args)
        {
            
            Console.WriteLine("....Programme d'enregistrement des RADIOMETRES....\n");

            Queue radiometres = new Queue();
            Queue threads = new Queue();

            //recherche des ports serie disponiibles
            Console.WriteLine("Ports serie disponibles : " +
                                String.Join(" ", SerialPort.GetPortNames())
                             );
           
            //Initialisations 
            foreach (string port in SerialPort.GetPortNames())
            {
                Radiometre rad = new Radiometre(port);
                radiometres.Enqueue(rad);
                Thread tRad = new Thread(new ThreadStart(rad.demarrer));
                tRad.Name = "thread " + port;
                threads.Enqueue(tRad);

            }
            foreach (Thread tRad in threads)
                tRad.Start();
     
            Console.WriteLine("\n...Attente... ");
            Console.ReadLine();
            Radiometre.Terminer = true;
            //foreach (Thread tRad in threads) if (tRad.IsAlive)Console.WriteLine("Thread vivant "+tRad.Name);
            foreach (Thread tRad in threads)
            {
                if(tRad.IsAlive){tRad.Abort(); tRad.Join();}   
            }
                
            foreach(Radiometre rad in radiometres)
                rad.finaliser();
            

        }
    }
}
