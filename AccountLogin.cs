using System.Xml;

namespace Miiverse_PC
{
    /// <summary>
    ///   Static methods used for logging in to Pretendo accounts.
    /// </summary>
    internal static class AccountLogin
    {
        /// <summary>The hard-coded client ID used by the Wii U.</summary>
        private const string clientId = "a2efa818a34fa16b8afbc8a74eba3eda";

        /// <summary>The hard-coded client secret used by the Wii U.</summary>
        private const string clientSecret = "c91cdb5658bd4954ade78533a339cf9a";

        private static readonly HttpClient client = new();

        /// <summary>
        ///   Gets a Pretendo account OAuth 2.0 access token asynchronously.
        /// </summary>
        /// <param name="pnid">The account's PNID.</param>
        /// <param name="passwordHash">The account's password hash.</param>
        /// <returns>
        ///   A Task with the access token as a base64-encoded string.
        /// </returns>
        internal static async Task<string> GetOauth2TokenAsync(string pnid, string passwordHash)
        {
            var requestValues = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "user_id", pnid },
                { "password", passwordHash },
                { "password_type", "hash" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://account.pretendo.cc/v1/api/oauth20/access_token/generate")
            {
                Content = new FormUrlEncodedContent(requestValues),
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "X-Nintendo-Client-ID", clientId },
                    { "X-Nintendo-Client-Secret", clientSecret }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            string xmlResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResponse);
            var tokenNode = xmlDocument.GetElementsByTagName("token");
            return tokenNode[0].InnerText;
        }
    }
}
