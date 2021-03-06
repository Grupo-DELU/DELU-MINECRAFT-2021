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
			var doc = api.GetChunks(new Vector2Int(101, 11), 4, 4);

			doc.GetAwaiter().GetResult();
		}
	}
}
