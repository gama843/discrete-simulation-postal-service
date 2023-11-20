// Diskretni simulace - Overovani reseni problemu s postou (viz https://ksvi.mff.cuni.cz/~holan/posta.html)
// Daniel Kuchta,  1. rocnik @ MFF UK 
// letni semestr 2021/2022
// NPRG031 Programování 2

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SimulacePosty
{
    public enum TypUdalosti
    {
        VytvorDopisy,
        ZacinaPrekladatDopisy,
        DodatocnyNakladZInehoAuta,
        OdhlasSaZPosty
    }

    class Udalost
    {
        public int Kdy;
        public Auto Kdo;
        public TypUdalosti Co;

        public Udalost(int kdy, Auto kdo, TypUdalosti co)
        {
            Kdy = kdy;
            Kdo = kdo;
            Co = co;
        }
    }

    class Kalendar
    {
        List<Udalost> seznam;

        public Kalendar()
        {
            seznam = new List<Udalost>();
        }

        public void Pridej(Udalost udalost)
        {
            seznam.Add(udalost);
        }

        public Udalost VyberPrvni()
        {
            Udalost prvni = null;
            foreach (Udalost ud in seznam)
                if ((prvni == null) || (ud.Kdy < prvni.Kdy))
                    prvni = ud;
            seznam.Remove(prvni);
            return prvni;
        }
    }

    class Posta
    {
        public Point ID;
        //ulozene dopisy - key: destinace value: expirace
        public Dictionary<Point, int> dopisy;
        //slovnik s pritomnymi autami - key: instance auta value: cas jeho odchodu z tejto posty
        public Dictionary<Auto, int> pritomne_auta = new Dictionary<Auto, int>();

        public Posta(Point ID)
        {
            this.ID = ID;
            dopisy = new Dictionary<Point, int>();
        }
    }

    class Zastavka
    {
        public Point ID;
        public int prichod;
        public int odchod;

        public Zastavka(string popis)
        {
            string[] popisy = popis.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            this.ID = new Point(int.Parse(popisy[0]), int.Parse(popisy[1]));
            this.prichod = int.Parse(popisy[2]);
            this.odchod = int.Parse(popisy[4]);
        }
    }

    class Auto
    {
        //ID auta, pro debug ucely
        public string ID;

        //seznam zastavek - reprezentuje trasu auta s informaciami o prichode a odchode z danej zastavky voci aktualnemu casu
        public List<Zastavka> trasa;

        //seznam pravidel pro nakladani a vykladani spolu s inkluzivnimi indexami na trase, pro ktere dane pravidlo plati - e.g. L xlc 10 15 znamena, L - naloz, xlc - vsetky dopis, ktorych destinace ma X vacsie ako X aktualnej posty, 10 15 - na zastavkach 10 az 15 (obe vratane) na svojej trase  
        public List<string> policies = new List<string>(); 

        //seznam pravidel pro nakladani na aktualni zastavce 
        public List<string> CoNakladat = new List<string>();

        //seznam pravidel pro vykladani na aktualni zastavce 
        public List<string> CoVykladat = new List<string>();

        //ulozene dopisy - key: destinace value: expirace 
        public Dictionary<Point, int> naklad;

        private int cislo_zastavky;
        private Model model;
        int aktualny_index;

        public Auto(Model model, int delay, string ID, List<Zastavka> trasa, List<string> policies)
        {
            this.model = model;
            this.ID = ID;
            this.trasa = trasa;

            //spracuj pravidla 
            foreach (var policy in policies)
            {
                this.policies.Add(policy);
            }

            naklad = new Dictionary<Point, int>();
            cislo_zastavky = -1;
            //naplanuj prvy preklad dopisov na poste na indexe 0 z trasy daneho auta, delay sa pouziva pre oneskoreny start auta - t.j. nie v case 0 
            model.Naplanuj(new Udalost(delay + trasa[cislo_zastavky + 1].prichod, this, TypUdalosti.ZacinaPrekladatDopisy));
        }

        //podla aktualneho cisla zastavky (teda indexu na trase) nastavi aktualne platne pravidla pre nakladanie a vykladanie dopisov
        private void NastavAktualnePolicies()
        {
            CoNakladat.Clear();
            CoVykladat.Clear();

            foreach (string policy in policies)
            {
                List<string> string_split = policy.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                int lower = int.Parse(string_split[2]);
                int upper = int.Parse(string_split[3]);

                if (lower <= cislo_zastavky & cislo_zastavky <= upper)
                {
                    if (string_split[0] == "L")
                    {
                        CoNakladat.Add(string_split[1]);
                    }

                    else if (string_split[0] == "U")
                    {
                        CoVykladat.Add(string_split[1]);
                    }
                }
            }
        }

        //spracuva pravidla pre nakladanie dopisov - xlc, xsc, xec, xlx, xex, xsx a ich y verzie, viac info v dokumentacii
        private void ZpracujNakladovuPolicy (string policy)
        {
            foreach (var dopis in model.seznam_post[aktualny_index].dopisy)
            {
                bool condition = new();

                if (policy[0] == 'x')
                {
                    if (policy[1] == 's')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X < model.seznam_post[aktualny_index].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X < value;
                        }
                    }

                    else if (policy[1] == 'l')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X > model.seznam_post[aktualny_index].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X > value;
                        }
                    }

                    else if (policy[1] == 'e')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X == model.seznam_post[aktualny_index].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X == value;
                        }
                    }
                }

                else if (policy[0] == 'y')
                {
                    if (policy[1] == 's')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y < model.seznam_post[aktualny_index].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y < value;
                        }
                    }

                    else if (policy[1] == 'l')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y > model.seznam_post[aktualny_index].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y > value;
                        }
                    }

                    else if (policy[1] == 'e')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y == model.seznam_post[aktualny_index].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y == value;
                        }
                    }
                }

                if (naklad.ContainsKey(dopis.Key))
                {
                    if (dopis.Value < naklad[dopis.Key])
                    {
                        if (condition)
                        {
                            naklad[dopis.Key] = dopis.Value;
                            model.seznam_post[aktualny_index].dopisy.Remove(dopis.Key);
                        }
                    }
                }

                else
                {
                    if (condition)
                    {
                        naklad[dopis.Key] = dopis.Value;
                        model.seznam_post[aktualny_index].dopisy.Remove(dopis.Key);
                    }
                }

            }
        }

        //spracuva pravidla pre vykladanie dopisov - xlc, xsc, xec, xlx, xex, xsx a ich y verzie, viac info v dokumentacii
        private void ZpracujVykladovuPolicy(string policy)
        {
            foreach (var dopis in naklad)
            {
                bool condition = new();

                if (policy[0] == 'x')
                {
                    if (policy[1] == 's')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X < trasa[cislo_zastavky].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X < value;
                        }
                    }

                    else if (policy[1] == 'l')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X > trasa[cislo_zastavky].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X > value;
                        }
                    }

                    else if (policy[1] == 'e')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.X == trasa[cislo_zastavky].ID.X;
                        }

                        if (policy[2] == 'x')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.X == value;
                        }
                    }
                }

                else if (policy[0] == 'y')
                {
                    if (policy[1] == 's')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y < trasa[cislo_zastavky].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y < value;
                        }
                    }

                    else if (policy[1] == 'l')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y > trasa[cislo_zastavky].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y > value;
                        }
                    }

                    else if (policy[1] == 'e')
                    {
                        if (policy[2] == 'c')
                        {
                            condition = dopis.Key.Y == trasa[cislo_zastavky].ID.Y;
                        }

                        if (policy[2] == 'y')
                        {
                            int value = int.Parse(policy.Substring(3));
                            condition = dopis.Key.Y == value;
                        }
                    }
                }

                if (condition)
                {
                    //ak uz na tejto poste, nie je starsi list, s rovnakou destinaciou
                    if (model.seznam_post[aktualny_index].dopisy.ContainsKey(dopis.Key))
                    {
                        if (model.seznam_post[aktualny_index].dopisy[dopis.Key] > dopis.Value)
                        {
                            model.seznam_post[aktualny_index].dopisy[dopis.Key] = dopis.Value;
                        }
                    }

                    else
                    {
                        model.seznam_post[aktualny_index].dopisy[dopis.Key] = dopis.Value;
                    }
                    //odstranim ho z auta
                    naklad.Remove(dopis.Key);
                }

            }
        }

        //funkcia pre vytvorenie dopisov podla pravidiel - xlc, xsc, xec, xlx, xex, xsx a ich y verzie
        private void VytvorDopisyPreXYPolicy(string policy)
        {
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    //ak tam este nie je list do danej zastavky, vytvorim ho, ak tam je, tak bude nutne starsi
                    Point cielova_posta = new Point(i, j);

                    bool condition = new();

                    if (policy[0] == 'x')
                    {
                        if (policy[1] == 's')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = i < trasa[cislo_zastavky].ID.X;
                            }

                            if (policy[2] == 'x')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = i < value;
                            }
                        }

                        else if (policy[1] == 'l')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = i > trasa[cislo_zastavky].ID.X;
                            }

                            if (policy[2] == 'x')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = i > value;
                            }
                        }

                        else if (policy[1] == 'e')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = i == trasa[cislo_zastavky].ID.X;
                            }

                            if (policy[2] == 'x')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = i == value;
                            }
                        }
                    }

                    else if (policy[0] == 'y')
                    {
                        if (policy[1] == 's')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = j < trasa[cislo_zastavky].ID.Y;
                            }

                            if (policy[2] == 'y')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = j < value;
                            }
                        }

                        else if (policy[1] == 'l')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = j > trasa[cislo_zastavky].ID.Y;
                            }

                            if (policy[2] == 'y')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = j > value;
                            }
                        }

                        else if (policy[1] == 'e')
                        {
                            if (policy[2] == 'c')
                            {
                                condition = j == trasa[cislo_zastavky].ID.Y;
                            }

                            if (policy[2] == 'y')
                            {
                                int value = int.Parse(policy.Substring(3));
                                condition = j == value;
                            }
                        }

                    }

                    if (condition)
                    {
                        if (!model.seznam_post[aktualny_index].dopisy.ContainsKey(cielova_posta))
                        {
                            model.seznam_post[aktualny_index].dopisy[cielova_posta] = model.cas + 1425;
                        }
                    }
                }
            }

            //odstranim dopis pre aktualne mesto 
            model.seznam_post[aktualny_index].dopisy.Remove(trasa[cislo_zastavky].ID);
        }

        //vykonaj vsetky aktualne platni pravidla pre nakladanie dopisov
        private void NalozDopisy()
        {
            if (CoNakladat.Contains("all"))
            {
                foreach (var dopis in model.seznam_post[aktualny_index].dopisy)
                {
                    //ak nie je expirovany
                    if (dopis.Value > model.cas)
                    {
                        //ak mam dopis do mesta, kam smeruje tento dopis
                        if (naklad.ContainsKey(dopis.Key))
                        {
                            //a ak je dopis z posty starsi
                            if (dopis.Value < naklad[dopis.Key])
                            {
                                //nalozim ho na korbu, inak ho neberiem, lebo mam starsi
                                naklad[dopis.Key] = dopis.Value;
                            }
                        }

                        //ak taky dopis este nemam
                        else
                        {
                            //beriem hocico
                            naklad[dopis.Key] = dopis.Value;
                        }

                        //z posty ho ale zmazem
                        model.seznam_post[aktualny_index].dopisy.Remove(dopis.Key);
                    }
                }
            }

            if (CoNakladat.Contains("en_route"))
            {
                //pozriem sa na kazde mesto po mojej ceste
                for (int i = cislo_zastavky + 1; i < trasa.Count; i++)
                {
                    //ci je donho nejaky list na tejto poste
                    if (model.seznam_post[aktualny_index].dopisy.ContainsKey(trasa[i].ID))
                    {
                        //a ci uz mam nalozeny nejaky list do tohto mesta
                        if (naklad.ContainsKey(trasa[i].ID))
                        {
                            //a starsi z nich
                            if (model.seznam_post[aktualny_index].dopisy[trasa[i].ID] < naklad[trasa[i].ID])
                            {
                                //nalozim
                                naklad[trasa[i].ID] = model.seznam_post[aktualny_index].dopisy[trasa[i].ID];
                            }
                        }
                        //ak nemam, tak beriem hocico
                        else
                        {
                            naklad[trasa[i].ID] = model.seznam_post[aktualny_index].dopisy[trasa[i].ID];
                        }

                        //kazdopadne, na poste nic neostava
                        model.seznam_post[aktualny_index].dopisy.Remove(trasa[i].ID);
                    }
                    //ak nie je, tak padla, vsetko je hotovo a kazdy sa tesi 
                }
            }

            //handing x/y policies
            foreach (string policy in CoNakladat)
            {
                if (policy.StartsWith("x") | policy.StartsWith("y"))
                {
                    ZpracujNakladovuPolicy(policy);
                }
            }
        }

        //vyloz list do aktualneho mesta (ak je po expiracii), alebo nanho zabudni, pretoze je uspesne doruceny 
        private void VylozDopisUrcenyDoAktualnehoMesta()
        {
            //ak mam list smerujuci do aktualneho mesta
            Point aktualne_ID = model.seznam_post[aktualny_index].ID;
            if (naklad.ContainsKey(aktualne_ID))
            {
                //a je po expiracii, ulozim ho na poste, aby sa nasiel pri kontrole
                if (naklad[aktualne_ID] < model.cas)
                {
                    model.seznam_post[aktualny_index].dopisy[aktualne_ID] = naklad[aktualne_ID];
                }

                //vyhodim ho z auta - bud je doruceny, alebo je na poste
                naklad.Remove(aktualne_ID);
            }
        }

        //vykonaj vsetky aktualne platni pravidla pre vykladanie dopisov
        private void VylozDopisy()
        {
            if (CoVykladat.Contains("nen_route"))
            {
                List<Point> posty_en_route = new List<Point>();
                //pozriem sa na kazde mesto po mojej ceste
                for (int i = cislo_zastavky + 1; i < trasa.Count; i++)
                {
                    posty_en_route.Add(trasa[i].ID);
                }

                foreach (var dopis in naklad)
                {
                    if (!posty_en_route.Contains(dopis.Key))
                    {
                        //ci je donho nejaky list na tejto poste
                        if (model.seznam_post[aktualny_index].dopisy.ContainsKey(dopis.Key))
                        {
                            //a starsi z nich
                            if (model.seznam_post[aktualny_index].dopisy[dopis.Key] > naklad[dopis.Key])
                            {
                                //necham na poste
                                model.seznam_post[aktualny_index].dopisy[dopis.Key] = dopis.Value;
                            }
                        }

                        //ak tak taky list nie je 
                        else
                        {
                            model.seznam_post[aktualny_index].dopisy[dopis.Key] = dopis.Value;
                        }

                        //kazdopadne, v aute nic neostava
                        naklad.Remove(dopis.Key);
                    }
                }
            }

            //handing x/y policies
            foreach (string policy in CoVykladat)
            {
                if (policy.StartsWith("x") | policy.StartsWith("y"))
                {
                    ZpracujVykladovuPolicy(policy);
                }
            }

            if (cislo_zastavky + 1 == trasa.Count)
            {
                cislo_zastavky = 0;
            }
        }

        public void Zpracuj(Udalost ud)
        {
            switch (ud.Co)
            {
                //vytvara dopisy v case, aby ich aktualne auto nestihlo zobrat 
                case TypUdalosti.VytvorDopisy:
                    
                    //najdem si postu, v meste, v ktorom sa nachadzam
                    for (int i = 0; i < model.seznam_post.Count; i++)
                    {
                        if (model.seznam_post[i].ID == trasa[cislo_zastavky].ID)
                        {
                            aktualny_index = i;
                        }
                    }

                    if (CoNakladat.Contains("all"))
                    {
                        //pre kazdu postu 
                        for (int i = 0; i < 32; i++)
                        {
                            for (int j = 0; j < 32; j++)
                            {
                                //ak tam este nie je list do danej zastavky, vytvorim ho, ak tam je, tak bude nutne starsi
                                Point cielova_posta = new Point(i, j);
                                if (!model.seznam_post[aktualny_index].dopisy.ContainsKey(cielova_posta))
                                {
                                    //Console.WriteLine("Vytvarim dopisy s expiraci: {0}", model.cas + 1425);
                                    model.seznam_post[aktualny_index].dopisy[cielova_posta] = model.cas + 1425;
                                }
                            }
                        }

                        //odstranim dopis pre aktualne mesto 
                        model.seznam_post[aktualny_index].dopisy.Remove(trasa[cislo_zastavky].ID);
                    }

                    if (CoNakladat.Contains("en_route"))
                    {
                        //pre kazdu postu pozdlz trasy
                        for (int i = cislo_zastavky + 1; i < trasa.Count; i++)
                        {
                            //ak tam este nie je list do danej zastavky, vytvorim ho, ak tam je, tak bude nutne starsi
                            if (!model.seznam_post[aktualny_index].dopisy.ContainsKey(trasa[i].ID))
                            {
                                model.seznam_post[aktualny_index].dopisy[trasa[i].ID] = model.cas + 1425;
                            }
                        }
                        //odstranim dopis pre aktualne mesto 
                        model.seznam_post[aktualny_index].dopisy.Remove(trasa[cislo_zastavky].ID);
                    }

                    //handing x/y policies
                    foreach (string policy in CoNakladat)
                    {
                        if (policy.StartsWith("x") | policy.StartsWith("y"))
                        {
                            VytvorDopisyPreXYPolicy(policy);
                        }
                    }

                    model.Naplanuj(new Udalost(model.cas + trasa[cislo_zastavky + 1].prichod, this, TypUdalosti.ZacinaPrekladatDopisy));
                    break;

                //presuva auto do noveho mesta a zabezpecuje naklad novych dopisov, vylozenie dopisu urceneho do daneho mesta a vsetkych ostatnych podla pravidiel na vykladanie pre aktualnu postu 
                case TypUdalosti.ZacinaPrekladatDopisy:

                    //presuniem sa do dalsieho mesta 
                    cislo_zastavky++;

                    //najdem si postu, v meste, v ktorom sa nachadzam
                    for (int i = 0; i < model.seznam_post.Count; i++)
                    {
                        if (model.seznam_post[i].ID == trasa[cislo_zastavky].ID)
                        {
                            aktualny_index = i;
                        }
                    }

                    //pingni ostatne auta na poste
                    foreach (Auto auto_na_poste in model.seznam_post[aktualny_index].pritomne_auta.Keys)
                    {
                        //s ktorymi tu stravime aspon 15 minut 
                        if ((trasa[cislo_zastavky].prichod - model.seznam_post[aktualny_index].pritomne_auta[auto_na_poste]) >= 15)
                        {
                            //, nech si nalozia co chcu
                            model.Naplanuj(new Udalost(model.cas, auto_na_poste, TypUdalosti.DodatocnyNakladZInehoAuta));
                        }
                    }

                    //sam sa prihlas sa postu
                    model.seznam_post[aktualny_index].pritomne_auta[this] = model.cas + trasa[cislo_zastavky].odchod;
                    model.Naplanuj(new Udalost(model.cas + trasa[cislo_zastavky].odchod - 15, this, TypUdalosti.OdhlasSaZPosty));

                    //aktualizuje pravidla pre nakladanie a vykladanie dopisov 
                    NastavAktualnePolicies();

                    //..
                    NalozDopisy();

                    //..
                    VylozDopisUrcenyDoAktualnehoMesta();

                    //a dalsie podla toho co mam vykladat
                    VylozDopisy();

                    int cas_vytvarania_dopisov = model.cas + trasa[cislo_zastavky].odchod - 14;
                    model.Naplanuj(new Udalost(cas_vytvarania_dopisov, this, TypUdalosti.VytvorDopisy));
                    break;

                //v pripade, ze do aktualnej posty prislo dalsie auto, mozem od neho precerpat dopisy, ak spolu budeme este aspon 15 minut v meste
                case TypUdalosti.DodatocnyNakladZInehoAuta:
                    NalozDopisy();
                    break;

                //odchadzam z mesta, teda vymazem sa zo zoznamu aut na aktualnej poste 
                case TypUdalosti.OdhlasSaZPosty:
                    model.seznam_post[aktualny_index].pritomne_auta.Remove(this);
                    break;
            }
        }
    }

    class Model
    {
        public int cas;
        //po tomto case sa simulace ukonci, nakolko sa uz bude vsetko opakovat 
        private int dokdySimulovat;
        private Kalendar kalendar;
        //seznam aut pre kontrolu expirovanych dopisov
        private List<Auto> seznam_aut = new List<Auto>();
        //seznam post pre kontrolu expirovanych dopisov
        public List<Posta> seznam_post = new List<Posta>();
        string cesta_k_vstupnim_datam = "test_data.txt";

        public void Naplanuj(Udalost ud)
        {
            kalendar.Pridej(ud);
        }

        //na zacatku vytvori instance vsech poste a v kazdej poste vytvori dopisy do vsech mest
        public void VytvorPostyADopisy()
        {
            Dictionary<Point, int> dopisy_do_vsech_mest = new Dictionary<Point, int>();
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    Point mesto = new Point(i, j);
                    dopisy_do_vsech_mest[mesto] = 1426;
                }
            }

            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    Posta posta = new Posta(new Point(i, j));
                    foreach (var dopis in dopisy_do_vsech_mest)
                    {
                        posta.dopisy[dopis.Key] = dopis.Value;
                    }
                    //odstran dopis do aktualneho mesta
                    posta.dopisy.Remove(new Point(i, j));
                    seznam_post.Add(posta);
                }
            }
        }

        //vytvori vsechny procesy - teda auta, nastavi im trasy, delay a pravidla pre nakladanie a vykladanie dopisov 
        public void VytvorProcesy()
        {
            System.IO.StreamReader subor = new System.IO.StreamReader(cesta_k_vstupnim_datam);
            List<Zastavka> trasa = new List<Zastavka>();
            List<string> policies = new List<string>();
            int delay = 0;
            while (!subor.EndOfStream)
            {
                string s = subor.ReadLine();
                if (s != "")
                {
                    switch (s[0])
                    {
                        case 'A':
                            string ID = s.Substring(2);
                            Auto auto = new Auto(this, delay, ID, trasa, policies);
                            seznam_aut.Add(auto);
                            trasa = new List<Zastavka>();
                            policies = new List<string>();
                            break;
                        case 'S':
                            trasa.Add(new Zastavka(s.Substring(1)));
                            break;
                        case 'D':
                            delay = int.Parse(s.Substring(2));
                            break;
                        case 'L':
                            policies.Add(s);
                            break;
                        case 'U':
                            policies.Add(s);
                            break;
                    }
                }
            }
            subor.Close();
        }

        //po skonceni simulace prejde vsechny posty a auta a skontroluje jestli se tam nenachazi list po expiraci 
        public bool SkontrolujDopisy()
        {
            bool vsechno_ok = true;
            int dopisy_po_expiraci_na_poste = 0;
            int dopisy_po_expiraci_v_autach = 0;

            foreach (Posta posta in seznam_post)
            {
                foreach (var dopis in posta.dopisy)
                {
                    if (dopis.Value < cas)
                    {
                        Console.WriteLine("Chyba: Na poste ({0},{1}) je dopis po expiraci. Destinace dopisu: ({2},{3}). Expirace: {4}", posta.ID.X, posta.ID.Y, dopis.Key.X, dopis.Key.Y, dopis.Value);
                        vsechno_ok = false;
                        dopisy_po_expiraci_na_poste++;
                    }
                }
            }

            foreach (Auto auto in seznam_aut)
            {
                foreach (var dopis in auto.naklad)
                {
                    if (dopis.Value < cas)
                    {
                        Console.WriteLine("Chyba: V aute {0} je dopis po expiraci. Destinace dopisu: ({1},{2}). Expirace: {3}", auto.ID, dopis.Key.X, dopis.Key.Y, dopis.Value);
                        vsechno_ok = false;
                        dopisy_po_expiraci_v_autach++;
                    } 
                }
            }

            Console.WriteLine("Dopisy po expiraci na poste: {0}", dopisy_po_expiraci_na_poste);
            Console.WriteLine("Dopisy po expiraci v autach: {0}", dopisy_po_expiraci_v_autach);
            return vsechno_ok;
        }


        //pomocna funkce na spocteni nejvetsiho spolecneho delitele
        static int NejvetsiSpolecnyDelitel(int a, int b)
        {
            while (b != 0)
            {
                int tmp = b;
                b = a % b;
                a = tmp;
            }
            return a;
        }

        //pomocna funkce na spocteni nejmensiho spolecneho nasobku 
        static int NejmensiSpolecnyNasobek(int a, int b)
        {
            return (a / NejvetsiSpolecnyDelitel(a, b)) * b;
        }

        //spocte cas dokdy ma zmysel simulovat - nejmensi spolecny nasobek period vsech aut + 1440 minut (24h) 
        private int SpocitajCasSimulace()
        {
            int nejmensiSpolecnyNasobekPeriod = 1;

            foreach (Auto auto in seznam_aut)
            {
                int dlzka_trasy = 0;
                for (int i = 0; i < auto.trasa.Count; i++)
                {
                    dlzka_trasy += auto.trasa[i].prichod + auto.trasa[i].odchod;
                }
                nejmensiSpolecnyNasobekPeriod = NejmensiSpolecnyNasobek(nejmensiSpolecnyNasobekPeriod, dlzka_trasy);
                
            }

            //1440 minut treba pripocitat kvoli potencialne shitnutym odchodom aut s rovnakou periodou
            return nejmensiSpolecnyNasobekPeriod + 1440;
        }

        //skontroluje jestli vstupni data neobsahuju invalidne trasy - totiz maximalni rychlost, kterou jse de presunut mezdi dvema sousednimi postami je 6 minut
        private bool SkontrolujValidituTras()
        {
            bool problem = false;

            foreach (Auto auto in seznam_aut)
            {
                for (int i = 0; i < auto.trasa.Count - 1; i++)
                {
                    int x1 = auto.trasa[i].ID.X;
                    int y1 = auto.trasa[i].ID.Y;
                    int x2 = auto.trasa[i+1].ID.X;
                    int y2 = auto.trasa[i+1].ID.Y;

                    if (((Math.Abs(x1-x2) + Math.Abs(y1-y2)) * 6) > auto.trasa[i+1].prichod)
                    {
                        problem = true;
                        Console.WriteLine("Auto {0} nema validnu trasu. Problem je mezi zastavkami ({1},{2}) a ({3},{4}).", auto.ID, x1, y1, x2, y2);
                    }
                }

                if (problem == true) return false;
            }
            return true;
        }

        public bool Vypocet()
        {
            cas = 0;
            kalendar = new Kalendar();
            
            VytvorProcesy();
            if (SkontrolujValidituTras() == false)
            {
                Console.WriteLine("Chyba: Trasy nie su validne.");
                return false;
            }

            else
            {
                Console.WriteLine("Kontrola tras probehla uspesne.");
            }

            dokdySimulovat = SpocitajCasSimulace();

            if (dokdySimulovat > 200000)
            {
                Console.WriteLine("Chyba: Simulace bude trvat prilis dlouho.");
                return false;
            }

            Console.WriteLine("Predpokladana dlouzka simulace je v poradku.");

            VytvorPostyADopisy();
            
            Udalost ud;

            if (seznam_aut.Count > 1023)
            {
                Console.WriteLine("Chyba: Prilis mnoho aut. Problem jde vyresit i s 1023 autami.");
                return false;
            }

            Console.WriteLine("Celkovy pocet aut: {0}", seznam_aut.Count);
            Console.WriteLine("Probiha simulace, prosim cekejte.");

            while ((ud = kalendar.VyberPrvni()) != null & cas < dokdySimulovat)
            {
                cas = ud.Kdy;
                ud.Kdo.Zpracuj(ud);
            }

            if (SkontrolujDopisy() == true)
            {
                Console.WriteLine("Reseni je korektne. Gratulujeme.");
                return true;
            }

            else
            {
                Console.WriteLine("Reseni neni korektne. Je nam to lito.");
                return false;
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Model model = new();
            model.Vypocet();
        }
    }
}