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
    "artistReplacements": [
        "text"
    ],
    "titleReplacements": [
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
    printfn $"  Exclusions:          %d{settings.Exclusions.Length}"
    printfn $"  Artist Replacements: %d{settings.ArtistReplacements.Length}"
    printfn $"  Title Replacements:  %d{settings.TitleReplacements.Length}"
