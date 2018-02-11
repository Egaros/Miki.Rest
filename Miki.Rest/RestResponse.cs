using System.Net.Http;

namespace Miki.Rest
{
    public class RestResponse<T>
    {
		public string Body { get; internal set; }
		public T Data { get; internal set; }
		public HttpResponseMessage HttpResponseMessage { get; internal set; }
		public bool Success { get; internal set; }
	}
}