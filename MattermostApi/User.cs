using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MattermostApi {
	public class NotifyProps : ApiEntryBase {
		public string email;
		public string push;
		public string desktop;
		public string desktop_sound;
		public string mention_keys;
		public string channel;
		public string first_name;
	}

	public class TimeZone : ApiEntryBase {
		public string useAutomaticTimezone;
		public string manualTimezone;
		public string automaticTimezone;
	}

	public class GetUsersQuery : ListRequest {
		public string in_team;
		public string not_in_team;
		public string in_channel;
		public string not_in_channel;
		public bool? group_constrained;
		public bool? without_team;
		public string sort;
	}

	public class SearchUsersQuery : ListRequest {
		public string term;
		public string team_id;
		public string not_in_team_id;
		public string in_channel_id;
		public string not_in_channel_id;
		public bool? group_constrained;
		public bool? allow_inactive;
		public bool? without_team;
		public int limit = 100;
	}

	public class TeamForUser : ApiEntryBase {
		public string id;
		public DateTime create_at;
		public DateTime update_at;
		public DateTime delete_at;
		public string display_name;
		public string name;
		public string description;
		public string email;
		public string type;
		public string allowed_domains;
		public string invite_id;
		public bool allow_open_invite;
		public string company_name;
		public string scheme_id;
		public string group_constrained;
	}

	public class UserCreateInfo : ApiEntryBase {
		public string email;
		public string username;
		public string first_name;
		public string last_name;
		public string nickname;
		public string auth_data;
		/// <summary>
		/// The authentication service, one of "email", "gitlab", "ldap", "saml", "office365", "google", and ""
		/// </summary>
		public string auth_service;
		public string password;
		public string locale;
		public JObject props;
		public NotifyProps notify_props;
	}

	public class User : ApiEntryWithId {
		public string username;
		public string first_name;
		public string last_name;
		public string nickname;
		public string email;
		public bool email_verified;
		public string auth_service;
		public string roles;
		public string locale;
		public NotifyProps notify_props;
		public JObject props;
		public DateTime last_password_update;
		public DateTime last_picture_update;
		public int failed_attempts;
		public bool mfa_active;
		public TimeZone timezone;
		public string terms_of_service_id;
		public DateTime terms_of_service_create_at;

		public static async Task<ApiList<User>> GetAll(Api api, GetUsersQuery query = null) {
			return await api.GetAsync<ApiList<User>>("users", query);
		}

		public static async Task<ApiList<User>> GetAllById(Api api, params string [] ids) {
			return await api.PostAsync<ApiList<User>>("users", null, ids);
		}

		public static async Task<ApiList<User>> GetAllByName(Api api, params string[] names) {
			return await api.PostAsync<ApiList<User>>("users/usernames", null, names);
		}

		public static async Task<ApiList<User>> Search(Api api, string term, SearchUsersQuery query = null) {
			if (query == null)
				query = new SearchUsersQuery();
			query.term = term;
			return await api.PostAsync<ApiList<User>>("users/search", null, query);
		}

		public static async Task<User> GetById(Api api, string id) {
			return await api.GetAsync<User>(Api.Combine("users", id));
		}

		public static async Task<User> GetByName(Api api, string name) {
			return await api.GetAsync<User>(Api.Combine("users", "username", name));
		}

		public static async Task<User> GetByEmail(Api api, string email) {
			return await api.GetAsync<User>(Api.Combine("users", "email", email));
		}

		public static async Task<User> Create(Api api, UserCreateInfo user) {
			return await api.PostAsync<User>("users", null, user);
		}

		public async Task<User> Update(Api api) {
			return await api.PutAsync<User>(Api.Combine("users", id), null, this);
		}

		public async Task Deactivate(Api api) {
			await api.DeleteAsync(Api.Combine("users", id));
		}

		public async Task<ApiList<TeamForUser>> GetTeams(Api api, ListRequest request = null) {
			return await api.GetAsync<ApiList<TeamForUser>>(Api.Combine("users", id, "teams"), request);
		}

		public async Task UpdateRoles(Api api, string channel_id, string roles) {
			await api.PutAsync(Api.Combine("channels", channel_id, "members", id, "roles"), null, new {
				roles
			});
		}

		public async Task<ApiList<UserInChannel>> ChannelMembersForTeam(Api api, string team_id) {
			return await api.GetAsync<ApiList<UserInChannel>>(Api.Combine("users", id, "teams", team_id, "channels", "members"));
		}

		public async Task<ApiList<Channel>> ChannelsForTeam(Api api, string team_id) {
			return await api.GetAsync<ApiList<Channel>>(Api.Combine("users", id, "teams", team_id, "channels"));
		}

		public async Task<UnreadMessageCount> GetUnreadMessages(Api api, string channel_id) {
			return await api.GetAsync<UnreadMessageCount>(Api.Combine("users", id, "channels", channel_id, "unread"));
		}
	}
}
