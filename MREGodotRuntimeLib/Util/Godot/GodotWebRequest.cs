using System;
using System.IO;
using System.Collections.Generic;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
    internal class GodotWebRequest : HTTPClient
    {
        Uri uri;
        Error err;
        HTTPClient.Method method;
        DownloadHandler downloadHandler;

        private List<string> headers = new List<string>();
        public GodotWebRequest(Uri uri, HTTPClient.Method method = HTTPClient.Method.Get, DownloadHandler downloadHandler = null)
        {
            this.uri = uri;
            this.method = method;
            this.downloadHandler = downloadHandler;
        }

        public void SetRequestHeader(string name, string value)
        {
            headers.Add($"{value}: {name}");
        }

        public Error SendWebRequest()
        {
            err = ConnectToHost(uri.Host, uri.Port);
            if (err != Error.Ok)
                throw new InvalidOperationException($"Failed to connect to host: {uri}. Error: {err}.");

            while (GetStatus() == HTTPClient.Status.Connecting || GetStatus() == HTTPClient.Status.Resolving)
            {
                Poll();
                OS.DelayMsec(10);
            }
            if (GetStatus() != HTTPClient.Status.Connected)
                throw new InvalidOperationException($"Failed to connect to host: {uri}. Error: {err}.");

            err = Request(method, uri.PathAndQuery, headers.ToArray());
            if (err != Error.Ok)
                throw new InvalidOperationException($"Failed to Sends a request to the connected host: {uri}. Error: {err}.");

            while (GetStatus() == HTTPClient.Status.Requesting)
            {
                Poll();
                OS.DelayMsec(10);
            }
            if (GetStatus() != HTTPClient.Status.Body && GetStatus() != HTTPClient.Status.Connected)
                throw new InvalidOperationException($"Failed to Sends a request to the connected host: {uri}. Error: {err}.");

            if (HasResponse())
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    while (GetStatus() == HTTPClient.Status.Body)
                    {
                        Poll();
                        byte[] chunk = ReadResponseBodyChunk();
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
                    downloadHandler.ParseData(stream);
                }
            }
            return err;
        }
    }
}