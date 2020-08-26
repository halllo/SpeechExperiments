using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SpeechExperiments
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
			var configuration = builder.Build();


			//await TextToSpeech.Transform(configuration, new FileInfo(@".\..\..\..\..\..\the-republic-study-guide.txt"));
			await SpeechToText.Transform(configuration, new FileInfo(@".\..\..\..\..\..\sample2.wav"));


			Console.WriteLine("Please press a key to continue.");
			Console.ReadLine();
		}
	}
}
