using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace SpeechExperiments
{
	public static class TextToSpeech
	{
		public static async Task Transform(IConfiguration config, FileInfo textFile)
		{
			if (!textFile.Exists) throw new FileNotFoundException("not found", textFile.FullName);

			await SynthesizeSpeechAsync(config, textFile);
			await ConcatenateWavFiles(textFile);
			await ConvertWavToMp3(textFile);

			Log($"done :)", ConsoleColor.Green);
		}



		private static async Task SynthesizeSpeechAsync(IConfiguration config, FileInfo textFile)
		{
			var text = await File.ReadAllLinesAsync(textFile.FullName);

			int contineWith = 0;
			int counter = contineWith;
			foreach (var page in text.Page(25).Skip(contineWith))
			{
				var outputFile = Path.Combine(textFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(textFile.Name)}{counter:D4}.wav");

				Log($"synthesizing page {counter}", ConsoleColor.Gray);

				var speechConfig = SpeechConfig.FromSubscription(config["SubscriptionKey"], config["Region"]);
				using var speech = new SpeechSynthesizer(speechConfig,
					AutoDetectSourceLanguageConfig.FromOpenRange(),
					AudioConfig.FromWavFileOutput(outputFile));

				string textToConvert = string.Join(Environment.NewLine, page);
				var result = await speech.SpeakTextAsync(textToConvert);
				if (result.Reason != ResultReason.SynthesizingAudioCompleted)
				{
					throw new Exception(result.Reason.ToString());
				}

				counter++;
			}
		}

		private static IEnumerable<List<T>> Page<T>(this IEnumerable<T> sequence, int pageSize)
		{
			return sequence.Select((c, i) => new { c, i }).GroupBy(c => c.i / pageSize).Select(g => g.Select(gc => gc.c).ToList());
		}
		private static void Log(string text, ConsoleColor color)
		{
			var colorBefore = Console.ForegroundColor;
			Console.ForegroundColor = color;

			Console.WriteLine(text);

			Console.ForegroundColor = colorBefore;
		}









		private static async Task ConcatenateWavFiles(FileInfo textFile)
		{
			var outputFile = Path.Combine(textFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(textFile.Name)}.wav");
			var sourceFiles = Directory.EnumerateFiles(textFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(textFile.Name)}0*.wav").ToArray();
			byte[] buffer = new byte[1024];
			WaveFileWriter waveFileWriter = null;

			try
			{
				Log($"concatenating {sourceFiles.Count()} wav files", ConsoleColor.Gray);
				foreach (string sourceFile in sourceFiles)
				{
					using (WaveFileReader reader = new WaveFileReader(sourceFile))
					{
						if (waveFileWriter == null)
						{
							// first time in create new Writer
							waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
						}
						else
						{
							if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
							{
								throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
							}
						}

						int read;
						while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
						{
							waveFileWriter.Write(buffer, 0, read);
						}
					}
				}
			}
			finally
			{
				if (waveFileWriter != null)
				{
					waveFileWriter.Dispose();
				}
			}
		}









		private static async Task ConvertWavToMp3(FileInfo textFile)
		{
			var inputFile = Path.Combine(textFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(textFile.Name)}.wav");
			var outputFile = Path.Combine(textFile.DirectoryName, $"{Path.GetFileNameWithoutExtension(textFile.Name)}.mp3");

			using (var reader = new WaveFileReader(inputFile))
			{
				try
				{
					Log($"converting to mp3", ConsoleColor.Gray);

					MediaFoundationApi.Startup();
					MediaFoundationEncoder.EncodeToMp3(reader, outputFile);
					MediaFoundationApi.Shutdown();
				}
				catch (InvalidOperationException ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}
	}
}
