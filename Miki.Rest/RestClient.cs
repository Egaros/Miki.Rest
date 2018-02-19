using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Rest
{
	// TODO: redo this entire client
    public class RestClient : IDisposable
    {
        private HttpClient client;

        public RestClient(string base_address)
        {
            client = new HttpClient();

			if (!base_address.EndsWith("/"))
				base_address += "/";

			client.BaseAddress = new Uri(base_address);
		}

		// TODO: remove or rework this
        public RestClient AddHeader(string name, string value)
        {
            client.DefaultRequestHeaders.Add(name, value);
            return this;
        }

		public RestClient SetAuthorization(string key)
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(key);
			return this;
		}
		public RestClient SetAuthorization(string scheme, string value)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, value);
            return this;
        }
		
		public async Task<Stream> GetStreamAsync(string url)
		{
			var response = await GetResponseAsync(url);
			return await response.Content.ReadAsStreamAsync();
		}

        public async Task<RestResponse> GetAsync(string url)
        {
			url = url.TrimStart('/');
			HttpResponseMessage response = await GetResponseAsync(url);

			RestResponse r = new RestResponse();
            r.Success = response.IsSuccessStatusCode;
            r.Body = await response.Content.ReadAsStringAsync();
            return r;
        }
		public async Task<RestResponse<T>> GetAsync<T>(string url)
		{
			url = url.TrimStart('/');

			HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            RestResponse<T> r = new RestResponse<T>();
            r.Success = response.IsSuccessStatusCode;
            r.Body = await response.Content.ReadAsStringAsync();
            r.Data = JsonConvert.DeserializeObject<T>(r.Body);
            return r;
        }
		
		public async Task<RestResponse> PostAsync(string url, string value)
		{
			url = url.TrimStart('/');

			HttpResponseMessage response = await client.PostAsync(url, new StringContent(value, Encoding.UTF8, "application/json"));

			RestResponse r = new RestResponse();
			r.HttpResponseMessage = response;
			r.Success = response.IsSuccessStatusCode;
			r.Body = await response.Content.ReadAsStringAsync();

			return r;
		}
        public async Task<RestResponse<T>> PostAsync<T>(string url, string value)
		{
			RestResponse<T> response = new RestResponse<T>(await PostAsync(url, value));
			response.Data = JsonConvert.DeserializeObject<T>(response.Body);
			return response;
		}
		public async Task<RestResponse<T>> PostAsync<T>(string url)
		{
			url = url.TrimStart('/');

			List<object> arguments = new List<object>();
			HttpResponseMessage response = await client.PostAsync(url, null);
			RestResponse<T> r = new RestResponse<T>();
			r.Success = response.IsSuccessStatusCode;
			r.Body = await response.Content.ReadAsStringAsync();
			r.Data = JsonConvert.DeserializeObject<T>(r.Body);
			return r;
		}

		public async Task<RestResponse<T>> PatchAsync<T>(string url, string value)
		{
			url = url.TrimStart('/');

			HttpMethod method = new HttpMethod("PATCH");
			HttpRequestMessage request = new HttpRequestMessage(method, url)
			{
				Content = new StringContent(value, Encoding.UTF8, "application/json")
			};

			HttpResponseMessage response = default(HttpResponseMessage); 
			try
			{
				response = await client.SendAsync(request);
			}
			catch (TaskCanceledException e)
			{
				Console.WriteLine("ERROR: " + e.ToString());
			}

			RestResponse<T> r = new RestResponse<T>();
			r.HttpResponseMessage = response;
			r.Success = response.IsSuccessStatusCode;
			r.Data = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
			return r;
		}

		// TODO: check if it actually works?
		public async Task<RestResponse<string>> PostMultipartAsync(params MultiformItem[] items)
		{
			using (var content =  new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
			{
				for (int i = 0; i < items.Length; i++)
				{
					if (items[i].FileName == null)
					{
						content.Add(items[i].Content, items[i].Name);
						continue;
					}
					content.Add(items[i].Content, items[i].Name, items[i].FileName);
				}

				using (var message = await client.PostAsync("", content))
				{
					var input = await message.Content.ReadAsStringAsync();
					RestResponse<string> response = new RestResponse<string>();
					response.Data = input;
					response.HttpResponseMessage = message;
					return response;
				}
			}
		}

		private async Task<HttpResponseMessage> GetResponseAsync(string url)
			=> await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);

		public void Dispose()
		{
			client.Dispose();
		}
	}

	public class MultiformItem
	{
		public HttpContent Content { get; set; }
		public string Name { get; set; }
		public string FileName { get; set; } = null;
	}
}