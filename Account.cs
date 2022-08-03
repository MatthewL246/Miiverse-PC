﻿using System.Net;
using System.Xml;

namespace Miiverse_PC
{
    /// <summary>Represents a Pretendo account or PNID.</summary>
    internal sealed class Account
    {
        /// <summary>The hard-coded client ID used by the Wii U.</summary>
        private const string clientId = "a2efa818a34fa16b8afbc8a74eba3eda";

        /// <summary>The hard-coded client secret used by the Wii U.</summary>
        private const string clientSecret = "c91cdb5658bd4954ade78533a339cf9a";

        /// <summary>
        ///   The title ID of the Wii U Miiverse applet in the US.
        /// </summary>
        private const string miiverseTitleId = "000500301001610A";

        private static readonly HttpClient client = new();

        /// <summary>
        ///   Initializes a new instance of the <see cref="Account" /> class.
        /// </summary>
        /// <param name="username">
        ///   The PNID's username: <see cref="PnidUsername" />
        /// </param>
        /// <param name="passwordHash">
        ///   The PNID's password hash: <see cref="PnidPasswordHash" />
        /// </param>
        public Account(string username, string passwordHash)
        {
            PnidUsername = username;
            PnidPasswordHash = passwordHash;
        }

        /// <summary>
        ///   The protocol and domain or IP address of the account server that
        ///   will be sent OAuth 2.0 login requests.
        /// </summary>
        public string AccountServer { get; set; } = "https://account.pretendo.cc";

        /// <summary>
        ///   Is true if account access token, Miiverse service token, and
        ///   Miiverse portal host are all not null. Is false otherwise.
        /// </summary>
        public bool IsSignedIn => !(OauthToken is null || MiiverseToken is null || MiiversePortalHost is null);

        /// <summary>
        ///   The protocol and domain or IP address of the Miiverse discovery
        ///   server, which responds with the Miiverse portal host.
        /// </summary>
        public string MiiverseDiscoveryServer { get; set; } = "https://discovery.olv.pretendo.cc";

        /// <summary>
        ///   The Miiverse Wii U portal host sent by the discovery server.
        /// </summary>
        public string? MiiversePortalHost { get; private set; }

        /// <summary>
        ///   The Miiverse service token returned by the account server.
        /// </summary>
        public string? MiiverseToken { get; private set; }

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
        ///   Creates a Pretendo account service token for the Miiverse title
        ///   asynchronously using the previously-created OAuth access token.
        /// </summary>
        /// <returns>
        ///   A task object representing a string with a formatted error message
        ///   based on the server response.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> CreateMiiverseTokenAsync()
        {
            if (OauthToken is null)
            {
                throw new InvalidOperationException("The account OAuth 2.0 access token does not exist.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, AccountServer + "/v1/api/provider/service_token/@me")
            {
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "Authorization", "Bearer " + OauthToken },
                    { "X-Nintendo-Client-ID", clientId },
                    { "X-Nintendo-Client-Secret", clientSecret },
                    { "X-Nintendo-Title-ID", miiverseTitleId }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            string xmlResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResponse);
            MiiverseToken = xmlDocument.GetElementsByTagName("token")[0]?.InnerText;

            return GenerateErrorMessage("Miiverse service token creation", response.StatusCode, xmlDocument);
        }

        /// <summary>
        ///   Creates a Pretendo account OAuth 2.0 access token asynchronously.
        /// </summary>
        /// <returns>
        ///   A task object representing a string with a formatted error message
        ///   based on the server response.
        /// </returns>
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> CreateOauth2TokenAsync()
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
            OauthToken = xmlDocument.GetElementsByTagName("token")[0]?.InnerText;

            return GenerateErrorMessage("account access token creation", response.StatusCode, xmlDocument);
        }

        /// <summary>
        ///   Gets the Miiverse portal host from the discovery server
        ///   asynchronously.
        /// </summary>
        /// <returns>
        ///   A task object representing a string with a formatted error message
        ///   based on the server response.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> GetMiiversePortalHostAsync()
        {
            if (MiiverseToken is null)
            {
                throw new InvalidOperationException("The Miiverse service token does not exist.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, MiiverseDiscoveryServer + "/v1/endpoint")
            {
                Headers =
                {
                    {"Host", "discovery.olv.pretendo.cc" },
                    {"X-Nintendo-ServiceToken", MiiverseToken }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            string xmlResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResponse);
            MiiversePortalHost = xmlDocument.GetElementsByTagName("portal_host")[0]?.InnerText;

            return GenerateErrorMessage("Miiverse portal discovery", response.StatusCode, xmlDocument);
        }

        /// <summary>
        ///   Gets a Pretendo user's profile XML data asynchronously.
        /// </summary>
        /// <returns>
        ///   A task object with a string containing the XML user profile data.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        public async Task<string> GetUserProfileXmlAsync()
        {
            if (OauthToken is null)
            {
                throw new InvalidOperationException("The account OAuth 2.0 access token does not exist.");
            }
            var request = new HttpRequestMessage(HttpMethod.Get, AccountServer + "/v1/api/people/@me/profile")
            {
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "Authorization", "Bearer " + OauthToken },
                    { "X-Nintendo-Client-ID", clientId },
                    { "X-Nintendo-Client-Secret", clientSecret }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>Generates a nicely-formatted error message.</summary>
        /// <param name="requestName">
        ///   The name of the request that is shown in the error message.
        /// </param>
        /// <param name="httpStatus">
        ///   The HTTP status code of the request.
        /// </param>
        /// <param name="responseXmlDocument">
        ///   The XML document of the error response.
        /// </param>
        /// <returns>A formatted error message string.</returns>
        private static string GenerateErrorMessage(string requestName, HttpStatusCode httpStatus, XmlDocument responseXmlDocument)
        {
            string? errorCode = responseXmlDocument.GetElementsByTagName("code")[0]?.InnerText;
            string? errorMessage = responseXmlDocument.GetElementsByTagName("message")[0]?.InnerText;

            return $"Error in the {requestName} request.\n" +
                $"HTTP error code: {(int)httpStatus} {httpStatus}\n" +
                $"Server error code: {errorCode}\n" +
                $"Server error message: {errorMessage}";
        }
    }
}
