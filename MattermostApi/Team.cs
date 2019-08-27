using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MattermostApi {
	public class UserInTeam : ApiEntryBase {
		public string team_id;
		public string user_id;
		public string roles;
		public DateTime delete_at;
		public bool scheme_user;
		public bool scheme_admin;
		public string explicit_roles;
	}

	public class Team : ApiEntryWithId {
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

		static async public Task<Team> Create(Api api, string name, string display_name, bool closed = false) {
			return await api.PostAsync<Team>("teams", null, new {
				name,
				display_name,
				type = closed ? "I" : "O"
			});
		}
		static async public Task<ApiList<Team>> GetAll(Api api, ListRequest page = null) {
			if (page == null)
				page = new ListRequest();
			return await api.GetAsync<ApiList<Team>>("teams", page);
		}

		static async public Task<ApiList<Team>> GetTeamsForUser(Api api, string userId, ListRequest page = null) {
			if (page == null)
				page = new ListRequest();
			return await api.GetAsync<ApiList<Team>>(Api.Combine("users", userId, "teams"), page);
		}

		static async public Task<Team> GetById(Api api, string id) {
			return await api.GetAsync<Team>(Api.Combine("teams", id));
		}

		static async public Task<Team> GetByName(Api api, string name) {
			return await api.GetAsync<Team>(Api.Combine("teams", "name", name));
		}

		static async public Task<ApiList<Team>> Search(Api api, string term) {
			return await api.PostAsync<ApiList<Team>>("teams/search", null, new {
				term
			});
		}

		async public Task<Team> Update(Api api) {
			return await api.PutAsync<Team>(Api.Combine("teams", id), null, this);
		}

		async public Task Delete(Api api) {
			await api.DeleteAsync(Api.Combine("teams", id));
		}

		async public Task<ApiList<UserInTeam>> GetMembers(Api api, ListRequest request = null) {
			return await api.GetAsync<ApiList<UserInTeam>>(Api.Combine("teams", id, "members"), request);
		}

		public async Task<Channel> CreateChannel(Api api, string name, string display_name, bool closed = false, string purpose = null, string header = null) {
			return await Channel.Create(api, id, name, display_name, closed, purpose, header);
		}

		public async Task<ApiList<Channel>> GetChannels(Api api, ListRequest request = null) {
			return await api.GetAsync<ApiList<Channel>>(Api.Combine("teams", id, "channels"), request);
		}

		public async Task<ApiList<Channel>> SearchChannels(Api api, string term, ListRequest request = null) {
			return await api.PostAsync<ApiList<Channel>>(Api.Combine("teams", id, "channels", "search"), request, new {
				term
			});
		}

		public async Task<Channel> GetChannelByName(Api api, string name) {
			return await api.GetAsync<Channel>(Api.Combine("teams", id, "channels", "name", name));
		}

		public async Task<ApiList<UserInChannel>> ChannelMembersForUser(Api api, string user_id) {
			return await api.GetAsync<ApiList<UserInChannel>>(Api.Combine("users", user_id, "teams", id, "channels", "members"));
		}

		public async Task<ApiList<Channel>> GetChannelsForUser(Api api, string user_id) {
			return await api.GetAsync<ApiList<Channel>>(Api.Combine("users", user_id, "teams", id, "channels"));
		}

		public async Task<UserInTeam> AddUser(Api api, string user_id) {
			return await AddUser(api, id, user_id);
		}

		static public async Task<UserInTeam> AddUser(Api api, string team_id, string user_id) {
			return await api.PostAsync<UserInTeam>(Api.Combine("teams", team_id, "members"), null, new {
				team_id,
				user_id
			});
		}

		public async Task UpdateRoles(Api api, string user_id, string roles) {
			await api.PutAsync(Api.Combine("teams", id, "members", user_id, "roles"), null, new {
				roles
			});
		}

		public async Task UpdateRoles(Api api, string userId, bool scheme_admin, bool scheme_user = true) {
			await api.PutAsync(Api.Combine("teams", id, "members", userId, "schemeRoles"), null, new {
				scheme_admin,
				scheme_user
			});
		}
	}
}
