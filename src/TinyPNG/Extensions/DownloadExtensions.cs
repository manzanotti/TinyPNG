﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TinyPng.Responses;

namespace TinyPng;

public static class DownloadExtensions
{
    private const string _jpegType = "image/jpeg";

    /// <summary>
    /// Downloads the result of a TinyPng Compression operation
    /// </summary>
    /// <param name="compressResponse"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public static async Task<TinyPngImageResponse> Download(this Task<TinyPngCompressResponse> compressResponse, PreserveMetadata metadata = PreserveMetadata.None)
    {
        if (compressResponse == null)
            throw new ArgumentNullException(nameof(compressResponse));

        var compressResult = await compressResponse;

        return await Download(compressResult, metadata);
    }

    /// <summary>
    /// Downloads the result of a TinyPng Compression operation
    /// </summary>
    /// <param name="compressResponse"></param>
    /// <param name="metadata"></param>
    /// <exception cref="InvalidOperationException">Thrown if you attempt to preserve metadata on an unsupported filetype</exception>
    /// <returns></returns>
    public static async Task<TinyPngImageResponse> Download(this TinyPngCompressResponse compressResponse, PreserveMetadata metadata = PreserveMetadata.None)
    {
        if (compressResponse == null)
            throw new ArgumentNullException(nameof(compressResponse));

        var msg = new HttpRequestMessage(HttpMethod.Get, compressResponse.Output.Url)
        {
            Content = CreateContent(metadata, compressResponse.Output.Type)
        };

        var response = await compressResponse._httpClient.SendAsync(msg).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return new TinyPngImageResponse(response);
        }

        var errorMsg = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(await response.Content.ReadAsStreamAsync());
        throw new TinyPngApiException((int)response.StatusCode, response.ReasonPhrase, errorMsg.Error, errorMsg.Message);
    }

    private static HttpContent CreateContent(PreserveMetadata metadata, string type)
    {
        if (metadata == PreserveMetadata.None)
            return null;

        var preserve = new List<string>();

        if (metadata.HasFlag(PreserveMetadata.Copyright))
        {
            preserve.Add("copyright");
        }
        if (metadata.HasFlag(PreserveMetadata.Creation))
        {
            if (type != _jpegType)
                throw new InvalidOperationException($"Creation metadata can only be preserved with type {_jpegType}");

            preserve.Add("creation");
        }
        if (metadata.HasFlag(PreserveMetadata.Location))
        {
            if (type != _jpegType)
                throw new InvalidOperationException($"Location metadata can only be preserved with type {_jpegType}");

            preserve.Add("location");
        }

        var json = JsonSerializer.Serialize(new { preserve });

        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}

