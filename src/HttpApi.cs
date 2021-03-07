// C# 
using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

// Third party
using Cyotek.Data.Nbt;
using Cyotek.Data.Nbt.Serialization;

// DELU
using DeluMC.Utils;

namespace DeluMC.HttpApi
{
    /// <summary>
    /// This class will use HttpClient and Cyotek to request and 
    /// parse data from minecraft. Please try not to use this class by yourself
    /// since you will be coupling your logic to an interface to some external 
    /// technology. 
    /// 
    /// You can find a reference for the minecraft mod api here: 
    /// https://github.com/nilsgawlik/gdmc_http_interface#installing-this-mod-with-the-forge-mod-launcher
    /// </summary>
    class HttpApi
    {
        /// <summary>
        /// Requests are performed to this base endpoint
        /// </summary>
        private const String _baseEndpoint = "http://localhost:9000/";

        /// <summary>
        /// Endpoint to interact with blocks, both retrieving and editing blocks
        /// </summary>
        private const String _blocksEndpoint = "blocks";

        /// <summary>
        ///  Endpoint to send commands as plain text always
        /// </summary>
        private const String _commandsEndpoint = "command";

        /// <summary>
        /// Endpoint to retrieve chunk data as byte stream
        /// </summary>
        private const String _ChunksEndpoint = "chunks";


        /// <summary>
        /// Endpoint to retrieve build area specification
        /// </summary>
        private const String _buildAreaEndpoint = "buildarea";

        /// <summary>
        /// We use this client to perform requests to minecraft
        /// </summary>
        private static HttpClient _client = new HttpClient();

        /// <summary>
        /// Our actual stored data
        /// </summary>
        private NbtDocument _mcWorldData;

        /// <summary>
        /// data as it comes from minecraft
        /// </summary>
        private Stream _rawMcWorkdData;

        /// <summary>
        /// If this class should store raw data, you can use it to perform an undo operation
        /// </summary>
        private bool _ShouldStoreRawData = false;

        /// <summary>
        /// Create a new HttpApi to talk to minecraft
        /// </summary>
        public HttpApi(bool storeRawData = false)
        {
            _ShouldStoreRawData = storeRawData;

            // Init our http client; use the same base URI to every endpoint
            _client.BaseAddress = new Uri(_baseEndpoint);
        }

        /// <summary>
        /// When requesting the build area from minecraft, 
        /// it is specified by two vector values, the lower
        /// extent and the upper extent,
        /// </summary>
        public struct BuildAreaExtents
        {
            public int xFrom { get; set; }
            public int yFrom { get; set; }
            public int zFrom { get; set; }
            public int xTo   { get; set; }
            public int yTo   { get; set; }
            public int zTo   { get; set; }
        }

        /// <summary>
        /// Retrieve chunk data from minecraft and process it returing a NBTDocument from
        /// our favorite NBT parsing library
        /// </summary>
        /// <param name="startPos"> Chunk position, it will be clamped to chunk coordinates just in case </param>
        /// <param name="dx"> how many chunks to retrieve in X direction </param>
        /// <param name="dz"> how many chunks to retrieve in Z direction </param>
        /// <returns>Parsed data from minecraft as an NBTDocument object</returns>
        public async Task<NbtDocument> GetChunks(Vector2Int startPos, int dx, int dz)
        {
            // Set up expected headers
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/octet-stream")
                );

            // Clamp coordinates so they are in chunk coordinates
            startPos.X /= 16;
            startPos.Z /= 16;

            // make them possitive
            dx = Math.Clamp(dx, 0, int.MaxValue);
            dz = Math.Clamp(dz, 0, int.MaxValue);

            // Request data    
            var ans = await _client.GetAsync($"{_ChunksEndpoint}?x={startPos.X}&z={startPos.Z}&dx={dx}&dz={dz}");

            // Check if request was ok
            if (ans.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException($"Couldn't retrieve data from minecrat, status code: {ans.StatusCode}");

            // parse content as a byte stream
            var content = await ans.Content.ReadAsStreamAsync();

            // Check if this is a valid document
            if (!NbtDocument.IsNbtDocument(content))
                throw new ArgumentException($"Inconsistent data from minecraft. Retrieved data is not a valid NBT document");

            // parse into an actual document object
            var document = NbtDocument.LoadDocument(content);

            // back up our data if it is necessary
            if (_ShouldStoreRawData)
                _rawMcWorkdData = content;

            return document;
        }

        /// <summary>
        /// Get extents for the building area specified in minecraft by the "/setbuildarea" command.
        /// It may throw 404 when no build area has been set so far.
        /// </summary>
        /// <returns>
        ///     Build area extents as they come from minecraft
        /// </returns>
        public async Task<BuildAreaExtents> GetBuildArea()
        {
            // Set up client expecting a json answer
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );

            // request data
            var ans = await _client.GetAsync($"{_baseEndpoint}{_buildAreaEndpoint}");

            // check if everything went ok
            if (ans.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException($"Could not retrieve build area data. Status code: {ans.StatusCode}");

            // parse json as an object
            var json = await ans.Content.ReadAsStringAsync();
            
            var extents = JsonSerializer.Deserialize<BuildAreaExtents>(json);

            return extents;
        }

        /// <summary>
        /// Use this function at your own risk. Try to send a set of blocks
        /// in a single String with a list of blocks specified as 
        /// required by the HttpInterface mod in minecraft, but it doesn't checks 
        /// for correctness of the passed string. 
        /// Note that every block should be specified in coordinates relative to the given origin
        /// </summary>
        /// <param name="blocks"> Blocks to build in the world as required by the mod </param>
        /// <param name="startX"> Start position's x coordinate </param>
        /// <param name="startY"> Start position's y coordinate </param>
        /// <param name="startZ"> Start position's z coordinate </param>
        /// <returns> Response returned by minecraft in plain text </returns>
        public async Task<string> PutBlocks(string blocks, int startX = 0, int startZ = 0, int startY = 0)
        {
            // reset client
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("text/plain")
                );

            // Create body to send
            var stringContent = new StringContent(blocks);

            // Request bulk block edition
            var uri = $"{_blocksEndpoint}?x={startX}&y={startY}&z={startZ}";
            var ans = await _client.PutAsync(uri, stringContent);

            // check if everything went ok 
            if (ans.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException($"Unable to put blocks in minecraft. Status code: {ans.StatusCode}");

            // read content as String
            return await ans.Content.ReadAsStringAsync();
        }
    }
}

