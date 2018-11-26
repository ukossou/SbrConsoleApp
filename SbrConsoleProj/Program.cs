using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.IO.Ports;
using System.Timers;
using System.Text;
using System.Globalization;

namespace SbrConsoleApp
{
    class Program
    {
        private static int NbRadiometres;
        private static Queue Radiometres;
        private static System.Timers.Timer TimerAffichage;

        private static int EcartMessage;
        private static int TailleMessage;
        static void Main(string[] args)
        {
            Console.Title = " SBR CARTEL ";
            TimerAffichage = new System.Timers.Timer(3000);
            TimerAffichage.Elapsed += new ElapsedEventHandler(updateAffichage);
           
            
            Radiometres = new Queue();
            Queue threads = new Queue();

            

            List<int> validatedCom = getValidatedCom();
            

            //Initialisation threads et radiometres
            foreach (int port in validatedCom)
            {
                    Radiometre rad = new Radiometre("COM" + port);
                    if(rad.ouverturePort())
                    {
                        Radiometres.Enqueue(rad);
                        Thread threadRad = new Thread(new ThreadStart(rad.demarrer));
                        threadRad.Name = "thread " + port;
                        threads.Enqueue(threadRad);
                    }
               
            }
            NbRadiometres = Radiometres.Count;
            foreach (Thread threadRad in threads)
                threadRad.Start();

            afficher();
            TimerAffichage.Start();
          
            lireMessage();
            

            foreach (Thread tRad in threads)
                if (tRad.IsAlive) { tRad.Abort(); tRad.Join(); }
            foreach (Radiometre rad in Radiometres)
                if (rad.Started)
                    rad.finaliser();
             
        }

        private static List<int> getValidatedCom()
        {
            //read COM ports on the system
            List<int> systemPorts = getSystemComPorts();
            List<int> usersPorts = new List<int>();

            Boolean validCom = false;

            while (!validCom)
            {
                //read COM port entered by the user
                usersPorts = readUserPorts(systemPorts);
                //validate COM ports to be used for radiometers
                validCom = validateComs(usersPorts, systemPorts);
                if (!validCom)
                {
                    Console.WriteLine("COM numbers are not valid ! please retry");
                    Console.Write("Hit ENTER to continue");
                    Console.ReadLine();
                }
            }

            return usersPorts;
        }

        private static bool validateComs(List<int> usersPorts, List<int> systemPorts)
        {
            bool ok = true;
            foreach(var port in usersPorts)
            {
                if (!systemPorts.Contains(port))
                {
                    ok = false;
                    break;
                }
            }

            return ok;
        }

        private static List<int> getSystemComPorts()
        {
            List<int> systemPorts = new List<int>();
           
            foreach (string port in SerialPort.GetPortNames())
            {
                int noCom = Convert.ToInt32(port.Substring(3));
                systemPorts.Add(noCom);
            }

            return systemPorts;
        }

        //todo: faire la logique pour valider les num de port entres par le user
        private static List<int> readUserPorts(List<int> systemPorts)
        {
            Console.Clear();
            Console.Write("COM port(s) detected on the system : ");
            foreach (var port in systemPorts)
            {
                Console.Write(" COM"+ port);
            }

            List<int> userPorts = new List<int>();
            Console.Write("\n\nEnter radiometers COM numbers  ");
            Console.WriteLine("(ex. : 4, 11, 16, 15 )");

            string userInput = Console.ReadLine();
            string[] ports = userInput.Split(',');

            foreach (var port in ports)
            {
                int portNb = Convert.ToInt32(port);
                userPorts.Add(portNb);
            }

            return userPorts;
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
                if (message == "mes")
                {
                    Radiometre.Ecrire = true;
                }
                
                //Thread.Sleep(10000);
            }
        }

        private static void afficher()
        {
            Console.Clear();
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
            for (int i = debut; i <= debut+NbRadiometres*4; i=i+4)
            {
                dessinerLigne(i, nbColonnes);
            }

            Console.SetCursorPosition(0, nbLignes-1);
            Console.Write("\"quit\" : quitter   \"mes\" : mesurer");
            Console.SetCursorPosition(45, nbLignes-1);
        }

        private static void updateAffichage(Object source, ElapsedEventArgs e )
        {
            //sauvegarde de la position courante du curseur
            int ligneCurseur = Console.CursorTop;
            int colonneCurseur = Console.CursorLeft;

            //Mise a jour des champs
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
                Console.Write(String.Format("{0,-10}", statut));

                //frequence 
                Console.SetCursorPosition(EcartMessage+1, positionLigne);
                Console.Write(String.Format("{0,-10}",radiometre.FrequenceRad));
                //TskyV
                Console.SetCursorPosition(EcartMessage*2+1, positionLigne);
                Console.Write(String.Format("{0,-10}",radiometre.TskyV));
                //TskyH
                Console.SetCursorPosition(EcartMessage * 3 + 1, positionLigne);
                Console.Write(String.Format("{0,-10}", radiometre.TskyH));
                //Tload
                Console.SetCursorPosition(EcartMessage*4+1, positionLigne);
                Console.Write(String.Format("{0,-10}",radiometre.TLoad));
                //Derniere ecriture
                Console.SetCursorPosition(EcartMessage*5+1, positionLigne);
                Console.Write(String.Format("{0,-10}",radiometre.derniereEcriture.ToShortTimeString()));
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
