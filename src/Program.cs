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
			var doc = api.GetBlock(0,0,0);

			var x = doc.GetAwaiter().GetResult();
			Console.WriteLine(x);
		}
	}
}
