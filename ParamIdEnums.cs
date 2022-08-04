namespace Miiverse_PC
{
    /// <summary>The country IDs used by Miiverse.</summary>
    // See https://wiibrew.org/wiki/Country_Codes
    internal enum CountryId
    {
        Japan = 1,

        Anguilla = 8,

        AntiguaAndBarbuda = 9,

        Argentina = 10,

        Aruba = 11,

        Bahamas = 12,

        Barbados = 13,

        Belize = 14,

        Bolivia = 15,

        Brazil = 16,

        BritishVirginIslands = 17,

        Canada = 18,

        CaymanIslands = 19,

        Chile = 20,

        Colombia = 21,

        CostaRica = 22,

        Dominica = 23,

        DominicanRepublic = 24,

        Ecuador = 25,

        ElSalvador = 26,

        FrenchGuiana = 27,

        Grenada = 28,

        Guadeloupe = 29,

        Guatemala = 30,

        Guyana = 31,

        Haiti = 32,

        Honduras = 33,

        Jamaica = 34,

        Martinique = 35,

        Mexico = 36,

        Montserrat = 37,

        NetherlandsAntilles = 38,

        Nicaragua = 39,

        Panama = 40,

        Paraguay = 41,

        Peru = 42,

        StKittsandNevis = 43,

        StLucia = 44,

        StVincentandtheGrenadines = 45,

        Suriname = 46,

        TrinidadandTobago = 47,

        TurksandCaicosIslands = 48,

        UnitedStates = 49,

        Uruguay = 50,

        USVirginIslands = 51,

        Venezuela = 52,

        Albania = 64,

        Australia = 65,

        Austria = 66,

        Belgium = 67,

        BosniaandHerzegovina = 68,

        Botswana = 69,

        Bulgaria = 70,

        Croatia = 71,

        Cyprus = 72,

        CzechRepublic = 73,

        Denmark = 74,

        Estonia = 75,

        Finland = 76,

        France = 77,

        Germany = 78,

        Greece = 79,

        Hungary = 80,

        Iceland = 81,

        Ireland = 82,

        Italy = 83,

        Latvia = 84,

        Lesotho = 85,

        Lichtenstein = 86,

        Lithuania = 87,

        Luxembourg = 88,

        FYRofMacedonia = 89,

        Malta = 90,

        Montenegro = 91,

        Mozambique = 92,

        Namibia = 93,

        Netherlands = 94,

        NewZealand = 95,

        Norway = 96,

        Poland = 97,

        Portugal = 98,

        Romania = 99,

        Russia = 100,

        Serbia = 101,

        Slovakia = 102,

        Slovenia = 103,

        SouthAfrica = 104,

        Spain = 105,

        Swaziland = 106,

        Sweden = 107,

        Switzerland = 108,

        Turkey = 109,

        UnitedKingdom = 110,

        Zambia = 111,

        Zimbabwe = 112,

        Azerbaijan = 113,

        IslamicRepublicofMauritania = 114,

        RepublicofMali = 115,

        RepublicofNiger = 116,

        RepublicofChad = 117,

        RepublicoftheSudan = 118,

        StateofEritrea = 119,

        RepublicofDjibouti = 120,

        SomaliRepublic = 121,

        Taiwan = 128,

        SouthKorea = 136,

        HongKong = 144,

        Macao = 145,

        Indonesia = 152,

        Singapore = 153,

        Thailand = 154,

        Philippines = 155,

        Malaysia = 156,

        China = 160,

        UAE = 168,

        India = 169,

        Egypt = 170,

        Oman = 171,

        Qatar = 172,

        Kuwait = 173,

        SaudiArabia = 174,

        Syria = 175,

        Bahrain = 176,

        Jordan = 177
    }

    /// <summary>The language IDs used by Miiverse.</summary>
    // See
    // https://github.com/PretendoNetwork/juxt-web/blob/main/src/util.js#L254
    internal enum LanguageId
    {
        Japanese,

        English,

        French,

        German,

        Italian,

        Spanish,

        Chinese,

        Korean,

        Dutch,

        Portuguese,

        Russian
    }

    /// <summary>The platform IDs of consoles.</summary>
    internal enum PlatformId
    {
        ThreeDS,

        WiiU
    }

    /// <summary>The region IDs used by Miiverse.</summary>
    internal enum RegionId
    {
        Japan = 0,

        America = 2,

        Europe = 4
    }

    /// <summary>The Wii U Menu title IDs for all regions.</summary>
    internal enum TitleId : ulong
    {
        JapanMenu = 0x0005001010040000,

        AmericaMenu = 0x0005001010040100,

        EuropeMenu = 0x0005001010040200,

        Unknown = 0xFFFFFFFFFFFFFFFF
    }
}
