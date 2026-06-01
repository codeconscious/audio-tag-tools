module DuplicateFinder.Settings

open Shared.Types
open Errors
open FSharp.Data

[<Literal>]
let settingsSample = """
{
    "playlist": {
        "saveDirectory": "path",
        "searchPath": "path",
        "replacePath": "path"
    },
    "exclusions": [
        {
            "artist": "artist"
        },
        {
            "title": "title"
        },
        {
            "artist": "artist",
            "title": "title"
        }
    ],
    "equivalentArtists": [
        ["artistName", "equivalentArtistName"]
    ],
    "artistReplacementPatterns": [
        "text"
    ],
    "titleReplacementPatterns": [
        "text"
    ]
}
"""

type SettingsProvider = JsonProvider<settingsSample>
type Settings = SettingsProvider.Root
type Exclusion = SettingsProvider.Exclusion

let parseToSettings (Json json) : Result<Settings, CommandError> =
    try json |> SettingsProvider.Parse |> Ok
    with exn -> Error (SettingsParseError exn.Message)

let printSummary (settings: Settings) =
    printfn "Settings:"
    printfn $"  Exclusions:                  %d{settings.Exclusions.Length}"
    printfn $"  Artist Replacement Patterns: %d{settings.ArtistReplacementPatterns.Length}"
    printfn $"  Title Replacement Patterns:  %d{settings.TitleReplacementPatterns.Length}"
