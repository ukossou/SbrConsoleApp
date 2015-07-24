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

            //recherche des ports serie disponiibles
            Console.WriteLine("Ports serie disponibles : " +
                                String.Join(" ", SerialPort.GetPortNames())
                             );
           
            //Initialisations 
            int nbPorts=SerialPort.GetPortNames().Length;
            Radiometre[] radiometres = new Radiometre[nbPorts];
            Thread[] tRad = new Thread[nbPorts];
            int indice = 0;
            foreach (string port in SerialPort.GetPortNames() )
            {
                Radiometre rad = new Radiometre(port);
                radiometres[indice] = rad;
                rad.demarrer();
                if(rad.started)
                 tRad[indice] = new Thread(new ThreadStart(rad.attendre));
                indice =+ 1;
            }

            foreach(Thread t in tRad)
            { if(t!=null)
                {
                    while (!t.IsAlive) ;
                    t.Start(); 
                }  
            }

            Console.WriteLine("\n...Attente... ");
            Console.ReadLine();

            foreach (Thread t in tRad)
            {
                if (t != null)
                {
                    t.Abort();
                }
            }

            foreach(Radiometre rad in radiometres )
                if(rad!=null)
                    rad.finaliser();

        }
    }
}
