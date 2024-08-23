using SpaceCore.Content.LanguageServer;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
try
{
    var app = new App(Console.OpenStandardInput(), Console.OpenStandardOutput());
    app.Listen().Wait();
}
catch (Exception e)
{
    Console.Error.WriteLine("Exception: " + e);
}