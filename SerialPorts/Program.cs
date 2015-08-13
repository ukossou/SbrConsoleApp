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
        private static int NbRadiometres;
        private static Queue Radiometres;

        private static int EcartMessage;
        static void Main(string[] args)
        {
            Console.Title = " SBR CARTEL ";
            //afficher();
           
            
            Radiometres = new Queue();
            Queue threads = new Queue();

            //Initialisation threads et radiometres
            foreach (string port in SerialPort.GetPortNames())
            {
                {
                    Radiometre rad = new Radiometre(port);
                    if(rad.ouverturePort())
                    {
                        Radiometres.Enqueue(rad);
                        Thread threadRad = new Thread(new ThreadStart(rad.demarrer));
                        threadRad.Name = "thread " + port;
                        threads.Enqueue(threadRad);
                    }
                }
            }
            NbRadiometres = Radiometres.Count;
            foreach (Thread threadRad in threads)
                threadRad.Start();

            afficher();
            //afficher(4);
            //Thread updateConsole = new Thread(new ThreadStart(updateAffichage));
            //updateConsole.Start();
          
            lireMessage();
            //foreach (Thread tRad in threads) if (tRad.IsAlive)Console.WriteLine("Thread vivant "+tRad.Name);
            foreach (Thread tRad in threads)
                if (tRad.IsAlive) { tRad.Abort(); tRad.Join(); }
            foreach (Radiometre rad in Radiometres)
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
                updateAffichage();
                Console.SetCursorPosition(40, 24);
                Thread.Sleep(10000);
            }
        }

        private static void afficher()
        {
            int nbColonnesMin = 80;
            int nbLignesMin = 25;

            if (Console.WindowWidth < nbColonnesMin)
                Console.WindowWidth = nbColonnesMin;
            if (Console.WindowHeight < nbLignesMin)
                Console.WindowHeight = nbLignesMin;

            int nbColonnes = Console.WindowWidth;
            int nbLignes = Console.WindowHeight;

            int ecartMessage = nbColonnes / 5;
            int tailleMessage = ecartMessage - 2;
            string[] entetes = { "STATUT", "FREQ", "T_CASE", "T_LOAD", "ECRITURE" };
            EcartMessage = ecartMessage;
            //ecriture des entetes
            for (int i = 0; i < 5; ++i)
            {
                Console.SetCursorPosition(i * ecartMessage + 1, 0);
                Console.Write(entetes[i]);
            }
         
            //Tracer des lignes de separation
            int debut = 1; 
            for (int i = debut; i <= debut+NbRadiometres*4; i=i+4)
            {
                dessinerLigne(i, nbColonnes);
            }

            Console.SetCursorPosition(0, 24);
            Console.Write("\"quit\" : quitter   \"mes\" : mesurer");
        }

        private static void updateAffichage()
        {
            int positionLigne = 3;
            foreach(Radiometre radiometre in Radiometres  )
            {
                //statut de la communication
                string statut = "";
                if (radiometre.InitOK)
                    statut = "attente";
                if (radiometre.Started)
                    statut = "OK";
                if (radiometre.RSenvoye)
                    statut = "perdue";
                Console.SetCursorPosition(0, positionLigne);
                Console.Write(statut);

                //frequence 
                Console.SetCursorPosition(EcartMessage, positionLigne);
                Console.Write(radiometre.FrequenceRad);

                //Tcase
                Console.SetCursorPosition(EcartMessage*2, positionLigne);
                Console.Write(radiometre.TCase);
                //Tload
                Console.SetCursorPosition(EcartMessage*3, positionLigne);
                Console.Write(radiometre.TLoad);
                //Derniere ecriture
                Console.SetCursorPosition(EcartMessage*4, positionLigne);
                Console.Write(radiometre.derniereEcriture.ToShortTimeString());
                positionLigne += 4;
            }
            //Thread.Sleep(10000);

        }
        private static void dessinerLigne(int ligne, int maxColonne)
        {
            Console.SetCursorPosition(0, ligne);
            for(int i=0; i<maxColonne; ++i)
            {
                Console.Write("-");
            }
        }

    }
}
