using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace SpeechExperiments
{
	public static class SpeechToText
	{
		public static async Task Transform(IConfiguration config, FileInfo audioFile)
		{
			if (!audioFile.Exists) throw new FileNotFoundException("not found", audioFile.FullName);

			await RecognizeSpeechAsync(config, audioFile);

			Log($"done :)", ConsoleColor.Green);
		}



		private static async Task RecognizeSpeechAsync(IConfiguration config, FileInfo audioFile)
		{
			var speechConfig = SpeechConfig.FromSubscription(config["SubscriptionKey"], config["Region"]);

			if (!audioFile.Exists) throw new FileNotFoundException(audioFile.FullName);

			var outputFile = new FileInfo(Path.Combine(audioFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(audioFile.Name)}.txt"));

			var audio = AudioConfig.FromWavFileInput(audioFile.FullName);
			using (var recognizer = new SpeechRecognizer(speechConfig, audio))
			{
				while (true)
				{
					var result = await recognizer.RecognizeOnceAsync();

					// Checks result.
					if (result.Reason == ResultReason.RecognizedSpeech)
					{
						Said(outputFile, result.Text);
					}
					else if (result.Reason == ResultReason.NoMatch)
					{
						Console.WriteLine($"NOMATCH: Speech could not be recognized.");
						break;
					}
					else if (result.Reason == ResultReason.Canceled)
					{
						var cancellation = CancellationDetails.FromResult(result);
						Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

						if (cancellation.Reason == CancellationReason.Error)
						{
							Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
							Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
							Console.WriteLine($"CANCELED: Did you update the subscription info?");
						}


						break;
					}
				}
			}
		}

		private static void Said(FileInfo outputFile, string text)
		{
			File.AppendAllLines(outputFile.FullName, new[] { text });
			if (!string.IsNullOrWhiteSpace(text))
			{
				Log(text, ConsoleColor.Cyan);
			}
			else
			{
				Log("(pause)", ConsoleColor.Gray);
			}
		}

		private static void Log(string text, ConsoleColor color)
		{
			var colorBefore = Console.ForegroundColor;
			Console.ForegroundColor = color;

			Console.WriteLine(text);

			Console.ForegroundColor = colorBefore;
		}
	}
}
