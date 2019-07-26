using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MattermostApi {
	public class PostData : ApiEntryBase {
		public JToken[] embeds;
		public JToken[] emojis;
		public FileInfo[] files;
		public JToken images;
		public JToken[] reactions;
	}

	public class PostUpdate : ApiEntryBase {
		public string id;
		public bool? is_pinned;
		public string message;
		public string[] file_ids;
		public bool? has_reactions;
		public JToken props;
	}

	public class CreatePostData {
		public string channel_id;
		public string user_id;
		public string root_id;
		public List<string> file_ids;
		public string message;
		public DateTime? create_at;
		public JToken props;
	}

	public class PostQuery : ListRequest {
		public DateTime? since;
		public string before;
		public string after;
	}

	public class PostList : ApiList<Post> {
		public override ApiList<Post> Convert(JObject j) {
			ApiList<Post> result = base.Convert(j);
			JArray order = (JArray)j["order"];
			JObject posts = (JObject)j["posts"];
			foreach(JToken p in order) {
				result.List.Add(((JObject)posts[p.ToString()]).ConvertToObject<Post>());
			}
			return result;
		}
	}

	public class PostSearchQuery : ListRequest {
		public string terms;
		public bool is_or_search;
		public int time_zone_offset;
		public bool include_deleted_channels;
	}

	public class Post : ApiEntryWithId {
		public DateTime edit_at;
		public string user_id;
		public string channel_id;
		public string root_id;
		public string parent_id;
		public string original_id;
		public string message;
		public string type;
		public JToken props;
		public string hashtag;
		public string[] filenames;
		public string[] file_ids;
		public string pending_post_id;
		public PostData metadata;

		static async public Task<Post> Create(Api api, string channel_id, string message, string root_id = null, JObject props = null, 
			params string [] file_ids) {
			return await api.PostAsync<Post>("posts", null, new {
				channel_id,
				message,
				root_id,
				file_ids,
				props
			});
		}

		static async public Task<Post> Create(Api api, CreatePostData data) {
			return await api.PostAsync<Post>("posts", null, data);
		}

		static async public Task<Post> CreateEphemeral(Api api, string user_id, string channel_id, string message) {
			return await api.PostAsync<Post>(Api.Combine("posts", "ephemeral"), null, new {
				user_id,
				post = new {
					channel_id,
					message
				}
			});
		}

		static async public Task<PostList> GetPosts(Api api, string channel_id, PostQuery range = null) {
			JObject j = await api.GetAsync(Api.Combine("channels", channel_id, "posts"), range);
			PostList result = j.ConvertToObject<PostList>();
			result.Request = range ?? new PostQuery();
			result.List = result.Convert(j).List;
			return result;
		}

		static async public Task<PostList> SearchPosts(Api api, string team_id, PostSearchQuery search) {
			JObject j = await api.GetAsync(Api.Combine("teams", team_id, "posts", "search"), search);
			PostList result = j.ConvertToObject<PostList>();
			result.Request = search ?? new PostSearchQuery();
			result.List = result.Convert(j).List;
			return result;
		}

		static async public Task<Post> GetById(Api api, string post_id) {
			return await api.GetAsync<Post>(Api.Combine("posts", post_id));
		}

		async public Task Delete(Api api) {
			await api.DeleteAsync(Api.Combine("posts", id));
		}

		static async public Task<Post> Update(Api api, PostUpdate data) {
			return await api.PutAsync<Post>(Api.Combine("posts", data.id), null, data);
		}

		async public Task Pin(Api api) {
			await api.PostAsync(Api.Combine("posts", id, "pin"));
		}

		async public Task Unpin(Api api) {
			await api.PostAsync(Api.Combine("posts", id, "unpin"));
		}

		async public Task PerformAction(Api api, string action_id) {
			await api.PostAsync(Api.Combine("posts", id, "actions", action_id));
		}

	}
}
