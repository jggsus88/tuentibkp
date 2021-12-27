using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tuentibkp
{
    class Program
    {

        private static TuentiAPI tntAPI;
        
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("Bienvenido a 20Bkp");
            Console.Write("Usuario: ");
            string user = Console.ReadLine();
            
            Console.Write("Contraseña: ");
            //string pass = Console.ReadLine();
            string pass = SecurePassWrite.GetConsolePassword();
            
            Console.WriteLine("Intentanto login");
            tntAPI = new TuentiAPI();
            string nombre = tntAPI.Login(user, pass);

            if (nombre != null)
            {
                Console.WriteLine("Login correcto");
                Console.WriteLine("Logueado con usuario " + nombre);                
                SeleccionaOpcion();
            }
            else
            {
                Console.WriteLine("Login incorrecto");
            }
        }

        private static void SeleccionaOpcion()
        {
            while (true)
            {
                Console.Clear();
                PintarMenu();
                Console.WriteLine("Selecciona una de las opciones del menu");
                string opt = Console.ReadLine();

                switch (opt?.Trim())
                {
                    case "1":
                        ListarAlbumes(tntAPI.GetAlbums());
                        Console.WriteLine("Pulse una tecla para volver al menú");
                        Console.ReadKey();
                        break;
                    case "2":
                        DescargarAlbumes(tntAPI.GetAlbums());
                        Console.WriteLine("Descarga completa");
                        Console.WriteLine("Pulse una tecla para volver al menú");
                        Console.ReadKey();
                        break;
                    case "3":
                        DescargarTodos();
                        Console.WriteLine("Descarga completa");
                        Console.WriteLine("Pulse una tecla para volver al menú");
                        Console.ReadKey();
                        break;
                    default:
                        Console.WriteLine("Debe seleccionar una opcion valida");
                        Console.WriteLine("Pulse una tecla para volver al menú");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void DescargarTodos()
        {
            var albumes = tntAPI.GetAlbums();
            Console.WriteLine("¿Desde qué año desea descargar las fotos?");
            string anioStr = Console.ReadLine();
            int anio = 0;
            if (Int32.TryParse(anioStr, out anio)) {
                foreach (var album in albumes)
                {
                    DescargarAlbum(album, anio);
                }
            }
            else
            {
                Console.WriteLine("Debe seleccionar una fecha valida");
                Console.WriteLine("Pulse una tecla para volver al menú");
                Console.ReadKey();
            }
        }

        private static void DescargarAlbum(KeyValuePair<string, string> album, int anio)
        {
            Console.Clear();
            Console.WriteLine("Descargando album " + album.Value);

            string nombreYnum = album.Value;
            int indexNum = nombreYnum.LastIndexOf('(');
            string nombreAlbum = nombreYnum.Substring(0, indexNum);
            string numFotos = nombreYnum.Substring(indexNum + 1);
            numFotos = numFotos.Substring(0, numFotos.Length - 1);
            numFotos = string.Join("", numFotos.Split('.'));

            tntAPI.DownloadAlbum(album.Key, nombreAlbum, anio, Convert.ToInt32(numFotos));
        }

        private static void DescargarAlbumes(Dictionary<string, string> dictionary)
        {
            ListarAlbumes(dictionary);
            Console.WriteLine("Seleccione el album que desea descargar");
            string numAlbum = Console.ReadLine();
            int num = Convert.ToInt32(numAlbum);
            var album = dictionary.ElementAt(num - 1);
            Console.WriteLine("¿Desde qué año desea descargar las fotos?");
            string anioStr = Console.ReadLine();
            int anio = 0;
            if (Int32.TryParse(anioStr, out anio))
            {
                DescargarAlbum(album, anio);
            }
            else
            {
                Console.WriteLine("Debe seleccionar una fecha valida");
                Console.WriteLine("Pulse una tecla para volver al menú");
                Console.ReadKey();
            }
        }

        private static void ListarAlbumes(Dictionary<string, string> dictionary)
        {
            int i = 1;
            foreach (string nombre in dictionary.Values)
            {
                Console.WriteLine(i + ". " + nombre);
                i++;
            }
        }

        private static void PintarMenu()
        {
            Console.WriteLine("---------------------");
            Console.WriteLine("Menu");
            Console.WriteLine("1. Listar albumes");
            Console.WriteLine("2. Descargar album");
            Console.WriteLine("3. Descargar total");
            Console.WriteLine("---------------------");
        }
    }
}
