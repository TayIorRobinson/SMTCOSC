# SMTCOSC
SMTCOSC is a C# program to send currently playing media information from applications that implement the WinRT [`SystemMediaTransportControls`](https://learn.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols) interface via the [Open Sound Control](https://en.wikipedia.org/wiki/Open_Sound_Control) protocol.


## Usage

`smtcosc.exe <ip address> <port>`

IP address defaults to 127.0.0.1 if not provided, port provides to 3671 if not provided.

## Resonite Example 

`resrec:///U-TaylorRobinson/R-A317C669CE99B75E10A14E08BD7DB091661A551210E06D00513774087D015F49`

The simple example shows all data you can recieve, but requires an inspector.

There's also a full UI with controls, but is a little less good as a starting point if you want to build your own thing.

## Messages


| Address                                    | Data type | Description/Example                                                                                                                                                                                                                                                                                 |
|--------------------------------------------|-----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `/smtcosc/activeSession`                   | string    | The app name of the current session, where the current sesison is defined as ["is the session the system believes the user would most likely want to control."](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager.getcurrentsession) |
| `/smtcosc/sessions`                        | string    | New-line delimited list of app names with currently active media sessions.                                                                                                                                                                                                                          |
| `/smtcosc/<appname>/app/name`              | string    | `Media Player` (not always provided, use the app ID as a fallback)                                                                                                                                                                                                                                  |
| `/smtcosc/<appname>/media/title`           | string    | `Never Gonna Give You Up`                                                                                                                                                                                                                                                                           | 
| `/smtcosc/<appname>/media/albumArtist`     | string    | `Rick Astley`                                                                                                                                                                                                                                                                               \       | 
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


## Controls

Any command with a first parameter that is `false` will be ignored. Otherwise, unless otherwise specified, sending a value of any type will perform the action.

| Address                                   | Data type | Description/Example                                                                                                                                                 |
|-------------------------------------------|-----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `/smtcosc/refresh`                        |           | Refreshes all sessions                                                                                                                                              |
| `/smtcosc/<appname>/refresh`              |           | Refreshes a specific session                                                                                                                                        |
| `/smtcosc/<appname>/playPause`            |           | Toggles playback                                                                                                                                                    | 
| `/smtcosc/<appname>/play`                 |           | Starts playback                                                                                                                                                     | 
| `/smtcosc/<appname>/pause`                |           | Pauses playback                                                                                                                                                     | 
| `/smtcosc/<appname>/stop`                 |           | Stops playback                                                                                                                                                      |
| `/smtcosc/<appname>/next`                 |           | Skips to the next media item                                                                                                                                        |
| `/smtcosc/<appname>/prev`                 |           | Skips to the previous media item. Some applications may seek back to the start of the current media item when the current media item has been playing for some time |
| `/smtcosc/<appname>/rewind`               |           | Rewinds playback                                                                                                                                                    |
| `/smtcosc/<appname>/fastForward`          |           | Fast-forwards playback                                                                                                                                              | 
| `/smtcosc/<appname>/record`               |           | Start recording                                                                                                                                                     |
| `/smtcosc/<appname>/chUp                  |           | Switches to the next channel                                                                                                                                        |
| `/smtcosc/<appname>/chDown`               |           | Switches to the previous channel                                                                                                                                    |
| `/smtcosc/<appname>/shuffle`              | int       | Sets shuffle state. `0` - sequential playback, `1` - shuffled playback. Other values are ignored.                                                                   |
| `/smtcosc/<appname>/repeatMode`           | string    | `None`, `Track, or `List`, other values are ignored.                                                                                                                |                                                                                                                                                                  
| `/smtcosc/<appname>/rate`                 | double    | Sets playback rate. Values less than 0 are ignored.                                                                                                                 |
| `/smtcosc/<appname>/seek`                 | long      | Seek to a specific position. Measured in ticks (1/1,000,000th second)                                                                                               |

