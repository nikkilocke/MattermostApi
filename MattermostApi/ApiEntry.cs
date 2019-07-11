using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MattermostApi {
	public class UnixMsecDateTimeConverter : Newtonsoft.Json.JsonConverter {
		static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public override bool CanConvert(Type objectType) {
			return objectType == typeof(DateTime);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			var t = (long)reader.Value;
			return t == 0 ? DateTime.MinValue : epoch.AddMilliseconds(t);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			long msec = value == null ? 0 : (long)((DateTime)value - epoch).TotalMilliseconds;
			writer.WriteValue(msec);
		}
	}

	public static class Extensions {
		static Extensions() {
			_humanSettings = new JsonSerializerSettings() {
				NullValueHandling = NullValueHandling.Ignore
			};
			// Force Enums to be converted as strings
			_humanSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			_apiSettings = new JsonSerializerSettings() {
				NullValueHandling = NullValueHandling.Ignore
			};
			// Force Enums to be converted as strings
			_apiSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			// Convert dates to Unix msec timestamps
			_apiSettings.Converters.Add(new UnixMsecDateTimeConverter());
			_serializer = JsonSerializer.Create(_apiSettings);
		}

		static readonly JsonSerializerSettings _humanSettings;
		static readonly JsonSerializerSettings _apiSettings;
		static readonly JsonSerializer _serializer;

		/// <summary>
		/// Convert object to Json string. 
		/// Note Enums are converted as strings, and Dates as Unix msec timestamps.
		/// </summary>
		public static string ToJson(this object o) {
			return Newtonsoft.Json.JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented, _apiSettings);
		}

		/// <summary>
		/// Convert object to Json string. 
		/// Note Enums are converted as strings.
		/// </summary>
		public static string ToHumanReadableJson(this object o) {
			return Newtonsoft.Json.JsonConvert.SerializeObject(o, Newtonsoft.Json.Formatting.Indented, _humanSettings);
		}

		/// <summary>
		/// Convert Object to JObject.
		/// Note Enums are converted as strings, and Dates as Unix msec timestamps.
		/// </summary>
		public static JObject ToJObject(this object o) {
			return o is JObject ? o as JObject : JObject.FromObject(o, _serializer);
		}

		/// <summary>
		/// Convert JToken to Object.
		/// Note Enums are converted as strings, and Dates as Unix msec timestamps.
		/// </summary>
		public static T ConvertToObject<T>(this JToken self) {
			return self.ToObject<T>(_serializer);
		}

		/// <summary>
		/// Is a JToken null or empty
		/// </summary>
		public static bool IsNullOrEmpty(this JToken token) {
			return (token == null) ||
				   (token.Type == JTokenType.Array && !token.HasValues) ||
				   (token.Type == JTokenType.Object && !token.HasValues) ||
				   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
				   (token.Type == JTokenType.Null);
		}

	}

	/// <summary>
	/// Just an object whose ToString shows the whole object as Json, for debugging.
	/// </summary>
	public class ApiEntryBase {
		override public string ToString() {
			return this.ToHumanReadableJson();
		}
	}

	/// <summary>
	/// Error returns contain these fields
	/// </summary>
	public class ApiError {
		public string id;
		public string message;
		public string request_id;
		public int status_code;
		public bool is_oauth;
	}

	/// <summary>
	/// Information sent to and returned from an Api call
	/// </summary>
	public class MetaData {
		/// <summary>
		/// An error returned by the Api
		/// </summary>
		public ApiError Error;
		/// <summary>
		/// The Uri called
		/// </summary>
		public string Uri;
		/// <summary>
		/// Last modified date for caching.
		/// </summary>
		public DateTime Modified;
	}

	/// <summary>
	/// Standard Api call return value.
	/// </summary>
	public class ApiEntry : ApiEntryBase {
		/// <summary>
		/// MetaData about the call and return values
		/// </summary>
		public MetaData MetaData;
		/// <summary>
		/// Any unexpected json items returned will be in here
		/// </summary>
		[JsonExtensionData]
		public IDictionary<string, JToken> AdditionalData;
		/// <summary>
		/// Whether the Api call returned an error object.
		/// </summary>
		[JsonIgnore]
		public bool Error {
			get { return MetaData.Error != null; }
		}
#if DEBUG
		/// <summary>
		/// For debugging, to flag up when there is AdditionalData
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (AdditionalData != null && AdditionalData.Count > 0) {
				System.Diagnostics.Debug.WriteLine("***ADDITIONALDATA***");
				System.Diagnostics.Debug.WriteLine(AdditionalData.ToHumanReadableJson());
			}
			return base.ToString();
		}
#endif
	}
	public class ApiEntryWithId : ApiEntry {
		public string id;
		public DateTime create_at;
		public DateTime update_at;
		public DateTime delete_at;
	}

	/// <summary>
	/// Requests to return lists which support paging
	/// </summary>
	public class ListRequest : ApiEntryBase {
		public int page;
		public int per_page = 60;
		public JObject PostParameters;
	}

	public class ApiList : ApiEntry {
		public ListRequest Request;

		public int RequestedCount {
			get { return Request == null ? 0 : (Request.page + 1) * Request.per_page; }
		}

	}
	/// <summary>
	/// Standard Api call List return
	/// </summary>
	/// <typeparam name="T">The type of item in the List</typeparam>
	public class ApiList<T> : ApiList where T : new() {
		public static ApiList<T> EmptyList(string uri) {
			ApiList<T> list = new ApiList<T> {
				MetaData = new MetaData() { Uri = uri }
			};
			list.Request = new ListRequest();
			return list;
		}
		/// <summary>
		/// List of items returned in this chunk.
		/// </summary>
		public List<T> List = new List<T>();

		/// <summary>
		/// Number of items retrieved in this chunk.
		/// </summary>
		public int Count {
			get { return List.Count; }
		}

		/// <summary>
		/// There is data on the server we haven't fetched yet
		/// </summary>
		public bool HasMoreData {
			get { return List.Count == Request.per_page; }
		}

		/// <summary>
		/// Get the next chunk of data from the server
		/// </summary>
		public async Task<ApiList<T>> GetNext(Api api) {
			if (!HasMoreData)
				return null;
			Request.page++;
			JObject j = await api.SendMessageAsync(Request.PostParameters == null ? HttpMethod.Get : HttpMethod.Post, 
				Api.AddGetParams(MetaData.Uri, new {
					Request.page, 
					Request.per_page
					}), Request.PostParameters);
			return Convert(j);
		}

		/// <summary>
		/// Return an Enumerable of all the items in the list, getting more from the server when required
		/// </summary>
		/// <param name="api"></param>
		/// <returns></returns>
		public IEnumerable<T> All(Api api) {
			ApiList<T> chunk = this;
			while (chunk != null && chunk.Count > 0) {
				foreach (T t in chunk.List)
					yield return t;
				chunk = chunk.GetNext(api).Result;
			}
		}

		/// <summary>
		/// Convert a JObject into the current type.
		/// Override for the odd list return that doesn't use pages.
		/// </summary>
		public virtual ApiList<T> Convert(JObject j) {
			return j.ConvertToObject<ApiList<T>>();
		}

	}

}