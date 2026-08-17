using System.Collections.Frozen;

namespace Alfred.UIKit.Suggest;

public static class BrandAllowlist
{
    public static readonly FrozenSet<string> Slugs = new[]
    {
        "netflix", "spotify", "youtube", "youtubemusic", "youtubetv", "applemusic", "apple", "appletv", "applepay",
        "applearcade", "icloud", "primevideo", "amazon", "amazonprime", "hbo", "max", "disneyplus", "crunchyroll",
        "tidal", "deezer", "soundcloud", "audiomack", "vimeo", "dazn", "mubi", "curiositystream", "nebula",
        "twitch", "kick", "patreon", "onlyfans", "cameo",
        "steam", "epicgames", "playstation", "xbox", "nintendo", "eaplay", "ubisoft", "gogdotcom", "roblox",
        "minecraft", "leagueoflegends", "valorant", "worldofwarcraft", "finalfantasy", "geforcenow",
        "github", "gitlab", "openai", "claude", "anthropic", "googlegemini", "perplexity", "midjourney",
        "cursor", "jetbrains", "vercel", "netlify", "cloudflare", "digitalocean", "heroku", "docker",
        "figma", "canva", "adobe", "notion", "slack", "discord", "zoom", "microsoft", "linkedin",
        "dropbox", "googledrive", "googleone", "gmail", "googlephotos", "protonmail", "proton", "protonvpn",
        "nordvpn", "expressvpn", "surfshark", "mullvad", "1password", "bitwarden", "dashlane", "lastpass",
        "todoist", "obsidian", "evernote", "linear", "trello", "asana", "clickup", "airtable", "miro",
        "grammarly", "deepl", "duolingo", "babbel", "busuu", "udemy", "coursera", "skillshare", "masterclass",
        "medium", "substack", "audible", "scribd", "blinkist", "kindle", "pocket",
        "strava", "fitbit", "garmin", "myfitnesspal", "headspace", "calm", "peloton", "whoop", "nikeplus",
        "revolut", "wise", "paypal", "stripe", "klarna", "n26", "monzo", "binance", "coinbase",
        "uber", "ubereats", "lyft", "bolt", "airbnb", "bookingdotcom", "expedia", "tripadvisor", "turkishairlines",
        "pegasusairlines", "getir", "trendyol", "hepsiburada", "yemeksepeti", "migros",
        "vodafone", "turktelekom", "o2", "verizon", "att", "tmobile",
        "tinder", "bumble", "hinge", "telegram", "whatsapp", "instagram", "x", "snapchat", "tiktok",
        "playstationvita", "googleplay", "appstore", "epicgamesstore",
        "blutv", "exxen", "spotifyforpodcasters", "shopify", "squarespace", "wix", "wordpress", "godaddy",
        "namecheap", "ovh", "hetzner", "contabo",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
