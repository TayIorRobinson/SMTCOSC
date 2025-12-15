using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using Windows.Media.Control;
using Windows.System;
using CoreOSC;
using CoreOSC.IO;

var port = 3671;
var ip = "127.0.0.1";
try {
    if (args.Length == 0) { }
    else if (args.Length == 1) {
        port = Convert.ToInt32(args[0]);
    } else if (args.Length == 2) {
        ip = args[0];
        port = Convert.ToInt32(args[1]);
    } else {
        throw new ArgumentException("Invalid amount of argments passed");
    }
} catch (Exception e) {
    Console.Error.WriteLine("Usage: ");
    Console.Error.WriteLine("    smtcosc.exe - Send data to " + ip + " on port " + port);
    Console.Error.WriteLine("    smtcosc.exe 1337 - Send data to " + ip + " on port 1337");
    Console.Error.WriteLine("    smtcosc.exe 192.168.1.1 1337 - Send data to 192.168.1.1 port 1337");
}

GlobalSystemMediaTransportControlsSessionManager  gSMTCSM = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
var socket = new UdpClient(ip, port);
Console.WriteLine(" Sending data to " + ip + ":" + port);

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
        Console.WriteLine("Failed to read session media properties for " + app + ": " + e);
    }
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
        Console.WriteLine("Failed to read session playback info for " + app + ": " + e);
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
        Console.WriteLine("Failed to read session timeline info for " + app + ": " + e);
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
    finally {
        updateLock.Release();
    }
}

gSMTCSM.SessionsChanged += (sender, e) => RefreshSessions();
gSMTCSM.CurrentSessionChanged += (sender, e) => RefreshSessions();
await RefreshSessions();

while (true) Thread.Sleep(100000);