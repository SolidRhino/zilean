namespace Zilean.Shared.Features.Torznab.Categories;

/// <summary>
/// Provides the standard Newznab-compatible Torznab category constants and helpers.
/// </summary>
public static class TorznabCategoryTypes
{
    /// <summary>
    /// Console parent category (1000) with all console sub-categories.
    /// </summary>
    public static TorznabCategory Console => new(1000, "Console")
    {
        SubCategories =
        {
            ConsoleNDS,
            ConsolePSP,
            ConsoleWii,
            ConsoleXBox,
            ConsoleXBox360,
            ConsoleWiiware,
            ConsoleXBox360Dlc,
            ConsolePs3,
            ConsoleOther,
            Console3DS,
            ConsolePSVita,
            ConsoleWiiU,
            ConsoleXBoxOne,
            ConsolePS4
        }
    };

    /// <summary>
    /// Movies parent category (2000) with all movie sub-categories.
    /// </summary>
    public static TorznabCategory Movies => new(2000, "Movies")
    {
        SubCategories =
        {
            MoviesForeign,
            MoviesOther,
            MoviesSD,
            MoviesHD,
            MoviesUHD,
            MoviesBluRay,
            Movies3D,
            MoviesDVD,
            MoviesWEBDL
        }
    };

    /// <summary>
    /// Audio parent category (3000) with all audio sub-categories.
    /// </summary>
    public static TorznabCategory Audio => new(3000, "Audio")
    {
        SubCategories =
        {
            AudioMp3,
            AudioVideo,
            AudioAudiobook,
            AudioLossless,
            AudioOther,
            AudioForeign
        }
    };

    /// <summary>
    /// PC parent category (4000) with all PC sub-categories.
    /// </summary>
    public static TorznabCategory PC => new(4000, "PC")
    {
        SubCategories =
        {
            PC0day,
            PCISO,
            PCMac,
            PCMobileOther,
            PCGames,
            PCMobileiOS,
            PCMobileAndroid
        }
    };

    /// <summary>
    /// TV parent category (5000) with all TV sub-categories.
    /// </summary>
    public static TorznabCategory TV => new(5000, "TV")
    {
        SubCategories =
        {
            TVWEBDL,
            TVForeign,
            TVSD,
            TVHD,
            TVUHD,
            TVOther,
            TVSport,
            TVAnime,
            TVDocumentary
        }
    };

    /// <summary>
    /// XXX parent category (6000) with all adult sub-categories.
    /// </summary>
    public static TorznabCategory XXX => new(6000, "XXX")
    {
        SubCategories =
        {
            XXXDVD,
            XXXXviD,
            XXXx264,
            XXXUHD,
            XXXPack,
            XXXImageSet,
            XXXOther,
            XXXSD,
            XXXWEBDL
        }
    };

    /// <summary>
    /// Books parent category (7000) with all book sub-categories.
    /// </summary>
    public static TorznabCategory Books => new(7000, "Books")
    {
        SubCategories =
        {
            BooksMags,
            BooksEBook,
            BooksComics,
            BooksTechnical,
            BooksOther,
            BooksForeign
        }
    };

    /// <summary>
    /// Other parent category (8000) with all misc sub-categories.
    /// </summary>
    public static TorznabCategory Other => new(8000, "Other")
    {
        SubCategories =
        {
            OtherMisc,
            OtherHashed
        }
    };

    /// <summary>
    /// All parent (top-level) Torznab categories.
    /// </summary>
    public static TorznabCategory[] ParentCats =>
    [
        Console,
        Movies,
        Audio,
        PC,
        TV,
        XXX,
        Books,
        Other
    ];

    /// <summary>
    /// All Torznab categories (parent and sub-categories) flattened into a single array.
    /// </summary>
    public static TorznabCategory[] AllCats =>
    [
        Console,
        ConsoleNDS,
        ConsolePSP,
        ConsoleWii,
        ConsoleXBox,
        ConsoleXBox360,
        ConsoleWiiware,
        ConsoleXBox360Dlc,
        ConsolePs3,
        ConsoleOther,
        Console3DS,
        ConsolePSVita,
        ConsoleWiiU,
        ConsoleXBoxOne,
        ConsolePS4,
        Movies,
        MoviesForeign,
        MoviesOther,
        MoviesSD,
        MoviesHD,
        MoviesUHD,
        MoviesBluRay,
        Movies3D,
        MoviesDVD,
        MoviesWEBDL,
        Audio,
        AudioMp3,
        AudioVideo,
        AudioAudiobook,
        AudioLossless,
        AudioOther,
        AudioForeign,
        PC,
        PC0day,
        PCISO,
        PCMac,
        PCMobileOther,
        PCGames,
        PCMobileiOS,
        PCMobileAndroid,
        TV,
        TVWEBDL,
        TVForeign,
        TVSD,
        TVHD,
        TVUHD,
        TVOther,
        TVSport,
        TVAnime,
        TVDocumentary,
        XXX,
        XXXDVD,
        XXXWMV,
        XXXXviD,
        XXXx264,
        XXXUHD,
        XXXPack,
        XXXImageSet,
        XXXOther,
        XXXSD,
        XXXWEBDL,
        Books,
        BooksMags,
        BooksEBook,
        BooksComics,
        BooksTechnical,
        BooksOther,
        BooksForeign,
        Other,
        OtherMisc,
        OtherHashed
    ];

    /// <summary>
    /// Console/NDS sub-category (1010).
    /// </summary>
    public static TorznabCategory ConsoleNDS => new(1010, "Console/NDS");
    /// <summary>
    /// Console/PSP sub-category (1020).
    /// </summary>
    public static TorznabCategory ConsolePSP => new(1020, "Console/PSP");
    /// <summary>
    /// Console/Wii sub-category (1030).
    /// </summary>
    public static TorznabCategory ConsoleWii => new(1030, "Console/Wii");
    /// <summary>
    /// Console/XBox sub-category (1040).
    /// </summary>
    public static TorznabCategory ConsoleXBox => new(1040, "Console/XBox");
    /// <summary>
    /// Console/XBox 360 sub-category (1050).
    /// </summary>
    public static TorznabCategory ConsoleXBox360 => new(1050, "Console/XBox 360");
    /// <summary>
    /// Console/Wiiware sub-category (1060).
    /// </summary>
    public static TorznabCategory ConsoleWiiware => new(1060, "Console/Wiiware");
    /// <summary>
    /// Console/XBox 360 DLC sub-category (1070).
    /// </summary>
    public static TorznabCategory ConsoleXBox360Dlc => new(1070, "Console/XBox 360 DLC");
    /// <summary>
    /// Console/PS3 sub-category (1080).
    /// </summary>
    public static TorznabCategory ConsolePs3 => new(1080, "Console/PS3");
    /// <summary>
    /// Console/Other sub-category (1090).
    /// </summary>
    public static TorznabCategory ConsoleOther => new(1090, "Console/Other");
    /// <summary>
    /// Console/3DS sub-category (1110).
    /// </summary>
    public static TorznabCategory Console3DS => new(1110, "Console/3DS");
    /// <summary>
    /// Console/PS Vita sub-category (1120).
    /// </summary>
    public static TorznabCategory ConsolePSVita => new(1120, "Console/PS Vita");
    /// <summary>
    /// Console/WiiU sub-category (1130).
    /// </summary>
    public static TorznabCategory ConsoleWiiU => new(1130, "Console/WiiU");
    /// <summary>
    /// Console/XBox One sub-category (1140).
    /// </summary>
    public static TorznabCategory ConsoleXBoxOne => new(1140, "Console/XBox One");
    /// <summary>
    /// Console/PS4 sub-category (1180).
    /// </summary>
    public static TorznabCategory ConsolePS4 => new(1180, "Console/PS4");
    /// <summary>
    /// Movies/Foreign sub-category (2010).
    /// </summary>
    public static TorznabCategory MoviesForeign => new(2010, "Movies/Foreign");
    /// <summary>
    /// Movies/Other sub-category (2020).
    /// </summary>
    public static TorznabCategory MoviesOther => new(2020, "Movies/Other");
    /// <summary>
    /// Movies/SD sub-category (2030).
    /// </summary>
    public static TorznabCategory MoviesSD => new(2030, "Movies/SD");
    /// <summary>
    /// Movies/HD sub-category (2040).
    /// </summary>
    public static TorznabCategory MoviesHD => new(2040, "Movies/HD");
    /// <summary>
    /// Movies/UHD sub-category (2045).
    /// </summary>
    public static TorznabCategory MoviesUHD => new(2045, "Movies/UHD");
    /// <summary>
    /// Movies/BluRay sub-category (2050).
    /// </summary>
    public static TorznabCategory MoviesBluRay => new(2050, "Movies/BluRay");
    /// <summary>
    /// Movies/3D sub-category (2060).
    /// </summary>
    public static TorznabCategory Movies3D => new(2060, "Movies/3D");
    /// <summary>
    /// Movies/DVD sub-category (2070).
    /// </summary>
    public static TorznabCategory MoviesDVD => new(2070, "Movies/DVD");
    /// <summary>
    /// Movies/WEB-DL sub-category (2080).
    /// </summary>
    public static TorznabCategory MoviesWEBDL => new(2080, "Movies/WEB-DL");
    /// <summary>
    /// Audio/MP3 sub-category (3010).
    /// </summary>
    public static TorznabCategory AudioMp3 => new(3010, "Audio/MP3");
    /// <summary>
    /// Audio/Video sub-category (3020).
    /// </summary>
    public static TorznabCategory AudioVideo => new(3020, "Audio/Video");
    /// <summary>
    /// Audio/Audiobook sub-category (3030).
    /// </summary>
    public static TorznabCategory AudioAudiobook => new(3030, "Audio/Audiobook");
    /// <summary>
    /// Audio/Lossless sub-category (3040).
    /// </summary>
    public static TorznabCategory AudioLossless => new(3040, "Audio/Lossless");
    /// <summary>
    /// Audio/Other sub-category (3050).
    /// </summary>
    public static TorznabCategory AudioOther => new(3050, "Audio/Other");
    /// <summary>
    /// Audio/Foreign sub-category (3060).
    /// </summary>
    public static TorznabCategory AudioForeign => new(3060, "Audio/Foreign");
    /// <summary>
    /// PC/0day sub-category (4010).
    /// </summary>
    public static TorznabCategory PC0day => new(4010, "PC/0day");
    /// <summary>
    /// PC/ISO sub-category (4020).
    /// </summary>
    public static TorznabCategory PCISO => new(4020, "PC/ISO");
    /// <summary>
    /// PC/Mac sub-category (4030).
    /// </summary>
    public static TorznabCategory PCMac => new(4030, "PC/Mac");
    /// <summary>
    /// PC/Mobile-Other sub-category (4040).
    /// </summary>
    public static TorznabCategory PCMobileOther => new(4040, "PC/Mobile-Other");
    /// <summary>
    /// PC/Games sub-category (4050).
    /// </summary>
    public static TorznabCategory PCGames => new(4050, "PC/Games");
    /// <summary>
    /// PC/Mobile-iOS sub-category (4060).
    /// </summary>
    public static TorznabCategory PCMobileiOS => new(4060, "PC/Mobile-iOS");
    /// <summary>
    /// PC/Mobile-Android sub-category (4070).
    /// </summary>
    public static TorznabCategory PCMobileAndroid => new(4070, "PC/Mobile-Android");
    /// <summary>
    /// TV/WEB-DL sub-category (5010).
    /// </summary>
    public static TorznabCategory TVWEBDL => new(5010, "TV/WEB-DL");
    /// <summary>
    /// TV/Foreign sub-category (5020).
    /// </summary>
    public static TorznabCategory TVForeign => new(5020, "TV/Foreign");
    /// <summary>
    /// TV/SD sub-category (5030).
    /// </summary>
    public static TorznabCategory TVSD => new(5030, "TV/SD");
    /// <summary>
    /// TV/HD sub-category (5040).
    /// </summary>
    public static TorznabCategory TVHD => new(5040, "TV/HD");
    /// <summary>
    /// TV/UHD sub-category (5045).
    /// </summary>
    public static TorznabCategory TVUHD => new(5045, "TV/UHD");
    /// <summary>
    /// TV/Other sub-category (5050).
    /// </summary>
    public static TorznabCategory TVOther => new(5050, "TV/Other");
    /// <summary>
    /// TV/Sport sub-category (5060).
    /// </summary>
    public static TorznabCategory TVSport => new(5060, "TV/Sport");
    /// <summary>
    /// TV/Anime sub-category (5070).
    /// </summary>
    public static TorznabCategory TVAnime => new(5070, "TV/Anime");
    /// <summary>
    /// TV/Documentary sub-category (5080).
    /// </summary>
    public static TorznabCategory TVDocumentary => new(5080, "TV/Documentary");
    /// <summary>
    /// XXX/DVD sub-category (6010).
    /// </summary>
    public static TorznabCategory XXXDVD => new(6010, "XXX/DVD");
    /// <summary>
    /// XXX/WMV sub-category (6020).
    /// </summary>
    public static TorznabCategory XXXWMV => new(6020, "XXX/WMV");
    /// <summary>
    /// XXX/XviD sub-category (6030).
    /// </summary>
    public static TorznabCategory XXXXviD => new(6030, "XXX/XviD");
    /// <summary>
    /// XXX/x264 sub-category (6040).
    /// </summary>
    public static TorznabCategory XXXx264 => new(6040, "XXX/x264");
    /// <summary>
    /// XXX/UHD sub-category (6045).
    /// </summary>
    public static TorznabCategory XXXUHD => new(6045, "XXX/UHD");
    /// <summary>
    /// XXX/Pack sub-category (6050).
    /// </summary>
    public static TorznabCategory XXXPack => new(6050, "XXX/Pack");
    /// <summary>
    /// XXX/ImageSet sub-category (6060).
    /// </summary>
    public static TorznabCategory XXXImageSet => new(6060, "XXX/ImageSet");
    /// <summary>
    /// XXX/Other sub-category (6070).
    /// </summary>
    public static TorznabCategory XXXOther => new(6070, "XXX/Other");
    /// <summary>
    /// XXX/SD sub-category (6080).
    /// </summary>
    public static TorznabCategory XXXSD => new(6080, "XXX/SD");
    /// <summary>
    /// XXX/WEB-DL sub-category (6090).
    /// </summary>
    public static TorznabCategory XXXWEBDL => new(6090, "XXX/WEB-DL");
    /// <summary>
    /// Books/Mags sub-category (7010).
    /// </summary>
    public static TorznabCategory BooksMags => new(7010, "Books/Mags");
    /// <summary>
    /// Books/EBook sub-category (7020).
    /// </summary>
    public static TorznabCategory BooksEBook => new(7020, "Books/EBook");
    /// <summary>
    /// Books/Comics sub-category (7030).
    /// </summary>
    public static TorznabCategory BooksComics => new(7030, "Books/Comics");
    /// <summary>
    /// Books/Technical sub-category (7040).
    /// </summary>
    public static TorznabCategory BooksTechnical => new(7040, "Books/Technical");
    /// <summary>
    /// Books/Other sub-category (7050).
    /// </summary>
    public static TorznabCategory BooksOther => new(7050, "Books/Other");
    /// <summary>
    /// Books/Foreign sub-category (7060).
    /// </summary>
    public static TorznabCategory BooksForeign => new(7060, "Books/Foreign");
    /// <summary>
    /// Other/Misc sub-category (8010).
    /// </summary>
    public static TorznabCategory OtherMisc => new(8010, "Other/Misc");
    /// <summary>
    /// Other/Hashed sub-category (8020).
    /// </summary>
    public static TorznabCategory OtherHashed => new(8020, "Other/Hashed");

    /// <summary>
    /// Gets the category name for the given Torznab category ID.
    /// </summary>
    /// <param name="torznabCatId">The Newznab-compatible numeric category ID.</param>
    /// <returns>The category name, or <see cref="string.Empty"/> when no category matches.</returns>
    public static string GetCatDesc(int torznabCatId) =>
        AllCats.FirstOrDefault(c => c.Id == torznabCatId)?.Name ?? string.Empty;

    /// <summary>
    /// Gets the category matching the given name.
    /// </summary>
    /// <param name="name">The category name to look up (e.g. <c>Movies/HD</c>).</param>
    /// <returns>The matching <see cref="TorznabCategory"/>, or <c>null</c> when no category matches.</returns>
    public static TorznabCategory GetCatByName(string name) => AllCats.FirstOrDefault(c => c.Name == name);
}
