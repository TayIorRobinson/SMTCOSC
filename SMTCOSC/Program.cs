using System.Net;
using System.Net.Sockets;
using Windows.Media;
using Windows.Media.Control;
using CoreOSC;
using CoreOSC.IO;

var dataPort = 3671;
var ctrlPort = 3672;
var ip = "127.0.0.1";
try {
    if (args.Length == 0) { }
    else if (args.Length == 2) {
        dataPort = Convert.ToInt32(args[0]);
        ctrlPort = Convert.ToInt32(args[1]);
    } else if (args.Length == 3) {
        ip = args[0];
        dataPort = Convert.ToInt32(args[1]);
        ctrlPort = Convert.ToInt32(args[2]);
    } else {
        throw new ArgumentException("Invalid amount of argments passed");
    }
} catch (Exception e) {
    Console.Error.WriteLine("Usage: ");
    Console.Error.WriteLine("    smtcosc.exe - Send data to " + ip + " on port " + dataPort + ", receiving control messages on port " + ctrlPort);
    Console.Error.WriteLine("    smtcosc.exe 1337 4200 - Send data to " + ip + " on port 1337, receiving control messages on port 4200");
    Console.Error.WriteLine("    smtcosc.exe 192.168.1.1 1337 4200 - Send data to 192.168.1.1 port 1337, receiving control messages on port 4200");
}

Dictionary<string, string> appNames = new Dictionary<string, string>();

// windows is a cursed pile of legacy nightmares    
try {
    Console.WriteLine("Enumerating app names...");
    var ShellComType = Type.GetTypeFromProgID("Shell.Application");
    dynamic ShellApplication = Activator.CreateInstance(ShellComType);
    var apps = ShellApplication.NameSpace("shell:::{4234d49b-0245-4df3-b780-3893943456e1}'").Items();
    foreach (var app in apps) appNames.Add(app.Path, app.Name);
    Console.WriteLine("Found " + appNames.Count + " apps");
}
catch (Exception e) {
    Console.Error.WriteLine(e);
}


GlobalSystemMediaTransportControlsSessionManager  gSMTCSM = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
var socket = new UdpClient(ip, dataPort);
Console.WriteLine("Sending data to " + ip + ":" + dataPort);

SemaphoreSlim updateLock = new(1);
HashSet<GlobalSystemMediaTransportControlsSession> watchedSessions = new();

async Task SendSessionMediaProperties(GlobalSystemMediaTransportControlsSession session) {
    var app = session.SourceAppUserModelId;
    var prefix = "/smtcosc/" + app + "/media";
    try {
        var properties = await session.TryGetMediaPropertiesAsync();
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/title"), [properties.Title]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/albumArtist"), [properties.AlbumArtist]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/albumTitle"), [properties.AlbumTitle]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/albumTrackCount"), [properties.AlbumTrackCount]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/artist"), [properties.Artist]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/genres"), [String.Join('\n',properties.Genres)]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/type"), [properties.PlaybackType?.ToString() ?? "null"]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/subtitle"), [properties.Subtitle]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/trackNumber"), [properties.TrackNumber]));
    } catch (Exception e) {
        Console.Error.WriteLine("Failed to read session media properties for " + app + ": " + e);
    }
    await SendSessionAppInfo(session);
}


async Task SendSessionPlaybackInfo(GlobalSystemMediaTransportControlsSession session) {
    var app = session.SourceAppUserModelId;
    var prefix = "/smtcosc/" + app + "/playback";
    try {
        var info = session.GetPlaybackInfo();
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/type"), [info.PlaybackType?.ToString() ?? "null"]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/repeatMode"), [info.AutoRepeatMode?.ToString() ?? "null"]));
        var shuffle = info.IsShuffleActive;
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/shuffle"), [shuffle == true ? 1 : shuffle == false ? 0 : -1]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/rate"), [info.PlaybackRate ?? 0]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/status"), [info.PlaybackStatus.ToString()]));
        // todo: controls
    } catch (Exception e) {
        Console.Error.WriteLine("Failed to read session playback info for " + app + ": " + e);
    }
}
async Task SendSessionTimelineProperties(GlobalSystemMediaTransportControlsSession session) {
    var app = session.SourceAppUserModelId;
    var prefix = "/smtcosc/" + app + "/timeline";
    try {
        var info = session.GetTimelineProperties();
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/endTime"), [info.EndTime.TotalSeconds]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/maxSeekTime"), [info.MaxSeekTime.TotalSeconds]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/minSeekTime"), [info.MinSeekTime.TotalSeconds]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/position"), [info.Position.TotalSeconds]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/startTime"), [info.StartTime.TotalSeconds]));
        await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/updated"), [info.LastUpdatedTime.ToUnixTimeMilliseconds()]));
    } catch (Exception e) {
        Console.Error.WriteLine("Failed to read session timeline info for " + app + ": " + e);
    }
}


async Task SendSessionAppInfo(GlobalSystemMediaTransportControlsSession session) {
    var app = session.SourceAppUserModelId;
    var prefix = "/smtcosc/" + app + "/app";
    try {
        if (appNames.TryGetValue(app, out var appName))
            await socket.SendMessageAsync(new OscMessage(new Address($"{prefix}/name"), [appName]));
    } catch (Exception e) {
        Console.Error.WriteLine("Failed to read session app info for " + app + ": " + e);
    }
}




async Task RefreshSessions() {
    await updateLock.WaitAsync();
    try {
        var activeSession = gSMTCSM.GetCurrentSession();
        await socket.SendMessageAsync(
            new OscMessage(new Address("/smtcosc/activeSession"),
                [activeSession.SourceAppUserModelId])
        );
        var sessions = gSMTCSM.GetSessions();
        await socket.SendMessageAsync(
            new OscMessage(new Address("/smtcosc/sessions"), [
                String.Join("\n", sessions.Select(a => a.SourceAppUserModelId))
            ])
        );

        foreach (var session in sessions) {
            await SendSessionMediaProperties(session);
            await SendSessionPlaybackInfo(session);
            await SendSessionTimelineProperties(session);
            
            if (watchedSessions.Contains(session)) continue;
            session.MediaPropertiesChanged += (sender, e) => SendSessionMediaProperties(session);
            session.PlaybackInfoChanged += (sender, e) => SendSessionPlaybackInfo(session);
            session.TimelinePropertiesChanged += (sender, e) => SendSessionTimelineProperties(session);
            watchedSessions.Add(session);
        }
    }
    catch (Exception e) {
        Console.Error.WriteLine("Error in RefreshSessions " + e);
    }
    finally {
        updateLock.Release();
    }
}

gSMTCSM.SessionsChanged += (sender, e) => RefreshSessions();
gSMTCSM.CurrentSessionChanged += (sender, e) => RefreshSessions();
await RefreshSessions();

async Task HandleIncomingMessage(OscMessage oscMessage) {
    Console.WriteLine("Received control message on: " + oscMessage.Address.Value);
    var addr = oscMessage.Address.Value;
    if (!addr.StartsWith("/smtcosc/")) return;
    var param = oscMessage.Arguments.FirstOrDefault();
    if (param is OscFalse) return;
    
    var split = addr.Split('/');
    if (split.Length == 3) {
        var command = split[2];
        if (command == "refresh") await RefreshSessions();
    }  else if (split.Length == 4) {
        var appName = split[2];
        var command = split[3];
        var session = gSMTCSM.GetSessions().FirstOrDefault(a => a.SourceAppUserModelId == appName);
        if (session is null) {
            Console.Error.WriteLine("Session " + appName + " not found!");
            return;
        }

        if (command == "refresh") {
            await SendSessionMediaProperties(session);
            await SendSessionPlaybackInfo(session);
            await SendSessionTimelineProperties(session);
        } else if (command == "playPause") {
            await session.TryTogglePlayPauseAsync();
        } else if (command == "stop") {
            await session.TryStopAsync();
        } else if (command == "prev") {
            await session.TrySkipPreviousAsync();
        } else if (command == "next") {
            await session.TrySkipNextAsync();
        } else if (command == "rewind") {
            await session.TryRewindAsync();
        } else if (command == "record") {
            await session.TryRecordAsync();
        } else if (command == "play") {
            await session.TryPlayAsync();
        } else if (command == "pause") {
            await session.TryPauseAsync();
        } else if (command == "fastForward") {
            await session.TryFastForwardAsync();
        } else if (command == "chUp") {
            await session.TryChangeChannelUpAsync();
        } else if (command == "chDown") {
            await session.TryChangeChannelDownAsync();
        } else if (command == "shuffle") {
            var arg = (int)param;
            if (arg == 0 || arg == 1)
                await session.TryChangeShuffleActiveAsync(arg == 1);
        } else if (command == "seek") {
            var arg = (long)param;
            if (arg >= 0)  await session.TryChangePlaybackPositionAsync(arg);
        } else if (command == "rate") {
            var arg = (double)param;
            if (arg >= 0) await session.TryChangePlaybackRateAsync((double)param);
        } else if (command == "repeatMode") {
            var arg = (string)param;
            if (arg == "List") await session.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.List);
            else if (arg == "None") await session.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.None);
            else if (arg == "Track") await session.TryChangeAutoRepeatModeAsync(MediaPlaybackAutoRepeatMode.Track);
        }

    }
    
}

if (ctrlPort > 0) {
    try {
        using (var udpClient = new UdpClient()) {
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, ctrlPort));
            Console.WriteLine("Receiving control commands on " + ctrlPort);
            while (true) {
                var message = await udpClient.ReceiveMessageAsync();
                try {
                    await HandleIncomingMessage(message);
                } catch (Exception e) {
                    Console.Error.WriteLine("Error handling message: " + e);
                }
            }
        }
    }
    catch (Exception e) {
        Console.Error.WriteLine("Recieve failure " + e);
    }
}

Console.WriteLine("Receiving is disabled.");
while (true) await Task.Delay(10000);
