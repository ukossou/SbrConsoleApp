using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Timers;
using System.Text;
using System.Globalization;

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

            //Initialisation threads et radiomtres
            foreach (string port in SerialPort.GetPortNames())
            {
                {
                    Radiometre rad = new Radiometre(port);
                    if(rad.ouverturePort())
                    {
                        radiometres.Enqueue(rad);
                        Thread threadRad = new Thread(new ThreadStart(rad.demarrer));
                        threadRad.Name = "thread " + port;
                        threads.Enqueue(threadRad);
                    }
                }
            }

            foreach (Thread threadRad in threads)
                threadRad.Start();

            //foreach (Thread tRad in threads)
                //tRad.Join();
            Console.WriteLine("\n...initialisation terminee pour les COMs ");

            foreach(Radiometre rad in radiometres )
            {
                if(rad.InitOK)
                    Console.Write(" "+rad.NomPort);
            }

            Console.ReadLine();

            Console.WriteLine("\n...Attente... quit : pour arreter");
            lireMessage();
            //foreach (Thread tRad in threads) if (tRad.IsAlive)Console.WriteLine("Thread vivant "+tRad.Name);
            foreach (Thread tRad in threads)
                if (tRad.IsAlive) { tRad.Abort(); tRad.Join(); }
            foreach (Radiometre rad in radiometres)
                if (rad.Started)
                    rad.finaliser();
        }
        private static void lireMessage()
        {
            String message;
            while (!Radiometre.Terminer)
            {

                message = Console.ReadLine();

                if (message == "quit")
                {
                    Radiometre.Terminer = true;
                }
            }
        }

    }
}
