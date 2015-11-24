﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TinyPng;

namespace TinyPngApi
{
    public class TinyPng : IDisposable
    {
        private readonly string _apiKey;
        private const string ApiEndpoint = "https://api.tinify.com/shrink";
        private HttpClient httpClient = new HttpClient();
        private readonly JsonSerializerSettings jsonSettings;

        /// <summary>
        /// Wrapper for the tinypng.com API
        /// </summary>
        /// <param name="apiKey">Your tinypng.com API key, signup here: https://tinypng.com/developers </param>
        public TinyPng(string apiKey) 
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            //configure basic auth api key formatting.
            var auth = $"api:{apiKey}";
            var authByteArray = System.Text.Encoding.ASCII.GetBytes(auth);
            _apiKey = Convert.ToBase64String(authByteArray);

            //add auth to the default outgoing headers.
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("basic", _apiKey);
            //configure json settings for camelCase.
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

        }


        private HttpContent CreateContent(byte[] source)
        {
            return new ByteArrayContent(source);
        }
        private HttpContent CreateContent(Stream source)
        {
            return new StreamContent(source);
        }

        /// <summary>
        /// Compress file
        /// </summary>
        /// <param name="pathToFile">Path to file on disk</param>
        /// <returns>TinyPngApiResult, <see cref="TinyPngApiResult"/></returns>
        public async Task<TinyPngApiResult> Compress(string pathToFile)
        {
            if (pathToFile == null)
                throw new ArgumentNullException(nameof(pathToFile));

            using (var file = File.OpenRead(pathToFile))
            {
                return await Compress(file);
            }
        }

        /// <summary>
        /// Compress byte array of image
        /// </summary>
        /// <param name="data">Byte array of the data to compress</param>
        /// <returns></returns>
        public async Task<TinyPngApiResult> Compress(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var response = await httpClient.PostAsync(ApiEndpoint, CreateContent(data));

            if (response.IsSuccessStatusCode)
            {
                return await Deserialize(response);
            }
            throw new Exception($"Api Service returned a non-success status code when attempting to compress an image: {response.StatusCode}");
        }

        /// <summary>
        /// Compress stream
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<TinyPngApiResult> Compress(Stream data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var response = await httpClient.PostAsync(ApiEndpoint, CreateContent(data));

            if (response.IsSuccessStatusCode)
            {
                return await Deserialize(response);
            }
            throw new Exception($"Api Service returned a non-success status code when attempting to compress an image: {response.StatusCode}");
        }

        private async Task<TinyPngApiResult> Deserialize(HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<TinyPngApiResult>(await response.Content.ReadAsStringAsync(), jsonSettings);
        }

        public async Task<TinyPngApiResult> Resize(TinyPngApiResult result, int height, int width, ResizeType resizeType = ResizeType.Fit)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, result.Output.Url);
            
            msg.Headers.Add("Content-Type", "application/json");
            var requestBody = JsonConvert.SerializeObject(new ResizeOperation(resizeType, width, height));

            var response = await httpClient.SendAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                return await Deserialize(response);
            }
            throw new Exception($"Api Service returned a non-success status code when attempting to compress an image: {response.StatusCode}");


        }
        #region IDisposable Support
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient?.Dispose();
            }
        }
        #endregion
    }

    

    public static class Extensions
    {

        public async static Task<byte[]> Resize (this TinyPngApiResult result)
        {
            using (HttpClient client = new HttpClient())
            {
                //var msg = new HttpRequestMessage();
                //msg.Headers.a


                //var response = await client.GetAsync(result.Output.Url);
                //if (response.IsSuccessStatusCode)
                //{
                //    return await response.Content.ReadAsByteArrayAsync();
                //}
                throw new Exception($"Api Service returned a non-success status code when attempting to access a compressed image: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Get the image data as a byte array
        /// </summary>
        /// <param name="result">The result from compress</param>
        /// <returns>Byte array of the image data</returns>
        public async static Task<byte[]> GetImageByteData(this TinyPngApiResult result)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(result.Output.Url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                throw new Exception($"Api Service returned a non-success status code when attempting to access a compressed image: {response.StatusCode}");
            }

        }

        /// <summary>
        /// Gets the image data as a stream
        /// </summary>
        /// <param name="result">The result from compress</param>
        /// <returns>Stream of compressed image data</returns>
        public async static Task<Stream> GetImageStreamData(this TinyPngApiResult result)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(result.Output.Url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStreamAsync();
                }
                throw new Exception($"Api Service returned a non-success status code when attempting to access a compressed image: {response.StatusCode}");
            }

        }

        /// <summary>
        /// Writes the image to disk
        /// </summary>
        /// <param name="result">The result from compress</param>
        /// <param name="filePath">The path to store the file</param>
        /// <returns></returns>
        public async static Task SaveImageToDisk(this TinyPngApiResult result, string filePath)
        {
            var byteData = await result.GetImageByteData();
            File.WriteAllBytes(filePath, byteData);
        }


    }
}
