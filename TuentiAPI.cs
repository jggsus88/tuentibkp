using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;

namespace tuentibkp
{

    public enum Rutas
    {
        PROFILE = 0,
        ALBUM = 1,
        PHOTO = 2
    }
    
    public class TuentiAPI
    {
        private string UrlBase = "www.tuenti.com";
        private string[] Urls = { "/#m=Profile&func=index", "/#m=Albums&func=index&collection_key=", "/#m=Photo&func=view_photo&collection_key="};


        public System.Windows.Forms.WebBrowser Browser;
        public Browser FormBrowser = new Browser();
        WebClient wc = new WebClient();

        private string user;
        private string pass;
        
        public string Login(string User, string Password)
        {
            this.user = User;
            this.pass = Password;

            UpdateBrowser();
            //Browser = new WebBrowser();
            //FormBrowser.Show();
            Browser = FormBrowser.GetBrowser();
            Browser.ScriptErrorsSuppressed = true;
            Browser.DocumentCompleted += login_DocumentCompleted;
            Browser.Navigate(this.UrlBase);


            while (Browser.ReadyState != WebBrowserReadyState.Complete)
            {
                System.Windows.Forms.Application.DoEvents();
            }

            if (Browser.Document.GetElementById("shareboxCanvas") != null)
            {
                return Browser.Document.GetElementById("settingsDpdTrigger").FirstChild.InnerText;
            }
            else
            {
                return null;
            }
            
        }

        public Dictionary<string, string> GetAlbums()
        {
            Browser.Navigate(this.UrlBase + this.Urls[(int)Rutas.PROFILE]);

            while (Browser.ReadyState != WebBrowserReadyState.Complete)
            {
                System.Windows.Forms.Application.DoEvents();
            }

            Dictionary<string, string> Albums = new Dictionary<string, string>();
            HtmlElementCollection links = Browser.Document.GetElementsByTagName("a");
            foreach (HtmlElement link in links)
            {
                var href = link.GetAttribute("href");
                if (href != null && href.IndexOf("collection_key") > 0)
                {
                    var iniColKey = href.IndexOf("collection_key=") + "collection_key=".Length;
                    var finColKey = href.IndexOf("&", iniColKey);
                    var key = href.Substring(iniColKey, finColKey - iniColKey);
                    if (!Albums.Keys.Contains(key) && link.InnerText != null)
                    {
                        Albums.Add(key, link.InnerText);
                    }
                }
            }

            return Albums;
        }

        public void DownloadAlbum(string key, string name, int anio, double numFotos)
        {
            int anioActual = DateTime.Now.Year;
            // Recorro todos los años hasta el año actual
            string parentDir = CleanInput(name);
            if (Directory.Exists(parentDir))
            {
                parentDir = GetNextDirName(parentDir);
            } else
            {
                Directory.CreateDirectory(parentDir);
            }
            string directory = "";
            double contFotos = 0;
            int porcentaje = 0;
            for (int a = anio; a <= anioActual; a++)
            {
                for (int m = 1; m <= 12; m++)
                {
                    directory = parentDir + "/" + a + "_" + m;
                    Dictionary<string, string> photos = GetPhotos(key, a, m);

                    foreach (var photo in photos)
                    {
                        this.Browser.Navigate(this.UrlBase + Urls[(int)Rutas.PHOTO] + photo.Key);
                        while (Browser.ReadyState != WebBrowserReadyState.Complete || Browser.DocumentTitle == "")
                        {
                            System.Windows.Forms.Application.DoEvents();
                        }
                        var img = this.Browser.Document.GetElementById("photo_image");
                        if (img != null && img.GetAttribute("src") != null)
                        {
                            try
                            {
                                byte[] bytes = this.wc.DownloadData(img.GetAttribute("src"));
                                Bitmap b = new Bitmap(new MemoryStream(bytes));
                                if (!Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }
                                var nombrearchivo = CleanInput(photo.Value);
                                if (File.Exists(directory + "/" + nombrearchivo + ".jpg"))
                                {
                                    nombrearchivo = GetNextFileName(directory, nombrearchivo);
                                }
                                b.Save(directory + "/" + nombrearchivo + ".jpg");
                            }
                            catch (Exception e)
                            {
                                // Error loading image
                            }
                            contFotos++;

                            porcentaje = ConsultarAvance(contFotos, numFotos, porcentaje);

                            if (contFotos == numFotos) return;

                        }
                    }
                    
                }
            }
        }

        private int ConsultarAvance(double contFotos, double numFotos, int porcentajeActual)
        {
            int porcentaje = Convert.ToInt32((contFotos / numFotos) * 100);
            if (porcentaje != porcentajeActual)
            {
                Console.WriteLine(porcentaje + "% Completado");
            }
            return porcentaje;
        }

        private string GetNextFileName(string directory, string archivo)
        {
            int cont = 1;
            string nuevonombre = archivo + "(" + cont + ")";
            while (File.Exists(directory + "/" + nuevonombre + ".jpg"))
            {
                cont++;
                nuevonombre = archivo + "(" + cont + ")";
            }
            return nuevonombre;
        }

        private string GetNextDirName(string directory)
        {
            int cont = 1;
            string nuevonombre = directory + "(" + cont + ")";
            while (Directory.Exists(nuevonombre))
            {
                cont++;
                nuevonombre = directory + "(" + cont + ")";
            }
            return nuevonombre;
        }


        private string CleanInput(string strIn)
        {
            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(strIn, @"[^\w\.-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        private Dictionary<string, string> GetPhotos(string albumkey, int a, int m)
        {

            // Voy recorriendo paginas hasta no encontrar resultados
            Dictionary<string, string> photos = new Dictionary<string, string>();
            bool hayNuevas = false;
            int page = 0;
            do {
                hayNuevas = false;
                this.Browser.Navigate(this.UrlBase + Urls[(int)Rutas.ALBUM] + albumkey + "&year=" + a + "&month=" + m + "&photos_page=" + page);

                while (Browser.ReadyState != WebBrowserReadyState.Complete)
                {
                    System.Windows.Forms.Application.DoEvents();
                }

                HtmlElementCollection links = Browser.Document.GetElementById("albumPhotosContainer").GetElementsByTagName("a");
                foreach (HtmlElement link in links)
                {
                    var href = link.GetAttribute("href");
                    if (href != null && href.IndexOf("collection_key") > 0)
                    {
                        var iniColKey = href.IndexOf("collection_key=") + "collection_key=".Length;
                        var finColKey = href.IndexOf("&", iniColKey);
                        if (finColKey == -1)
                        {
                            finColKey = href.Length;
                        }
                        var key = href.Substring(iniColKey, finColKey - iniColKey);
                        var className = link.GetAttribute("className");
                        if (!photos.Keys.Contains(key) && className != null && className.IndexOf("thumb") != -1)
                        {
                            var title = link.GetAttribute("title");
                            if (title == "Ver foto")
                            {
                                title = "Sin titulo";
                            }
                            photos.Add(key, title);
                            hayNuevas = true;
                        }
                    }
                }
                page++;
            } while (hayNuevas);

            return photos;
        }
        

        private void login_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Browser.DocumentCompleted -= login_DocumentCompleted;
            if (Browser.Document.GetElementById("shareboxCanvas") == null)
            {
                Browser.Document.GetElementById("email").InnerText = this.user;
                Browser.Document.GetElementById("input_password").InnerText = this.pass;
                string idForm = "login_form";
                var form = Browser.Document.GetElementById(idForm);
                form.InvokeMember("submit");
            }
            //Browser.Document.InvokeScript(string.Format("document.getElementById('{0}').submit()", idForm));
        }
        
        private void UpdateBrowser()
        {
            const string BROWSER_EMULATION_KEY =
            @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            //
            // app.exe and app.vshost.exe
            String appname = Process.GetCurrentProcess().ProcessName + ".exe";
            //
            // Webpages are displayed in IE9 Standards mode, regardless of the !DOCTYPE directive.
            const int browserEmulationMode = 9999;
            //const int browserEmulationMode = 11000;
            //const int browserEmulationMode = 8000;

            RegistryKey browserEmulationKey =
                Registry.CurrentUser.OpenSubKey(BROWSER_EMULATION_KEY, RegistryKeyPermissionCheck.ReadWriteSubTree) ??
                Registry.CurrentUser.CreateSubKey(BROWSER_EMULATION_KEY);

            if (browserEmulationKey != null)
            {
                browserEmulationKey.SetValue(appname, browserEmulationMode, RegistryValueKind.DWord);
                browserEmulationKey.Close();
            }
        }
    }
}
