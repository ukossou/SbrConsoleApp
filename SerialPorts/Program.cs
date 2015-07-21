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
        static SerialPort PortSerie;
        static int FrequenceRad = 0;
        static System.Timers.Timer TimerReception;//Timer pour detecter si le radiometre cesse de transmettre 
        const int DELAIS_REC_MAX = 10000;
        static DirectoryInfo RepCourant;
        static DateTime DateCourante;
        static StreamWriter FichierCourant;
        static Queue DonneeLues;
        static void Main(string[] args)
        {
            Thread threadEcriture = new Thread(ecrireDisque);
            Console.WriteLine("....Programme de test du port avec les RADIOMETRES....\n");

            //recherche des ports serie disponiibles
            Console.WriteLine("Ports serie disponibles : "+
                                String.Join(" ",SerialPort.GetPortNames())
                             );

            //Initialisation du COM5
            Console.WriteLine("Ouverture du port " + "COM5");
            bool res=initialiserPort( "COM5", 38400 , 10000);
            if(res)//reussite de l'initialisation
                {
                    Console.WriteLine("\n...REUSSITE...ouverture... " + PortSerie.PortName + "\n");

                    //tester la communication 
                    if(testerCommunication())
                        Console.WriteLine("\n...Reception OK sur ... " + PortSerie.PortName + "\n"); 

                    FrequenceRad = detecterFrequence();
                    Console.WriteLine("\n...Frequence du RADIOMETRE... " + FrequenceRad + "\n");

                    //creer le repertoire qui contiendra les mesures
                    RepCourant = Directory.CreateDirectory("Rad-" + FrequenceRad);
                    //Console.WriteLine(infoRep.Name);

                    //creer le fichier de donnees avec la date courante

                    DateCourante = DateTime.Now;
                    Console.WriteLine("Date actuelle " + DateCourante.ToString());

                    char sep = Path.DirectorySeparatorChar;
                    string cheminFich = RepCourant.Name + sep 
                                        +"MesDu-"+DateCourante.ToString("d")
                                        +".txt";
                    Console.WriteLine("cheminFich " + cheminFich);
                    FichierCourant =new StreamWriter(File.Create(cheminFich));

                    //Initialisation de DonneesLues

                    DonneeLues = Queue.Synchronized(new Queue());
                    
                    //Verifier si le fichier existe deja avant d'ecrire autre chose
                    //Ecrire dans le fichier
                    String entete;
                    entete =  String.Format("{0,-20}","Date" );
                    entete += String.Format("{0,-10}", ",Rad Time");
                    entete += String.Format("{0,-5}", ",Rec");
                    entete += String.Format("{0,-9}", ",PWM");
                    entete += String.Format("{0,-9}", ",Tec V");
                    entete += String.Format("{0,-9}", ",T_ant");
                    entete += String.Format("{0,-9}", ",T_load");
                    entete += String.Format("{0,-9}", ",T_IF");
                    entete += String.Format("{0,-9}", ",T_case");
                    entete += String.Format("{0,-7 }", ",Supply");
                    entete += String.Format("{0,-9}", ",V-NdOn");
                    entete += String.Format("{0,-8}", ",V-NdOff");
                    entete += String.Format("{0,-6}", ",Flag");
                    entete += String.Format("{0,-10}", ",Angle");
                    entete += String.Format("{0,-9}", ",Temp");
                    entete += String.Format("{0,-11}", ",X-Data");
                    entete += String.Format("{0,-11}", ",Y-Data");

                    entete += String.Format("{0,-20}", ",Date");
                    entete += String.Format("{0,-10}", ",Rad Time");
                    entete += String.Format("{0,-5}", ",Rec");
                    entete += String.Format("{0,-8}", ",Freq");
                    entete += String.Format("{0,-10}", ",Vsky-V");
                    entete += String.Format("{0,-10}", ",Vsky-V+ND");
                    entete += String.Format("{0,-10}", ",Vsky-H");
                    entete += String.Format("{0,-10}", ",Vsky-V+ND");
                    entete += String.Format("{0,-10}", ",V_load");
                    entete += String.Format("{0,-10}", ",V_load+ND");
                    entete += String.Format("{0,-10}", ",Tsky-V");
                    entete += String.Format("{0,-10}", ",Tsky-H");
                    entete += String.Format("{0,-10}", ",Tsky(V-H)");
                   
                    //11 -'Software Date, Record, Time, Record_Type, PWM, TecV, T_ant, T_load, T_IF, T_case, Supply, V-NdOn, V-NdOff, Data_Flag, Angle,Temp Incl,X-Data, Y-Data
                    //21 -'Record,Time,Record_Type,Freq,Vsky-V,Vsky-V+ND,Vsky-H,Vsky-H+ND,VLoad,Vload+ND,Tsky-V,Tsky-H,TskyV-TskyH,'
                    //FichierCourant.BaseStream.
                    FichierCourant.WriteLine(entete);

                    
                       
                    //Initialisation de TimerReception
                    TimerReception = new System.Timers.Timer(DELAIS_REC_MAX);
                    TimerReception.Elapsed +=new ElapsedEventHandler(depassementTimerReception);

                    //ajout de l'evenement DataReceived
                    PortSerie.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    //Demarrage de TimerReception
                    TimerReception.Start();
                    threadEcriture.Start();
                }
            else
                Console.WriteLine("...ECHEC...ouverture... " + PortSerie.PortName + "\n");

                        
            Console.WriteLine("\n...FIN du programme... ");
            Console.ReadLine();
            threadEcriture.Abort();
            FichierCourant.Close();
            //fermeture du port serie
            if(PortSerie.IsOpen)
                PortSerie.Dispose();
        }


        private static void ecrireDisque()
        {
            string[] separateur = new string[] { "," };
            string[] mots;
            string ligne;

            int typeData = 0;//type de donnee envoyee par le radiometre "21" ou "11"
            while(true)
            {
                if(DonneeLues.Count >2)
                {
                    ligne = (string)DonneeLues.Dequeue();
                    mots = ligne.Split(separateur, StringSplitOptions.RemoveEmptyEntries);

                    mots[0]=DateTime.Now.ToString();
                    //verification de la validite du format de la donnee
                    if (mots.Length >= 13 && mots.Length <= 17)
                    {
                        string aEcrire = string.Join(" , ", mots);
                        typeData = Convert.ToInt32(mots[2]);
                        //Commencer les lignes avec les "11"
                        if(typeData==11)
                        {
                           aEcrire = aEcrire.Remove(aEcrire.Length-1);
                           aEcrire += " ";
                        }
                        
                        FichierCourant.Write(aEcrire);
                        Console.WriteLine("Type " + typeData + " "+aEcrire);
                    }
                    else
                    {
                        Console.WriteLine("Donne invalide " + ligne);
                    }
                    //Console.WriteLine("ligne "+ligne);
                    //FichierCourant.WriteLine(ligne);
                }
            }
        }
        private static bool testerCommunication()
        {
            bool bonneComm = false;
            int nombreEssais = 0;
            int maxEssais = 10;
            PortSerie.DiscardInBuffer();
            while( bonneComm==false && nombreEssais<maxEssais)
            {
                try
                {
                    PortSerie.ReadLine();
                    bonneComm = true;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Pas de donnes sur le port "+PortSerie.PortName);
                }
                finally
                {
                    //Console.WriteLine("Dans le finally du Timeout de testerCommunication() ");
                    //Envoyer les RS et attendre un peu avec un timer
                    if(!bonneComm)
                    {
                        Console.WriteLine("Attente de 10 secondes");
                        Thread.Sleep(10000);
                    }
                        
                }
                nombreEssais += 1;
            }
            return bonneComm;
        }
        private static int detecterFrequence()
        {
            int valeurFreq = 0;
            string[] separateur = new string[] {","};
            string[] mots;
            string ligne;

            PortSerie.DiscardInBuffer();
            int typeData = 0;//type de donnee envoyee par le radiometre "21" ou "11"
            
            while (typeData!=21)
            {
                    //portSerie.ReadLine();
                    ligne = PortSerie.ReadLine();
                    //Console.WriteLine("Frequence lue : " + ligne);
                    mots = ligne.Split(separateur, StringSplitOptions.RemoveEmptyEntries);
                    //Console.WriteLine("\n Mots lus : " + String.Join(" | ", mots));
                    typeData = Convert.ToInt32(mots[2]);
                    if(typeData==21)
                        valeurFreq = Convert.ToInt32(mots[3]);
             }
            
            return valeurFreq;
        }

        private static bool initialiserPort(string nom, int baudRate, int timeout)
        {
            bool succes = false;
            PortSerie = new SerialPort();
            PortSerie.PortName = nom;
            PortSerie.BaudRate = baudRate;
            PortSerie.ReadTimeout = 10000;

            //Ouverture du port serie
            PortSerie.Open();
            if (PortSerie.IsOpen)
                succes = true;

            return succes;
        }

        private static void depassementTimerReception(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Plus de Reception depuis  {0}", e.SignalTime);
            Console.WriteLine("Envoi de RS "); 
        }
        private static void DataReceivedHandler(
                        object sender,
                        SerialDataReceivedEventArgs e)
        {
            TimerReception.Stop();

            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();

            DonneeLues.Enqueue(indata);

            TimerReception.Start();
            //Console.WriteLine("Taille de DonneesLues " + Convert.ToString(DonneeLues.Count));
               
        }
        /*
        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }*/
    }
}
