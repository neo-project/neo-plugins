using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.API.Acl;
using Neo.FileStorage.API.Session;
using static Neo.FileStorage.Storage.Services.Object.Util.Helper;
using static Neo.FileStorage.Storage.Tests.Helper;

namespace Neo.FileStorage.Tests.Services.Object.Acl
{
    [TestClass]
    public class UT_Helper
    {
        private RequestMetaHeader GenerateMetaHeader(int depth, SessionToken session, BearerToken bearer)
        {
            RequestMetaHeader meta = new();
            meta.SessionToken = session;
            meta.BearerToken = bearer;
            for (int i = 0; i < depth; i++)
            {
                var link = meta;
                meta = new();
                meta.Origin = link;
            }
            return meta;
        }

        [TestMethod]
        public void TestOriginTokens()
        {
            var session = RandomSessionToken();
            var bearer = RandomBearerToken();
            for (int i = 0; i < 100; i++)
            {
                var meta = GenerateMetaHeader(i, session, bearer);
                Assert.AreEqual(session, OriginalSessionToken(meta));
                Assert.AreEqual(bearer, OriginalBearerToken(meta));
            }
        }
    }
}
