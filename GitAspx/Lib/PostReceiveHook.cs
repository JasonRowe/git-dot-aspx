namespace GitAspx.Lib {
	using System;
	using System.Collections.Generic;
	using System.Net;
	using GitSharp.Core.Transport;

	public class PostReceiveHook : IPostReceiveHook {
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
			catch { }
		}

		// To keep this simple I only look at the head commit and parse the bugzid. 
		void UpdateFogBugz(ReceivePack rp, ICollection<ReceiveCommand> commands) {
			using (var gitRepo = new GitSharp.Repository(repository.FullPath))
			{
				var commit = gitRepo.Head.CurrentCommit;

				if (commit != null && commit.Message != null && commit.Message.ToLower().Contains("bugzid:"))
				{
					var bugzid = ParseBugzId(commit.Message);

					var hashBase = commit.Hash;

					foreach (var change in commit.Changes)
					{
						if (change == null)
						{
							continue;
						}

						var fileName = change.Name;

						var fileOldSha = string.Empty;
						if (change.ReferenceObject != null)
						{
							fileOldSha = change.ReferenceObject.Hash;
						}

						var fileNewSha = string.Empty;
						if (change.ChangedObject != null)
						{
							fileNewSha = change.ChangedObject.Hash;
						}

						var hashBaseParent = string.Empty;
						if (change.ReferenceCommit != null)
						{
							hashBaseParent = change.ReferenceCommit.Hash;
						}

						SubmitData(fileOldSha, hashBaseParent, fileNewSha, hashBase, bugzid, fileName, repository.Name);
					}
				}
			}
		}

		void SubmitData(string fileOldSha, string hashBaseParent, string fileNewSha, string hashBase, string bugId, string fileName, string repositoryName) {
			var r1 = string.Format("hp={0};hpb={1}", fileOldSha, hashBaseParent);
			var r2 = string.Format("h={0};hb={1}", fileNewSha, hashBase);

			var postUri = string.Format("{0}?ixBug={1}&sFile={2}&sPrev={3}&sNew={4}&sRepo={5}", fogBugzApi, bugId, fileName, r1, r2, repositoryName);

			var myRequest = WebRequest.Create(postUri);

			myRequest.BeginGetResponse(null, null);
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