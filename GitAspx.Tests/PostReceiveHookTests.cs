namespace GitAspx.Tests
{
	using GitAspx.Lib;
	using NUnit.Framework;

	[TestFixture]
	public class PostReceiveHookTests
	{
		[Test]
		public void ParseBugzId() {
			var postRecieveHook = new PostReceiveHook(string.Empty, null);
			var bugId = postRecieveHook.ParseBugzId(@"my test commit blah blah
BugzId: 712712");
			Assert.That(bugId == "712712");
		}

		[Test]
		public void ParseBugzIdNoSpace() {
			var postRecieveHook = new PostReceiveHook(string.Empty, null);
			var bugId = postRecieveHook.ParseBugzId(@"my test commit blah blah
BugzId:712712");
			Assert.That(bugId == "712712");
		}

		[Test]
		public void ParseBugzIdNoSpaceNoReturn() {
			var postRecieveHook = new PostReceiveHook(string.Empty, null);
			var bugId = postRecieveHook.ParseBugzId(@"my test commit blah blah BugzId:712712");
			Assert.That(bugId == "712712");
		}

		[Test]
		public void ParseBugzIdNoSpaceNoReturnUpperCase() {
			var postRecieveHook = new PostReceiveHook(string.Empty, null);
			var bugId = postRecieveHook.ParseBugzId(@"my test commit blah blah BUGZID:712712");
			Assert.That(bugId == "712712");
		}

		[Test]
		public void ParseBugzIdNoId() {
			var postRecieveHook = new PostReceiveHook(string.Empty, null);
			var bugId = postRecieveHook.ParseBugzId(@"my test commit blah blah");
			Assert.That(bugId == string.Empty);
		}
	}
}
