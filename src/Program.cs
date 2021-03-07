// C#
using System;
using System.Net.Http;

// DELU
using DeluMC.HttpApi;
using DeluMC.Utils;


namespace DeluMC
{
	class Program
	{
		static void Main(string[] args)
		{
			var api = new HttpApi.HttpApi();
			var doc = api.PutBlocks("~0 ~0 ~0 minecraft:stone\n~0 ~1 ~0 minecraft:stone\n~0 ~2 ~0 minecraft:stone", -7, 10, 5);

			var x = doc.GetAwaiter().GetResult();
			Console.WriteLine(x);
		}
	}
}
