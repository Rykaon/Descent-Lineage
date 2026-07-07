using System;

public class CommandLineArgs
{
    public static void Apply()
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "-server")
            {
                NetworkConfig.Role = NetworkGameRole.DedicatedServer;
            }

            if (arg == "-client")
            {
                NetworkConfig.Role = NetworkGameRole.Client;
            }

            if (arg == "-ip" && i + 1 < args.Length)
            {
                NetworkConfig.Address = args[i + 1];
            }

            if (arg == "-port" && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort port))
                {
                    NetworkConfig.Port = port;
                }
            }
        }
    }
}
