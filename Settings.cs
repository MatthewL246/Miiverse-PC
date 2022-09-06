namespace Miiverse_PC
{
    /// <summary>A data record class representing user settings.</summary>
    internal record class Settings()
    {
        /// <summary>
        ///   The protocol and domain or IP address of the Nintendo/Pretendo
        ///   account server that will be sent OAuth 2.0 login requests and
        ///   Miiverse service token requests. Default: Pretendo official server.
        /// </summary>

        public string AccountServer { get; init; } = "https://account.pretendo.cc";

        /// <summary>
        ///   The protocol and domain or IP address of the Miiverse discovery
        ///   server, which responds with the Miiverse portal host. Default:
        ///   Pretendo official server.
        /// </summary>

        public string DiscoveryServer { get; init; } = "https://discovery.olv.pretendo.cc";

        /// <summary>
        ///   The Miiverse portal host, which may override the one sent by the
        ///   discovery server. Default: null (use the discovery server).
        /// </summary>
        public string? PortalServer { get; set; }

        /// <summary>
        ///   The language ID sent in the header when loading the Miiverse
        ///   portal in the browser, used for localization. Default: English.
        /// </summary>
        public LanguageId Language { get; init; } = LanguageId.English;

        /// <summary>
        ///   The country ID sent in the header when loading the Miiverse portal
        ///   in the browser, currently unused. Default: English.
        /// </summary>
        public CountryId Country { get; init; } = CountryId.UnitedStates;

        /// <summary>
        ///   The platform ID used to determine which console portal to use, Wii
        ///   U or 3DS. Default: Wii U.
        /// </summary>
        public PlatformId Platform { get; init; } = PlatformId.WiiU;

        /// <summary>
        ///   The client ID used by the console for account requests. Default:
        ///   the Wii U's client ID.
        /// </summary>

        public string ConsoleClientId { get; init; } = "a2efa818a34fa16b8afbc8a74eba3eda";

        /// <summary>
        ///   The client secret used by the console for account requests.
        ///   Default: the Wii U's client secret.
        /// </summary>

        public string ConsoleClientSecret { get; init; } = "c91cdb5658bd4954ade78533a339cf9a";

        /// <summary>
        ///   The title ID of the Miiverse applet. Default: the Wii U Miiverse
        ///   applet in the US.
        /// </summary>

        public string MiiverseTitleId { get; init; } = "000500301001610A";

        /// <summary>
        ///   The thumbprint of the account server's root certificate hash,
        ///   which allows connecting to servers with self-signed certificates.
        ///   Default: the certificate hash of my personal server.
        /// </summary>
        public string AllowedServerRootCertificateHash { get; init; } = "209F918F628347868A559F52B68D007B6DD4554F";
    }
}
