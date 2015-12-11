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
        private static System.Timers.Timer TimerAffichage;
        private static Radiometre l_band;
        private static Thread thread_l_band;  
        private static int EcartMessage;
        private static int TailleMessage;
        static void Main(string[] args)
        {
            Console.Title = " SBR CARTEL ";
            TimerAffichage = new System.Timers.Timer(3000);
            TimerAffichage.Elapsed += new ElapsedEventHandler(updateAffichage);


            //Initialisation du radiometre
            l_band = new Radiometre("COM1");
            if(l_band.ouverturePort())
            {
              //Console.WriteLine("Ouverture reussie "); 
              thread_l_band = new Thread(new ThreadStart(l_band.demarrer));
              thread_l_band.Name = "thread " + "COM1";
              thread_l_band.Start();
             }
       
            //Affichage des informations    
            afficher();
            TimerAffichage.Start();
            lireMessage();
         
        }
        private static void lireMessage()
        {
            String message;
            while (!Radiometre.Terminer)
            {

                message = Console.ReadLine();
                message = message.ToLower();

                if (message == "quit")
                {
                    Radiometre.Terminer = true;
                }
                if (message == "mes")
                {
                    Radiometre.Ecrire = true;
                }

            }

            if (l_band != null && thread_l_band != null)
            {
                if (thread_l_band.IsAlive) { thread_l_band.Abort(); thread_l_band.Join(); }
                if (l_band.Started)
                    l_band.finaliser();
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

            EcartMessage = nbColonnes / 6;
            TailleMessage = EcartMessage - 2;
            string[] entetes = { "STATUT", "FREQ", "TskyV", "TskyH", "T_LOAD", "ECRITURE" };
           
            //ecriture des entetes
            for (int i = 0; i < entetes.Length; ++i)
            {
                Console.SetCursorPosition(i * EcartMessage + 1, 0);
                Console.Write(entetes[i]);
            }
         
            //Tracer des lignes de separation
            int debut = 1; 
            for (int i = debut; i <= debut+4; i=i+4)
            {
                dessinerLigne(i, nbColonnes);
            }

            Console.SetCursorPosition(0, nbLignes-3);
            Console.Write("\"quit\" : quitter   \"mes\" : mesurer");
            Console.SetCursorPosition(45, nbLignes-3);
        }

        private static void updateAffichage(Object source, ElapsedEventArgs e )
        {
            //sauvegarde de la position courante du curseur
            int ligneCurseur = Console.CursorTop;
            int colonneCurseur = Console.CursorLeft;

            //Mise a jour des champs
            int positionLigne = 3;
            if(l_band != null)
            {
                
                //statut de la communication
                string statut = "";
                if (l_band.InitOK)
                    statut = "attente";
                if (l_band.Started)
                    statut = "OK";
                if (l_band.RSenvoye)
                    statut = "perdue";
                Console.SetCursorPosition(0, positionLigne);
                Console.Write(String.Format("{0,-10}", statut));

                //frequence 
                Console.SetCursorPosition(EcartMessage+1, positionLigne);
                Console.Write(String.Format("{0,-10}",l_band.FrequenceRad));
                //TskyV
                Console.SetCursorPosition(EcartMessage*2+1, positionLigne);
                Console.Write(String.Format("{0,-10}",l_band.TskyV));
                //TskyH
                Console.SetCursorPosition(EcartMessage * 3 + 1, positionLigne);
                Console.Write(String.Format("{0,-10}", l_band.TskyH));
                //Tload
                Console.SetCursorPosition(EcartMessage*4+1, positionLigne);
                Console.Write(String.Format("{0,-10}",l_band.TLoad));
                //Derniere ecriture
                Console.SetCursorPosition(EcartMessage*5+1, positionLigne);
                Console.Write(String.Format("{0,-10}",l_band.derniereEcriture));
                positionLigne += 4;
            }
            Console.SetCursorPosition(colonneCurseur,ligneCurseur);

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
