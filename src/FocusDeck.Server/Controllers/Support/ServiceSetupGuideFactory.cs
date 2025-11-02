using System;
using System.Collections.Generic;
using FocusDeck.Server.Controllers.Models;
using FocusDeck.Shared.Models.Automations;
using Microsoft.AspNetCore.Http;

namespace FocusDeck.Server.Controllers.Support
{
    internal static class ServiceSetupGuideFactory
    {
        private static readonly IReadOnlyDictionary<ServiceType, Func<HttpRequest, ServiceSetupGuide>> GuideFactories =
            new Dictionary<ServiceType, Func<HttpRequest, ServiceSetupGuide>>
            {
                [ServiceType.AppleMusic] = _ => new ServiceSetupGuide
                {
                    SetupType = "OAuth",
                    Title = "Connect Apple Music",
                    Description = "Connect Apple Music to control playback during study sessions.",
                    Steps = new List<string>
                    {
                        "Go to https://developer.apple.com/account.",
                        "Sign in with an Apple Developer Program account (membership required).",
                        "Open \"Certificates, Identifiers & Profiles\".",
                        "Create a new MusicKit identifier.",
                        "Generate a MusicKit private key.",
                        "Note: Apple Music integration currently requires an Apple Developer Program membership ($99/year).",
                        "This integration is in development; consider Spotify as an alternative if you lack access."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Apple Developer", Url = "https://developer.apple.com/account" },
                        new() { Label = "MusicKit Documentation", Url = "https://developer.apple.com/documentation/musickit" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "developerToken",
                            Label = "Developer Token",
                            HelpText = "Your Apple Music MusicKit developer token.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.Canvas] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Canvas",
                    Description = "Provide your institution's Canvas URL and an API token.",
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "canvasBaseUrl",
                            Label = "Canvas URL",
                            HelpText = "The URL of your Canvas instance (e.g., https://pcc.instructure.com).",
                            InputType = "text"
                        },
                        new()
                        {
                            Key = "access_token",
                            Label = "Access Token",
                            HelpText = "Account -> Settings -> Approved Integrations -> \"+ New Access Token\".",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.Discord] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Discord",
                    Description = "Send FocusDeck notifications to a Discord channel using a webhook.",
                    Steps = new List<string>
                    {
                        "Open Discord and switch to the server you want to connect.",
                        "Server Settings -> Integrations -> Webhooks.",
                        "Create a webhook or choose an existing one.",
                        "Name it \"FocusDeck\" and pick a destination channel.",
                        "Click \"Copy Webhook URL\".",
                        "Paste the URL into FocusDeck and click Connect."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Discord Webhooks Guide", Url = "https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks" },
                        new() { Label = "Discord Developer Portal", Url = "https://discord.com/developers/applications" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "webhookUrl",
                            Label = "Webhook URL",
                            HelpText = "The Discord webhook URL copied from server settings.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.GoogleCalendar] = request => CreateGoogleOAuthGuide(
                    request,
                    ServiceType.GoogleCalendar,
                    "Connect Google Calendar",
                    "Google Calendar uses OAuth 2.0. Create a Google Cloud project and provide the client credentials.",
                    "https://www.googleapis.com/auth/calendar.readonly",
                    "https://console.cloud.google.com/apis/library/calendar-json.googleapis.com"),
                [ServiceType.GoogleDrive] = request => CreateGoogleOAuthGuide(
                    request,
                    ServiceType.GoogleDrive,
                    "Connect Google Drive",
                    "Google Drive uses OAuth 2.0. Create a Google Cloud project and provide the client credentials.",
                    "https://www.googleapis.com/auth/drive.readonly",
                    "https://console.cloud.google.com/apis/library/drive.googleapis.com"),
                [ServiceType.GoogleGenerativeAI] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Google Generative AI (Gemini)",
                    Description = "Use Gemini for intelligent study assistance and automation.",
                    Steps = new List<string>
                    {
                        "Navigate to https://aistudio.google.com/app/apikey.",
                        "Sign in with your Google account.",
                        "Click \"Create API Key\" and pick or create a Google Cloud project.",
                        "Copy the generated API key.",
                        "Paste the key into FocusDeck and click Connect."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Google AI Studio", Url = "https://aistudio.google.com/app/apikey" },
                        new() { Label = "Gemini API Documentation", Url = "https://ai.google.dev/docs" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "apiKey",
                            Label = "API Key",
                            HelpText = "Your Gemini API key from Google AI Studio.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.HomeAssistant] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Home Assistant",
                    Description = "Provide your Home Assistant base URL and a long-lived access token.",
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "haBaseUrl",
                            Label = "Home Assistant URL",
                            HelpText = "Full URL to Home Assistant (e.g., http://homeassistant.local:8123).",
                            InputType = "text"
                        },
                        new()
                        {
                            Key = "access_token",
                            Label = "Long-Lived Access Token",
                            HelpText = "Profile -> Long-Lived Access Tokens -> Create Token, then paste it here.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.IFTTT] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect IFTTT",
                    Description = "Trigger IFTTT applets using FocusDeck webhook events.",
                    Steps = new List<string>
                    {
                        "Go to https://ifttt.com/maker_webhooks and click Connect.",
                        "Open the Documentation page to view your unique webhook key.",
                        "Copy the provided key.",
                        "Paste it into FocusDeck and press Connect.",
                        "Build IFTTT applets that react to FocusDeck events."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "IFTTT Webhooks", Url = "https://ifttt.com/maker_webhooks" },
                        new() { Label = "IFTTT Platform", Url = "https://platform.ifttt.com/" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "webhookKey",
                            Label = "Webhook Key",
                            HelpText = "Your unique IFTTT webhook key.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.Notion] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Notion",
                    Description = "Link Notion to sync tasks and notes into FocusDeck.",
                    Steps = new List<string>
                    {
                        "Visit https://www.notion.so/my-integrations and create a new integration.",
                        "Name it \"FocusDeck\" (logo optional).",
                        "Grant capabilities: Read content, Update content.",
                        "Submit to create the integration and copy the Internal Integration Token (starts with secret_).",
                        "In Notion, open the page or database you want to share.",
                        "Share -> Invite -> search for the FocusDeck integration.",
                        "Grant access and then save the configuration in FocusDeck."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Notion Integrations", Url = "https://www.notion.so/my-integrations" },
                        new() { Label = "Notion API Documentation", Url = "https://developers.notion.com/" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "apiKey",
                            Label = "Internal Integration Token",
                            HelpText = "The secret token generated for your Notion integration.",
                            InputType = "password"
                        }
                    },
                    OAuthButtonText = "Save Configuration"
                },
                [ServiceType.PhilipsHue] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Philips Hue",
                    Description = "Control Philips Hue lights based on your study sessions.",
                    Steps = new List<string>
                    {
                        "Ensure your Hue Bridge is powered on and connected to your network.",
                        "Find the bridge IP address (router UI or the Hue mobile app).",
                        "Press the link button on the Hue Bridge.",
                        "Within 30 seconds, enter the bridge IP below and click Connect.",
                        "FocusDeck creates a dedicated user on the bridge for automation control."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Discover Bridge IP", Url = "https://discovery.meethue.com/" },
                        new() { Label = "Hue API Documentation", Url = "https://developers.meethue.com/" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "bridgeIp",
                            Label = "Bridge IP Address",
                            HelpText = "Local IP of the Philips Hue Bridge (e.g., 192.168.1.100).",
                            InputType = "text"
                        }
                    }
                },
                [ServiceType.Slack] = _ => new ServiceSetupGuide
                {
                    SetupType = "OAuth",
                    Title = "Connect Slack",
                    Description = "Send FocusDeck notifications and automate workspace tasks via Slack.",
                    Steps = new List<string>
                    {
                        "Navigate to https://api.slack.com/apps and sign in.",
                        "Create a new app from scratch and choose your workspace.",
                        "Under OAuth & Permissions, add bot scopes: chat:write and chat:write.public.",
                        "Install the app to the workspace.",
                        "Copy the Bot User OAuth Token (starts with xoxb-).",
                        "Optionally configure an incoming webhook for channel-specific notifications.",
                        "Paste values into FocusDeck and connect."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Slack API Apps", Url = "https://api.slack.com/apps" },
                        new() { Label = "Slack API Documentation", Url = "https://api.slack.com/docs" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "access_token",
                            Label = "Bot User OAuth Token",
                            HelpText = "The token from Slack OAuth & Permissions (xoxb-...).",
                            InputType = "password"
                        },
                        new()
                        {
                            Key = "webhookUrl",
                            Label = "Incoming Webhook URL (Optional)",
                            HelpText = "Provide if you want a dedicated channel webhook.",
                            InputType = "text"
                        }
                    }
                },
                [ServiceType.Spotify] = request =>
                {
                    var redirectUri = BuildRedirectUri(request, ServiceType.Spotify);
                    return new ServiceSetupGuide
                    {
                        SetupType = "OAuth",
                        Title = "Connect Spotify",
                        Description = "Spotify uses OAuth 2.0. Provide developer credentials and authorize FocusDeck.",
                        Steps = new List<string>
                        {
                            "Visit https://developer.spotify.com/dashboard and sign in.",
                            "Click \"Create an App\" and accept the terms.",
                            "Name it \"FocusDeck\" with a short description.",
                            $"Open \"Edit Settings\" and add the redirect URI: {redirectUri}.",
                            "Save the settings.",
                            "Copy the Client ID and Client Secret.",
                            "Paste both values into FocusDeck and save.",
                            "Start the OAuth flow to authorize your account."
                        },
                        Links = new List<SetupLink>
                        {
                            new() { Label = "Spotify Developer Dashboard", Url = "https://developer.spotify.com/dashboard" },
                            new() { Label = "Spotify API Documentation", Url = "https://developer.spotify.com/documentation/web-api" }
                        },
                        Fields = new List<SetupField>
                        {
                            new()
                            {
                                Key = "clientId",
                                Label = "Client ID",
                                HelpText = "From your Spotify Developer Dashboard.",
                                InputType = "text"
                            },
                            new()
                            {
                                Key = "clientSecret",
                                Label = "Client Secret",
                                HelpText = "Click \"Show Client Secret\" and paste it here.",
                                InputType = "password"
                            }
                        },
                        OAuthButtonText = "Start OAuth Flow"
                    };
                },
                [ServiceType.Todoist] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Todoist",
                    Description = "Sync tasks with Todoist using your personal API token.",
                    Steps = new List<string>
                    {
                        "Log in to https://todoist.com.",
                        "Click your avatar -> Settings -> Integrations.",
                        "Locate the API token section.",
                        "Copy the API token.",
                        "Paste it into FocusDeck and click Connect."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Todoist Settings", Url = "https://todoist.com/app/settings/integrations" },
                        new() { Label = "Todoist API Documentation", Url = "https://developer.todoist.com/rest/v2/" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "access_token",
                            Label = "API Token",
                            HelpText = "The API token from Todoist settings.",
                            InputType = "password"
                        }
                    }
                },
                [ServiceType.Zapier] = _ => new ServiceSetupGuide
                {
                    SetupType = "Simple",
                    Title = "Connect Zapier",
                    Description = "Trigger Zaps via a webhook to automate workflows with thousands of apps.",
                    Steps = new List<string>
                    {
                        "Sign in at https://zapier.com.",
                        "Create a Zap with \"Webhooks by Zapier\" as the trigger (Catch Hook).",
                        "Copy the provided webhook URL.",
                        "Paste the URL into FocusDeck and click Connect."
                    },
                    Links = new List<SetupLink>
                    {
                        new() { Label = "Zapier Webhooks", Url = "https://zapier.com/app/webhooks" },
                        new() { Label = "Zapier Platform", Url = "https://zapier.com" }
                    },
                    Fields = new List<SetupField>
                    {
                        new()
                        {
                            Key = "webhookUrl",
                            Label = "Webhook URL",
                            HelpText = "Your Zapier webhook URL for catching FocusDeck events.",
                            InputType = "password"
                        }
                    }
                }
            };

        public static bool TryCreate(ServiceType service, HttpRequest request, out ServiceSetupGuide guide)
        {
            if (GuideFactories.TryGetValue(service, out var factory))
            {
                guide = factory(request);
                return true;
            }

            guide = null!;
            return false;
        }

        private static string BuildRedirectUri(HttpRequest request, ServiceType service) =>
            $"{request.Scheme}://{request.Host}/api/services/oauth/{service}/callback";

        private static ServiceSetupGuide CreateGoogleOAuthGuide(
            HttpRequest request,
            ServiceType service,
            string title,
            string description,
            string scope,
            string apiLibraryUrl)
        {
            var redirectUri = BuildRedirectUri(request, service);
            return new ServiceSetupGuide
            {
                SetupType = "OAuth",
                Title = title,
                Description = description,
                Steps = new List<string>
                {
                    "Go to https://console.cloud.google.com/ and sign in.",
                    "Create a new project (or select an existing one).",
                    $"Enable the API: APIs & Services -> Library -> search for the API and enable it.",
                    "Configure the OAuth consent screen (APIs & Services -> OAuth consent screen).",
                    "Choose the appropriate user type (External is typical).",
                    "Fill in the app name, support email, and developer contact details.",
                    $"Add scopes including {scope}.",
                    "Add yourself as a test user if the app is in testing mode.",
                    "Create OAuth client credentials: APIs & Services -> Credentials -> Create credentials -> OAuth client ID.",
                    "Select \"Web application\" as the application type.",
                    $"Add the authorized redirect URI: {redirectUri}.",
                    "Copy the Client ID and Client Secret.",
                    "Paste the credentials into FocusDeck, save, then start the OAuth flow."
                },
                Links = new List<SetupLink>
                {
                    new() { Label = "Google Cloud Console", Url = "https://console.cloud.google.com/" },
                    new() { Label = "API Library", Url = apiLibraryUrl },
                    new() { Label = "OAuth Credentials", Url = "https://console.cloud.google.com/apis/credentials" }
                },
                Fields = new List<SetupField>
                {
                    new()
                    {
                        Key = "clientId",
                        Label = "Client ID",
                        HelpText = "From Google Cloud Console (typically ends with .apps.googleusercontent.com).",
                        InputType = "text"
                    },
                    new()
                    {
                        Key = "clientSecret",
                        Label = "Client Secret",
                        HelpText = "From the OAuth 2.0 credentials page.",
                        InputType = "password"
                    }
                },
                OAuthButtonText = "Start OAuth Flow"
            };
        }
    }
}



