﻿using System.Net;
using System.Text;
using System.Xml;

namespace Miiverse_PC
{
    /// <summary>Represents a Pretendo account or PNID.</summary>
    internal sealed class Account
    {
        /// <summary>
        ///   The account server's self-signed certificate hash.
        /// </summary>
        private const string accountServerCertificateHash = "4C437B566E7FA361A55E1F007AA5FAAA6FA8FB68";

        /// <summary>The hard-coded client ID used by the Wii U.</summary>
        private const string clientId = "a2efa818a34fa16b8afbc8a74eba3eda";

        /// <summary>The hard-coded client secret used by the Wii U.</summary>
        private const string clientSecret = "c91cdb5658bd4954ade78533a339cf9a";

        /// <summary>
        ///   The discovery server's self-signed certificate hash.
        /// </summary>
        private const string discoveryServerCertificateHash = "25215120B7E6FD592ECA3598FB51DDD3A3E3A280";

        /// <summary>
        ///   The title ID of the Wii U Miiverse applet in the US.
        /// </summary>
        private const string miiverseTitleId = "000500301001610A";

        private static readonly HttpClient client = new(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) =>
            {
                // Returns true if the certificate is valid or if it is invalid
                // and its hash matches an expected hash
                return errors == System.Net.Security.SslPolicyErrors.None
                    || (certificate is not null
                    && (certificate.GetCertHashString() == accountServerCertificateHash
                    || certificate.GetCertHashString() == discoveryServerCertificateHash));
            }
        });

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
        ///   Is true if account access token, Miiverse service token, Miiverse
        ///   portal host, and ParamPack data all exist. Is false otherwise.
        /// </summary>
        public bool IsSignedIn => !(OauthToken is null || MiiverseToken is null || MiiversePortalServer is null || ParamPackData is null);

        /// <summary>
        ///   The protocol and domain or IP address of the Miiverse discovery
        ///   server, which responds with the Miiverse portal host.
        /// </summary>
        public string MiiverseDiscoveryServer { get; set; } = "https://discovery.olv.pretendo.cc";

        /// <summary>
        ///   The Miiverse Wii U portal host sent by the discovery server.
        /// </summary>
        public string? MiiversePortalServer { get; set; }

        /// <summary>
        ///   The Miiverse service token returned by the account server.
        /// </summary>
        public string? MiiverseToken { get; private set; }

        /// <summary>
        ///   The OAuth 2.0 access token returned by the account server.
        /// </summary>
        public string? OauthToken { get; private set; }

        /// <summary>
        ///   The data used in the X-Nintendo-ParamPack header (base64-encoded).
        /// </summary>
        public string? ParamPackData { get; private set; }

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

        /// <summary>Creates the data for the ParamPack header.</summary>
        /// <param name="languageId">The language ID.</param>
        /// <param name="countryId">The country ID.</param>
        /// <param name="platformId">The platform ID (3DS or Wii U).</param>
        /// <exception cref="InvalidOperationException" />
        public void CreateParamPack(LanguageId languageId, CountryId countryId, PlatformId platformId)
        {
            var regionId = countryId switch
            {
                CountryId.Japan => RegionId.Japan, // Japan only
                >= CountryId.Anguilla and <= CountryId.Venezuela => RegionId.America, // Countries that count as "America"
                >= CountryId.Albania and <= CountryId.Jordan => RegionId.Europe, // All other countries that count as "Europe"
                _ => throw new InvalidOperationException("Invalid country ID"),
            };

            TitleId titleId;
            if (platformId == PlatformId.WiiU)
            {
                titleId = regionId switch
                {
                    RegionId.Japan => TitleId.JapanMenu,
                    RegionId.America => TitleId.AmericaMenu,
                    RegionId.Europe => TitleId.EuropeMenu,
                    _ => TitleId.Unknown,
                };
            }
            else
            {
                // 3DS seems to always send a title ID of 0xFFFFFFFFFFFFFFFF
                titleId = TitleId.Unknown;
            }

            var paramPackValues = new Dictionary<string, string>
            {
                { "title_id", titleId.ToString("d") },
                { "platform_id", platformId.ToString("d") },
                { "region_id", regionId.ToString("d") },
                { "language_id", languageId.ToString("d") },
                { "country_id", countryId.ToString("d") }
            };

            // The ParamPack data is formatted as "\name\value\" for each pair
            // Maximum size with current parameters is about 100
            StringBuilder paramPackBuilder = new('\\', 128);
            foreach (var item in paramPackValues)
            {
                paramPackBuilder.Append(item.Key).Append('\\').Append(item.Value).Append('\\');
            }

            byte[] paramPackBytes = Encoding.UTF8.GetBytes(paramPackBuilder.ToString());
            ParamPackData = Convert.ToBase64String(paramPackBytes);
        }

        /// <summary>
        ///   Gets the Miiverse portal server from the discovery server
        ///   asynchronously.
        /// </summary>
        /// <param name="platform">
        ///   The <see cref="PlatformId" /> of the target portal server.
        /// </param>
        /// <returns>
        ///   A task object representing a string with a formatted error message
        ///   based on the server response.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> GetMiiversePortalServerAsync(PlatformId platform)
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

            string portalHostTag = platform == PlatformId.ThreeDS ? "n3ds_host" : "portal_host";
            MiiversePortalServer = "https://" + xmlDocument.GetElementsByTagName(portalHostTag)[0]?.InnerText;

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
