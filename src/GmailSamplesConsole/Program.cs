using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GmailSamplesConsole.Helpers;
using GmailSamplesConsole.Models;
using Microsoft.Extensions.Configuration;

namespace GmailSamplesConsole
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false);

      IConfiguration config = builder.Build();

      var settings = new GmailSettings();
      config.GetSection("GMailSettings").Bind(settings);

      var googleSection = new GoogleClientSecret();
      config.GetSection("GoogleClientSecret").Bind(googleSection);
      var secretString = JsonSerializer.Serialize(googleSection);

      Console.WriteLine(secretString);

      var googleSectionStream = new MemoryStream(Encoding.UTF8.GetBytes(secretString));
      
      var service = await GmailApiHelper.GetService(googleSectionStream);

      await GmailApiHelper.ProcessMessages(service, settings);
    }
  }
}
