using System;
using System.IO;
using System.Collections.Generic;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
    internal class GodotWebRequest : SceneTree
    {
        Uri uri;
        Error err;
        HTTPClient.Method method;
        DownloadHandler downloadHandler;

        private static Dictionary<string, HTTPClient> clientCache = new Dictionary<string, HTTPClient>();

        private List<string> headers = new List<string>();
        private Godot.Collections.Dictionary responseHeadersDictionary;
        private long responseCode;
        public GodotWebRequest(Uri uri, HTTPClient.Method method = HTTPClient.Method.Get, DownloadHandler downloadHandler = null)
        {
            this.uri = uri;
            this.method = method;
            this.downloadHandler = downloadHandler;
        }

        public void SetRequestHeader(string name, string value)
        {
            headers.Add($"{name}: {value}");
        }

        public long GetResponseCode()
        {
            return responseCode;
        }

        public string GetResponseHeader(string key)
        {
            return responseHeadersDictionary[key] as string;
        }

        public Error SendWebRequest()
        {
            if (!clientCache.TryGetValue(uri.Host, out HTTPClient client))
            {
                client = new HTTPClient();
                clientCache[uri.Host] = client;
            }
            err = client.ConnectToHost(uri.Host, uri.Port);
            if (err != Error.Ok)
                throw new InvalidOperationException($"Failed to connect to host: {uri}. Error: {err}.");

            while (client.GetStatus() == HTTPClient.Status.Connecting || client.GetStatus() == HTTPClient.Status.Resolving)
            {
                client.Poll();
                OS.DelayMsec(10);
            }
            if (client.GetStatus() != HTTPClient.Status.Connected)
                throw new InvalidOperationException($"Failed to connect to host: {uri}. Error: {err}.");

            err = client.Request(method, uri.PathAndQuery, headers.ToArray());
            if (err != Error.Ok)
                throw new InvalidOperationException($"Failed to Sends a request to the connected host: {uri}. Error: {err}.");

            while (client.GetStatus() == HTTPClient.Status.Requesting)
            {
                client.Poll();
                OS.DelayMsec(10);
            }
            if (client.GetStatus() != HTTPClient.Status.Body && client.GetStatus() != HTTPClient.Status.Connected)
                throw new InvalidOperationException($"Failed to Sends a request to the connected host: {uri}. Error: {err}.");

            if (client.HasResponse())
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    while (client.GetStatus() == HTTPClient.Status.Body)
                    {
                        client.Poll();
                        byte[] chunk = client.ReadResponseBodyChunk();
                        if (chunk.Length == 0)
                        {
                            OS.DelayMsec(10);
                        }
                        else
                        {
                            stream.Write(chunk, 0, chunk.Length);
                        }
                    }
                    stream.Position = 0;
                    if (stream.Length > 0)
                        downloadHandler.ParseData(stream);
                }
            }

            responseCode = client.GetResponseCode();
            responseHeadersDictionary = client.GetResponseHeadersAsDictionary();
            client.Close();

            return err;
        }
    }
}