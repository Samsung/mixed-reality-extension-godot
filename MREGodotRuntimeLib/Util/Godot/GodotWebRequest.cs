using System;
using System.IO;
using System.Collections.Generic;
using Godot;
using System.Threading.Tasks;

namespace MixedRealityExtension.Util.GodotHelper
{
    internal class GodotWebRequest : SceneTree
    {
        Uri uri;
        Error err;
        HTTPClient.Method method;

        private static Dictionary<string, HTTPClient> clientCache = new Dictionary<string, HTTPClient>();

        private List<string> headers = new List<string>();
        private Godot.Collections.Dictionary responseHeadersDictionary;
        private long responseCode;
        public GodotWebRequest(Uri uri, HTTPClient.Method method = HTTPClient.Method.Get)
        {
            this.uri = uri;
            this.method = method;
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

        public async Task<MemoryStream> LoadStreamAsync()
        {
            if (!clientCache.TryGetValue(uri.Host, out HTTPClient client))
            {
                client = new HTTPClient();
                clientCache[uri.Host] = client;
            }
            err = client.ConnectToHost(uri.Host, uri.Port);
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to connect to host: {uri}. Error: {err}.");
                return null;
            }

            while (client.GetStatus() == HTTPClient.Status.Connecting || client.GetStatus() == HTTPClient.Status.Resolving)
            {
                client.Poll();
                OS.DelayMsec(10);
            }
            if (client.GetStatus() != HTTPClient.Status.Connected)
            {
                GD.PrintErr($"Failed to connect to host: {uri}. Error: {err}.");
                return null;
            }

            err = client.Request(method, uri.PathAndQuery, headers.ToArray());
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to sends a request to the connected host: {uri}. Error: {err}.");
                return null;
            }

            while (client.GetStatus() == HTTPClient.Status.Requesting)
            {
                client.Poll();
                if (OS.HasFeature("web"))
                {
                    // Synchronous HTTP requests are not supported on the web,
                    // so wait for the next main loop iteration.
                    await ToSignal(Engine.GetMainLoop(), "idle_frame");
                }
                else
                {
                    OS.DelayMsec(10);
                }
            }
            if (client.GetStatus() != HTTPClient.Status.Body && client.GetStatus() != HTTPClient.Status.Connected)
            {
                GD.PrintErr($"Failed to sends a request to the connected host: {uri}. Error: {err}.");
                return null;
            }

            MemoryStream stream = null;
            if (client.HasResponse())
            {
                stream = new MemoryStream();

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
            }

            responseCode = client.GetResponseCode();
            responseHeadersDictionary = client.GetResponseHeadersAsDictionary();
            client.Close();

            return stream;
        }
    }
}