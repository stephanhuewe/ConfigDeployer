using Renci.SshNet;
using Salaros.Configuration;

class Program
{
    static void Main(string[] args)
    {
        var configFileFromPath = new ConfigParser("config.cnf");
        string configFile = configFileFromPath.GetValue("Config", "configfile");    
        string hostOrIP = configFileFromPath.GetValue("Server", "hostOrIP");
        string port = configFileFromPath.GetValue("Server", "port");
        string username = configFileFromPath.GetValue("Server", "username");
        string password = configFileFromPath.GetValue("Server", "password");
             

        // Setup Credentials and Server Information
        ConnectionInfo ConnNfo = new ConnectionInfo(hostOrIP, Int32.Parse(port), username,
            new AuthenticationMethod[]{

                // Pasword based Authentication
                new PasswordAuthenticationMethod(username,password)                
            }
        );

        string SourceConfigFile = configFileFromPath.GetValue("Config", "SourceConfigFile");
        string TargetConfigFile = configFileFromPath.GetValue("Config", "TargetConfigFile");
        string TargetConfigPath = configFileFromPath.GetValue("Config", "TargetConfigPath");

        // deploy config file
        using (var sftp = new SftpClient(ConnNfo))
        {
            string uploadfn = SourceConfigFile;

            sftp.Connect();
            sftp.ChangeDirectory(TargetConfigPath);
            using (var uplfileStream = File.OpenRead(uploadfn))
            {
                sftp.UploadFile(uplfileStream, TargetConfigFile, true);
            }
            sftp.Disconnect();
        }

        string servicename = configFileFromPath.GetValue("Config", "servicename");
        bool restart = bool.Parse(configFileFromPath.GetValue("Config", "restart"));

        if (restart)
        {

            // Execute (SHELL) Commands
            using (var sshclient = new SshClient(ConnNfo))
            {
                sshclient.Connect();

                Console.WriteLine(sshclient.CreateCommand($"/etc/init.d/{servicename} restart").Execute());
                sshclient.Disconnect();
            }
        }
        Console.ReadKey();
    }
}