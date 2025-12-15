# SMTCOSC
SMTCOSC is a C# program to send currently playing media information from applications that implement the WinRT [`SystemMediaTransportControls`](https://learn.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols) interface via the [Open Sound Control](https://en.wikipedia.org/wiki/Open_Sound_Control) protocol.


## Usage

`smtcosc.exe <ip address> <port>`

IP address defaults to 127.0.0.1 if not provided, port provides to 3671 if not provided.

## Resonite Example 

Paste this link into Resonite: `resrec:///U-TaylorRobinson/R-57d0881c-e528-4e41-970e-a11429eeab78`.

You'll need to pull up your inspector and change the `HandlingUser` on the `OSC_Reciever` on the root `smtcosc` slot.


## Messages


| Address                                    | Data type | Description/Example                                                                                                                                                                                                                                                                                 |
|--------------------------------------------|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `/smtcosc/activeSession`                   | string    | The app name of the current session, where the current sesison is defined as ["is the session the system believes the user would most likely want to control."](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager.getcurrentsession) |
| `/smtcosc/sessions`                        | string    | New-line delimited list of app names with currently active media sessions.                                                                                                                                                                                                                          |
| `/smtcosc/<appname>/media/title`           | string    | `Never Gonna Give You Up`                                                                                                                                                                                                                                                                           | 
| `/smtcosc/<appname>/media/albumArtist`     | string    | `Rick Astley`                                                                                                                                                                                                                                                                                       | 
| `/smtcosc/<appname>/media/albumTitle`      | string    | `Whenever You Need Somebody`                                                                                                                                                                                                                                                                        | 
| `/smtcosc/<appname>/media/artist`          | string    | `Rick Astley`                                                                                                                                                                                                                                                                                       |
| `/smtcosc/<appname>/media/genres`          | string    | New-line delimited list of genres for the current media.                                                                                                                                                                                                                                            |
| `/smtcosc/<appname>/media/type`            | string    | `Music`/`Video`/`Image`/`Unknown`/`null`                                                                                                                                                                                                                                                            |
| `/smtcosc/<appname>/media/subtitle`        | string    | Subtitle of the current media                                                                                                                                                                                                                                                                       |
| `/smtcosc/<appname>/media/albumTrackCount` | int       | `10`                                                                                                                                                                                                                                                                                                | 
| `/smtcosc/<appname>/media/trackNumber`     | int       | `1`                                                                                                                                                                                                                                                                                                 |
| `/smtcosc/<appname>/playback/type`         | string    | `Music`/`Video`/`Image`/`Unknown`/`null`                                                                                                                                                                                                                                                            |
| `/smtcosc/<appname>/playback/repeatMode`   | string    | `None`/`Track`/`List`/`null`                                                                                                                                                                                                                                                                        |
| `/smtcosc/<appname>/playback/status`       | string    | `Closed`/`Opened`/`Changing`/`Stopped`/`Playing`/`Paused`                                                                                                                                                                                                                                           |
| `/smtcosc/<appname>/playback/shuffle`      | int       | `0` - sequential playback, `1` - shuffle playback, `-1` - unknown                                                                                                                                                                                                                                   |
| `/smtcosc/<appname>/playback/rate`         | double    | The rate at which playback is happening. (`0` if unknown)                                                                                                                                                                                                                                           |
| `/smtcosc/<appname>/timeline/updated`      | long      | The UNIX timestamp (in millis) at when the position was updated.                                                                                                                                                                                                                                    |
| `/smtcosc/<appname>/timeline/position`     | double    | The current playback position, in seconds.                                                                                                                                                                                                                                                          |
| `/smtcosc/<appname>/timeline/startTime`    | double    | The start timestamp of the current media items. Usually just 0.                                                                                                                                                                                                                                     |
| `/smtcosc/<appname>/timeline/endTime`      | double    | The end timestamp of the current media item. Usually just the length in seconds                                                                                                                                                                                                                     |
| `/smtcosc/<appname>/timeline/maxSeekTime`  | double    | The furthest timestamp you can seek to.                                                                                                                                                                                                                                                             |
| `/smtcosc/<appname>/timeline/minSeekTime`  | double    | The earliest timestamp which you can seek to.                                                                                                                                                                                                                                                       |


## TODO:
 - [ ] Playback control support
 - [ ] Optimise!
    - [ ] Duplicated data gets sent a lot
    - [ ] Use OSC containers instead of sending each message separately.