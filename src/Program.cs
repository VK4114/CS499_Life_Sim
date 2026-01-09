
using System;
using Utilities;
class Program
{
    static void Main(string[] args)
    {
        var log = Log.For<Program>();

        log.Debug("Application starting up!!!!!!!!!!!!!.");
        Console.WriteLine("Hello, World!");

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        
    }
}
