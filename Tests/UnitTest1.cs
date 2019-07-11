using MattermostApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests {
	public class Settings : MattermostApi.Settings {
		public bool LoginTests = false;
		public bool ModifyTests = true;
		public bool DestructiveTests = false;
		public string Login;
		public string Password;
		public string TestTeamName;
		public string TestTeam;
		public string AdminUser;
		public string AdminUserName;
		public string TestUser;
		public string TestChannel;
		public string TestChannelName;
		public string TestEmail;
		public override List<string> Validate() {
			List<string> errors = base.Validate();
			return errors;
		}
	}

	public class TestBase {
		static Settings _settings;
		static Api _api;

		public static Api Api {
			get {
				if (_api == null) {
					_api = new Api(Settings);
				}
				return _api;
			}
		}

		public static Settings Settings {
			get {
				if (_settings == null) {
					string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MattermostApi");
					Directory.CreateDirectory(dataPath);
					string filename = Path.Combine(dataPath, "TestSettings.json");
					_settings = new Settings();
					_settings.Load(filename);
					List<string> errors = _settings.Validate();
					if (errors.Count > 0)
						throw new ApplicationException(string.Join("\r\n", errors));
				}
				return _settings;
			}
		}


		public static T RunTest<T>(Task<T> task) {
			T t = task.Result;
			Console.WriteLine(t);
			return t;
		}

		public static void RunTest(Task task) {
			task.Wait();
		}

		const string alphabet = "ybndrfg8ejkmcpqxot1uwisza345h769";

		public string UniqueId() {
			Guid guid = Guid.NewGuid();
			byte[] bytes = guid.ToByteArray();
			StringBuilder output = new StringBuilder();
for (int bitIndex = 0; bitIndex < bytes.Length * 8; bitIndex += 5) {
				int dualbyte = bytes[bitIndex / 8] << 8;
				if (bitIndex / 8 + 1 < bytes.Length)
					dualbyte |= bytes[bitIndex / 8 + 1];
				dualbyte = 0x1f & (dualbyte >> (16 - bitIndex % 8 - 5));
				output.Append(alphabet[dualbyte]);
			}
			return output.ToString().Substring(0, 26);
		}


	}
	[TestClass]
	public class LoginTests : TestBase {
		[TestMethod]
		public void Login() {
			if(Settings.LoginTests)
				RunTest(Api.LoginAsync());
		}
		[TestMethod]
		public void LoginWithPassword() {
			if (Settings.LoginTests)
				RunTest(Api.LoginAsync(Settings.Login, Settings.Password));
		}
	}
	[TestClass]
	public class TeamTests : TestBase {
		[TestMethod]
		public void GetTeams() {
			var l = RunTest(Team.GetAll(Api));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestTeam));
		}
		[TestMethod]
		public void GetUsersForTeam() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var l = RunTest(t.GetMembers(Api));
			Assert.IsTrue(l.All(Api).Any(l => l.user_id == Settings.AdminUser));
		}
		[TestMethod]
		public void GetTeamByName() {
			var o = RunTest(Team.GetByName(Api, Settings.TestTeamName));
			Assert.AreEqual(Settings.TestTeam, o.id);
		}
		[TestMethod]
		public void GetTeamById() {
			var o = RunTest(Team.GetById(Api, Settings.TestTeam));
			Assert.AreEqual(Settings.TestTeam, o.id);
		}
		[TestMethod]
		public void SearchTeams() {
			var l = RunTest(Team.Search(Api, Settings.TestTeamName));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestTeam));
		}
		[TestMethod]
		public void UpdateTeam() {
			if (!Settings.ModifyTests)
				return;
			var o = Team.GetById(Api, Settings.TestTeam).Result;
			string original = o.display_name;
			o.display_name = "Changed Display name";
			var changed = RunTest(o.Update(Api));
			Assert.AreEqual(o.display_name, changed.display_name);
			o.display_name = original;
			changed = RunTest(o.Update(Api));
			Assert.AreEqual(original, changed.display_name);
		}
		[TestMethod]
		public void CreateAndDeleteTeam() {
			if (!Settings.DestructiveTests)
				return;
			string name = UniqueId();
			string display_name = "Test Create";
			var o = RunTest(Team.Create(Api, name, display_name));
			Assert.AreEqual(name, o.name);
			Assert.AreEqual(display_name, o.display_name);
			RunTest(o.Delete(Api));
		}
	}
	[TestClass]
	public class UserTests : TestBase {
		[TestMethod]
		public void ListUsers() {
			var l = RunTest(User.GetAll(Api));
			Assert.IsTrue(l.All(Api).Any(u => u.id == Settings.AdminUser));
		}
		[TestMethod]
		public void SearchUsers() {
			var l = RunTest(User.Search(Api, Settings.AdminUserName));
			Assert.IsTrue(l.All(Api).Any(u => u.id == Settings.AdminUser));
		}
		[TestMethod]
		public void GetUserById() {
			var o = RunTest(User.GetById(Api, Settings.AdminUser));
			Assert.AreEqual(Settings.AdminUser, o.id);
		}
		[TestMethod]
		public void GetUserByName() {
			var o = RunTest(User.GetByName(Api, Settings.AdminUserName));
			Assert.AreEqual(Settings.AdminUser, o.id);
		}
		[TestMethod]
		public void GetUserByEmail() {
			var o = RunTest(User.GetByEmail(Api, Settings.TestEmail));
			Assert.AreEqual(Settings.TestEmail, o.email);
		}
		[TestMethod]
		public void GetTeamsForUser() {
			var o = User.GetById(Api, Settings.AdminUser).Result;
			var l = RunTest(o.GetTeams(Api));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestTeam));
		}
	}
	[TestClass]
	public class ChannelTests : TestBase {
		[TestMethod]
		public void GetChannels() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var l = RunTest(t.GetChannels(Api));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestChannel));
		}
		[TestMethod]
		public void GetChannelById() {
			var o = RunTest(Channel.GetById(Api, Settings.TestChannel));
			Assert.AreEqual(Settings.TestChannel, o.id);
		}
		[TestMethod]
		public void GetChannelByName() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var o = RunTest(t.GetChannelByName(Api, Settings.TestChannelName));
			Assert.AreEqual(Settings.TestChannel, o.id);
		}
		[TestMethod]
		public void SearchChannels() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var l = RunTest(t.SearchChannels(Api, Settings.TestChannelName));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestChannel));
		}
		[TestMethod]
		public void ChannelMembers() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var l = RunTest(t.ChannelMembersForUser(Api, Settings.AdminUser));
			Assert.IsTrue(l.All(Api).Any(l => l.channel_id == Settings.TestChannel));
		}
		[TestMethod]
		public void GetChannelsForUser() {
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			var l = RunTest(t.GetChannelsForUser(Api, Settings.AdminUser));
			Assert.IsTrue(l.All(Api).Any(l => l.id == Settings.TestChannel));
		}
		[TestMethod]
		public void CreateAndUpdateChannel() {
			if (!Settings.DestructiveTests)
				return;
			var t = Team.GetById(Api, Settings.TestTeam).Result;
			string name = UniqueId();
			string display_name = "Test new channel";
			var o = RunTest(t.CreateChannel(Api, name, display_name));
			Assert.AreEqual(name, o.name);
			Assert.AreEqual(display_name, o.display_name);
			RunTest(o.AddUser(Api, Settings.TestUser));
			var l = t.ChannelMembersForUser(Api, Settings.TestUser).Result;
			Assert.IsTrue(l.All(Api).Any(l => l.channel_id == o.id));
			RunTest(o.RemoveUser(Api, Settings.TestUser));
			l = t.ChannelMembersForUser(Api, Settings.TestUser).Result;
			Assert.IsFalse(l.All(Api).Any(l => l.channel_id == o.id));
			o.name = UniqueId();
			o.display_name = "Changed name";
			var changed = RunTest(o.Update(Api));
			Assert.AreEqual(o.name, changed.name);
			Assert.AreEqual(o.display_name, changed.display_name);
			RunTest(changed.Delete(Api));
		}
		[TestMethod]
		public void GetMembers() {
			var c = Channel.GetById(Api, Settings.TestChannel).Result;
			var l = RunTest(c.GetMembers(Api));
			Assert.IsTrue(l.All(Api).Any(l => l.user_id == Settings.AdminUser));
		}
		[TestMethod]
		public void GetUnreadMessages() {
			var c = Channel.GetById(Api, Settings.TestChannel).Result;
			var l = RunTest(c.GetUnreadMessages(Api, Settings.AdminUser));
		}
	}
	[TestClass]
	public class PostTests : TestBase {
		[TestMethod]
		public void CreatePost() {
			var c = Channel.GetById(Api, Settings.TestChannel).Result;
			string message = "Test post " + (c.total_msg_count + 1);
			var o = RunTest(Post.Create(Api, Settings.TestChannel, message));
			Assert.AreEqual(Settings.TestChannel, o.channel_id);
			Assert.AreEqual(message, o.message);
		}
		[TestMethod]
		public void GetPosts() {
			var l = Post.GetPosts(Api, Settings.TestChannel, new PostQuery() { per_page = 2 }).Result;
			foreach (Post p in l.All(Api))
				Console.WriteLine(p);
		}
		[TestMethod]
		public void SearchPosts() {
			var l = Post.SearchPosts(Api, Settings.TestTeam, new PostSearchQuery() { terms = "test", per_page = 2 }).Result;
			foreach (Post p in l.All(Api))
				Console.WriteLine(p);
		}
	}
}