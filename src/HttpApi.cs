// C# 
using System;
using System.IO;
using System.Text;
using System.Net.Http;
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
    /// technology
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
    }
}

