using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace work
{
    class Program
    {
        static string fileContents = "";

        static void output(string msg)
        {
            fileContents += msg + "\n";
        }

        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Specified input file")]
            public string Input { get; set; }
            
            [Option('o', "output", Required = false, HelpText = "Set output to file.")]
            public string Output { get; set; }
            
            [Option('s', "silent", Required = false, HelpText = "Silence output for notices")]
            public bool Silent { get; set; }
            
            [Option('t', "totp", Required = false, HelpText = "Display TOTP codes")]
            public bool displayTOTP { get; set; }
        }


        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                        string json = File.ReadAllText(Environment.CurrentDirectory + "/" + o.Input.Trim());
                        dynamic dynJson = JsonConvert.DeserializeObject(json);
                        Dictionary<string, string> folders = new Dictionary<string, string>();

                        Console.WriteLine($"Converting Bitwarden JSON to Dashlane importable CSV ...");
                        
                        foreach (var folder in dynJson.folders)
                            folders.Add((string)folder.id, (string)folder.name);
                        
                        output($"website,name,login,login2,password,category,note");
                        int i = 0;
                        bool totp = false;
                        foreach (var item in dynJson.items)
                        {
                            dynamic login = item.login;

                            if (login == null)
                                continue;

                            dynamic uris = login.uris;
                            string uri = uris != null && uris.Count > 0 && uris[0].uri != null ? uris[0].uri : "";
                            string notes = item.notes != null ? ((string)item.notes).Replace("\n", " ") : "";
                            string category = (string)item.folderId != null ? folders[(string)item.folderId] : "";
                            string user = login.username;
                            string pass = login.password;

                            if (login.totp != null)
                            {
                                if (!o.Silent)
                                {
                                    string otp = ((string)login.totp).StartsWith("otpauth://") ? "https://api.qrserver.com/v1/create-qr-code/?size=150x150&data="+login.totp : "";
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.Write($"Notice: ");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    Console.Write($"{item.name} - ");
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.Write($"{uri}");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    Console.Write($" has TOTP enabled");

                                    if (otp != "")
                                    {
                                        Console.Write($" you can use the following url to re-anable it: ");
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.Write($"{otp}");
                                    }

                                    Console.ForegroundColor = ConsoleColor.Gray;
                                    
                                    if (o.displayTOTP)
                                        Console.Write($" TOTP: {login.totp}");

                                    Console.WriteLine($"");

                                    totp = true;
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(uri) && !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
                                output($"{uri},{item.name},{user},,{pass},{category},{notes}"); 

                            i++;
                        }  

                        if (!string.IsNullOrEmpty(o.Output))
                            File.WriteAllText(Environment.CurrentDirectory + "/" + o.Output.Trim(), fileContents);

                        if (totp)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Some entries have TOTP enabled you need to manually re-enable this for every entry using the Dashlane app!");
                        }

                        Console.ForegroundColor = ConsoleColor.Green;

                        if (!string.IsNullOrEmpty(o.Output))
                            Console.WriteLine($"Done! Converted {i} entries and saved it to {o.Output}");
                        else
                            Console.WriteLine($"Done! Converted {i} entries.");

                        Console.ForegroundColor = ConsoleColor.Gray;
                   });
        }
    }
}
