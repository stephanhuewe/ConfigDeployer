using Renci.SshNet;
using Salaros.Configuration;

class Program
{
    static void Main(string[] args)
    {
        string file = "config.cnf";
        if (args.Length == 1)
        {
            file = args[0];
        }             

        var configFileFromPath = new ConfigParser(file);

        ConnectionInfo connectionInfo = SetupConnection(configFileFromPath);
        DeployFile(configFileFromPath, connectionInfo);
        RestartService(configFileFromPath, connectionInfo);
        Console.ReadKey();
    }

    private static void RestartService(ConfigParser configFileFromPath, ConnectionInfo connectionInfo)
    {
        string servicename = configFileFromPath.GetValue("Service", "servicename");
        bool restart = bool.Parse(configFileFromPath.GetValue("Service", "restart"));

        if (restart)
        {
            using (var sshclient = new SshClient(connectionInfo))
            {
                sshclient.Connect();

                Console.WriteLine(sshclient.CreateCommand($"/etc/init.d/{servicename} restart").Execute());
                sshclient.Disconnect();
            }
        }
    }

    private static void DeployFile(ConfigParser configFileFromPath, ConnectionInfo connectionInfo)
    {
        string SourceConfigFile = configFileFromPath.GetValue("Config", "SourceConfigFile");
        string TargetConfigFile = configFileFromPath.GetValue("Config", "TargetConfigFile");
        string TargetConfigPath = configFileFromPath.GetValue("Config", "TargetConfigPath");

        // deploy config file
        using (var sftp = new SftpClient(connectionInfo))
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

        Console.WriteLine($"Uploading file {SourceConfigFile} to {TargetConfigPath}/{TargetConfigFile}");
    }

    private static ConnectionInfo SetupConnection(ConfigParser configFileFromPath)
    {
        string hostOrIP = configFileFromPath.GetValue("Server", "hostOrIP");
        string port = configFileFromPath.GetValue("Server", "port");
        string username = configFileFromPath.GetValue("Server", "username");
        string password = configFileFromPath.GetValue("Server", "password");
      
        ConnectionInfo ConnNfo = new ConnectionInfo(hostOrIP, Int32.Parse(port), username,
            new AuthenticationMethod[]{

                // Pasword based Authentication
                new PasswordAuthenticationMethod(username,password)
            }
        );
        Console.WriteLine($"Getting Connection to {hostOrIP} - Port {port} with user {username}");
        return ConnNfo;
    }
}