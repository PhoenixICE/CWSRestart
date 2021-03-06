﻿using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CWSWeb
{
    class Program
    {
        private static string usersDbFile = Path.Combine(Directory.GetCurrentDirectory(), "users.db");

        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;

            Console.Clear();

            int port = 8181;

            if (args.Length == 1)
            {
                Int32.TryParse(args[0], out port);
            }

            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Files")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Files"));

            HostConfiguration config = new HostConfiguration();

            config.UrlReservations.CreateAutomatically = true;
            config.UrlReservations.User = new System.Security.Principal.SecurityIdentifier("S-1-1-0").Translate(typeof(System.Security.Principal.NTAccount)).ToString();

            NancyHost host = new NancyHost(new ApplicationBootstrapper(), config, new Uri(String.Format("http://localhost:{0}", port)));
            Console.WriteLine("Starting server on http://localhost:" + port.ToString());

            Helper.CacheUpdater updater = new Helper.CacheUpdater();

            Helper.Users.Authentication.TryLoadUsersFromFile(usersDbFile);

            host.Start();
            MessageLoop();

            updater.StopUpdater();
            host.Stop();

            Helper.Users.Authentication.TrySaveUsersToFile(usersDbFile);
        }

        private static void MessageLoop()
        {
            centerText("---------------------");
            centerText("CWSWeb");
            centerText("---------------------");

            string message = "start";
            Helper.Settings.Instance.Client = new CWSProtocol.Client("WebServer");

            string name;

            while (message != "quit")
            {
                switch (message.ToLower())
                {
                    case "test":

                        centerText("---------------------");
                        centerText("Connection test");
                        centerText("---------------------");

                        if (Helper.Settings.Instance.Client.Test())
                            Console.WriteLine("Connection succesful.");
                        else
                            Console.WriteLine("No connection possible.");
                        break;
                    case "list":
                        centerText("---------------------");
                        centerText("User list");
                        centerText("---------------------");
                        List<Tuple<string, string>> names = Helper.Users.Authentication.GetUserNames();

                        foreach (Tuple<string, string> t in names)
                            Console.WriteLine("{0} - can access: {1}", t.Item1, t.Item2);
                        break;
                    case "add":
                        centerText("---------------------");
                        centerText("Add user");
                        centerText("---------------------");
                        Console.WriteLine("Enter the username");
                        name = Console.ReadLine();

                        Console.WriteLine("Enter the password");
                        string password = Console.ReadLine();

                        Console.Clear();

                        if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(password))
                        {
                            if (Helper.Users.Authentication.AddUser(name, password, Helper.Users.Authentication.ADMINISTRATOR))
                            {
                                Console.WriteLine("User added succesfully");
                            }
                            else
                            {
                                Console.WriteLine("Could not add user. A user with this name already exists");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Please enter both the username and password");
                        }
                        break;

                    case "prem":
                        centerText("---------------------");
                        centerText("Add premium user");
                        centerText("---------------------");

                        Console.WriteLine("Enter the username");
                        name = Console.ReadLine();

                        Console.WriteLine("Enter the password");
                        password = Console.ReadLine();

                        if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(password))
                        {
                            if (Helper.Users.Authentication.AddUser(name, password, Helper.Users.Authentication.PREMIUM))
                            {
                                Console.WriteLine("User added succesfully");
                            }
                            else
                            {
                                Console.WriteLine("Could not add user. A user with this name already exists");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Please enter both the username and password");
                        }

                        break;

                    case "remove":
                        centerText("---------------------");
                        centerText("Remove user");
                        centerText("---------------------");
                        Console.WriteLine("Please enter the username you want to delete");
                        name = Console.ReadLine();
                        if (!String.IsNullOrEmpty(name) && Helper.Users.Authentication.RemoveUser(name))
                        {
                            Console.WriteLine("User was removed");
                        }
                        else
                        {
                            Console.WriteLine("User not found");
                        }
                        break;
                    case "save":
                        Helper.Users.Authentication.TrySaveUsersToFile(usersDbFile);
                        break;
                    case "load":
                        Helper.Users.Authentication.TryLoadUsersFromFile(usersDbFile);
                        break;
                }

                message = "";
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Enter \"quit\" to quit");
                Console.WriteLine("Enter \"test\" to test communicating with CWSRestart");
                Console.WriteLine("Enter \"list\" to list all admins");
                Console.WriteLine("Enter \"add\" to add an admin");
                Console.WriteLine("Enter \"prem\" to add a premium user");
                Console.WriteLine("Enter \"remove\" to remove an admin");
                Console.WriteLine("Enter \"save\" to save users");
                Console.WriteLine("Enter \"load\" to restore users");
                Console.ForegroundColor = ConsoleColor.White;
                message = Console.ReadLine().ToLower();
                Console.Clear();
            }
        }

        private static void centerText(String text)
        {
            Console.Write(new string(' ', (Console.WindowWidth - text.Length) / 2));
            Console.WriteLine(text);
        }
    }
}
