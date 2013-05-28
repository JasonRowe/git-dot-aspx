namespace GitAspx.Lib
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using GitSharp.Core.Transport;

	public class PostReceiveHook : IPostReceiveHook{
		readonly string fogBugzApi;
		readonly Repository repository;

		public PostReceiveHook(string fogBugzApi, Repository repository) {
			this.fogBugzApi = fogBugzApi;
			this.repository = repository;
		}

		public void OnPostReceive(ReceivePack rp, ICollection<ReceiveCommand> commands) {
			try
			{
				if (!string.IsNullOrEmpty(fogBugzApi) && commands != null && commands.Count > 0)
				{
					UpdateFogBugz(rp, commands);
				}
			}
			catch
			{
				//TODO add log
			}
		}

		/// <summary>
		/// To keep this simple I only look at the head commit and parse the bugzid. 
		/// </summary>
		/// <param name="rp"></param>
		/// <param name="commands"></param>
		void UpdateFogBugz(ReceivePack rp, ICollection<ReceiveCommand> commands) {
			//TODO walk through all changes in commit and handle all bugzid updates

			using (var gitRepo = new GitSharp.Repository(repository.FullPath))
			{
				var commit = gitRepo.Head.CurrentCommit;

				if (commit != null && commit.Message != null && commit.Message.ToLower().Contains("bugzid:"))
				{

					var bugzid = ParseBugzId(commit.Message);

					var hashBase = commit.Hash;

					//look through the current changes being committed
					foreach (var change in commit.Changes)
					{

						var fileName = change.Name;
						var fileOldSha = change.ReferenceObject.Hash;
						var fileNewSha = change.ChangedObject.Hash;
						var hasBaseParent = change.ReferenceCommit.Hash;

						SubmitData(fileOldSha, hasBaseParent, fileNewSha, hashBase, bugzid, fileName, repository.Name);
					}
				}
			}
		}

		void SubmitData(string fileOldSha, string hashBaseParent, string fileNewSha, string hashBase, string bugId, string fileName, string repositoryName) {
			//# Build the FogBugz URI
			var r1 = string.Format("hp={0};hpb={1}", fileOldSha, hashBaseParent);
			var r2 = string.Format("h={0};hb={1}", fileNewSha, hashBase);

			var postUri = string.Format("{0}/cvsSubmit.asp?ixBug={1}&sFile={2}&sPrev={3}&sNew={4}&sRepo={5}", fogBugzApi, bugId, fileName, r1, r2, repositoryName);

			var myRequest = WebRequest.Create(postUri);
			var myResponse = myRequest.GetResponse();
			myResponse.Close();
		}

		public string ParseBugzId(string message) {

			if (string.IsNullOrEmpty(message))
			{
				return string.Empty;
			}

			var messageToLower = message.ToLower();

			var bugIdResult = string.Empty;

			var stringSeparators = new[] { "bugzid:" };
			var result = messageToLower.Split(stringSeparators, StringSplitOptions.None);

			if (result.Length > 1)
			{
				var bugId = result[1].Split(new[] { "\n", "\r\n", " " }, StringSplitOptions.RemoveEmptyEntries);

				if (bugId.Length > 0)
				{
					bugIdResult = bugId[0];
				}
			}

			return bugIdResult;
		}
	}
}