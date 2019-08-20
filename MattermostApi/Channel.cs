using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MattermostApi {
	public class ChannelNotifyProps : ApiEntryBase {
		// TODO: Make Enum True, False, Default (lower case)
		public string email;
		// TODO: Make Enum All, Mention, None, Default (lower case)
		public string push;
		// TODO: Make Enum All, Mention, None, Default (lower case)
		public string desktop;
		// TODO: Make Enum All, Mention, None (do not send) (lower case)
		public string mark_unread;
	}

	public class UnreadMessageCount : ApiEntryBase {
		public string team_id;
		public string channel_id;
		public int msg_count;
		public int mention_count;
	}

	public class UserInChannel : ApiEntryBase {
		public string channel_id;
		public string user_id;
		public string roles;
		public DateTime last_viewed_at;
		public int msg_count;
		public int mention_count;
		ChannelNotifyProps notify_props;
		public DateTime last_update_at;
	}

	public class FileInfo : ApiEntryWithId {
		public string user_id;
		public string name;
		public string extension;
		public int size;
		public string mime_type;
		public int width;
		public int height;
		public bool has_preview_image;
	}

	public class FileUploadResult : ApiEntry {
		public FileInfo[] file_infos;
		public string[] client_ids;
	}

	public class Channel : ApiEntryWithId {
		public string team_id;
		public string type;
		public string display_name;
		public string name;
		public string header;
		public string purpose;
		public DateTime last_post_at;
		public int total_msg_count;
		public DateTime extra_update_at;
		public string creator_id;

		public static async Task<Channel> Create(Api api, string team_id, string name, string display_name, bool closed = false, string purpose = null, string header = null) {
			return await api.PostAsync<Channel>("channels", null, new {
				team_id,
				name,
				display_name,
				purpose,
				header,
				type = closed ? "P" : "O"
			});
		}
		public static async Task<Channel> GetById(Api api, string id) {
			return await api.GetAsync<Channel>(Api.Combine("channels", id));
		}

		public async Task<Channel> Update(Api api) {
			return await api.PutAsync<Channel>(Api.Combine("channels", id), null, this);
		}

		public async Task Delete(Api api) {
			await api.DeleteAsync(Api.Combine("channels", id));
		}

		public async Task<Channel> MakePrivate(Api api) {
			return await api.PostAsync<Channel>(Api.Combine("channels", id, "convert"));
		}

		public async Task<ApiList<UserInChannel>> GetMembers(Api api, ListRequest request = null) {
			return await api.GetAsync<ApiList<UserInChannel>>(Api.Combine("channels", id, "members"), request);
		}

		public async Task<UserInChannel> GetMember(Api api, string user_id) {
			return await api.GetAsync<UserInChannel>(Api.Combine("channels", id, "members", user_id));
		}

		public async Task<UserInChannel> AddUser(Api api, string user_id, string post_root_id = null) {
			return await AddUser(api, id, user_id, post_root_id);
		}

		static public async Task<UserInChannel> AddUser(Api api, string channel_id, string user_id, string post_root_id = null) {
			return await api.PostAsync<UserInChannel>(Api.Combine("channels", channel_id, "members"), null, new {
				user_id,
				post_root_id
			});
		}

		public async Task RemoveUser(Api api, string user_id) {
			await api.DeleteAsync(Api.Combine("channels", id, "members", user_id));
		}

		public async Task UpdateRoles(Api api, string user_id, string roles) {
			await api.PutAsync(Api.Combine("channels", id, "members", user_id, "roles"), null, new {
				roles
			});
		}

		public async Task UpdateRoles(Api api, string userId, bool scheme_admin, bool scheme_user = true) {
			await api.PutAsync(Api.Combine("channels", id, "members", userId, "schemeRoles"), null, new {
				scheme_admin,
				scheme_user
			});
		}

		public async Task UpdateNotifications(Api api, string user_id, ChannelNotifyProps notifications) {
			await api.PutAsync(Api.Combine("channels", id, "members", user_id, "notify_props"), null, notifications);
		}

		public async Task<UnreadMessageCount> GetUnreadMessages(Api api, string user_id) {
			return await api.GetAsync<UnreadMessageCount>(Api.Combine("users", user_id, "channels", id, "unread"));
		}

		async public Task<Post> CreatePost(Api api, CreatePostData data) {
			data.channel_id = id;
			return await Post.Create(api, data);
		}

		async public Task<Post> CreatePost(Api api, string message, string root_id = null, JObject props = null, params string[] file_ids) {
			return await Post.Create(api, id, message, root_id, props, file_ids);
		}

		async public Task<FileUploadResult> UploadFile(Api api, string filename, string client_id = null) {
			return await api.PostFormAsync<FileUploadResult>("files", null, new {
				channel_id = id,
				client_id,
				files = filename
			}, "files");
		}
	}
}
