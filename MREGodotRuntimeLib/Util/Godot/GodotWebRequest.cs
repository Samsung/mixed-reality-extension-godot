using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MixedRealityExtension.Util.GodotHelper
{
	// This class is from https://github.com/KhronosGroup/UnityGLTF.
	internal class GodotWebRequest : IDisposable
	{
		private readonly HttpClient httpClient = new HttpClient();
		private readonly Uri baseAddress;

		public event Action<HttpRequestMessage> BeforeRequestCallback;

		/// <summary>
		/// The HTTP response of the last call to LoadStream
		/// </summary>
		public HttpResponseMessage LastResponse { get; private set; }

		public GodotWebRequest(string rootUri)
		{
			ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
			string path = System.IO.Path.GetFileName(rootUri);
			string host = rootUri.Substring(0, rootUri.Length - path.Length);
			baseAddress = new Uri(host);
		}

		public async Task<MemoryStream> LoadStreamAsync(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException(nameof(gltfFilePath));
			}

			if (LastResponse != null)
			{
				LastResponse.Dispose();
				LastResponse = null;
			}

			try
			{

				var tokenSource = new CancellationTokenSource(30000);
				var message = new HttpRequestMessage(HttpMethod.Get, new Uri(baseAddress, gltfFilePath));
				BeforeRequestCallback?.Invoke(message);
				LastResponse = await httpClient.SendAsync(message, tokenSource.Token);
			}
			catch (TaskCanceledException)
			{
				throw new HttpRequestException("Connection timeout");
			}

			LastResponse.EnsureSuccessStatusCode();

			// HACK: Download the whole file before returning the stream
			// Ideally the parsers would wait for data to be available, but they don't.
			var result = new MemoryStream((int?)LastResponse.Content.Headers.ContentLength ?? 5000);
			await LastResponse.Content.CopyToAsync(result);
			return result;
		}

		// enables HTTPS support
		// https://answers.unity.com/questions/50013/httpwebrequestgetrequeststream-https-certificate-e.html
		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain, look at each error to determine the cause.
			if (errors != SslPolicyErrors.None)
			{
				for (int i = 0; i<chain.ChainStatus.Length; i++)
				{
					if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
					{
						chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
						chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
						chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
						chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
						bool chainIsValid = chain.Build((X509Certificate2)certificate);
						if (!chainIsValid)
						{
							isOk = false;
						}
					}
				}
			}

			return isOk;
		}

		public void Dispose()
		{
			if (LastResponse != null)
			{
				LastResponse.Dispose();
			}
		}
	}
}
