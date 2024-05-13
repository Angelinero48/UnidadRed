using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using JsonSerializer = System.Text.Json.JsonSerializer;

class Program
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();


    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;


    [DllImport("mpr.dll")]
    public static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

    [DllImport("mpr.dll")]
    public static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential)]
    public class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplaytype DisplayType;
        public int Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }

    public enum ResourceScope : int
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    };

    public enum ResourceType : int
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8,
    }

    public enum ResourceDisplaytype : int
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        Shareadmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        Ndscontainer = 0x0b
    }
    public class Config
    {
        public string letter { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string remotePath { get; set; }
    }
    static void Main(string[] args)
    {

        IntPtr handle = GetConsoleWindow();

        // Si se ha encontrado la ventana de la consola, la oculta
        if (handle != IntPtr.Zero)
        {
            ShowWindow(handle, SW_HIDE);
        }
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string configFilePath = Path.Combine(baseDir, "config.json");

        Console.WriteLine(baseDir);

        if (args.Length > 0 && args[0] == "delete")
        {
            
                File.Delete(configFilePath);

                RegistryKey keyDel = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                // Comprobar si ya existe el valor (Su ejecutable/archivo)
                if (keyDel.GetValueNames().Contains("UnidadRed") == true)      // Nombre del valor en la clave de registro
                {
                    keyDel.DeleteValue("UnidadRed");
                }

                // Cerrar clave de registro (Recomendable para prevenir inconvenientes)
                keyDel.Close();
        }
        if (args.Length > 0)
        {
            var config = new Config
            {
                letter = args[0],
                remotePath = args[1],
                username = args[2],
                password = args[3]
            };


            //Codificarlo en base64, y guardarlo en el directorio del ejecutable.
            string jsonConfig = JsonConvert.SerializeObject(config);

            string base64Config = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonConfig));


            File.WriteAllText(configFilePath, base64Config);

            var netResource = new NetResource()
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                LocalName = $"{args[0]}",
                RemoteName = @$"{args[1]}"
            };


            string username = args[2];

            string password = args[3];

            int flags = 0;

            int result = WNetAddConnection2(netResource, password, username, flags);

            if (result == 0)
            {
                Console.WriteLine("Conexión establecida con éxito.");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Error al conectar: {result}, Código de error de Windows: {error}");
            }



        }
        else
        {

            string base64Config = File.ReadAllText(configFilePath);

            // Decodificar los datos Base64 a UTF-8
            byte[] jsonBytes = Convert.FromBase64String(base64Config);
            string jsonConfig = Encoding.UTF8.GetString(jsonBytes);


            Config config = JsonConvert.DeserializeObject<Config>(jsonConfig);

            string letter = config.letter;
            string remotePath = config.remotePath;
            string user = config.username;
            string pass = config.password;



            var netResource = new NetResource()
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                LocalName = $"{letter}",
                RemoteName = @$"{remotePath}"
            };


            string username = user;

            string password = pass;

            int flags = 0;

            int result = WNetAddConnection2(netResource, password, username, flags);

            if (result == 0)
            {
                Console.WriteLine("Conexión establecida con éxito.");
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"Error al conectar: {result}, Código de error de Windows: {error}");
            }
        }

        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        // Comprobar si ya existe el valor (Su ejecutable/archivo)
        if (key.GetValueNames().Contains("UnidadRed") == false)      // Nombre del valor en la clave de registro
        {
            key.SetValue("UnidadRed", $"{baseDir}UnidadRed.exe");        // Parametro 1: Nombre del valor, Parametro 2: Ruta del archivo
        }

        // Cerrar clave de registro (Recomendable para prevenir inconvenientes)
        key.Close();
    }


}