﻿using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Miiverse_PC
{
    /// <summary>Represents a Pretendo account or PNID.</summary>
    internal sealed class Account
    {
        private readonly HttpClient client;

        /// <summary>The plaintext password of the PNID.</summary>
        private readonly string pnidPassword;

        /// <summary>The settings used by the account.</summary>
        private readonly Settings settings;

        /// <summary>
        ///   The OAuth 2.0 access token returned by the account server.
        /// </summary>
        private string? oauthToken;

        /// <summary>
        ///   Initializes a new instance of the <see cref="Account" /> class.
        /// </summary>
        /// <param name="username">The PNID's username: <see cref="PnidUsername" /></param>
        /// <param name="password">The PNID's password: <see cref="PnidPasswordHash" /></param>
        public Account(string username, string password, Settings accountSettings)
        {
            PnidUsername = username;
            pnidPassword = password;
            settings = accountSettings;
            MiiversePortalServer = settings.PortalServer;

            HttpClientHandler handler = new()
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) =>
                {
                    // Returns true if the certificate is valid or if it is
                    // invalid and its hash matches an expected hash
                    return errors == System.Net.Security.SslPolicyErrors.None
                        || chain?.ChainElements.Last().Certificate.GetCertHashString() == settings.AllowedServerRootCertificateHash;
                }
            };
            client = new(handler);
        }

        /// <summary>
        ///   Is true if account access token, Miiverse service token, Miiverse
        ///   portal host, and ParamPack data all exist. Is false otherwise.
        /// </summary>
        public bool IsSignedIn => !(oauthToken is null || MiiverseToken is null || MiiversePortalServer is null || ParamPackData is null);

        /// <summary>
        ///   The Miiverse portal server retrieved from the discovery server,
        ///   may be overridden by settings.
        /// </summary>
        public string MiiversePortalServer { get; private set; }

        /// <summary>
        ///   The Miiverse service token returned by the account server.
        /// </summary>
        public string? MiiverseToken { get; private set; }

        /// <summary>Is true if the OAuth access token is null.</summary>
        public bool OauthTokenIsNull => oauthToken is null;

        /// <summary>The data used in the X-Nintendo-ParamPack header (base64-encoded).</summary>
        public string? ParamPackData { get; private set; }

        /// <summary>
        ///   The password hash of the PNID (generated by the console using a
        ///   special hashing algorithm).
        /// </summary>
        public string? PnidPasswordHash { get; private set; }

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
            if (oauthToken is null)
            {
                throw new InvalidOperationException("The account OAuth 2.0 access token does not exist.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, settings.AccountServer + "/v1/api/provider/service_token/@me")
            {
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "Authorization", "Bearer " + oauthToken },
                    { "X-Nintendo-Client-ID", settings.ConsoleClientId },
                    { "X-Nintendo-Client-Secret", settings.ConsoleClientSecret },
                    { "X-Nintendo-Title-ID", settings.MiiverseTitleId }
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
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> CreateOauth2TokenAsync()
        {
            if (PnidPasswordHash is null)
            {
                throw new InvalidOperationException("The PNID password hash does not exist.");
            }

            var requestValues = new Dictionary<string, string>(4)
            {
                { "grant_type", "password" },
                { "user_id", PnidUsername },
                { "password", PnidPasswordHash },
                { "password_type", "hash" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, settings.AccountServer + "/v1/api/oauth20/access_token/generate")
            {
                Content = new FormUrlEncodedContent(requestValues),
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "X-Nintendo-Client-ID", settings.ConsoleClientId },
                    { "X-Nintendo-Client-Secret", settings.ConsoleClientSecret}
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            string xmlResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResponse);
            oauthToken = xmlDocument.GetElementsByTagName("token")[0]?.InnerText;

            return GenerateErrorMessage("account access token creation", response.StatusCode, xmlDocument);
        }

        /// <summary>Creates the data for the ParamPack header.</summary>
        public void CreateParamPack()
        {
            var regionId = settings.Country switch
            {
                CountryId.Japan => RegionId.Japan, // Japan only
                >= CountryId.Anguilla and <= CountryId.Venezuela => RegionId.America, // Countries that count as "America"
                _ => RegionId.Europe, // All other countries that count as "Europe"
            };

            TitleId titleId;
            if (settings.Platform == PlatformId.WiiU)
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

            var paramPackValues = new Dictionary<string, string>(5)
            {
                { "title_id", ((ulong)titleId).ToString() },
                { "platform_id", ((int)settings.Platform).ToString() },
                { "region_id", ((int)regionId).ToString() },
                { "language_id", ((int)settings.Language).ToString() },
                { "country_id", ((int)settings.Country).ToString() }
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
        ///   Gets the Miiverse portal server from the discovery server asynchronously.
        /// </summary>
        /// <returns>
        ///   A task object representing a string with a formatted error message
        ///   based on the server response.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task<string> GetMiiversePortalServerAsync()
        {
            if (MiiverseToken is null)
            {
                throw new InvalidOperationException("The Miiverse service token does not exist.");
            }
            if (!settings.IsDiscoveryServerUsed)
            {
                // The portal server was already set by the settings
                return $"Portal server already set to {MiiversePortalServer}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, settings.DiscoveryServer + "/v1/endpoint")
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

            string portalHostTag = settings.Platform == PlatformId.ThreeDS ? "n3ds_host" : "portal_host";
            MiiversePortalServer = "https://" + xmlDocument.GetElementsByTagName(portalHostTag)[0]?.InnerText;

            return GenerateErrorMessage("Miiverse portal discovery", response.StatusCode, xmlDocument);
        }

        /// <summary>Gets a Pretendo user's profile XML data asynchronously.</summary>
        /// <returns>
        ///   A task object with a string containing the XML user profile data.
        /// </returns>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="HttpRequestException" />
        public async Task<string> GetUserProfileXmlAsync()
        {
            if (oauthToken is null)
            {
                throw new InvalidOperationException("The account OAuth 2.0 access token does not exist.");
            }
            var request = new HttpRequestMessage(HttpMethod.Get, settings.AccountServer + "/v1/api/people/@me/profile")
            {
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "Authorization", "Bearer " + oauthToken },
                    { "X-Nintendo-Client-ID", settings.ConsoleClientId },
                    { "X-Nintendo-Client-Secret", settings.ConsoleClientSecret }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///   Checks if the current PNID's username exists and is linked to a
        ///   PID, then hashes the current PNID's password using the
        ///   <see cref="NintendoPasswordHash" /> algorithm asynchronously.
        /// </summary>
        /// <returns>An empty Task object.</returns>
        /// <exception cref="HttpRequestException" />
        /// <exception cref="XmlException" />
        public async Task HashPnidPasswordAsync()
        {
            if (pnidPassword.Length == 64)
            {
                // Assume the password is already a hash if it is 64 characters
                PnidPasswordHash = pnidPassword;
                return;
            }

            string urlQuery = $"?input={PnidUsername}&input_type=user_id&output_type=pid";
            var request = new HttpRequestMessage(HttpMethod.Get, settings.AccountServer + "/v1/api/admin/mapped_ids" + urlQuery)
            {
                Headers =
                {
                    { "Host", "account.pretendo.cc" },
                    { "X-Nintendo-Client-ID", settings.ConsoleClientId },
                    { "X-Nintendo-Client-Secret", settings.ConsoleClientSecret }
                }
            };

            var response = await client.SendAsync(request).ConfigureAwait(false);
            string xmlResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResponse);

            string? pidElement = xmlDocument.GetElementsByTagName("out_id")[0]?.InnerText;
            bool parseSuccess = uint.TryParse(pidElement, out uint pid);

            if (parseSuccess)
            {
                PnidPasswordHash = NintendoPasswordHash(pid, pnidPassword);
            }
        }

        /// <summary>Generates a nicely-formatted error message.</summary>
        /// <param name="requestName">
        ///   The name of the request that is shown in the error message.
        /// </param>
        /// <param name="httpStatus">The HTTP status code of the request.</param>
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

        /// <summary>
        ///   Computes a password hash using the same algorithm that Nintendo
        ///   consoles and servers use. Requires a PID as well as the password.
        /// </summary>
        /// <param name="pid">The PID of the account that is using the password.</param>
        /// <param name="password">The password of the account.</param>
        /// <returns>The computed hash as a string in hex format.</returns>
        private static string NintendoPasswordHash(uint pid, string password)
        {
            // See https://github.com/PretendoNetwork/account/blob/957fbaa395db994b3c23bd01fd2cd953031f7f5d/src/util.js#L14
            byte[] pidBytes = BitConverter.GetBytes(pid);
            byte[] magicBytes = new byte[] { 0x02, 0x65, 0x43, 0x46 };
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            byte[] hashBytes = pidBytes.Concat(magicBytes).Concat(passwordBytes).ToArray();
            var hash = SHA256.Create();
            hash.ComputeHash(hashBytes);

            // Hash has been computed, return as hex string
            return Convert.ToHexString(hash.Hash!).ToLower();
        }
    }
}
