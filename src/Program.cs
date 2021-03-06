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
			var doc = api.GetBuildArea();

			var x = doc.GetAwaiter().GetResult();

			Console.WriteLine($"xFrom: {x.xFrom}, yFrom: {x.yFrom},zFrom: {x.zFrom}, xTo: {x.xTo}, yTo: {x.yTo}, zTo: {x.zTo}");
		}
	}
}
