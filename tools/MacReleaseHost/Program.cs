using Microsoft.NET.HostModel.AppHost;
if (args.Length != 3) throw new ArgumentException("Expected: apphost template, new host path, managed assembly binding.");
if (File.Exists(args[1])) throw new IOException("Host destination already exists.");
HostWriter.CreateAppHost(args[0], args[1], args[2], enableMacOSCodeSign: false);
Console.WriteLine("Created SDK apphost binding: " + args[2]);
