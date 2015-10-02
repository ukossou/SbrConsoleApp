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
    class Radiometre
    {
        private SerialPort PortSerie;
        private int TIMEOUT_RECEPTION = 10000;//10 secondes
        private int BaudRate = 38400;
        private DirectoryInfo RepCourant;
        private System.Timers.Timer TimerJour, TimerReception;
        private StreamWriter FichierCourant;
        private Queue DonneeLues;
        private Thread ThreadEcriture;
        

        public DateTime derniereEcriture;
        public string TskyV;
        public string TskyH;
        public string TLoad;
        public string NomPort;
        public int FrequenceRad = 0;
        public bool Started = false;
        public bool InitOK = false;
        public bool RSenvoye = false;

        public static bool Terminer = false;
        public static bool Ecrire = false;

        public Radiometre(string nom)
        {
            NomPort = nom;
            PortSerie = new SerialPort();
            PortSerie.PortName = nom;
            PortSerie.BaudRate = BaudRate;
            //PortSerie.ReadTimeout = 30000;
            TskyV = "Inconnue";
            TskyH = "Inconnue";
            TLoad = "Inconnue";
        }

        public void demarrer()
        {
            lock (this)
            {
                if (InitOK)//reussite de l'initialisation
                {
                    PortSerie.ReadLine();//attendre des donnees
                    ThreadEcriture = new Thread(ecrireDisque);
                    FrequenceRad = detecterFrequence();
                    //Console.WriteLine("\n...Reception OK sur ... " + PortSerie.PortName
                                        //+ "===" + FrequenceRad + "===\n");
                    //creer le repertoire qui contiendra les mesures
                    RepCourant = Directory.CreateDirectory("Rad-" + FrequenceRad);

                    //creer le fichier de donnees 
                    creerFichier();

                    //Initialisation du tampon de donnees
                    DonneeLues = Queue.Synchronized(new Queue());

                    //ajout des evenements
                    PortSerie.ReadTimeout = 30000;
                    PortSerie.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                    TimerReception = new System.Timers.Timer(TIMEOUT_RECEPTION);
                    TimerReception.Elapsed += new ElapsedEventHandler(timeoutReception);
                    TimerReception.Start();

                    //Demarrer le thread d'ecriture
                    ThreadEcriture.Start();
                    Started = true;
                    initialiserTimerJour();
                }
            }
        }

        public void attendre()
        {
            if (Started)
            {
                //Console.WriteLine("Mesure demarree sur----> " + PortSerie.PortName + "  " + FrequenceRad);
                while (!Terminer)
                {
                    Thread.Yield();
                    Thread.Sleep(3600000);
                    //Console.WriteLine("Thread actif sur " + PortSerie.PortName);
                }
            }

        }

        public void finaliser()
        {
            if (Started)
            {
                ThreadEcriture.Abort();
                ThreadEcriture.Join();
            }

            if (FichierCourant != null)
               FichierCourant.Dispose();

            //fermeture du port serie
            if (PortSerie != null)
            {
                try
                {
                    PortSerie.Close();
                    //Console.WriteLine("***Fermeture radiometre**** " + FrequenceRad);
                }
                catch (Exception) { }
                finally { }
            }
            Console.WriteLine("Finalisation " + NomPort);
        }


        private void initialiserTimerJour()
        {
            DateTime demain = DateTime.Now.Date.AddDays(1);
            TimeSpan ecartDemain = demain.Subtract(DateTime.Now);
            //TimeSpan ecartDemain = new TimeSpan(0, 1, 0);
            if (TimerJour == null)
            {
                TimerJour = new System.Timers.Timer(ecartDemain.TotalMilliseconds);
                TimerJour.Elapsed += new ElapsedEventHandler(depassementTimerJour);
            }
            else
            {
                TimerJour.Interval = ecartDemain.TotalMilliseconds;
            }

            //DateCourante = DateTime.Now;
            TimerJour.Start();

        }
        private void creerFichier()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");//Pour uniformiser le format date sur tous les PC
            char sep = Path.DirectorySeparatorChar;

            string cheminFich = RepCourant.Name + sep
                                + "MesDu-" + DateTime.Now.Date.ToString("d")//DateTime.Now.ToString().Replace(":","_")
                                + "_" + FrequenceRad
                                + ".txt";
            FileStream fileStream = new FileStream(cheminFich, FileMode.Append, FileAccess.Write, FileShare.Read);

            if (FichierCourant == null)
            {//premiere creation du fichier
                FichierCourant = new StreamWriter(fileStream);
                lock (FichierCourant)
                {
                    if (FichierCourant.BaseStream.Length < 100)
                        FichierCourant.WriteLine(creerEntete());
                }

            }
            else //FichierCourant != null
            {
                lock (FichierCourant)
                {
                    FichierCourant.BaseStream.Close();
                    FichierCourant = new StreamWriter(fileStream);
                    FichierCourant.WriteLine(creerEntete());
                }
            }

        }
        private string creerEntete()
        {
            StringBuilder entete = new StringBuilder();
            entete.Append(String.Format("{0,-20}", "Date"));
            entete.Append(String.Format("{0,-10}", ",Rad Time"));
            entete.Append(String.Format("{0,-5}", ",Rec"));
            entete.Append(String.Format("{0,-9}", ",PWM"));
            entete.Append(String.Format("{0,-9}", ",Tec V"));
            entete.Append(String.Format("{0,-9}", ",T1_cal_bb"));
            entete.Append(String.Format("{0,-9}", ",T2_cal_bb"));
            entete.Append(String.Format("{0,-9}", ",T_load"));
            entete.Append(String.Format("{0,-9}", ",T_case"));
            entete.Append(String.Format("{0,-7 }", ",Supply"));
            entete.Append(String.Format("{0,-9}", ",V-NdOn"));
            entete.Append(String.Format("{0,-8}", ",V-NdOff"));
            entete.Append(String.Format("{0,-6}", ",Flag"));
            entete.Append(String.Format("{0,-10}", ",Angle"));
            entete.Append(String.Format("{0,-9}", ",Temp"));
            entete.Append(String.Format("{0,-11}", ",X-Data"));
            entete.Append(String.Format("{0,-11}", ",Y-Data"));

            entete.Append(String.Format("{0,-20}", ",Date"));
            entete.Append(String.Format("{0,-10}", ",Rad Time"));
            entete.Append(String.Format("{0,-5}", ",Rec"));
            entete.Append(String.Format("{0,-8}", ",Freq"));
            entete.Append(String.Format("{0,-10}", ",Vsky-V"));
            entete.Append(String.Format("{0,-10}", ",Vsky-H"));
            entete.Append(String.Format("{0,-10}", ",V_load+ND"));
            entete.Append(String.Format("{0,-10}", ",V_load"));
            entete.Append(String.Format("{0,-10}", ",Tsky-V"));
            entete.Append(String.Format("{0,-10}", ",Tsky-H"));
            entete.Append(String.Format("{0,-10}", ",Tsky(V-H)"));

            return entete.ToString();
        }

        private void ecrireDisque()
        {
            string[] separateur = new string[] { "," };
            string[] mots;
            string ligne;
            int borne = 5;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");

            int typeData = 0;//type de donnee envoyee par le radiometre "21" ou "11"
            while (true)
            {
                if (DonneeLues.Count > borne)
                {
                    for (int i = 0; i < borne; i++)
                    {
                        ligne = (string)DonneeLues.Dequeue();
                        mots = ligne.Split(separateur, StringSplitOptions.RemoveEmptyEntries);

                        //verification de la validite du format de la donnee
                        if (mots.Length >= 10 && mots.Length <= 17)
                        {
                            string aEcrire = string.Join(" , ", mots);
                            try
                            {
                                typeData = Convert.ToInt32(mots[2]);
                            }
                            catch (Exception) { }
                            finally { }
                            //Commencer les lignes avec les "11"
                            if (typeData == 11)
                            {
                                TLoad = mots[7];
                                aEcrire = aEcrire.Remove(aEcrire.Length - 1);
                                aEcrire += " ";
                            }
                            if (typeData == 21)
                            {
                                TskyV = mots[8];
                                TskyH = mots[9];
                            }
                            lock (FichierCourant)
                            {
                                if(Ecrire)
                                {
                                    FichierCourant.Write(aEcrire);
                                    derniereEcriture = DateTime.Now;
                                }
                                    
                            }
                        }
                        
                    }


                }
                else
                    Thread.Sleep(5000);
            }
        }

        private int detecterFrequence()
        {
            int valeurFreq = 0;
            string[] separateur = new string[] { "," };
            string[] mots;
            string ligne = "";

            PortSerie.DiscardInBuffer();
            int typeData = 0;//type de donnee envoyee par le radiometre "21" ou "11"

            while (typeData != 21)
            {
                //portSerie.ReadLine();
                try
                {
                    ligne = PortSerie.ReadLine();
                }
                catch (System.TimeoutException)
                { }
                finally { };
                mots = ligne.Split(separateur, StringSplitOptions.RemoveEmptyEntries);
                if (mots.Length >= 13)
                {
                    try { typeData = Convert.ToInt32(mots[2]); }
                    catch (FormatException) { }
                    finally { }
                }
                if (typeData == 21)
                    valeurFreq = Convert.ToInt32(mots[3]);
            }

            return valeurFreq;
        }

        public bool ouverturePort()
        {
            bool succesOverture = false;
            //Ouverture du port serie
            try
            {
                PortSerie.Open();
                succesOverture = true;
            }
            catch (Exception) { }
            finally { InitOK = succesOverture; }
            return succesOverture;
        }

        private void depassementTimerJour(Object source, ElapsedEventArgs e)
        {
            TimerJour.Stop();
            initialiserTimerJour();
            creerFichier();
        }

        private void timeoutReception(Object source, ElapsedEventArgs e)
        {
            PortSerie.Write("RS" + Convert.ToChar(13));
            RSenvoye = true;
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)             
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-CA");//Pour uniformiser le format date sur tous les PC
            SerialPort sp = (SerialPort)sender;

            TimerReception.Stop();
            if (RSenvoye)
            {
                sp.DiscardInBuffer();
                RSenvoye = false;
            }


            try
            {
                string indata = sp.ReadLine();
                //enlever le premier champ du radiometre pour inserer l'lheure courante
                indata = indata.Remove(0, 7);
                indata = DateTime.Now.ToString() + indata;
                DonneeLues.Enqueue(indata); 
            }
            catch (Exception)
            {}
            finally
            { }

            TimerReception.Start();
        }
    }
}
