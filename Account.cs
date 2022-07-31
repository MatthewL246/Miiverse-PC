﻿using System.Xml;

namespace Miiverse_PC
{
    /// <summary>Represents a Pretendo account or PNID.</summary>
    internal sealed class Account
    {
        /// <summary>The hard-coded client ID used by the Wii U.</summary>
        private const string clientId = "a2efa818a34fa16b8afbc8a74eba3eda";

        /// <summary>The hard-coded client secret used by the Wii U.</summary>
        private const string clientSecret = "c91cdb5658bd4954ade78533a339cf9a";

        private static readonly HttpClient client = new();

        /// <summary>
        ///   Initializes a new instance of the <see cref="Account" /> class.
        /// </summary>
        /// <param name="accountServer">
        ///   The account server: <see cref="AccountServer" />
        /// </param>
        /// <param name="username">
        ///   The PNID's username: <see cref="PnidUsername" />
        /// </param>
        /// <param name="passwordHash">
        ///   The PNID's password hash: <see cref="PnidPasswordHash" />
        /// </param>
        public Account(string accountServer, string username, string passwordHash)
        {
            if (!accountServer.StartsWith("http"))
            {
                // The account server address must start with "http(s)://"
                accountServer = "https://" + accountServer;
            }
            AccountServer = accountServer;
            PnidUsername = username;
            PnidPasswordHash = passwordHash;
        }

        /// <summary>
        ///   The protocol and domain or IP address of the account server that
        ///   will be sent OAuth 2.0 login requests.
        /// </summary>
        public string AccountServer { get; }

        /// <summary>
        ///   The OAuth 2.0 access token returned by the account server.
        /// </summary>
        public string? OauthToken { get; private set; }

        /// <summary>
        ///   The password hash of the PNID (generated by the console using a
        ///   special hashing algorithm).
        /// </summary>
        public string PnidPasswordHash { get; }

        /// <summary>The username of the PNID.</summary>
        public string PnidUsername { get; }

        /// <summary>
        ///   Creates a Pretendo account OAuth 2.0 access token asynchronously.
        /// </summary>
        /// <returns>A Task tracking the status of the request</returns>
        public async Task CreateOauth2TokenAsync()
        {
            var requestValues = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "user_id", PnidUsername },
                { "password", PnidPasswordHash },
                { "password_type", "hash" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, AccountServer + "/v1/api/oauth20/access_token/generate")
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
            OauthToken = tokenNode[0].InnerText;
        }
    }
}
