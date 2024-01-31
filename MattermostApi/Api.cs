using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MattermostApi {
	public class Api : IDisposable {
		HttpClient _client;
		CookieContainer _cookies;

		public Api(ISettings settings) {
			Settings = settings;
			_cookies = new CookieContainer();
			_client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false, CookieContainer = _cookies });
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
			_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
			_client.DefaultRequestHeaders.Add("User-Agent", Settings.ApplicationName);
			if (Settings.RedirectUri.Port > 0)
				RedirectPort = Settings.RedirectUri.Port;
		}

		public void Dispose() {
			if (_client != null) {
				_client.Dispose();
				_client = null;
			}
		}

		/// <summary>
		/// HttpClient, so you can set a timeeout for long operations
		/// </summary>
		public HttpClient Client { get { return _client; } }

		/// <summary>
		/// The Settings object to use for this Api instance.
		/// Will be Saved every time the AccessToken changes or is refreshed.
		/// </summary>
		public ISettings Settings;

		/// <summary>
		/// Port to listen on for the redirect after OAuth login. Set in constructor from <see cref="Settings.RedirectUrl" />,
		/// but can be overridden if you have port redictection in a router or something.
		/// </summary>
		public int RedirectPort = 80;

		/// <summary>
		/// Action to open a web browser for the user to login. Just uses operating system browser if not set.
		/// </summary>
		public Action<string> OpenBrowser = openBrowser;

		/// <summary>
		/// Function to wait for a connection to the RedirectUri, extract the code from the Get parameters and return it.
		/// First parameter is this Api.
		/// Second parameter is the expected state value
		/// Default version listens for a request and parses the parameter.
		/// </summary>
		public Func<Api, string, Task<string>> WaitForRedirect = waitForRedirect;

		/// <summary>
		/// Log messages will be passed to this handler
		/// </summary>
		public delegate void LogHandler(string message);

		/// <summary>
		/// Event receives all log messages (to, for example, save them to file or display them to the user)
		/// </summary>
		public event LogHandler LogMessage;

		/// <summary>
		/// Event receives all log messages (to, for example, save them to file or display them to the user)
		/// </summary>
		public event LogHandler ErrorMessage;

		/// <summary>
		/// The most recent requests sent
		/// </summary>
		public string LastRequest;

		/// <summary>
		/// The most recent response received
		/// </summary>
		public string LastResponse;

		/// <summary>
		/// Post to the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<T> PostAsync<T>(string application, object getParameters = null, object postParameters = null) where T : new() {
			JObject j = await PostAsync(application, getParameters, postParameters);
			if (typeof(ApiList).IsAssignableFrom(typeof(T))) {
				JObject r = (getParameters == null ? (object)new ListRequest() : getParameters).ToJObject();
				r["PostParameters"] = postParameters.ToJToken();
				j["Request"] = r;
			}
			return convertTo<T>(j);
		}

		/// <summary>
		/// Post to the Api, returning a JObject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<JObject> PostAsync(string application, object getParameters = null, object postParameters = null) {
			await LoginOrRefreshIfRequiredAsync();
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Post, uri, postParameters);
		}

		/// <summary>
		/// Get from  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<T> GetAsync<T>(string application, object getParameters = null) where T : new() {
			JObject j = await GetAsync(application, getParameters);
			if (typeof(ApiList).IsAssignableFrom(typeof(T)))
				j["Request"] = (getParameters == null ? (object)new ListRequest() : getParameters).ToJObject();
			return convertTo<T>(j);
		}

		/// <summary>
		/// Get from  the Api, returning a Jobject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<JObject> GetAsync(string application, object getParameters = null) {
			await LoginOrRefreshIfRequiredAsync();
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Get, uri);
		}

		/// <summary>
		/// Put to  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<T> PutAsync<T>(string application, object getParameters = null, object postParameters = null) where T : new() {
			JObject j = await PutAsync(application, getParameters, postParameters);
			return convertTo<T>(j);
		}

		/// <summary>
		/// Put to  the Api, returning a JObject
		/// </summary>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		/// <param name="postParameters">Any post parameters to pass (in an object or JObject)</param>
		public async Task<JObject> PutAsync(string application, object getParameters = null, object postParameters = null) {
			await LoginOrRefreshIfRequiredAsync();
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Put, uri, postParameters);
		}

		/// <summary>
		/// Delete to  the Api, returning an object
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<T> DeleteAsync<T>(string application, object getParameters = null) where T : new() {
			JObject j = await DeleteAsync(application, getParameters);
			return convertTo<T>(j);
		}

		/// <summary>
		/// Delete to  the Api, returning a JObject
		/// </summary>
		/// <typeparam name="T">The object type expected</typeparam>
		/// <param name="application">The part of the url after the company</param>
		/// <param name="getParameters">Any get parameters to pass (in an object or JObject)</param>
		public async Task<JObject> DeleteAsync(string application, object getParameters = null) {
			await LoginOrRefreshIfRequiredAsync();
			string uri = MakeUri(application);
			uri = AddGetParams(uri, getParameters);
			return await SendMessageAsync(HttpMethod.Delete, uri);
		}

		/// <summary>
		/// API post using multipart/form-data.
		/// </summary>
		/// <param name="application">The full Uri you want to call (including any get parameters)</param>
		/// <param name="getParameters">Get parameters (or null if none)</param>
		/// <param name="postParameters">Post parameters as an  object or JObject
		/// </param>
		/// <returns>The result as a T Object.</returns>
		public async Task<T> PostFormAsync<T>(string application, object getParameters, object postParameters, params string[] fileParameterNames) where T:new() {
			JObject j = await PostFormAsync(application, getParameters, postParameters, fileParameterNames);
			return convertTo<T>(j);
		}

		/// <summary>
		/// API post using multipart/form-data.
		/// </summary>
		/// <param name="application">The full Uri you want to call (including any get parameters)</param>
		/// <param name="getParameters">Get parameters (or null if none)</param>
		/// <param name="postParameters">Post parameters as an  object or JObject
		/// </param>
		/// <returns>The result as a JObject, with MetaData filled in.</returns>
		public async Task<JObject> PostFormAsync(string application, object getParameters, object postParameters, params string[] fileParameterNames) {
			string uri = AddGetParams(MakeUri(application), getParameters);
			using (DisposableCollection objectsToDispose = new DisposableCollection()) { 
				MultipartFormDataContent content = objectsToDispose.Add(new MultipartFormDataContent());
				JObject data = postParameters.ToJObject();
				foreach (var o in data) {
					if (o.Value.Type == JTokenType.Null)
						continue;
					if (Array.IndexOf(fileParameterNames, o.Key) >= 0) {
						string filename = o.Value.ToString();
						FileStream fs = objectsToDispose.Add(new FileStream(filename, FileMode.Open));
						HttpContent v = objectsToDispose.Add(new StreamContent(fs));
						content.Add(v, o.Key, Path.GetFileName(filename));
					} else {
						HttpContent v = objectsToDispose.Add(new StringContent(o.Value.ToString()));
						content.Add(v, o.Key);
					}
				}
				return await SendMessageAsync(HttpMethod.Post, uri, content);
			}

		}

		/// <summary>
		/// Log in - sends username and password and updates Settings with the Token returned.
		/// </summary>
		public async Task<User> LoginAsync(string login_id, string password) {
			string uri = MakeUri("users/login");
			using (HttpResponseMessage result = await SendMessageAsyncAndGetResponse(HttpMethod.Post, uri, new {
				login_id,
				password
			})) {
				JObject j = await parseJObjectFromResponse(uri, result);
				if (result.Headers.TryGetValues("Token", out IEnumerable<string> values)) {
					string token = values.FirstOrDefault();
					if(!string.IsNullOrEmpty(token)) {
						Cookie c = _cookies.GetCookies(Settings.ServerUri)["MMCSRF"];
						updateToken(new Token() {
							access_token = token,
							expires_in = Settings.LoginExpiryTime,
							csrf_token = c?.Value
						});
					}
				}
				return convertTo<User>(j);
			}
		}

		/// <summary>
		/// Log in - pops up a web browser (using <see cref="OpenBrowser"/>) to allow the user to log in to BaseCamp.
		/// Calls <see cref="WaitForRedirect"/> to collect the redirected Get and parse the code out of it.
		/// Then exchanges the code for a Token, and updates Settings with the Token.
		/// </summary>
		public async Task LoginAsync() {
			string state = Guid.NewGuid().ToString();
			OpenBrowser(AddGetParams(Settings.ServerUri + "oauth/authorize", new {
				response_type = "code",
				client_id = Settings.ClientId,
				redirect_uri = Settings.RedirectUri,
				state
			}));
			string code = await WaitForRedirect(this, state);
			Dictionary<string, string> parms = new Dictionary<string, string>();
			parms["grant_type"] = "authorization_code";
			parms["client_id"] = Settings.ClientId;
			parms["redirect_uri"] = Settings.RedirectUri.ToString();
			parms["client_secret"] = Settings.ClientSecret;
			parms["code"] = code;
			using (FormUrlEncodedContent content = new FormUrlEncodedContent(parms)) {
				var result = await SendMessageAsync(HttpMethod.Post, Settings.ServerUri + "oauth/access_token", content);
				Token token = result.ToObject<Token>();
				if (string.IsNullOrEmpty(token.access_token))
					throw new ApiException(this, "No access token returned");
				updateToken(token);
			}
		}

		/// <summary>
		/// If the Token has nearly expired, refresh it.
		/// </summary>
		public async Task RefreshAsync() {
			var result = await SendMessageAsync(HttpMethod.Post, Settings.ServerUri + "oauth/access_token", new {
				grant_type = "refresh_token",
				client_id = Settings.ClientId,
				redirect_uri = Settings.RedirectUri,
				client_secret = Settings.ClientSecret,
				refresh_token = Settings.RefreshToken
			});
			Token token = result.ToObject<Token>();
			if (string.IsNullOrEmpty(token.access_token))
				throw new ApiException(this, "No access token returned");
			updateToken(token);
		}

		/// <summary>
		/// Update Settings with info from the Token
		/// </summary>
		/// <param name="token">As returned by the auth call</param>
		void updateToken(Token token) {
			Settings.AccessToken = token.access_token;
			if (!string.IsNullOrEmpty(token.refresh_token))
				Settings.RefreshToken = token.refresh_token;
			if (!string.IsNullOrEmpty(token.csrf_token))
				Settings.CsrfToken = token.csrf_token;
			try {
				Settings.TokenExpires = DateTime.Now.AddSeconds(token.expires_in);
			} catch {
				Settings.TokenExpires = DateTime.Now.AddDays(1);
			}
			Settings.Save();
		}

		/// <summary>
		/// Login or Refresh if the token is missing or due to expire.
		/// </summary>
		/// <returns></returns>
		public async Task LoginOrRefreshIfRequiredAsync() {
			if (string.IsNullOrEmpty(Settings.AccessToken) || Settings.TokenExpires <= DateTime.Now)
				await LoginAsync();
			else if (!string.IsNullOrEmpty(Settings.RefreshToken) && Settings.TokenExpires <= DateTime.Now + Settings.RefreshTokenIfDueToExpireBefore)
				await RefreshAsync();
		}

		/// <summary>
		/// Log a message to trace and, if present, to the LogMessage event handlers
		/// </summary>
		public void Log(string message) {
			message = "Mattermost log:" + message;
			System.Diagnostics.Trace.WriteLine(message);
			LogMessage?.Invoke(message);
		}

		/// <summary>
		/// Log a message to trace and, if present, to the ErrorMessage event handlers
		/// </summary>
		public void Error(string message) {
			message = "Mattermost error:" + message;
			System.Diagnostics.Trace.WriteLine(message);
			ErrorMessage?.Invoke(message);
		}

		/// <summary>
		/// Combine a list of arguments into a string, with "/" between them (escaping if required)
		/// </summary>
		public static string Combine(params object[] args) {
			return string.Join("/", args.Select(a => Uri.EscapeUriString(a.ToString())));
		}

		static readonly char[] argSplit = new char[] { '=' };

		/// <summary>
		/// Add or Replace Get Parameters to a uri
		/// </summary>
		/// <param name="parameters">Object whose properties are the arguments - e.g. new {
		/// 		type = "web_server",
		/// 		client_id = Settings.ClientId,
		/// 		redirect_uri = Settings.RedirectUri
		/// 	}</param>
		/// <returns>uri?arg1=value1&amp;arg2=value2...</returns>
		public static string AddGetParams(string uri, object parameters = null) {
			if (parameters != null) {
				Uri u = new Uri(uri);
				Dictionary<string, string> query = new Dictionary<string, string>();
				foreach(string arg in u.Query.Split('&', '?')) {
					if (string.IsNullOrEmpty(arg)) continue;
					string[] parts = arg.Split(argSplit, 2);
					if (parts.Length < 2) continue;
					query[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);

				}
				JObject j = parameters.ToJObject();
				List<string> p = new List<string>();
				foreach (var v in j) {
					if (v.Value.IsNullOrEmpty())
						query.Remove(v.Key);
					else
						query[v.Key] = v.Value.ToString();
				}
				uri = uri.Split('?')[0] + "?" + string.Join("&", query.Keys.Select(k => Uri.EscapeUriString(k) + "=" + Uri.EscapeUriString(query[k])));
			}
			return uri;
		}

		/// <summary>
		/// General API message sending.
		/// </summary>
		/// <param name="method">Get/Post/etc.</param>
		/// <param name="uri">The full Uri you want to call (including any get parameters)</param>
		/// <param name="postParameters">Post parameters as an :-
		/// object (converted to Json, MIME type application/json)
		/// JObject (converted to Json, MIME type application/json)
		/// string (sent as is, MIME type text/plain)
		/// FileStream (sent as stream, with Attachment file name, Content-Length, and MIME type according to file extension)
		/// </param>
		/// <returns>The result as a JObject, with MetaData filled in.</returns>
		public async Task<JObject> SendMessageAsync(HttpMethod method, string uri, object postParameters = null) {
			using (HttpResponseMessage result = await SendMessageAsyncAndGetResponse(method, uri, postParameters)) {
				return await parseJObjectFromResponse(uri, result);
			}
		}

		/// <summary>
		/// Send a message and get the result.
		/// Deal with rate limiting return values and redirects.
		/// </summary>
		/// <param name="method">Get/Post/etc.</param>
		/// <param name="uri">The full Uri you want to call (including any get parameters)</param>
		/// <param name="postParameters">Post parameters as an object or JObject</param>
		public async Task<HttpResponseMessage> SendMessageAsyncAndGetResponse(HttpMethod method, string uri, object postParameters = null) {
			LastRequest = "";
			LastResponse = "";
			for (; ; ) {
				string content = null;
				using (DisposableCollection disposeMe = new DisposableCollection()) {
					var message = disposeMe.Add(new HttpRequestMessage(method, uri));
					if (!string.IsNullOrEmpty(Settings.AccessToken))
						message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.AccessToken);
					if (!string.IsNullOrEmpty(Settings.CsrfToken))
						message.Headers.Add("X-CSRF-Token", Settings.CsrfToken);
					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
					message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
					message.Headers.Add("User-Agent", Settings.ApplicationName);
					if (postParameters != null) {
						if (postParameters is string s) {
							content = s;
							message.Content = disposeMe.Add(new StringContent(content, Encoding.UTF8, "text/plain"));
						} else if (postParameters is FileStream f) {
							content = Path.GetFileName(f.Name);
							f.Position = 0;
							message.Content = disposeMe.Add(new StreamContent(f));
							string contentType = MimeMapping.MimeUtility.GetMimeMapping(content);
							message.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
							message.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") {
								FileName = content
							};
							message.Content.Headers.ContentLength = f.Length;
							content = "File: " + content;
						} else if (postParameters is HttpContent) {
							message.Content = (HttpContent)postParameters;
						} else {
							content = postParameters.ToJson();
							message.Content = disposeMe.Add(new StringContent(content, Encoding.UTF8, "application/json"));
						}
					}
					LastRequest = $"{message}:{content}";
					HttpResponseMessage result;
					int backoff = 500;
					int delay;
					if (Settings.LogRequest > 0)
						Log($"Sent -> {(Settings.LogRequest > 1 ? message.ToString() : message.RequestUri.ToString())}:{content}");
					result = await _client.SendAsync(message);
					LastResponse = result.ToString();
					if (Settings.LogResult > 1)
						Log($"Received -> {result}");
					switch (result.StatusCode) {
						case HttpStatusCode.Found:      // Redirect
						case HttpStatusCode.MovedPermanently:
							uri = new Uri(message.RequestUri, result.Headers.Location).AbsoluteUri;
							delay = 1;
							break;
						case (HttpStatusCode)429:       // TooManyRequests
							delay = 5000;
							break;
						case HttpStatusCode.BadGateway:
						case HttpStatusCode.ServiceUnavailable:
						case HttpStatusCode.GatewayTimeout:
							backoff *= 2;
							delay = backoff;
							if (delay > 16000)
								return result;
							break;
						default:
							return result;
					}
					result.Dispose();
					Thread.Sleep(delay);
				}
			}
		}

		/// <summary>
		/// Build a JObject from a response
		/// </summary>
		/// <param name="uri">To store in the MetaData</param>
		async Task<JObject> parseJObjectFromResponse(string uri, HttpResponseMessage result) {
			JObject j = null;
			string message = result.ReasonPhrase;
			try {
				string data = await result.Content.ReadAsStringAsync();
				LastResponse += "\n" + data;
				if (data.StartsWith("{")) {
					j = JObject.Parse(data);
					string m = j["message"] + "";
					if (!string.IsNullOrEmpty(m))
						message = m;
				} else if (data.StartsWith("[")) {
					j = new JObject {
						["List"] = JArray.Parse(data)
					};
				} else {
					j = new JObject();
					if (!string.IsNullOrEmpty(data))
						j["content"] = data;
				}
				JObject metadata = new JObject();
				if (!result.IsSuccessStatusCode && j.ContainsKey("status_code")) {
					metadata["Error"] = j;
				}
				metadata["Uri"] = uri;
				if (result.Headers.TryGetValues("Last-Modified", out IEnumerable<string> values))
					metadata["Modified"] = values.FirstOrDefault();
				j["MetaData"] = metadata;
				if (Settings.LogResult > 0)
					Log("Received Data -> " + j);
			} catch(Exception ex) {
				throw new ApiException(this, ex);
			}
			if (!result.IsSuccessStatusCode)
				throw new ApiException(this, message);
			return j;
		}

		/// <summary>
		/// Convert a JObject to an Object.
		/// If it is an ApiEntry, and error is not empty, throw an exception.
		/// </summary>
		/// <typeparam name="T">Object to convert to</typeparam>
		T convertTo<T>(JObject j) where T : new() {
			T t = j.ConvertToObject<T>();
			if (t is ApiEntry e && e.Error)
				throw new ApiException(this, e.MetaData.Error.message);
			return t;
		}

		/// <summary>
		/// Default <see cref="OpenBrowser"/>
		/// </summary>
		static void openBrowser(string url) {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}"));
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				Process.Start("xdg-open", "'" + url + "'");
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				Process.Start("open", "'" + url + "'");
			} else {
				throw new ApplicationException("Unknown OS platform");
			}
		}

		/// <summary>
		/// Default <see cref="WaitForRedirect"/>
		/// </summary>
		static async Task<string> waitForRedirect(Api api, string state) {
			return await Task.Run(delegate () {
				IPHostEntry ipHost = Dns.GetHostEntry(api.Settings.RedirectUri.Host);
				IPAddress ipAddr = ipHost.AddressList[0];
				IPEndPoint localEndPoint = new IPEndPoint(ipAddr, api.RedirectPort);

				using (Socket listener = new Socket(ipAddr.AddressFamily,
					 SocketType.Stream, ProtocolType.Tcp)) {
					listener.Bind(localEndPoint);
					listener.Listen(10);
					for (; ; ) {
						api.Log("Waiting connection on port " + api.RedirectPort);
						// Suspend while waiting for 
						// incoming connection Using  
						// Accept() method the server  
						// will accept connection of client 
						using (Socket clientSocket = listener.Accept()) {

							// Data buffer 
							byte[] bytes = new Byte[1024];
							string data = null;

							while (true) {
								int numByte = clientSocket.Receive(bytes);
								if (numByte <= 0)
									break;
								data += Encoding.ASCII.GetString(bytes, 0, numByte);
								if (data.Replace("\r", "").IndexOf("\n\n") > -1)
									break;
							}
							if (api.Settings.LogResult > 1) {
								api.Log("Text received -> " + data);
							}

							string page = api.Settings.PageToSendAfterLogin;
							if (string.IsNullOrEmpty(page))
								page = $@"<html>
<body>
<div>Thankyou for giving access to your Mattermost account to {api.Settings.ApplicationName}.</div>
<div>Please close this window now.</div>
</body>
</html>";
							string headers = $@"HTTP/1.1 {(string.IsNullOrEmpty(api.Settings.RedirectAfterLogin) ? 200 : 303)} OK
Date: Fri, 31 May 2019 18:23:23 GMT
Server: Basecamp API for {api.Settings.ApplicationName}
Content-Type: text/html; charset=UTF-8
";
							if (!string.IsNullOrEmpty(api.Settings.RedirectAfterLogin))
								headers += "Location: " + api.Settings.RedirectAfterLogin;
							clientSocket.Send(Encoding.UTF8.GetBytes(headers + "\r\n\r\n" + page));

							// Close client Socket using the 
							// Close() method. After closing, 
							// we can use the closed Socket  
							// for a new Client Connection 
							clientSocket.Shutdown(SocketShutdown.Both);
							clientSocket.Close();
							string query = data.Split('\n')[0];
							Match m = Regex.Match(query, "code=([^& ]+)");

							if (m.Success) {
								if (!string.IsNullOrEmpty(state) && !query.Contains("state=" + state))
									throw new ApplicationException("OAuth2 state parameter doesn't match");
								return m.Groups[1].Value;
							}
						}
					}
				}
			});
		}

		static readonly Regex _http = new Regex("^https?://");

		/// <summary>
		/// Make the standard Uri (put BaseUri and CompanyId on the front)
		/// </summary>
		/// <param name="application">The remainder of the Uri</param>
		public string MakeUri(string application) {
			return _http.IsMatch(application) ? application : Settings.ServerUri + "api/v4/" + application;
		}

	}

	/// <summary>
	/// Exception to hold more information when an API call fails
	/// </summary>
	public class ApiException : ApplicationException {
		public ApiException(Api api, Exception ex) : base(ex.Message, ex) {
			Request = api.LastRequest;
			Response = api.LastResponse;
		}
		public ApiException(Api api, string message) : base(message) {
			Request = api.LastRequest;
			Response = api.LastResponse;
		}
		public string Request { get; private set; }
		public string Response { get; private set; }
		public override string ToString() {
			return base.ToString() + "\r\nRequest = " + Request + "\r\nResult = " + Response;
		}
	}

	/// <summary>
	/// Token returned from Auth call
	/// </summary>
	public class Token : ApiEntry {
		public string access_token;
		public string token_type;
		public string scope;
		public string refresh_token;
		public string csrf_token;
		public int expires_in;
	}


}
