using PredragDjoricPR1002018.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using System.Windows.Controls.Primitives;

namespace PredragDjoricPR1002018
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ZoomPan
        private Point start = new Point();
        private Point diffOffset = new Point();
        private int zoomMax = 4;
        private int zoomCurent = 1;

        //tacke
        public List<PowerEntity> listaElemenataIzXML = new List<PowerEntity>();
        public List<LineEntity> listaVodova = new List<LineEntity>();
        public List<SwitchEntity> listaSviceva = new List<SwitchEntity>();

        public double noviX, noviY;

        public double najmanjaLat = 45.2325;
        public double najvecaLat = 45.277031;
        public double najmanjaLon = 19.793909;
        public double najvecaLon = 19.894459;

        //jedan stepen po 
        public double razlikaMinMaxX, razlikaMinMaxY, pozicijaX, pozicijaZ;
        //preklapanje
        Dictionary<Point, double> kockicePoint = new Dictionary<Point, double>();

        public MainWindow()
        {
            InitializeComponent();

            UcitavanjeMatrice();
            UcitavanjeElemenata();
            JedanStepenU3D();
            // PointiIzVertices(); //Za dodatan zadatak
            FirstISecondEnd();
            CrtajEntitete();
            CrtanjeVodova();
        }
        #region Crtaj entitete
        double visina;
        private void CrtajEntitete()
        {
            foreach (var element in listaElemenataIzXML)
            {
                var kockica = new GeometryModel3D();
                ToLatLon(element.X, element.Y, 34, out noviX, out noviY);
                MestoNaCanvasu(noviX, noviY, out double relativnoX, out double relativnoY);
                double tacnoY = relativnoX * odstojanjeY;
                double tacnoX = relativnoY * odstojanjeX;   //zamenio jer rotira

                visina = kockicePoint[new Point(relativnoX, relativnoY)]++;//povecam vrednost za taj kljuc
                visina = visina * odstojanjeY; //visina na kojoj se nalazi kocka

                //trebace mi za temena
                var pocetakMape = mapa.Positions[0];
                Vector3D pocetakMapeVektor = new Vector3D(pocetakMape.X, pocetakMape.Y, pocetakMape.Z);
                var lokacijaKockice = new Vector3D(tacnoX, visina, -tacnoY);
                var tacnaLokacijaKockice = lokacijaKockice + pocetakMapeVektor; //stavio sam ga u nulu

                MeshGeometry3D mesh = new MeshGeometry3D();
                Point3DCollectionConverter pcc = new Point3DCollectionConverter();
                //velicina kockica                  // 0.15 da se ne seku, a -0,15 da bi islo ka gore posto je Z osa na dole
                var temena = (Point3DCollection)pcc.ConvertFromString("0 0 0  0.15 0 0  0 0.15 0  0.15 0.15 0  0 0 -0.15  0.15 0 -0.15  0 0.15 -0.15  0.15 0.15 -0.15");

                //podesavam polozaj temena
                for (int i = 0; i < temena.Count; i++)
                {
                    temena[i] += tacnaLokacijaKockice;
                }
                #region Nije dobro
                /*
                //polozaj temena                              //prva strana                                          druga                                                          treca
                var points = (Point3DCollection)pcc.ConvertFromString("0"+" 0 "+"0"+" "+odstojanjeX.ToString()+" 0 "+"0"+" "+"0"+" "+visina.ToString()+" "+"0"+" "+
                    //cetvrta                                                                         peta                                                          sesta
                    (odstojanjeX.ToString()+" "+visina.ToString()+" "+"0"+" "+"0"+" 0 "+(-odstojanjeY).ToString()+" "+odstojanjeX.ToString()+" 0 "+(-odstojanjeY).ToString()+" "+
                    //sedma                                                                           osma
                    "0"+" "+visina.ToString()+" "+(-odstojanjeY).ToString()+" "+odstojanjeX.ToString()+" "+visina.ToString()+" "+(-odstojanjeY)).ToString());
                */
                #endregion


                mesh.Positions = temena;
                Int32CollectionConverter i32cc = new Int32CollectionConverter(); //napred,dole, gore,         desno,        levo,         nazad
                var triangles = (Int32Collection)i32cc.ConvertFrom("0 1 2  2 1 3  4 5 0  0 5 1  2 3 6  6 3 7  1 5 3  3 5 7  4 0 6  6 0 2  5 4 7  7 4 6");
                mesh.TriangleIndices = triangles;
                kockica.Geometry = mesh;

                #region Boja kockice spram materijala
                DiffuseMaterial dm = new DiffuseMaterial();

                string iseciVisak = element.ToString();
                iseciVisak = iseciVisak.Substring(29, iseciVisak.Length - 29);
                if (iseciVisak == "SwitchEntity")
                {
                    dm.Brush = Brushes.Blue;
                    if (drugiChecked.IsChecked == true && drugi == "oboj")
                    {
                        foreach (var sw in listaSviceva)
                        {
                            if (sw.Name == element.Name && sw.Status == "Closed")
                            {
                                dm.Brush = Brushes.Red;
                            }
                            else if (sw.Name == element.Name && sw.Status == "Open")
                            {
                                dm.Brush = Brushes.Green;
                            }
                        }
                    }
                }
                else if(iseciVisak == "NodeEntity")
                {
                    dm.Brush = Brushes.Yellow;
                }
                else if (iseciVisak == "SubstationEntity")
                {
                    dm.Brush = Brushes.Magenta;
                }
                kockica.Material = dm;
                #endregion

                kockice.Children.Add(kockica); //dodaje na group iz xaml

                //Za hit testing
                element.modelEntitet = kockica;
            }
        }
        #endregion

        #region Crtanje vodova
        Vector3D tacnaLokacijaVoda2; //za crtanje linije
        public void CrtanjeVodova()
        {
            foreach (LineEntity line in listaVodova)
            {
                GeometryModel3D vod = new GeometryModel3D();

                for (int i = 0; i < line.Vertices.Count-1; i++)
                {
                    MeshGeometry3D linija = new MeshGeometry3D();

                    ToLatLon(line.Vertices[i].X, line.Vertices[i].Y, 34, out noviX, out noviY);
                    MestoNaCanvasu(noviX, noviY, out double relativnoX, out double relativnoY);
                    double tacnoY = relativnoX * odstojanjeY;
                    double tacnoX = relativnoY * odstojanjeX;   //zamenio jer rotira

                    //trebace mi za temena
                    var pocetakMape = mapa.Positions[0];
                    Vector3D pocetakMapeVektor = new Vector3D(pocetakMape.X, pocetakMape.Y, pocetakMape.Z);
                    var lokacijaVoda = new Vector3D(tacnoX + 0.05, 0, -tacnoY);
                    var tacnaLokacijaVoda = lokacijaVoda + pocetakMapeVektor; //stavio sam ga u nulu

                    //pokusavam liniju da iscrtam
                    MeshGeometry3D linija2 = new MeshGeometry3D();
                    if (i != line.Vertices.Count - 1)
                    {
                        //Ucitavanje naredne tacke kako bih spojio jednu liniju voda
                        ToLatLon(line.Vertices[i + 1].X, line.Vertices[i + 1].Y, 34, out noviX, out noviY);
                        MestoNaCanvasu(noviX, noviY, out double relativnoX2, out double relativnoY2);
                        double tacnoY2 = relativnoX2 * odstojanjeY;
                        double tacnoX2 = relativnoY2 * odstojanjeX;   //zamenio jer rotira
                        var lokacijaVoda2 = new Vector3D(tacnoX2 + 0.05, 0, -tacnoY2);
                        tacnaLokacijaVoda2 = lokacijaVoda2 - pocetakMapeVektor; //stavio sam ga u nulu
                    }


                    Point3DCollectionConverter pcc = new Point3DCollectionConverter();
                    //velicina voda                  // 0.15 da se ne seku, a -0,15 da bi islo ka gore posto je Z osa na dole
                    //podesavam polozaj temena
                    var temena = (Point3DCollection)pcc.ConvertFromString("0 0 0  0.05 0 0  0 0.05 0  0.05 0.05 0  0 0 -0.05  0.05 0 -0.05  0 0.05 -0.05  0.05 0.05 -0.05");
                    for (int z = 0; z < temena.Count; z++)
                    {
                        temena[z] += tacnaLokacijaVoda;
                    }
                    linija.Positions = temena;

                  //  /*

                   #region Zakomentarisi za normalnu sliku
                   //pokusavam da nacrtam liniju
                   var temena2 = (Point3DCollection)pcc.ConvertFromString("0 0 0  0.05 0 0  0 0.05 0  0.05 0.05 0  0 0 -0.05  0.05 0 -0.05  0 0.05 -0.05  0.05 0.05 -0.05");
                   if (i != line.Vertices.Count - 1)
                   {
                       for (int k = 0; k < temena.Count; k++)
                       {
                           temena2[k] += tacnaLokacijaVoda2;
                       }
                       linija2.Positions = temena2;
                   }
                   else
                   {
                       linija2.Positions = (Point3DCollection)pcc.ConvertFromString("0 0 0  0.05 0 0  0 0.05 0  0.05 0.05 0  0 0 -0.05  0.05 0 -0.05  0 0.05 -0.05  0.05 0.05 -0.05");
                   }

                   string zaSeckanje1 = linija.Positions.ToString();
                   string zaSeckanje2 = linija2.Positions.ToString();
                   string[] bezZareza1;
                   string[] bezZareza2;
                   string finalnaLinija="";

                   string[] temena1seckanje = zaSeckanje1.Split(' ');
                   string[] temena2seckanje = zaSeckanje2.Split(' ');
                   for (int f = 0; f < temena1seckanje.Length; f++)
                   {
                       bezZareza1 = temena1seckanje[f].Split(',');
                       bezZareza2 = temena2seckanje[f].Split(',');
                       if (f == 0)
                       {
                           finalnaLinija += bezZareza1[0]+","+bezZareza1[1]+","+bezZareza1[2]+" ";
                       }
                       else if (f == 1)
                       {
                           finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + " ";
                       }
                       else if (f == 2)
                       {
                           finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + "  ";
                       }
                       else if (f == 3)
                       {
                           finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + "  ";
                       }
                       else if (f == 4)
                       {
                            if (i != line.Vertices.Count - 1)
                            {
                                finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + " ";
                            }
                            else
                                finalnaLinija += bezZareza2[0] + "," + bezZareza2[1] + "," + bezZareza2[2] + "  ";
                       }
                       else if (f == 5)
                       {
                            if (i != line.Vertices.Count - 1)
                            {
                                finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + " ";
                            }
                            else
                                finalnaLinija += bezZareza2[0] + "," + bezZareza2[1] + "," + bezZareza2[2] + "  ";
                       }
                       else if (f == 6)
                       {
                            if (i != line.Vertices.Count - 1)
                            {
                                finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2] + " ";
                            }
                            else
                                finalnaLinija += bezZareza2[0] + "," + bezZareza2[1] + "," + bezZareza2[2] + "  ";
                       }
                       else if (f == 7)
                       {
                            if (i != line.Vertices.Count - 1)
                            {
                                finalnaLinija += bezZareza1[0] + "," + bezZareza1[1] + "," + bezZareza1[2];
                            }
                            else
                                finalnaLinija += bezZareza2[0] + "," + bezZareza2[1] + "," + bezZareza2[2];
                       }
                   }

                   linija.Positions = (Point3DCollection)pcc.ConvertFromString(finalnaLinija);
                                        
                    #endregion

                    // */

                    Int32CollectionConverter i32cc = new Int32CollectionConverter(); //napred,dole, gore,         desno,        levo,         nazad
                    var triangles = (Int32Collection)i32cc.ConvertFrom("0 1 2  2 1 3  4 5 0  0 5 1  2 3 6  6 3 7  1 5 3  3 5 7  4 0 6  6 0 2  5 4 7  7 4 6");
                    linija.TriangleIndices = triangles;
                    vod.Geometry = linija;


                    #region Boja voda
                    //boja voda po materijalu
                    DiffuseMaterial materijalVoda = new DiffuseMaterial();
                    if (otpornostVodova == "nemoj")
                    {
                        if (line.ConductorMaterial == "Steel")
                        {
                            materijalVoda.Brush = Brushes.Gray;
                        }
                        else if (line.ConductorMaterial == "Acsr")
                        {
                            materijalVoda.Brush = Brushes.LightSkyBlue;
                        }
                        else if (line.ConductorMaterial == "Copper")
                        {
                            materijalVoda.Brush = Brushes.Brown;
                        }
                    }
                    else if (otpornostVodova == "oboj")
                    {
                        if (line.R < 1)
                        {
                            materijalVoda.Brush = Brushes.Red;
                        }
                        else if (line.R >= 1 && line.R <= 2)
                        {
                            materijalVoda.Brush = Brushes.Orange;
                        }
                        else if (line.R > 2)
                        {
                            materijalVoda.Brush = Brushes.Yellow;
                        }
                    }
                    #endregion
                    vod.Material = materijalVoda;
                    vod.Geometry = linija;

                    //Za hit testing
                    line.modelVod = vod;
                    vodovi.Children.Add(vod);
                }
            }
        }
        #endregion

        #region Pozicioniranje i skaliranje entiteta
        private void MestoNaCanvasu(double noviX, double noviY, out double relativnoX, out double relativnoY)
        {
            double odstojanjeLat = (najvecaLat - najmanjaLat) / 199;
            double odstojanjeLon = (najvecaLon - najmanjaLon) / 300;

            //tacke na osi - trenutna pozicija/razmak od jedne do druge kocke
            //namestam da krecu od 0 zato oduzimanje
            relativnoX = Math.Round((noviX - najmanjaLat) / odstojanjeLat);
            //relativnoX = Math.Round((noviX - najmanjaLat) / odstojanjeLat); //jednu kockicu ubacujem na mesto po Lat
            //relativnoY = Math.Round((noviY - najmanjaLon) / odstojanjeLon);
            relativnoY = Math.Round((noviY - najmanjaLon) / odstojanjeLon);
        }

        public double odstojanjeX;
        public double odstojanjeY;
        int x,z,Xmin, Xmax, Zmin, Zmax = 0;
        private void JedanStepenU3D()
        {
            string pozicija = mapa.Positions.ToString();
            string[] temena = pozicija.Split(' ');
            string[] bezZareza;

            int i = 0;
            foreach(string teme in temena)
            {
                bezZareza = teme.Split(',');
                if (i == 0)
                {
                    Xmin += Int32.Parse(bezZareza[0]);

                    Zmin += Int32.Parse(bezZareza[2]);

                    Xmax += Int32.Parse(bezZareza[0]);

                    Zmax += Int32.Parse(bezZareza[2]);
                }
                else if (i == 1)
                {
                    x = Int32.Parse(bezZareza[0]);

                    z = Int32.Parse(bezZareza[2]);

                    if (x > Xmax)
                    {
                        Xmax = x;
                    }
                    if (x < Xmin)
                    {
                        Xmin = x;
                    }
                    if (z > Zmax)
                    {
                        Zmax = z;
                    }
                    if (z < Zmin)
                    {
                        Zmin = z;
                    }
                }
                else if (i == 2)
                {
                    x = Int32.Parse(bezZareza[0]);
                    z = Int32.Parse(bezZareza[2]);

                    if (x > Xmax)
                    {
                        Xmax = x;
                    }
                    if (x < Xmin)
                    {
                        Xmin = x;
                    }
                    if (z > Zmax)
                    {
                        Zmax = z;
                    }
                    if (z < Zmin)
                    {
                        Zmin = z;
                    }
                }
                else if (i == 3)
                {
                    x = Int32.Parse(bezZareza[0]);
                    z = Int32.Parse(bezZareza[2]);

                    if (x > Xmax)
                    {
                        Xmax = x;
                    }
                    if (x < Xmin)
                    {
                        Xmin = x;
                    }
                    if (z > Zmax)
                    {
                        Zmax = z;
                    }
                    if (z < Zmin)
                    {
                        Zmin = z;
                    }
                }
                i++;
            }

            //po kordinatama slike ovo su mi duzina i sirina
            razlikaMinMaxX = Xmax - Xmin;
            razlikaMinMaxY = Zmax - Zmin;

            //razvlacim sliku po dimenzijama matrice
            odstojanjeX = razlikaMinMaxX / 300; //celu mapu delim na delove
            odstojanjeY = razlikaMinMaxY / 200; //to je razmak izmedju elemenata
        }
        #endregion

        #region Dodatni
        public int brojac;
        public List<PowerEntity> listaObrisanih1 = new List<PowerEntity>();
        public List<PowerEntity> listaNeobrisanih1 = new List<PowerEntity>();
        private void Dodatni1_Click(object sender, RoutedEventArgs e)
        {
            if (prviDodatni.IsChecked == true)
            {
                foreach (var item in listaElemenataIzXML)
                {
                    brojac = 0;
                    foreach (var konekcija in listaKonekcija)
                    {
                        if (item.Id == konekcija)
                        {
                            brojac++;
                        }
                        if (brojac >= 3) break;
                    }
                    if (brojac >= 3)
                    {
                        listaNeobrisanih1.Add(item);
                        continue;
                    }
                    listaObrisanih1.Add(item);
                }
                //ako ne stavim ToList() on ce brisati sa istog mesta posto je pokazivac. Ovako odradi new list, i kopirao je samo vrednost
                listaElemenataIzXML = listaNeobrisanih1.ToList(); 

                kockice.Children.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }
                CrtajEntitete();
            }
            else
            {
                foreach (var item in listaObrisanih1)
                {
                    listaElemenataIzXML.Add(item);
                }
                listaObrisanih1.Clear();
                listaNeobrisanih1.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }

                CrtajEntitete();
            }
        }

        public int brojac2;
        public List<PowerEntity> listaObrisanih2pe = new List<PowerEntity>();
        public List<PowerEntity> listaNeobrisanih2 = new List<PowerEntity>();
        private void Dodatni2_Click(object sender, RoutedEventArgs e)
        {
            if (drugiDodatni.IsChecked == true)
            {
                foreach (var item in listaElemenataIzXML)
                {
                    brojac2 = 0;
                    foreach (var konekcija in listaKonekcija)
                    {
                        if (item.Id == konekcija)
                        {
                            brojac2++;
                        }
                        if (brojac2 > 5) break;
                    }
                    if (brojac2 < 3 || brojac2 > 5)
                    {
                        listaNeobrisanih2.Add(item);
                        continue;
                    }
                    //ako bas ta kockica ima 3-5 konekcija
                    listaObrisanih2pe.Add(item);
                }
                //ako ne stavim ToList() on ce brisati sa istog mesta posto je pokazivac. Ovako odradi new list, i kopirao je samo vrednost

                listaElemenataIzXML = listaNeobrisanih2.ToList();

                kockice.Children.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }
                CrtajEntitete();

            }
            else
            {
                //vrati obrisane elemente
                foreach (var item in listaObrisanih2pe)
                {
                    listaElemenataIzXML.Add(item);
                }
                listaObrisanih2pe.Clear();
                listaNeobrisanih2.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }

                CrtajEntitete();
            }
        }

        public int brojac3;
        public List<PowerEntity> listaObrisanih3pe = new List<PowerEntity>();
        public List<PowerEntity> listaNeobrisanih3 = new List<PowerEntity>();
        private void Dodatni3_Click(object sender, RoutedEventArgs e)
        {
            if (treciDodatni.IsChecked == true)
            {
                foreach (var item in listaElemenataIzXML)
                {
                    brojac3 = 0;
                    foreach (var konekcija in listaKonekcija)
                    {
                        if (item.Id == konekcija)
                        {
                            brojac3++;
                        }
                        if (brojac3 > 5) break;
                    }
                    if (brojac3 <= 5)
                    {
                        listaNeobrisanih3.Add(item);
                        continue;
                    }
                    listaObrisanih3pe.Add(item);
                }
                //ako ne stavim ToList() on ce brisati sa istog mesta posto je pokazivac. Ovako odradi new list, i kopirao je samo vrednost

                listaElemenataIzXML = listaNeobrisanih3.ToList();

                kockice.Children.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }
                CrtajEntitete();
            }
            else
            {
                //vrati obrisane elemente
                foreach (var item in listaObrisanih3pe)
                {
                    listaElemenataIzXML.Add(item);
                }
                listaObrisanih3pe.Clear();
                listaNeobrisanih3.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }

                CrtajEntitete();
            }
        }
        #endregion

        #region Korisnicki interfejs
        public string drugi;
        public List<LineEntity> vodoviZaBrisanje = new List<LineEntity>();
        public List<LineEntity> listaNeobrisanihVodova = new List<LineEntity>();
        public List<PowerEntity> listaEntitetaZaBrisanje = new List<PowerEntity>();
        public List<PowerEntity> listaNeobrisanihEntiteta = new List<PowerEntity>();
        private void Interfejs1_Click(object sender, RoutedEventArgs e)
        {
            if (prviChecked.IsChecked == true)
            {
                //sakriva vodove koji izlaze iz "open"
                foreach (var vod in listaVodova)
                {
                    bool zaBrisanjeVod = false;
                    foreach (var sw in listaSviceva)
                    {
                        if (sw.Status == "Open" && sw.Id == vod.FirstEnd)
                        {
                            vodoviZaBrisanje.Add(vod);
                            zaBrisanjeVod = true;
                            break;
                        }
                    }
                    if (zaBrisanjeVod == true) continue;
                    listaNeobrisanihVodova.Add(vod);
                }

                // i entitete koji su ***ZA TAJ*** vod SecondEnd
                foreach (var entitet in listaElemenataIzXML)
                {
                    bool zaBrisanjeEntitet = false;
                    foreach (var vod in vodoviZaBrisanje) // "ZA TAJ" tj. za obrisane
                    {
                        if (vod.SecondEnd == entitet.Id)
                        {
                            listaEntitetaZaBrisanje.Add(entitet);
                            zaBrisanjeEntitet = true;
                            break;
                        }
                    }
                    if (zaBrisanjeEntitet == true) continue;
                    listaNeobrisanihEntiteta.Add(entitet);
                }

                listaElemenataIzXML = listaNeobrisanihEntiteta.ToList();
                listaVodova = listaNeobrisanihVodova.ToList();

                vodovi.Children.Clear();
                kockice.Children.Clear();

                CrtanjeVodova();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }
                CrtajEntitete();
            }
            else
            {
                foreach (var vod in vodoviZaBrisanje)
                {
                    listaVodova.Add(vod);
                }
                vodoviZaBrisanje.Clear();
                listaNeobrisanihVodova.Clear();

                foreach (var entitet in listaEntitetaZaBrisanje)
                {
                    listaElemenataIzXML.Add(entitet);
                }
                listaEntitetaZaBrisanje.Clear();
                listaNeobrisanihEntiteta.Clear();

                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }
                CrtanjeVodova();
                CrtajEntitete();
            }
        }

        private void Interfejs2_Click(object sender, RoutedEventArgs e)
        {
            if (drugiChecked.IsChecked == true)
            {
                //sakriva vodove koji izlaze iz "open" i entitere koji su za taj vod SecondEnd
                kockice.Children.Clear();
                drugi = "oboj";
                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }

                CrtajEntitete();
            }
            else
            {
                kockice.Children.Clear();
                drugi = "nemoj";
                //da ne menja visinu
                for (int i = 0; i <= 200; i++)
                {
                    for (int j = 0; j <= 300; j++)
                    {
                        kockicePoint[new Point(i, j)] = 0;
                    }
                }

                CrtajEntitete();
            }
        }

        public string otpornostVodova="nemoj";
        private void Interfejs3_Click(object sender, RoutedEventArgs e)
        {
            if (treciChecked.IsChecked)
            {
                vodovi.Children.Clear();

                otpornostVodova="oboj";

                CrtanjeVodova();
            }
            else
            {
                //vraca sve kako je bilo
                vodovi.Children.Clear();

                otpornostVodova = "nemoj";

                CrtanjeVodova();
            }
        }
        #endregion

        #region UcitavanjeElemenata
        private void UcitavanjeElemenata()
        {
            // ---------Ucitavam elemente iz xml-a
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml"); // bin/debug
            XmlNodeList nodeList;
            
            //substations - trafostanice
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {
                SubstationEntity subEn = new SubstationEntity();
                subEn.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                subEn.Name = node.SelectSingleNode("Name").InnerText;
                subEn.X = double.Parse(node.SelectSingleNode("X").InnerText);
                subEn.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                subEn.ToolTip = "Substation\nID: " + subEn.Id + "  Name: " + subEn.Name;

                //Provera mi ne treba jer ne skaliram po elementima nego vec
                //imam zadate koordinate ivica
                //Kada ih crtam pozivam LatLon i MestoNaCanvasu
                ToLatLon(subEn.X, subEn.Y, 34, out noviX, out noviY);
                if(noviX >= najmanjaLat && noviX <= najvecaLat && noviY>=najmanjaLon && noviY <= najvecaLon)
                {
                    listaElemenataIzXML.Add(subEn);
                }
            }

            //svicevi
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {
                SwitchEntity sw = new SwitchEntity();
                sw.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sw.Name = node.SelectSingleNode("Name").InnerText;
                sw.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sw.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                sw.Status = node.SelectSingleNode("Status").InnerText;
                sw.ToolTip = "Switch\nID: " + sw.Id + "  Name: " + sw.Name + " Status: " + sw.Status;

                ToLatLon(sw.X, sw.Y, 34, out noviX, out noviY);
                if (noviX >= najmanjaLat && noviX <= najvecaLat && noviY >= najmanjaLon && noviY <= najvecaLon)
                {
                    listaSviceva.Add(sw);
                    listaElemenataIzXML.Add(sw);
                }
            }

            //nodovi
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {
                NodeEntity nod = new NodeEntity();
                nod.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nod.Name = node.SelectSingleNode("Name").InnerText;
                nod.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nod.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                nod.ToolTip = "Node\nID: " + nod.Id + "  Name: " + nod.Name;

                ToLatLon(nod.X, nod.Y, 34, out noviX, out noviY);
                if (noviX >= najmanjaLat && noviX <= najvecaLat && noviY >= najmanjaLon && noviY <= najvecaLon)
                {
                    listaElemenataIzXML.Add(nod);
                }
            }

            //ucitavanje vodova ->  u xml first end i second end
            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in nodeList)
            {
                LineEntity l = new LineEntity();
                l.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                l.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                {
                    l.IsUnderground = true;
                }
                else
                {
                    l.IsUnderground = false;
                }
                l.R = float.Parse(node.SelectSingleNode("R").InnerText);
                l.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                l.LineType = node.SelectSingleNode("LineType").InnerText;
                l.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                l.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                l.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);

                foreach (XmlNode point in node.ChildNodes[9].ChildNodes) //10. mesto u xml-u za LineEntity
                {
                    double X = double.Parse(point.SelectSingleNode("X").InnerText);
                    double Y = double.Parse(point.SelectSingleNode("Y").InnerText);
                    Model.Point p = new Model.Point(X, Y);
                    l.Vertices.Add(p);

                } 

                // da li postoje firstEnd i secondEnd medju entitetima
                // ako ne ignorisi vod
                // gleda vodove medju vec postojecim entitetima
                if (listaElemenataIzXML.Any(x => x.Id == l.FirstEnd))
                {
                    if (listaElemenataIzXML.Any(x => x.Id == l.SecondEnd))
                    {
                        listaVodova.Add(l);
                    }
                }
            }
        }
        #endregion

        #region ZoomPan, Rotacija, Hit testing
        private GeometryModel3D hitgeo;
        private void viewport1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport1.CaptureMouse();
            start = e.GetPosition(this);
            diffOffset.X = translacija.OffsetX;
            diffOffset.Y = translacija.OffsetZ; //menjam

            #region Hit testing
            Point mouseposition = e.GetPosition(viewport1);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);

            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            //test for a result in the Viewport3D     
            hitgeo = null;
            VisualTreeHelper.HitTest(viewport1, null, HTResult, pointparams);
        }

        public ToolTip toolTip = new ToolTip();
        private HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            RayHitTestResult rayResult = rawresult as RayHitTestResult;

            if (rayResult != null)
            {
                DiffuseMaterial darkSide = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Red));
                bool gasit = false;
                #region Kada se klikne entitet - ToolTip
                for (int j = 0; j < listaElemenataIzXML.Count; j++)
                {
                    if (listaElemenataIzXML[j].modelEntitet == rayResult.ModelHit)
                    {
                                                //onaj kog sam pogodio
                        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                        gasit = true;

                        if (listaElemenataIzXML[j] is NodeEntity)
                        {
                            toolTip.Content = "\tNode entity:\n\nID: " + listaElemenataIzXML[j].Id.ToString() + "\nName: " + listaElemenataIzXML[j].Name + "\nType: " + listaElemenataIzXML[j].GetType().Name;

                        }
                        else if(listaElemenataIzXML[j] is SubstationEntity)
                        {
                            toolTip.Content = "\tSubstation entity:\n\nID: " + listaElemenataIzXML[j].Id.ToString() + "\nName: " + listaElemenataIzXML[j].Name + "\nType: " + listaElemenataIzXML[j].GetType().Name;

                        }
                        else if(listaElemenataIzXML[j] is SwitchEntity)
                        {
                            toolTip.Content = "\tSwitch entity:\n\nID: " + listaElemenataIzXML[j].Id.ToString() + "\nName: " + listaElemenataIzXML[j].Name + "\nType: " + listaElemenataIzXML[j].GetType().Name;

                        }
                        toolTip.Height = 100;
                        toolTip.IsOpen = true;
                        
                        ToolTipService.SetPlacement(viewport1, PlacementMode.Mouse);
                    }
                }
                #endregion

                #region Kada se klikne na vod
                //firstEnd i SecondEnd su ID-jevi entiteta
                for (int i = 0; i < listaVodova.Count; i++)
                {
                    if ((GeometryModel3D)listaVodova[i].modelVod == rayResult.ModelHit)
                    {
                        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                        gasit = true;

                        var prviEntitet = listaVodova[i].FirstEnd;
                        var drugiEntitet = listaVodova[i].SecondEnd;

                        for (int g = 0; g < listaElemenataIzXML.Count; g++)
                        {
                            if(listaElemenataIzXML[g].Id == prviEntitet || listaElemenataIzXML[g].Id == drugiEntitet)
                            {
                                ((DiffuseMaterial)((GeometryModel3D)listaElemenataIzXML[g].modelEntitet).Material).Brush = Brushes.Red;
                               // hitgeo.Material = darkSide;// boji vod koji je kliknut
                            }
                        }
                    }
                }
                #endregion

                if (!gasit)
                {
                    hitgeo = null;
                }
            }
            return HitTestResultBehavior.Stop;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            otvorenTooltip();
            viewport1_MouseLeftButtonDown(viewport1, e);
        }

        private void otvorenTooltip()
        {
            if (toolTip.IsOpen == true)
            {
                System.Threading.Thread.Sleep(100);
                toolTip.IsOpen = false;
            }
        }

        #endregion

        private void viewport1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewport1.ReleaseMouseCapture();
        }

        Point startPosition = new Point(); //tamo gde kliknes
        private void viewport1_MouseMove(object sender, MouseEventArgs e)
        {
            if (viewport1.IsMouseCaptured)
            {
                Point end = e.GetPosition(this);
                double offsetX = end.X - start.X;
                double offsetY = end.Y - start.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = (offsetX * 100) / w;
                double translateY = -(offsetY * 100) / h;
                translacija.OffsetX = diffOffset.X + (translateX / (100 * skaliranje.ScaleX)) *8; //*8 da brze pomera
                translacija.OffsetZ = diffOffset.Y + (translateY / (100 * skaliranje.ScaleZ)) *8; //menjam na Z
                //camera.Position=System.Windows.Media.Media3D.ProjectionCamera;
            }
            #region Rotacija
            Point trenutnaPozicija = e.GetPosition(this);

            if (e.MiddleButton == MouseButtonState.Pressed)
            {

                double pomerajX = trenutnaPozicija.X - startPosition.X;
                double pomerajY = trenutnaPozicija.Y - startPosition.Y;
                double brzina = 0.5;
                //gore - dole
                // if ((rotateX.Angle + brzina * pomerajY) < 360 && (rotateX.Angle + brzina * pomerajY) > -360)
                rotateX.Angle += brzina * pomerajY;
                //levo - desno
                rotateY.Angle += brzina * pomerajX;
            }

            startPosition = trenutnaPozicija;
            #endregion
        }
        #region Zoom
        private void viewport1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.MouseDevice.GetPosition(this);
            //double scaleX = 1;
            //double scaleY = 1;
            int zoomMaxNapred = 13; //povecao
            if (e.Delta > 0 && zoomCurent < zoomMaxNapred)
            {
                /*
                scaleX = skaliranje.ScaleX + 0.1;
                scaleY = skaliranje.ScaleY + 0.1;
                zoomCurent++;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                */
                zoomCurent++;
                skaliranje.ScaleX *= 1.3; // + nije ravnomeran zoom
                skaliranje.ScaleY *= 1.3; //ovo ravnomerno poveca, finije su promene
                skaliranje.ScaleZ *= 1.3;
            }
            else if (e.Delta <= 0 && zoomCurent > -zoomMax)
            {
                /*
                scaleX = skaliranje.ScaleX - 0.1;
                scaleY = skaliranje.ScaleY - 0.1;
                zoomCurent--;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                */
                zoomCurent--;
                skaliranje.ScaleX /= 1.3;
                skaliranje.ScaleY /= 1.3;
                skaliranje.ScaleZ /= 1.3;
            }
        }
        #endregion

        #endregion

        #region Za dodatan - Pointi iz vertices za dodatan(ne treba vertices nego lista endova)
        public List<Model.Point> listaPointaVertices = new List<Model.Point>();
        private void PointiIzVertices()
        {
            foreach (var item in listaVodova)
            {
                foreach (var point in item.Vertices)
                {
                    listaPointaVertices.Add(point);
                }
            }
        }

        public List<long> listaKonekcija = new List<long>();
        private void FirstISecondEnd()
        {
            foreach (var item in listaVodova)
            {
                listaKonekcija.Add(item.FirstEnd);
                listaKonekcija.Add(item.SecondEnd);
            }
        }
        #endregion

        #region Ucitavanje matrice
        private void UcitavanjeMatrice()
        {
            //Pravim matricu
            Point rt;

            for (int i = 0; i <= 200; i++) // za vece ucitava dugo
            {
                for (int j = 0; j <= 300; j++)
                {
                    rt = new Point(i, j);
                    kockicePoint.Add(rt, 0);
                }
            }
        }
        #endregion

        #region ToLatLon
        //From UTM to Latitude and longitude in decimal
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }
        #endregion
    }
}
